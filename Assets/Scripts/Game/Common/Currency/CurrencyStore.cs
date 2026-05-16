using System;
using System.IO;
using Game.Common.Save;
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
    /// 当前缓存是否来自已登录账号的读写链路；账号切换后用于禁止旧缓存自动落盘。
    /// </summary>
    private static bool _hasLoadedForCurrentUser;

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
                if (!TryGetEncryptedFilePath(out var path))
                {
                    _cached = CreateDefault();
                    _hasLoadedForCurrentUser = false;
                    return _cached;
                }

                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        _cached = CreateDefault();
                        _hasLoadedForCurrentUser = false;
                        return _cached;
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _cached = CreateDefault();
                        _hasLoadedForCurrentUser = false;
                        return _cached;
                    }

                    _cached = JsonUtility.FromJson<CurrencyData>(json) ?? CreateDefault();
                    Normalize(_cached);
                    _hasLoadedForCurrentUser = true;
                    return _cached;
                }

                _cached = CreateDefault();
                _hasLoadedForCurrentUser = true;
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load currency failed: {ex.Message}");
                _cached = CreateDefault();
                _hasLoadedForCurrentUser = false;
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
                if (!TryGetEncryptedFilePath(out var filePath, createFolder: true))
                {
                    Debug.LogWarning("Save currency skipped: 当前没有登录账号。");
                    return;
                }

                _cached = data ?? CreateDefault();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(filePath, encrypted);
                _hasLoadedForCurrentUser = true;
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
        if (!_hasLoadedForCurrentUser)
        {
            Debug.LogWarning("Save current currency skipped: 当前账号资源数据尚未成功加载。");
            return;
        }

        Save(_cached ?? CreateDefault());
    }

    /// <summary>
    /// 清空运行时缓存；账号切换后调用，避免旧账号资源写入新账号目录。
    /// </summary>
    public static void ClearCache()
    {
        lock (FileLock)
        {
            _cached = CreateDefault();
            _hasLoadedForCurrentUser = false;
        }
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
    /// 获取当前登录账号下的资源文件路径。
    /// </summary>
    private static bool TryGetEncryptedFilePath(out string filePath, bool createFolder = false)
    {
        return LocalUserDataPaths.TryGetCurrentAccountDataFilePath(DataFileName, out filePath, createFolder);
    }

    #endregion
}
