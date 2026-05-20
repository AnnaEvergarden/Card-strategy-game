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
    /// 当前缓存所属账号；用于防止登出或切号后误写旧缓存。
    /// </summary>
    private static string _cachedOwner = string.Empty;

    /// <summary>
    /// 当前缓存是否允许被 SaveCurrent 自动写盘。
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
            var user = AccountStore.GetCurrentUser();
            if (!LocalUserDataPaths.TryGetUserDataFilePath(user, DataFileName, out var path))
            {
                _cached = CreateDefault();
                _cachedOwner = string.Empty;
                _canSaveCurrent = false;
                return _cached;
            }

            _cachedOwner = user.Trim();
            var fileExists = File.Exists(path);
            if (TryLoadFromFile(path, out var loaded))
            {
                _cached = loaded;
                Normalize(_cached);
                _canSaveCurrent = true;
                return _cached;
            }

            if (!fileExists && TryLoadFromFile(LocalUserDataPaths.GetLegacySharedDataFilePath(DataFileName), out loaded))
            {
                _cached = loaded;
                Normalize(_cached);
                _canSaveCurrent = true;
                Save(_cached);
                return _cached;
            }

            _cached = CreateDefault();
            _canSaveCurrent = !fileExists;
            if (fileExists)
            {
                Debug.LogWarning("Load currency failed: 已阻止自动保存覆盖现有资源文件。");
            }

            return _cached;
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
                var user = AccountStore.GetCurrentUser();
                if (!LocalUserDataPaths.TryGetUserDataFilePath(user, DataFileName, out var filePath))
                {
                    Debug.LogWarning("Save currency skipped: 当前没有登录账号。");
                    _canSaveCurrent = false;
                    return;
                }

                var folder = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? CreateDefault();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(filePath, encrypted);
                _cachedOwner = user.Trim();
                _canSaveCurrent = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save currency failed: {ex.Message}");
                _canSaveCurrent = false;
            }
        }
    }

    /// <summary>
    /// 保存当前缓存。
    /// </summary>
    /// <returns>是否成功写入当前登录账号的数据文件。</returns>
    public static bool SaveCurrent()
    {
        if (!CanSaveCurrentForActiveUser())
        {
            return false;
        }

        Save(_cached ?? CreateDefault());
        return _canSaveCurrent;
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
            if (!CanSaveCurrentForActiveUser())
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
            if (!CanSaveCurrentForActiveUser())
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
    /// 尝试从指定加密文件读取资源数据。
    /// </summary>
    private static bool TryLoadFromFile(string filePath, out CurrencyData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(filePath);
            if (bytes == null || bytes.Length <= 16) return false;

            var json = LocalDataCrypto.DecryptToUtf8(bytes);
            if (string.IsNullOrWhiteSpace(json)) return false;

            data = JsonUtility.FromJson<CurrencyData>(json);
            return data != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Load currency failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 判断当前缓存是否仍属于当前登录账号并允许自动保存。
    /// </summary>
    private static bool CanSaveCurrentForActiveUser()
    {
        var user = AccountStore.GetCurrentUser();
        return _canSaveCurrent &&
               !string.IsNullOrWhiteSpace(_cachedOwner) &&
               string.Equals(_cachedOwner, (user ?? string.Empty).Trim(), StringComparison.Ordinal);
    }

    #endregion
}
