using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 存档服务实现：通过键值对管理本地加密存档。
/// 内部使用 JSON 序列化 + AES 加密 + 文件存储。
/// 支持自动保存（应用退出时）和手动保存。
/// 非静态、非单例——由 ServiceContainer 注册，通过接口注入到各模块。
/// </summary>
public class SaveService : ISaveService, IDisposable
{
    /// <summary>
    /// 存档文件存储目录名（位于游戏根目录下）。
    /// </summary>
    private const string SaveDirectoryName = "UserData";

    /// <summary>
    /// 存档文件扩展名。
    /// </summary>
    private const string FileExtension = ".dat";

    /// <summary>
    /// 内存缓存：key → 最近保存/加载的数据对象。
    /// 避免同一数据在短时间内反复读写文件。
    /// </summary>
    private readonly Dictionary<string, object> _memoryCache = new Dictionary<string, object>();

    /// <summary>
    /// 缓存标记：key → 是否待持久化（有修改未写盘）。
    /// </summary>
    private readonly HashSet<string> _dirtyKeys = new HashSet<string>();

    /// <summary>
    /// 文件读写锁。
    /// </summary>
    private readonly object _fileLock = new object();

    /// <summary>
    /// 是否已释放。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 存档目录完整路径。
    /// </summary>
    public string SaveDirectory { get; }

    /// <summary>
    /// 构造存档服务并确保存档目录存在。
    /// </summary>
    public SaveService()
    {
        SaveDirectory = GetSaveDirectoryPath();
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }
        CleanupStaleTempFiles();

        // 订阅应用退出事件
        Application.quitting += OnApplicationQuitting;
    }

    /// <summary>
    /// 异步保存数据：序列化 → 加密 → 写文件。
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">存储键（如 "account"、"currency"）</param>
    /// <param name="data">要保存的数据对象</param>
    public async UniTask SaveAsync<T>(string key, T data) where T : class
    {
        if (_disposed || string.IsNullOrWhiteSpace(key))
            return;

        // 缓存到内存
        lock (_memoryCache)
        {
            _memoryCache[key] = data;
            _dirtyKeys.Add(key);
        }

        // 异步执行文件写入
        await UniTask.RunOnThreadPool(() =>
        {
            lock (_fileLock)
            {
                try
                {
                    string json = JsonUtility.ToJson(data);
                    byte[] encrypted = CryptoHelper.Encrypt(json);
                    string filePath = GetFilePath(key);

                    // 原子写入：临时文件 + File.Replace 原子替换（保留备份）
                    WriteAtomic(filePath, encrypted);

                    lock (_memoryCache)
                    {
                        _dirtyKeys.Remove(key);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveService] 保存失败 key={key}: {ex.Message}");
                }
            }
        });
    }

    /// <summary>
    /// 异步加载数据：读取文件 → 解密 → 反序列化。
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">存储键</param>
    /// <returns>数据对象；不存在或解密失败时返回 new T()</returns>
    public async UniTask<T> LoadAsync<T>(string key) where T : class, new()
    {
        if (_disposed || string.IsNullOrWhiteSpace(key))
            return new T();

        // 先查内存缓存
        lock (_memoryCache)
        {
            if (_memoryCache.TryGetValue(key, out var cached) && cached is T typed)
                return typed;
        }

        // 从文件加载；主文件缺失或损坏时自动从 .bak 备份恢复
        T result = null;
        await UniTask.RunOnThreadPool(() =>
        {
            lock (_fileLock)
            {
                try
                {
                    string filePath = GetFilePath(key);
                    result = TryLoadFromFile<T>(filePath);
                    if (result != null)
                        return;

                    string backupPath = filePath + ".bak";
                    if (File.Exists(backupPath))
                    {
                        byte[] backupBytes = File.ReadAllBytes(backupPath);
                        T recovered = TryDeserialize<T>(backupBytes);
                        if (recovered != null)
                        {
                            WriteAtomic(filePath, backupBytes);
                            result = recovered;
                            Debug.LogWarning($"[SaveService] 主存档损坏，已从备份恢复 key={key}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] 加载失败 key={key}: {ex.Message}");
                }
            }
        });

        if (result != null)
        {
            lock (_memoryCache)
            {
                _memoryCache[key] = result;
            }
            return result;
        }

        return new T();
    }

    /// <summary>
    /// 检查指定键的存档文件是否存在。
    /// </summary>
    /// <param name="key">存储键</param>
    /// <returns>是否存在</returns>
    public bool HasKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        string filePath = GetFilePath(key);
        return File.Exists(filePath) || File.Exists(filePath + ".bak");
    }

    /// <summary>
    /// 删除指定键的存档文件。
    /// </summary>
    /// <param name="key">存储键</param>
    public async UniTask DeleteAsync(string key)
    {
        if (_disposed || string.IsNullOrWhiteSpace(key))
            return;

        lock (_memoryCache)
        {
            _memoryCache.Remove(key);
            _dirtyKeys.Remove(key);
        }

        await UniTask.RunOnThreadPool(() =>
        {
            lock (_fileLock)
            {
                try
                {
                    string filePath = GetFilePath(key);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] 删除失败 key={key}: {ex.Message}");
                }
            }
        });
    }

    /// <summary>
    /// 保存所有脏数据（未写盘的修改）。
    /// </summary>
    public async UniTask FlushAllAsync()
    {
        List<KeyValuePair<string, object>> dirtyEntries;

        lock (_memoryCache)
        {
            dirtyEntries = new List<KeyValuePair<string, object>>();
            foreach (var key in _dirtyKeys)
            {
                if (_memoryCache.TryGetValue(key, out var data))
                {
                    dirtyEntries.Add(new KeyValuePair<string, object>(key, data));
                }
            }
        }

        foreach (var entry in dirtyEntries)
        {
            await SaveInternalAsync(entry.Key, entry.Value);
        }
    }

    /// <summary>
    /// 释放存档服务并强制写盘。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Application.quitting -= OnApplicationQuitting;
        FlushAllAsync().Forget();
    }

    /// <summary>
    /// 应用退出时自动保存脏数据。
    /// </summary>
    private async void OnApplicationQuitting()
    {
        await FlushAllAsync();
        Debug.Log("[SaveService] 应用退出，存档已保存");
    }

    /// <summary>
    /// 内部保存（无缓存层，直接写文件）。
    /// </summary>
    private async UniTask SaveInternalAsync(string key, object data)
    {
        await UniTask.RunOnThreadPool(() =>
        {
            lock (_fileLock)
            {
                try
                {
                    string json = JsonUtility.ToJson(data);
                    byte[] encrypted = CryptoHelper.Encrypt(json);
                    string filePath = GetFilePath(key);

                    WriteAtomic(filePath, encrypted);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveService] 内部保存失败 key={key}: {ex.Message}");
                }
            }
        });
    }

    /// <summary>
    /// 获取存档目录路径（位于游戏根目录下的 UserData 文件夹）。
    /// </summary>
    private static string GetSaveDirectoryPath()
    {
        var dataPath = Application.dataPath;
        var gameRoot = Directory.GetParent(dataPath)?.FullName ?? dataPath;
        return Path.Combine(gameRoot, SaveDirectoryName);
    }

    /// <summary>
    /// 获取指定键对应的文件完整路径。
    /// </summary>
    private string GetFilePath(string key)
    {
        string safeKey = key.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
        return Path.Combine(SaveDirectory, $"{safeKey}{FileExtension}");
    }

    /// <summary>
    /// 原子写入：先写临时文件，再用 File.Replace 原子替换目标文件并保留备份。
    /// 临时文件使用 WriteThrough + Flush(true) 保证落盘后才替换。
    /// 平台不支持 File.Replace 时抛出异常，主文件保持原样，不做非原子降级。
    /// </summary>
    private static void WriteAtomic(string filePath, byte[] data)
    {
        string tempPath = filePath + ".tmp";
        string backupPath = filePath + ".bak";

        using (var fs = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.WriteThrough))
        {
            fs.Write(data, 0, data.Length);
            fs.Flush(true);
        }

        if (File.Exists(filePath))
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);
            File.Replace(tempPath, filePath, backupPath);
            if (File.Exists(backupPath))
                File.Delete(backupPath);
        }
        else
        {
            File.Move(tempPath, filePath);
        }
    }

    /// <summary>
    /// 从指定文件读取并反序列化，文件不存在或数据无效时返回 null。
    /// </summary>
    private static T TryLoadFromFile<T>(string filePath) where T : class, new()
    {
        if (!File.Exists(filePath))
            return null;

        byte[] encrypted = File.ReadAllBytes(filePath);
        return TryDeserialize<T>(encrypted);
    }

    /// <summary>
    /// 解密并反序列化存档数据，失败时返回 null。
    /// </summary>
    private static T TryDeserialize<T>(byte[] encrypted) where T : class, new()
    {
        string json = CryptoHelper.Decrypt(encrypted);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 启动时清理上次异常退出残留的临时文件，避免无限堆积。
    /// </summary>
    private static void CleanupStaleTempFiles()
    {
        try
        {
            string pattern = "*" + FileExtension + ".tmp";
            foreach (var path in Directory.GetFiles(GetSaveDirectoryPath(), pattern))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveService] 清理临时文件失败: {ex.Message}");
        }
    }
}
