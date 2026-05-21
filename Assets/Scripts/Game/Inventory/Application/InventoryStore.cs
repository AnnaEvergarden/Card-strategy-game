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
            if (!LocalUserDataPaths.TryGetCurrentUserProgressFilePath(DataFileName, out var encryptedPath, out var ownerKey))
            {
                SetCache(new InventoryData(), string.Empty, false);
                return _cachedData;
            }

            try
            {
                if (TryLoadFromFile(encryptedPath, out var data))
                {
                    SetCache(data, ownerKey, true);
                    return _cachedData;
                }

                if (!File.Exists(encryptedPath) &&
                    LocalUserDataPaths.TryGetLegacySharedFilePath(DataFileName, out var legacyPath) &&
                    TryLoadFromFile(legacyPath, out data))
                {
                    SetCache(data, ownerKey, true);
                    Save(_cachedData);
                    return _cachedData;
                }

                SetCache(new InventoryData(), ownerKey, !File.Exists(encryptedPath));
                return _cachedData;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load inventory failed: {ex.Message}");
                SetCache(new InventoryData(), ownerKey, false);
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
            if (!LocalUserDataPaths.TryGetCurrentUserProgressFilePath(DataFileName, out var encryptedPath, out var ownerKey))
            {
                Debug.LogWarning("Save inventory skipped: 当前没有登录账号。");
                return;
            }

            try
            {
                LocalUserDataPaths.EnsureParentDirectory(encryptedPath);

                SetCache(data ?? new InventoryData(), ownerKey, true);
                var json = JsonUtility.ToJson(_cachedData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(encryptedPath, encrypted);
            }
            catch (Exception ex)
            {
                _canSaveCurrent = false;
                Debug.LogError($"Save inventory failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出前强制保存当前仓库缓存。
    /// </summary>
    /// <returns>缓存成功写入当前账号进度时返回 true。</returns>
    public static bool SaveCurrent()
    {
        if (!_canSaveCurrent || !LocalUserDataPaths.IsCurrentUserKey(_cachedOwnerKey))
        {
            Debug.LogWarning("Save inventory skipped: 缓存不属于当前登录账号。");
            return false;
        }

        Save(_cachedData ?? new InventoryData());
        return _canSaveCurrent && LocalUserDataPaths.IsCurrentUserKey(_cachedOwnerKey);
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

                    _ = SaveCurrent();
                    return;
                }
            }

            _cachedData.items.Add(new InventoryItemData
            {
                itemId = id,
                itemName = string.IsNullOrWhiteSpace(itemDisplayName) ? string.Empty : itemDisplayName.Trim(),
                count = count
            });
            _ = SaveCurrent();
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

                return SaveCurrent();
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
                    return SaveCurrent();
                }
            }

            return false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 尝试从指定路径读取仓库数据。
    /// </summary>
    private static bool TryLoadFromFile(string encryptedPath, out InventoryData data)
    {
        data = null;
        if (string.IsNullOrEmpty(encryptedPath) || !File.Exists(encryptedPath))
        {
            return false;
        }

        var bytes = File.ReadAllBytes(encryptedPath);
        if (bytes == null || bytes.Length <= 16)
        {
            return false;
        }

        var json = LocalDataCrypto.DecryptToUtf8(bytes);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        data = JsonUtility.FromJson<InventoryData>(json) ?? new InventoryData();
        data.items ??= new List<InventoryItemData>();
        return true;
    }

    /// <summary>
    /// 写入缓存状态与归属信息。
    /// </summary>
    private static void SetCache(InventoryData data, string ownerKey, bool canSave)
    {
        _cachedData = data ?? new InventoryData();
        _cachedData.items ??= new List<InventoryItemData>();
        _cachedOwnerKey = ownerKey ?? string.Empty;
        _canSaveCurrent = canSave;
    }

    #endregion
}
