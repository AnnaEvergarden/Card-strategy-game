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
            if (!TryGetCurrentDataPath(out var encryptedPath, out var ownerKey))
            {
                _cachedData = new InventoryData();
                _cachedOwnerKey = string.Empty;
                _canSaveCurrent = false;
                Debug.LogWarning("[InventoryStore] 当前没有登录账号，跳过仓库读取。");
                return _cachedData;
            }

            try
            {
                MigrateLegacyDataIfNeeded(encryptedPath);
                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        return MarkLoadFailed(ownerKey, "仓库文件为空或格式异常。");
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return MarkLoadFailed(ownerKey, "仓库解密结果为空。");
                    }

                    var data = JsonUtility.FromJson<InventoryData>(json);
                    SetCache(data ?? new InventoryData(), ownerKey, true);
                    return _cachedData;
                }

                SetCache(new InventoryData(), ownerKey, true);
                return _cachedData;
            }
            catch (Exception ex)
            {
                return MarkLoadFailed(ownerKey, $"读取仓库失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存仓库数据到本地文件。
    /// </summary>
    public static bool Save(InventoryData data)
    {
        lock (FileLock)
        {
            if (!TryGetCurrentDataPath(out var encryptedPath, out var ownerKey))
            {
                _canSaveCurrent = false;
                Debug.LogWarning("[InventoryStore] 当前没有登录账号，跳过仓库保存。");
                return false;
            }

            if (!CanWriteForOwner(ownerKey, encryptedPath))
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

                _cachedData = data ?? new InventoryData();
                _cachedOwnerKey = ownerKey;
                _canSaveCurrent = true;
                var json = JsonUtility.ToJson(_cachedData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(encryptedPath, encrypted);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save inventory failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 退出前强制保存当前仓库缓存。
    /// </summary>
    public static bool SaveCurrent()
    {
        if (!_canSaveCurrent)
        {
            Debug.LogError("[InventoryStore] 当前仓库缓存不可保存，已跳过以避免覆盖原存档。");
            return false;
        }

        return Save(_cachedData ?? new InventoryData());
    }

    /// <summary>
    /// 向道具仓库增加数量；已存在 itemId 则累加 count，否则新增一条（可选写入显示名）。
    /// </summary>
    public static bool AddItem(string itemId, int count = 1, string itemDisplayName = null)
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
                if (it != null && string.Equals(it.itemId, id, StringComparison.Ordinal))
                {
                    var oldCount = it.count;
                    var oldName = it.itemName;
                    it.count += count;
                    if (!string.IsNullOrWhiteSpace(itemDisplayName) && string.IsNullOrWhiteSpace(it.itemName))
                    {
                        it.itemName = itemDisplayName.Trim();
                    }

                    if (SaveCurrent())
                    {
                        return true;
                    }

                    it.count = oldCount;
                    it.itemName = oldName;
                    return false;
                }
            }

            var newItem = new InventoryItemData
            {
                itemId = id,
                itemName = string.IsNullOrWhiteSpace(itemDisplayName) ? string.Empty : itemDisplayName.Trim(),
                count = count
            };
            _cachedData.items.Add(newItem);
            if (SaveCurrent())
            {
                return true;
            }

            _cachedData.items.Remove(newItem);
            return false;
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
                var removed = it.count <= 0;
                if (removed)
                {
                    _cachedData.items.RemoveAt(i);
                }

                if (SaveCurrent())
                {
                    return true;
                }

                if (removed)
                {
                    _cachedData.items.Insert(i, it);
                }

                it.count += count;
                return false;
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
                    if (SaveCurrent())
                    {
                        return true;
                    }

                    _cachedData.items.Insert(i, it);
                    return false;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 账号切换时清空仓库缓存，避免旧账号数据被后续自动保存。
    /// </summary>
    public static void ResetCacheForAccountChange()
    {
        lock (FileLock)
        {
            _cachedData = new InventoryData();
            _cachedOwnerKey = string.Empty;
            _canSaveCurrent = false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 设置仓库缓存及其写入状态。
    /// </summary>
    private static void SetCache(InventoryData data, string ownerKey, bool canSave)
    {
        _cachedData = data ?? new InventoryData();
        _cachedOwnerKey = ownerKey ?? string.Empty;
        _canSaveCurrent = canSave;
    }

    /// <summary>
    /// 记录读档失败并返回安全默认值，但禁止后续覆盖保存。
    /// </summary>
    private static InventoryData MarkLoadFailed(string ownerKey, string message)
    {
        SetCache(new InventoryData(), ownerKey, false);
        Debug.LogError($"[InventoryStore] {message}");
        return _cachedData;
    }

    /// <summary>
    /// 判断当前缓存是否允许写入指定账号目录。
    /// </summary>
    private static bool CanWriteForOwner(string ownerKey, string path)
    {
        if (!string.IsNullOrEmpty(_cachedOwnerKey) &&
            !string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal))
        {
            Debug.LogError("[InventoryStore] 仓库缓存所属账号与当前账号不一致，已跳过保存。");
            return false;
        }

        if (!_canSaveCurrent && File.Exists(path))
        {
            Debug.LogError("[InventoryStore] 最近一次仓库读档失败，已跳过保存以避免覆盖原文件。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取当前账号仓库目录路径。
    /// </summary>
    private static string GetDataFolderPath()
    {
        return LocalUserDataPaths.TryGetCurrentUserDataFolderPath(out var folder, out _)
            ? folder
            : string.Empty;
    }

    /// <summary>
    /// 获取当前账号仓库文件路径。
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
    /// 首次进入账号目录时迁移早期重制版共享仓库文件，避免修复后旧进度不可见。
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
        Debug.Log($"[InventoryStore] 已迁移旧共享仓库文件到当前账号目录：{currentPath}");
    }

    #endregion
}
