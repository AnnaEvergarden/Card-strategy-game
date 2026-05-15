using System;
using System.IO;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 资源存储服务：持久化玩家金币、钻石、船票，并提供安全的增减接口（加密存储）。
/// </summary>
public static class CurrencyStore
{
    #region Fields

    /// <summary>
    /// 加密后的资源数据文件名。
    /// </summary>
    private const string DataFileName = "currency.dat";

    /// <summary>
    /// 内存缓存。
    /// </summary>
    private static CurrencyData _cached = new();

    /// <summary>
    /// 当前缓存所属账号；用于避免登出或切换账号后把旧缓存写入新账号。
    /// </summary>
    private static string _cachedUser = string.Empty;

    /// <summary>
    /// 文件读写锁。
    /// </summary>
    private static readonly object FileLock = new();

    #endregion

    #region Nested Models

    /// <summary>
    /// 资源快照数据。
    /// </summary>
    [Serializable]
    public sealed class CurrencyData
    {
        /// <summary>
        /// 金币数量。
        /// </summary>
        public int gold;

        /// <summary>
        /// 钻石数量。
        /// </summary>
        public int diamond;

        /// <summary>
        /// 船票数量。
        /// </summary>
        public int shipTicket;
    }

    #endregion

    #region Public API

    /// <summary>
    /// 读取资源数据；不存在时返回默认初始值。
    /// </summary>
    public static CurrencyData Load()
    {
        lock (FileLock)
        {
            try
            {
                if (!TryGetActiveFilePath(out var path, out var currentUser))
                {
                    _cachedUser = string.Empty;
                    _cached = CreateDefault();
                    return _cached;
                }

                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    if (bytes != null && bytes.Length > 16)
                    {
                        var json = LocalDataCrypto.DecryptToUtf8(bytes);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            _cached = JsonUtility.FromJson<CurrencyData>(json) ?? CreateDefault();
                            Normalize(_cached);
                            _cachedUser = currentUser;
                            return _cached;
                        }
                    }
                }

                if (TryLoadLegacyData(out var legacyData))
                {
                    _cached = legacyData;
                    _cachedUser = currentUser;
                    Save(_cached);
                    return _cached;
                }

                _cached = CreateDefault();
                _cachedUser = currentUser;
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load currency failed: {ex.Message}");
                _cached = CreateDefault();
                _cachedUser = string.Empty;
                return _cached;
            }
        }
    }

    /// <summary>
    /// 保存资源数据。
    /// </summary>
    public static void Save(CurrencyData data)
    {
        lock (FileLock)
        {
            try
            {
                if (!TryGetActiveFilePath(out var path, out var currentUser))
                {
                    Debug.LogWarning("Save currency skipped: no logged-in user.");
                    return;
                }

                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? CreateDefault();
                _cachedUser = currentUser;
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save currency failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存当前缓存。
    /// </summary>
    public static void SaveCurrent()
    {
        var currentUser = UserDataPathService.GetCurrentUser();
        if (string.IsNullOrWhiteSpace(currentUser))
        {
            Debug.Log("Save currency skipped: no logged-in user.");
            return;
        }

        if (!string.Equals(_cachedUser, currentUser, StringComparison.Ordinal))
        {
            Debug.Log("Save currency skipped: cached data belongs to another user or has not been loaded.");
            return;
        }

        Save(_cached ?? CreateDefault());
    }

    /// <summary>
    /// 尝试一次性消耗多种资源；任意资源不足则失败并且不扣减。
    /// </summary>
    public static bool TryConsume(int gold, int diamond, int shipTicket)
    {
        if (gold < 0 || diamond < 0 || shipTicket < 0)
        {
            return false;
        }

        if (!UserDataPathService.HasCurrentUser())
        {
            Debug.LogWarning("Consume currency failed: no logged-in user.");
            return false;
        }

        lock (FileLock)
        {
            var data = Load();
            if (data.gold < gold || data.diamond < diamond || data.shipTicket < shipTicket)
            {
                return false;
            }

            data.gold -= gold;
            data.diamond -= diamond;
            data.shipTicket -= shipTicket;
            SaveCurrent();
            return true;
        }
    }

    /// <summary>
    /// 增加资源（负数将被忽略）。
    /// </summary>
    public static void Add(int gold, int diamond, int shipTicket)
    {
        if (!UserDataPathService.HasCurrentUser())
        {
            Debug.LogWarning("Add currency skipped: no logged-in user.");
            return;
        }

        lock (FileLock)
        {
            var data = Load();
            if (gold > 0) data.gold += gold;
            if (diamond > 0) data.diamond += diamond;
            if (shipTicket > 0) data.shipTicket += shipTicket;
            SaveCurrent();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 创建默认资源（用于首次进入）。
    /// </summary>
    private static CurrencyData CreateDefault()
    {
        return new CurrencyData
        {
            gold = 10000,
            diamond = 100,
            shipTicket = 20
        };
    }

    /// <summary>
    /// 规范化资源值，避免负数。
    /// </summary>
    private static void Normalize(CurrencyData data)
    {
        data.gold = Mathf.Max(0, data.gold);
        data.diamond = Mathf.Max(0, data.diamond);
        data.shipTicket = Mathf.Max(0, data.shipTicket);
    }

    /// <summary>
    /// 尝试获取当前账号的资源文件路径。
    /// </summary>
    private static bool TryGetActiveFilePath(out string filePath, out string currentUser)
    {
        currentUser = UserDataPathService.GetCurrentUser();
        if (string.IsNullOrWhiteSpace(currentUser))
        {
            filePath = string.Empty;
            return false;
        }

        return UserDataPathService.TryGetCurrentUserDataFilePath(DataFileName, out filePath);
    }

    /// <summary>
    /// 尝试读取旧版共享资源文件，用于首次迁移到当前账号目录。
    /// </summary>
    private static bool TryLoadLegacyData(out CurrencyData data)
    {
        data = null;
        var legacyPath = UserDataPathService.GetLegacySharedDataFilePath(DataFileName);
        if (!File.Exists(legacyPath))
        {
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(legacyPath);
            if (bytes == null || bytes.Length <= 16)
            {
                return false;
            }

            var json = LocalDataCrypto.DecryptToUtf8(bytes);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            data = JsonUtility.FromJson<CurrencyData>(json) ?? CreateDefault();
            Normalize(data);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Load legacy currency failed: {ex.Message}");
            return false;
        }
    }

    #endregion
}
