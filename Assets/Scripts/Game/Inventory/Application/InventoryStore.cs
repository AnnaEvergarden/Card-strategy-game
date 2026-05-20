using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Auth;
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
            var user = AccountStore.GetCurrentUser();
            if (!LocalUserDataPaths.TryGetUserDataFilePath(user, DataFileName, out var path))
            {
                _cachedData = new InventoryData();
                _cachedOwner = string.Empty;
                _canSaveCurrent = false;
                return _cachedData;
            }

            _cachedOwner = user.Trim();
            var fileExists = File.Exists(path);
            if (TryLoadFromFile(path, out var loaded))
            {
                _cachedData = loaded;
                _canSaveCurrent = true;
                return _cachedData;
            }

            if (!fileExists && TryLoadFromFile(LocalUserDataPaths.GetLegacySharedDataFilePath(DataFileName), out loaded))
            {
                _cachedData = loaded;
                _canSaveCurrent = true;
                Save(_cachedData);
                return _cachedData;
            }

            _cachedData = new InventoryData();
            _canSaveCurrent = !fileExists;
            if (fileExists)
            {
                Debug.LogWarning("Load inventory failed: 已阻止自动保存覆盖现有仓库文件。");
            }

            return _cachedData;
        }
    }

    /// <summary>
    /// 保存仓库数据到本地文件。
    /// </summary>
    public static void Save(InventoryData data)
    {
        lock (FileLock)
        {
            try
            {
                var user = AccountStore.GetCurrentUser();
                if (!LocalUserDataPaths.TryGetUserDataFilePath(user, DataFileName, out var filePath))
                {
                    Debug.LogWarning("Save inventory skipped: 当前没有登录账号。");
                    _canSaveCurrent = false;
                    return;
                }

                var folder = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cachedData = data ?? new InventoryData();
                var json = JsonUtility.ToJson(_cachedData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(filePath, encrypted);
                _cachedOwner = user.Trim();
                _canSaveCurrent = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save inventory failed: {ex.Message}");
                _canSaveCurrent = false;
            }
        }
    }

    /// <summary>
    /// 退出前强制保存当前仓库缓存。
    /// </summary>
    public static void SaveCurrent()
    {
        if (!CanSaveCurrentForActiveUser())
        {
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
    /// 尝试从指定加密文件读取仓库数据。
    /// </summary>
    private static bool TryLoadFromFile(string filePath, out InventoryData data)
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

            data = JsonUtility.FromJson<InventoryData>(json);
            return data != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Load inventory failed: {ex.Message}");
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
