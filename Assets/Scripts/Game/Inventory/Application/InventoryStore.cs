using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Save;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 仓库存储服务：负责本地持久化玩家道具数据（加密存储）。
/// </summary>
public static class InventoryStore
{
    #region Fields

    /// <summary>
    /// 加密后的仓库数据文件名。
    /// </summary>
    private const string DataFileName = "inventory.dat";

    /// <summary>
    /// 最近一次加载或保存的仓库缓存。
    /// </summary>
    private static InventoryData _cachedData = new();

    /// <summary>
    /// 当前缓存所属的账号目录键，用于账号切换时阻止串档保存。
    /// </summary>
    private static string _cachedOwnerKey = string.Empty;

    /// <summary>
    /// 当前缓存是否允许自动保存；读档失败时为 false。
    /// </summary>
    private static bool _canSaveCurrent;

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
            if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out var encryptedPath))
            {
                ResetCacheForOwner(string.Empty);
                Debug.LogWarning("[InventoryStore] 当前没有登录账号，跳过仓库读档。");
                return _cachedData;
            }

            ResetCacheForOwner(ownerKey);
            try
            {
                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        _cachedData = new InventoryData();
                        _canSaveCurrent = false;
                        return _cachedData;
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _cachedData = new InventoryData();
                        _canSaveCurrent = false;
                        return _cachedData;
                    }

                    var data = JsonUtility.FromJson<InventoryData>(json);
                    _cachedData = data ?? new InventoryData();
                    _canSaveCurrent = true;
                    return _cachedData;
                }

                _cachedData = new InventoryData();
                _canSaveCurrent = true;
                return _cachedData;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load inventory failed: {ex.Message}");
                _cachedData = new InventoryData();
                _canSaveCurrent = false;
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
            if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out var encryptedPath))
            {
                Debug.LogWarning("[InventoryStore] 当前没有登录账号，跳过仓库保存。");
                return;
            }

            ResetCacheForOwner(ownerKey);
            try
            {
                LocalUserDataPaths.EnsureParentDirectory(encryptedPath);
                _cachedData = data ?? new InventoryData();
                var json = JsonUtility.ToJson(_cachedData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(encryptedPath, encrypted);
                _canSaveCurrent = true;
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
        if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out _))
        {
            Debug.LogWarning("[InventoryStore] 当前没有登录账号，跳过仓库自动保存。");
            return;
        }

        if (!string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal) || !_canSaveCurrent)
        {
            Debug.LogWarning("[InventoryStore] 仓库缓存未成功加载或账号已切换，跳过自动保存以避免覆盖存档。");
            return;
        }

        Save(_cachedData ?? new InventoryData());
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
            if (!_canSaveCurrent)
            {
                Debug.LogWarning("[InventoryStore] 仓库读档失败，拒绝增加道具以避免覆盖原存档。");
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
            if (!_canSaveCurrent)
            {
                Debug.LogWarning("[InventoryStore] 仓库读档失败，拒绝消耗道具以避免产生无法保存的状态。");
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
            if (!_canSaveCurrent)
            {
                Debug.LogWarning("[InventoryStore] 仓库读档失败，拒绝删除道具以避免覆盖原存档。");
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
    /// 账号切换时重置仓库缓存状态，避免旧账号数据写入新账号目录。
    /// </summary>
    private static void ResetCacheForOwner(string ownerKey)
    {
        ownerKey ??= string.Empty;
        if (string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal))
        {
            return;
        }

        _cachedOwnerKey = ownerKey;
        _cachedData = new InventoryData();
        _canSaveCurrent = false;
    }

    #endregion
}
