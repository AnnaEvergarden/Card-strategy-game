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
    /// 当前缓存所属账号目录键。
    /// </summary>
    private static string _cachedOwnerKey = string.Empty;

    /// <summary>
    /// 当前缓存是否允许保存；读档失败或未登录时为 false。
    /// </summary>
    private static bool _canSaveCurrent;

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
            if (!TryGetCurrentDataPath(out var path, out var ownerKey))
            {
                _cached = CreateDefault();
                _cachedOwnerKey = string.Empty;
                _canSaveCurrent = false;
                Debug.LogWarning("[CurrencyStore] 当前没有登录账号，跳过资源读取。");
                return _cached;
            }

            try
            {
                MigrateLegacyDataIfNeeded(path);
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    if (bytes != null && bytes.Length > 16)
                    {
                        var json = LocalDataCrypto.DecryptToUtf8(bytes);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            SetCache(JsonUtility.FromJson<CurrencyData>(json) ?? CreateDefault(), ownerKey, true);
                            Normalize(_cached);
                            return _cached;
                        }
                    }

                    return MarkLoadFailed(ownerKey, "资源文件为空或格式异常。");
                }

                SetCache(CreateDefault(), ownerKey, true);
                return _cached;
            }
            catch (Exception ex)
            {
                return MarkLoadFailed(ownerKey, $"读取资源失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存资源数据。
    /// </summary>
    public static bool Save(CurrencyData data)
    {
        lock (FileLock)
        {
            if (!TryGetCurrentDataPath(out var path, out var ownerKey))
            {
                _canSaveCurrent = false;
                Debug.LogWarning("[CurrencyStore] 当前没有登录账号，跳过资源保存。");
                return false;
            }

            if (!CanWriteForOwner(ownerKey, path))
            {
                return false;
            }

            try
            {
                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? CreateDefault();
                _cachedOwnerKey = ownerKey;
                _canSaveCurrent = true;
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save currency failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 保存当前缓存。
    /// </summary>
    public static bool SaveCurrent()
    {
        if (!_canSaveCurrent)
        {
            Debug.LogError("[CurrencyStore] 当前资源缓存不可保存，已跳过以避免覆盖原存档。");
            return false;
        }

        return Save(_cached ?? CreateDefault());
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

            var oldGold = data.gold;
            var oldDiamond = data.diamond;
            var oldShipTicket = data.shipTicket;
            data.gold -= gold;
            data.diamond -= diamond;
            data.shipTicket -= shipTicket;
            if (SaveCurrent())
            {
                return true;
            }

            data.gold = oldGold;
            data.diamond = oldDiamond;
            data.shipTicket = oldShipTicket;
            return false;
        }
    }

    /// <summary>
    /// 增加资源（负数将被忽略）。
    /// </summary>
    public static bool Add(int gold, int diamond, int shipTicket)
    {
        lock (FileLock)
        {
            var data = Load();
            if (!_canSaveCurrent)
            {
                return false;
            }

            var oldGold = data.gold;
            var oldDiamond = data.diamond;
            var oldShipTicket = data.shipTicket;
            if (gold > 0) data.gold += gold;
            if (diamond > 0) data.diamond += diamond;
            if (shipTicket > 0) data.shipTicket += shipTicket;
            if (SaveCurrent())
            {
                return true;
            }

            data.gold = oldGold;
            data.diamond = oldDiamond;
            data.shipTicket = oldShipTicket;
            return false;
        }
    }

    /// <summary>
    /// 账号切换时清空资源缓存，避免旧账号数据被后续自动保存。
    /// </summary>
    public static void ResetCacheForAccountChange()
    {
        lock (FileLock)
        {
            _cached = new CurrencyData();
            _cachedOwnerKey = string.Empty;
            _canSaveCurrent = false;
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
    /// 设置资源缓存及其写入状态。
    /// </summary>
    private static void SetCache(CurrencyData data, string ownerKey, bool canSave)
    {
        _cached = data ?? CreateDefault();
        _cachedOwnerKey = ownerKey ?? string.Empty;
        _canSaveCurrent = canSave;
    }

    /// <summary>
    /// 记录读档失败并返回安全默认值，但禁止后续覆盖保存。
    /// </summary>
    private static CurrencyData MarkLoadFailed(string ownerKey, string message)
    {
        SetCache(CreateDefault(), ownerKey, false);
        Debug.LogError($"[CurrencyStore] {message}");
        return _cached;
    }

    /// <summary>
    /// 判断当前缓存是否允许写入指定账号目录。
    /// </summary>
    private static bool CanWriteForOwner(string ownerKey, string path)
    {
        if (!string.IsNullOrEmpty(_cachedOwnerKey) &&
            !string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal))
        {
            Debug.LogError("[CurrencyStore] 资源缓存所属账号与当前账号不一致，已跳过保存。");
            return false;
        }

        if (!_canSaveCurrent && File.Exists(path))
        {
            Debug.LogError("[CurrencyStore] 最近一次资源读档失败，已跳过保存以避免覆盖原文件。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取当前账号资源目录路径。
    /// </summary>
    private static string GetDataFolderPath()
    {
        return LocalUserDataPaths.TryGetCurrentUserDataFolderPath(out var folder, out _)
            ? folder
            : string.Empty;
    }

    /// <summary>
    /// 获取当前账号资源文件路径。
    /// </summary>
    private static bool TryGetCurrentDataPath(out string path, out string ownerKey)
    {
        if (!LocalUserDataPaths.TryGetCurrentUserDataFolderPath(out var folder, out ownerKey))
        {
            path = string.Empty;
            return false;
        }

        path = Path.Combine(folder, DataFileName);
        return true;
    }

    /// <summary>
    /// 首次进入账号目录时迁移早期重制版共享资源文件，避免修复后旧进度不可见。
    /// </summary>
    private static void MigrateLegacyDataIfNeeded(string currentPath)
    {
        if (File.Exists(currentPath))
        {
            return;
        }

        var legacyPath = LocalUserDataPaths.GetLegacySharedDataFilePath(DataFileName);
        if (!File.Exists(legacyPath))
        {
            return;
        }

        var folder = Path.GetDirectoryName(currentPath);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.Copy(legacyPath, currentPath, false);
        Debug.Log($"[CurrencyStore] 已迁移旧共享资源文件到当前账号目录：{currentPath}");
    }

    #endregion
}
