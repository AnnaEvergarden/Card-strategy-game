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
    /// 文件读写锁。
    /// </summary>
    private static readonly object FileLock = new();

    /// <summary>
    /// 当前缓存所属的账号目录键。
    /// </summary>
    private static string _cachedUserKey = string.Empty;

    /// <summary>
    /// 当前缓存是否来自可安全保存的数据源。
    /// </summary>
    private static bool _cachedCanSave;

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
            if (!PlayerDataPath.TryGetCurrentUserFilePath(DataFileName, out var path, out var userKey))
            {
                MarkCache(CreateDefault(), string.Empty, false);
                return _cached;
            }

            try
            {
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    if (bytes != null && bytes.Length > 16)
                    {
                        var json = LocalDataCrypto.DecryptToUtf8(bytes);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var data = JsonUtility.FromJson<CurrencyData>(json) ?? CreateDefault();
                            Normalize(data);
                            MarkCache(data, userKey, true);
                            return _cached;
                        }
                    }

                    Debug.LogWarning($"Load currency failed: 数据文件无效，已阻止自动覆盖 => {path}");
                    MarkCache(CreateDefault(), userKey, false);
                    return _cached;
                }

                MarkCache(CreateDefault(), userKey, true);
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load currency failed: {ex.Message}");
                MarkCache(CreateDefault(), userKey, false);
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
            if (!PlayerDataPath.TryEnsureCurrentUserFolder(out _, out var userKey) ||
                !PlayerDataPath.TryGetCurrentUserFilePath(DataFileName, out var path, out _))
            {
                Debug.LogWarning("Save currency skipped: 当前没有登录账号。");
                return;
            }

            try
            {
                var saveData = data ?? CreateDefault();
                Normalize(saveData);
                var json = JsonUtility.ToJson(saveData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
                MarkCache(saveData, userKey, true);
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
        lock (FileLock)
        {
            if (!CanSaveCurrentCache(out var reason))
            {
                Debug.LogWarning($"Save currency skipped: {reason}");
                return;
            }

            Save(_cached ?? CreateDefault());
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
            if (!_cachedCanSave)
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
    /// 增加资源（负数将被忽略）。
    /// </summary>
    public static void Add(int gold, int diamond, int shipTicket)
    {
        lock (FileLock)
        {
            var data = Load();
            if (!_cachedCanSave)
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
    /// 更新内存缓存及其账号归属。
    /// </summary>
    private static void MarkCache(CurrencyData data, string userKey, bool canSave)
    {
        _cached = data ?? CreateDefault();
        _cachedUserKey = userKey ?? string.Empty;
        _cachedCanSave = canSave;
    }

    /// <summary>
    /// 判断当前缓存是否仍属于当前登录账号且可安全写回。
    /// </summary>
    private static bool CanSaveCurrentCache(out string reason)
    {
        reason = string.Empty;
        if (!PlayerDataPath.TryGetCurrentUserKey(out var currentUserKey))
        {
            reason = "当前没有登录账号。";
            return false;
        }

        if (!_cachedCanSave)
        {
            reason = "当前缓存不是从可安全保存的数据源加载，避免覆盖已有存档。";
            return false;
        }

        if (!string.Equals(_cachedUserKey, currentUserKey, StringComparison.Ordinal))
        {
            reason = "当前缓存所属账号与登录账号不一致，避免跨账号覆盖。";
            return false;
        }

        return true;
    }

    #endregion
}
