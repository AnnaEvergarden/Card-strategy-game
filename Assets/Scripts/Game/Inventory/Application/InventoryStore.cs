using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Auth;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 仓库存储服务：负责本地持久化玩家道具数据（加密存储）。
/// </summary>
public static class InventoryStore
{
    #region Fields

    /// <summary>
    /// 旧版共享仓库数据文件夹名，当前版本仅用于单账号旧档迁移。
    /// </summary>
    private const string DataFolderName = "UserData";

    /// <summary>
    /// 加密后的仓库数据文件名。
    /// </summary>
    private const string DataFileName = "inventory.dat";

    /// <summary>
    /// 最近一次加载或保存的仓库缓存。
    /// </summary>
    private static InventoryData _cachedData = new();

    /// <summary>
    /// 当前缓存是否允许写回磁盘；读档失败后禁止自动保存以保护原文件。
    /// </summary>
    private static bool _canSaveCached = true;

    /// <summary>
    /// 当前缓存所属账号存储键，用于账号切换时重载数据。
    /// </summary>
    private static string _cachedProfileKey = string.Empty;

    /// <summary>
    /// 文件读写锁。
    /// </summary>
    private static readonly object FileLock = new();

    #endregion

    #region Nested Models

    /// <summary>
    /// 单个道具数据。
    /// </summary>
    [Serializable]
    public sealed class InventoryItemData
    {
        /// <summary>
        /// 道具唯一 ID。
        /// </summary>
        public string itemId;

        /// <summary>
        /// 道具显示名。
        /// </summary>
        public string itemName;

        /// <summary>
        /// 数量。
        /// </summary>
        public int count;
    }

    /// <summary>
    /// 仓库快照数据。
    /// </summary>
    [Serializable]
    public sealed class InventoryData
    {
        /// <summary>
        /// 道具列表。
        /// </summary>
        public List<InventoryItemData> items = new();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 读取仓库数据，文件不存在时返回空仓库。
    /// </summary>
    public static InventoryData Load()
    {
        lock (FileLock)
        {
            var profileKey = AccountStore.GetCurrentUserStorageKey();
            try
            {
                var encryptedPath = GetLoadFilePath();

                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        throw new InvalidDataException("仓库数据文件为空或长度无效");
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidDataException("仓库数据解密结果为空");
                    }

                    var data = JsonUtility.FromJson<InventoryData>(json);
                    _cachedData = data ?? new InventoryData();
                    MarkLoadSucceeded(profileKey);
                    return _cachedData;
                }

                _cachedData = new InventoryData();
                MarkLoadSucceeded(profileKey);
                return _cachedData;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load inventory failed: {ex.Message}");
                _cachedData = new InventoryData();
                MarkLoadFailed(profileKey);
                return _cachedData;
            }
        }
    }

    /// <summary>
    /// 保存仓库数据到本地文件。
    /// </summary>
    public static void Save(InventoryData data)
    {
        lock (FileLock)
        {
            var profileKey = AccountStore.GetCurrentUserStorageKey();
            if (!_canSaveCached && string.Equals(_cachedProfileKey, profileKey, StringComparison.Ordinal))
            {
                Debug.LogWarning("Save inventory skipped because the last load failed; keeping existing file untouched.");
                return;
            }

            try
            {
                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cachedData = data ?? new InventoryData();
                _cachedProfileKey = profileKey;
                var json = JsonUtility.ToJson(_cachedData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(GetEncryptedFilePath(), encrypted);
                _canSaveCached = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save inventory failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出前强制保存当前仓库缓存。
    /// </summary>
    public static void SaveCurrent()
    {
        lock (FileLock)
        {
            if (!IsCacheForCurrentProfile())
            {
                Load();
            }

            Save(_cachedData ?? new InventoryData());
        }
    }

    /// <summary>
    /// 向道具仓库增加数量；已存在 itemId 则累加 count，否则新增一条（可选写入显示名）。
    /// </summary>
    public static void AddItem(string itemId, int count = 1, string itemDisplayName = null)
    {
        if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
        {
            return;
        }

        lock (FileLock)
        {
            Load();
            if (!_canSaveCached)
            {
                return;
            }

            _cachedData.items ??= new List<InventoryItemData>();
            var id = itemId.Trim();
            for (var i = 0; i < _cachedData.items.Count; i++)
            {
                var it = _cachedData.items[i];
                if (it != null && string.Equals(it.itemId, id, StringComparison.Ordinal))
                {
                    it.count += count;
                    if (!string.IsNullOrWhiteSpace(itemDisplayName) && string.IsNullOrWhiteSpace(it.itemName))
                    {
                        it.itemName = itemDisplayName.Trim();
                    }

                    SaveCurrent();
                    return;
                }
            }

            _cachedData.items.Add(new InventoryItemData
            {
                itemId = id,
                itemName = string.IsNullOrWhiteSpace(itemDisplayName) ? string.Empty : itemDisplayName.Trim(),
                count = count
            });
            SaveCurrent();
        }
    }

    /// <summary>
    /// 消耗道具仓库中指定 itemId 的数量；数量不足或不存在时返回 false，不修改数据。
    /// 扣减后若为 0 则移除该条目。
    /// </summary>
    public static bool TryConsumeItem(string itemId, int count = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
        {
            return false;
        }

        lock (FileLock)
        {
            Load();
            if (!_canSaveCached)
            {
                return false;
            }

            _cachedData.items ??= new List<InventoryItemData>();
            var id = itemId.Trim();
            for (var i = 0; i < _cachedData.items.Count; i++)
            {
                var it = _cachedData.items[i];
                if (it == null || !string.Equals(it.itemId, id, StringComparison.Ordinal))
                {
                    continue;
                }

                if (it.count < count)
                {
                    return false;
                }

                it.count -= count;
                if (it.count <= 0)
                {
                    _cachedData.items.RemoveAt(i);
                }

                SaveCurrent();
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 从道具仓库中删除指定 itemId 的整条记录（不论数量）。
    /// </summary>
    /// <returns>是否删除了已存在的条目。</returns>
    public static bool RemoveItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        lock (FileLock)
        {
            Load();
            if (!_canSaveCached)
            {
                return false;
            }

            _cachedData.items ??= new List<InventoryItemData>();
            var id = itemId.Trim();
            for (var i = 0; i < _cachedData.items.Count; i++)
            {
                var it = _cachedData.items[i];
                if (it != null && string.Equals(it.itemId, id, StringComparison.Ordinal))
                {
                    _cachedData.items.RemoveAt(i);
                    SaveCurrent();
                    return true;
                }
            }

            return false;
        }
    }

    #endregion

    #region Private Methods

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
    /// 获取当前账号的仓库数据目录。
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
        var gameRootPath = Directory.GetParent(dataPath)?.FullName;
        if (string.IsNullOrEmpty(gameRootPath)) gameRootPath = dataPath;
        return Path.Combine(gameRootPath, DataFolderName);
    }

    /// <summary>
    /// 获取当前账号仓库数据文件完整路径。
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
