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
    /// 当前缓存所属的账号目录键。
    /// </summary>
    private static string _cachedUserKey = string.Empty;

    /// <summary>
    /// 当前缓存是否来自可安全保存的数据源。
    /// </summary>
    private static bool _cachedCanSave;

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
            if (!PlayerDataPath.TryGetCurrentUserFilePath(DataFileName, out var encryptedPath, out var userKey))
            {
                MarkCache(new InventoryData(), string.Empty, false);
                return _cachedData;
            }

            try
            {
                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        Debug.LogWarning($"Load inventory failed: 数据文件无效，已阻止自动覆盖 => {encryptedPath}");
                        MarkCache(new InventoryData(), userKey, false);
                        return _cachedData;
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Debug.LogWarning($"Load inventory failed: 数据内容为空，已阻止自动覆盖 => {encryptedPath}");
                        MarkCache(new InventoryData(), userKey, false);
                        return _cachedData;
                    }

                    var data = JsonUtility.FromJson<InventoryData>(json);
                    MarkCache(data ?? new InventoryData(), userKey, true);
                    return _cachedData;
                }

                MarkCache(new InventoryData(), userKey, true);
                return _cachedData;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load inventory failed: {ex.Message}");
                MarkCache(new InventoryData(), userKey, false);
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
            if (!PlayerDataPath.TryEnsureCurrentUserFolder(out _, out var userKey) ||
                !PlayerDataPath.TryGetCurrentUserFilePath(DataFileName, out var path, out _))
            {
                Debug.LogWarning("Save inventory skipped: 当前没有登录账号。");
                return;
            }

            try
            {
                var saveData = data ?? new InventoryData();
                var json = JsonUtility.ToJson(saveData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
                MarkCache(saveData, userKey, true);
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
            if (!CanSaveCurrentCache(out var reason))
            {
                Debug.LogWarning($"Save inventory skipped: {reason}");
                return;
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
            if (!_cachedCanSave)
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
            if (!_cachedCanSave)
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
            if (!_cachedCanSave)
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
    /// 更新内存缓存及其账号归属。
    /// </summary>
    private static void MarkCache(InventoryData data, string userKey, bool canSave)
    {
        _cachedData = data ?? new InventoryData();
        _cachedUserKey = userKey ?? string.Empty;
        _cachedCanSave = canSave;
    }

    /// <summary>
    /// 判断当前缓存是否仍属于当前登录账号且可安全写回。
    /// </summary>
    private static bool CanSaveCurrentCache(out string reason)
    {
        reason = string.Empty;
        if (!PlayerDataPath.TryGetCurrentUserKey(out var currentUserKey))
        {
            reason = "当前没有登录账号。";
            return false;
        }

        if (!_cachedCanSave)
        {
            reason = "当前缓存不是从可安全保存的数据源加载，避免覆盖已有存档。";
            return false;
        }

        if (!string.Equals(_cachedUserKey, currentUserKey, StringComparison.Ordinal))
        {
            reason = "当前缓存所属账号与登录账号不一致，避免跨账号覆盖。";
            return false;
        }

        return true;
    }

    #endregion
}
