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
    /// 当前缓存所属的账号目录键；账号切换后用于阻止旧缓存写入新账号。
    /// </summary>
    private static string _cachedOwnerKey = string.Empty;

    /// <summary>
    /// 当前缓存是否允许自动保存；读档失败时保持 false，避免退出保存覆盖原文件。
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
            if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out var path))
            {
                ResetCacheForOwner(string.Empty);
                Debug.LogWarning("[CurrencyStore] 当前没有登录账号，跳过资源读档。");
                return _cached;
            }

            ResetCacheForOwner(ownerKey);
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
                            _cached = JsonUtility.FromJson<CurrencyData>(json) ?? CreateDefault();
                            Normalize(_cached);
                            _canSaveCurrent = true;
                            return _cached;
                        }
                    }

                    _cached = CreateDefault();
                    _canSaveCurrent = false;
                    return _cached;
                }

                _cached = CreateDefault();
                _canSaveCurrent = true;
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load currency failed: {ex.Message}");
                _cached = CreateDefault();
                _canSaveCurrent = false;
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
            if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out var path))
            {
                Debug.LogWarning("[CurrencyStore] 当前没有登录账号，跳过资源保存。");
                return;
            }

            ResetCacheForOwner(ownerKey);
            try
            {
                LocalUserDataPaths.EnsureParentDirectory(path);
                _cached = data ?? CreateDefault();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
                _canSaveCurrent = true;
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
        if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out _))
        {
            Debug.LogWarning("[CurrencyStore] 当前没有登录账号，跳过资源自动保存。");
            return;
        }

        if (!string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal) || !_canSaveCurrent)
        {
            Debug.LogWarning("[CurrencyStore] 资源缓存未成功加载或账号已切换，跳过自动保存以避免覆盖存档。");
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

        lock (FileLock)
        {
            var data = Load();
            if (!_canSaveCurrent)
            {
                Debug.LogWarning("[CurrencyStore] 资源读档失败，拒绝消耗以避免产生无法保存的状态。");
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
            if (!_canSaveCurrent)
            {
                Debug.LogWarning("[CurrencyStore] 资源读档失败，拒绝增加资源以避免覆盖原存档。");
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
    /// 账号切换时重置缓存状态，避免旧账号数据写入新账号目录。
    /// </summary>
    private static void ResetCacheForOwner(string ownerKey)
    {
        ownerKey ??= string.Empty;
        if (string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal))
        {
            return;
        }

        _cachedOwnerKey = ownerKey;
        _cached = CreateDefault();
        _canSaveCurrent = false;
    }

    #endregion
}
