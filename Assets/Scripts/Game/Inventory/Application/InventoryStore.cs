using System;
using System.Collections.Generic;
using System.IO;
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
    /// 上一次读取是否失败；失败时禁止保存默认仓库覆盖原文件。
    /// </summary>
    private static bool _saveBlocked;

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
    /// 读取当前账号仓库数据；文件不存在时返回空仓库，读取失败时禁止后续保存覆盖原文件。
    /// </summary>
    public static InventoryData Load()
    {
        lock (FileLock)
        {
            try
            {
                if (!TryGetEncryptedFilePath(out var encryptedPath))
                {
                    return MarkLoadFailed(new InventoryData(), "Load inventory skipped: 当前没有登录账号。");
                }

                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        return MarkLoadFailed(new InventoryData(), "Load inventory failed: 仓库存档文件过短或为空。");
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return MarkLoadFailed(new InventoryData(), "Load inventory failed: 仓库存档解密结果为空。");
                    }

                    var data = JsonUtility.FromJson<InventoryData>(json);
                    _cachedData = data ?? new InventoryData();
                    _cachedData.items ??= new List<InventoryItemData>();
                    _saveBlocked = false;
                    return _cachedData;
                }

                _cachedData = new InventoryData();
                _saveBlocked = false;
                return _cachedData;
            }
            catch (Exception ex)
            {
                return MarkLoadFailed(new InventoryData(), $"Load inventory failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存仓库数据到当前账号本地文件。
    /// </summary>
    public static void Save(InventoryData data)
    {
        lock (FileLock)
        {
            try
            {
                if (_saveBlocked)
                {
                    Debug.LogError("Save inventory skipped: 上一次读取仓库存档失败，禁止覆盖原文件。");
                    return;
                }

                if (!TryGetDataFolderPath(out var folder))
                {
                    Debug.LogWarning("Save inventory skipped: 当前没有登录账号。");
                    return;
                }

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cachedData = data ?? new InventoryData();
                _cachedData.items ??= new List<InventoryItemData>();
                var json = JsonUtility.ToJson(_cachedData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                if (TryGetEncryptedFilePath(out var filePath))
                {
                    File.WriteAllBytes(filePath, encrypted);
                }
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
        Save(_cachedData ?? new InventoryData());
    }

    /// <summary>
    /// 清理运行时缓存；账号切换后必须先清理，避免旧账号仓库写入新账号目录。
    /// </summary>
    public static void ClearCache()
    {
        lock (FileLock)
        {
            _cachedData = new InventoryData();
            _saveBlocked = true;
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
            if (_saveBlocked)
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
    /// 消耗道具仓库中指定 itemId 的数量；数量不足、不存在或读档失败时返回 false，不修改数据。
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
            if (_saveBlocked)
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
            if (_saveBlocked)
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
    /// 标记读取失败并返回安全默认值；后续保存会被阻止，避免数据丢失。
    /// </summary>
    /// <param name="fallback">返回给调用方的默认数据。</param>
    /// <param name="message">失败原因。</param>
    private static InventoryData MarkLoadFailed(InventoryData fallback, string message)
    {
        Debug.LogWarning(message);
        _cachedData = fallback;
        _cachedData.items ??= new List<InventoryItemData>();
        _saveBlocked = true;
        return _cachedData;
    }

    /// <summary>
    /// 尝试获取当前账号的仓库数据目录。
    /// </summary>
    private static bool TryGetDataFolderPath(out string folderPath)
    {
        return LocalSavePath.TryGetCurrentAccountDataFolderPath(out folderPath);
    }

    /// <summary>
    /// 尝试获取当前账号的仓库数据文件路径。
    /// </summary>
    private static bool TryGetEncryptedFilePath(out string filePath)
    {
        return LocalSavePath.TryGetCurrentAccountDataFilePath(DataFileName, out filePath);
    }

    #endregion
}
