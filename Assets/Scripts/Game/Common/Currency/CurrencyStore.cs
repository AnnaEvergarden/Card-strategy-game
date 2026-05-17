using System;
using System.IO;
using Game.Common.Auth;
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
    /// 当前缓存所属账号；用于防止切号后把旧账号缓存保存到新账号目录。
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
                if (!LocalUserDataPaths.TryResolveCurrentUserReadableFilePath(
                        DataFileName,
                        out var path,
                        out var userPath,
                        out var currentUser,
                        out var isLegacySharedFile))
                {
                    Debug.LogWarning("[CurrencyStore] 当前没有登录账号，跳过读取资源数据。");
                    _cached = CreateDefault();
                    _cachedUser = string.Empty;
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
                            if (isLegacySharedFile)
                            {
                                LocalUserDataPaths.CopyLegacySharedFileIfNeeded(path, userPath);
                            }

                            return _cached;
                        }
                    }

                    _cachedUser = string.Empty;
                    _cached = CreateDefault();
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
                if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var path, out var currentUser))
                {
                    Debug.LogWarning("[CurrencyStore] 当前没有登录账号，跳过保存资源数据。");
                    return;
                }

                var folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(folder))
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
        if (!CanSaveCurrentCache())
        {
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
            if (!CanSaveCurrentCache())
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
            if (!CanSaveCurrentCache())
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
    /// 判断当前缓存是否允许保存到当前登录账号，避免切号串档。
    /// </summary>
    private static bool CanSaveCurrentCache()
    {
        var currentUser = AccountStore.GetCurrentUser();
        if (string.IsNullOrEmpty(currentUser))
        {
            Debug.LogWarning("[CurrencyStore] 当前没有登录账号，跳过保存资源缓存。");
            return false;
        }

        if (!string.Equals(_cachedUser, currentUser, StringComparison.Ordinal))
        {
            Debug.LogWarning("[CurrencyStore] 当前资源缓存不属于登录账号，跳过保存以避免串档。");
            return false;
        }

        return true;
    }

    #endregion
}
