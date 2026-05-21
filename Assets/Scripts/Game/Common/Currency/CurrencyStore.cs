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
    /// 当前缓存所属账号键，用于防止切号后把旧缓存写入新账号。
    /// </summary>
    private static string _cachedOwnerKey = string.Empty;

    /// <summary>
    /// 当前缓存是否来自可安全保存的账号进度。
    /// </summary>
    private static bool _canSaveCurrent;

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
            if (!LocalUserDataPaths.TryGetCurrentUserProgressFilePath(DataFileName, out var path, out var ownerKey))
            {
                SetCache(CreateDefault(), string.Empty, false);
                return _cached;
            }

            try
            {
                if (TryLoadFromFile(path, out var data))
                {
                    SetCache(data, ownerKey, true);
                    return _cached;
                }

                if (!File.Exists(path) &&
                    LocalUserDataPaths.TryGetLegacySharedFilePath(DataFileName, out var legacyPath) &&
                    TryLoadFromFile(legacyPath, out data))
                {
                    SetCache(data, ownerKey, true);
                    Save(_cached);
                    return _cached;
                }

                SetCache(CreateDefault(), ownerKey, !File.Exists(path));
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load currency failed: {ex.Message}");
                SetCache(CreateDefault(), ownerKey, false);
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
            if (!LocalUserDataPaths.TryGetCurrentUserProgressFilePath(DataFileName, out var path, out var ownerKey))
            {
                Debug.LogWarning("Save currency skipped: 当前没有登录账号。");
                return;
            }

            try
            {
                LocalUserDataPaths.EnsureParentDirectory(path);

                SetCache(data ?? CreateDefault(), ownerKey, true);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
            }
            catch (Exception ex)
            {
                _canSaveCurrent = false;
                Debug.LogError($"Save currency failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存当前缓存。
    /// </summary>
    /// <returns>缓存成功写入当前账号进度时返回 true。</returns>
    public static bool SaveCurrent()
    {
        if (!_canSaveCurrent || !LocalUserDataPaths.IsCurrentUserKey(_cachedOwnerKey))
        {
            Debug.LogWarning("Save currency skipped: 缓存不属于当前登录账号。");
            return false;
        }

        Save(_cached ?? CreateDefault());
        return _canSaveCurrent && LocalUserDataPaths.IsCurrentUserKey(_cachedOwnerKey);
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
            if (!_canSaveCurrent)
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
            return SaveCurrent();
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
            if (!_canSaveCurrent)
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
    /// 尝试从指定路径读取资源数据。
    /// </summary>
    private static bool TryLoadFromFile(string path, out CurrencyData data)
    {
        data = null;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return false;
        }

        var bytes = File.ReadAllBytes(path);
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

    /// <summary>
    /// 写入缓存状态与归属信息。
    /// </summary>
    private static void SetCache(CurrencyData data, string ownerKey, bool canSave)
    {
        _cached = data ?? CreateDefault();
        Normalize(_cached);
        _cachedOwnerKey = ownerKey ?? string.Empty;
        _canSaveCurrent = canSave;
    }

    #endregion
}
