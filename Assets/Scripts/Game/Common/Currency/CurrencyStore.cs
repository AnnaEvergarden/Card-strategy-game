using System;
using System.IO;
using Game.Common.Auth;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 资源存储服务：持久化玩家金币、钻石、船票，并提供安全的增减接口（加密存储）。
/// </summary>
public static class CurrencyStore
{
    #region Fields

    /// <summary>
    /// 旧版共享资源数据文件夹名，当前版本仅用于单账号旧档迁移。
    /// </summary>
    private const string DataFolderName = "UserData";

    /// <summary>
    /// 加密后的资源数据文件名。
    /// </summary>
    private const string DataFileName = "currency.dat";

    /// <summary>
    /// 内存缓存。
    /// </summary>
    private static CurrencyData _cached = new();

    /// <summary>
    /// 当前缓存是否允许写回磁盘；读档失败后置为 false 以避免覆盖旧档。
    /// </summary>
    private static bool _canSaveCached = true;

    /// <summary>
    /// 当前缓存所属账号存储键，用于切换账号时强制重载。
    /// </summary>
    private static string _cachedProfileKey = string.Empty;

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
            var profileKey = AccountStore.GetCurrentUserStorageKey();
            try
            {
                var path = GetLoadFilePath();
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        throw new InvalidDataException("资源数据文件为空或长度无效");
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidDataException("资源数据解密结果为空");
                    }

                    _cached = JsonUtility.FromJson<CurrencyData>(json) ?? CreateDefault();
                    Normalize(_cached);
                    MarkLoadSucceeded(profileKey);
                    return _cached;
                }

                _cached = CreateDefault();
                MarkLoadSucceeded(profileKey);
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load currency failed: {ex.Message}");
                _cached = CreateDefault();
                MarkLoadFailed(profileKey);
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
            var profileKey = AccountStore.GetCurrentUserStorageKey();
            if (!_canSaveCached && string.Equals(_cachedProfileKey, profileKey, StringComparison.Ordinal))
            {
                Debug.LogWarning("Save currency skipped because the last load failed; keeping existing file untouched.");
                return;
            }

            try
            {
                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? CreateDefault();
                _cachedProfileKey = profileKey;
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(GetEncryptedFilePath(), encrypted);
                _canSaveCached = true;
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
            if (!IsCacheForCurrentProfile())
            {
                Load();
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
            if (!_canSaveCached)
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
            if (!_canSaveCached)
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
    /// 标记读档成功，并把缓存绑定到当前账号。
    /// </summary>
    private static void MarkLoadSucceeded(string profileKey)
    {
        _cachedProfileKey = profileKey;
        _canSaveCached = true;
    }

    /// <summary>
    /// 标记读档失败，后续自动保存会跳过以保护原文件。
    /// </summary>
    private static void MarkLoadFailed(string profileKey)
    {
        _cachedProfileKey = profileKey;
        _canSaveCached = false;
    }

    /// <summary>
    /// 判断当前缓存是否属于当前登录账号。
    /// </summary>
    private static bool IsCacheForCurrentProfile()
    {
        return string.Equals(_cachedProfileKey, AccountStore.GetCurrentUserStorageKey(), StringComparison.Ordinal);
    }

    /// <summary>
    /// 获取当前账号的数据目录。
    /// </summary>
    private static string GetDataFolderPath()
    {
        return AccountStore.GetCurrentUserDataFolderPath();
    }

    /// <summary>
    /// 获取旧版共享数据目录，用于单账号项目升级时兼容旧档。
    /// </summary>
    private static string GetLegacyDataFolderPath()
    {
        var dataPath = Application.dataPath;
        var gameRoot = Directory.GetParent(dataPath)?.FullName;
        if (string.IsNullOrEmpty(gameRoot))
        {
            gameRoot = dataPath;
        }

        return Path.Combine(gameRoot, DataFolderName);
    }

    /// <summary>
    /// 获取当前账号资源数据文件完整路径。
    /// </summary>
    private static string GetEncryptedFilePath() => Path.Combine(GetDataFolderPath(), DataFileName);

    /// <summary>
    /// 获取本次读取应使用的文件路径；当前账号无档且仅有单账号时允许读取旧共享档。
    /// </summary>
    private static string GetLoadFilePath()
    {
        var currentPath = GetEncryptedFilePath();
        if (File.Exists(currentPath))
        {
            return currentPath;
        }

        var legacyPath = Path.Combine(GetLegacyDataFolderPath(), DataFileName);
        return AccountStore.CanMigrateLegacyUserData() && File.Exists(legacyPath) ? legacyPath : currentPath;
    }

    #endregion
}
