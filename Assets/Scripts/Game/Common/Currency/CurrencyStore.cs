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
    /// 上一次读取是否失败；失败时禁止保存默认数据覆盖原文件。
    /// </summary>
    private static bool _saveBlocked;

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
    /// 读取当前账号资源数据；不存在时返回默认初始值，读取失败时禁止后续保存覆盖原文件。
    /// </summary>
    public static CurrencyData Load()
    {
        lock (FileLock)
        {
            try
            {
                if (!TryGetEncryptedFilePath(out var path))
                {
                    return MarkLoadFailed(CreateDefault(), "Load currency skipped: 当前没有登录账号。");
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
                            _saveBlocked = false;
                            return _cached;
                        }
                    }

                    return MarkLoadFailed(CreateDefault(), "Load currency failed: 资源存档文件无效或解密为空。");
                }

                _cached = CreateDefault();
                _saveBlocked = false;
                return _cached;
            }
            catch (Exception ex)
            {
                return MarkLoadFailed(CreateDefault(), $"Load currency failed: {ex.Message}");
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
                if (_saveBlocked)
                {
                    Debug.LogError("Save currency skipped: 上一次读取资源存档失败，禁止覆盖原文件。");
                    return;
                }

                if (!TryGetDataFolderPath(out var folder))
                {
                    Debug.LogWarning("Save currency skipped: 当前没有登录账号。");
                    return;
                }

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? CreateDefault();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                if (TryGetEncryptedFilePath(out var filePath))
                {
                    File.WriteAllBytes(filePath, encrypted);
                }
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
        Save(_cached ?? CreateDefault());
    }

    /// <summary>
    /// 清理运行时缓存；账号切换后必须先清理，避免旧账号数据写入新账号目录。
    /// </summary>
    public static void ClearCache()
    {
        lock (FileLock)
        {
            _cached = new CurrencyData();
            _saveBlocked = true;
        }
    }

    /// <summary>
    /// 尝试一次性消耗多种资源；任意资源不足、未登录或读档失败则不扣减。
    /// </summary>
    public static bool TryConsume(int gold, int diamond, int shipTicket)
    {
        if (gold < 0 || diamond < 0 || shipTicket < 0)
        {
            return false;
        }

        lock (FileLock)
        {
            var data = Load();
            if (_saveBlocked)
            {
                return false;
            }

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
    /// 增加资源（负数将被忽略）；未登录或读档失败时不会写入。
    /// </summary>
    public static void Add(int gold, int diamond, int shipTicket)
    {
        lock (FileLock)
        {
            var data = Load();
            if (_saveBlocked)
            {
                return;
            }

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
    /// 标记读取失败并返回安全默认值；后续保存会被阻止，避免数据丢失。
    /// </summary>
    /// <param name="fallback">返回给调用方的默认数据。</param>
    /// <param name="message">失败原因。</param>
    private static CurrencyData MarkLoadFailed(CurrencyData fallback, string message)
    {
        Debug.LogWarning(message);
        _cached = fallback;
        _saveBlocked = true;
        return _cached;
    }

    /// <summary>
    /// 尝试获取当前账号的资源数据目录。
    /// </summary>
    private static bool TryGetDataFolderPath(out string folderPath)
    {
        return LocalSavePath.TryGetCurrentAccountDataFolderPath(out folderPath);
    }

    /// <summary>
    /// 尝试获取当前账号的资源数据文件路径。
    /// </summary>
    private static bool TryGetEncryptedFilePath(out string filePath)
    {
        return LocalSavePath.TryGetCurrentAccountDataFilePath(DataFileName, out filePath);
    }

    #endregion
}
