using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Save;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 卡牌仓库持久化数据：记录玩家拥有的卡牌列表（加密存储）。
/// </summary>
public static class CardCollectionStore
{
    #region Fields

    /// <summary>
    /// 加密后的卡牌仓库文件名。
    /// </summary>
    private const string DataFileName = "card_collection.dat";

    /// <summary>
    /// 内存缓存，供退出保存使用。
    /// </summary>
    private static CardCollectionData _cached = new();

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
    /// 单张卡牌在仓库中的记录。
    /// </summary>
    [Serializable]
    public sealed class CardEntry
    {
        /// <summary>
        /// 卡牌配置 ID（与 CardConfig 中 id 一致）。
        /// </summary>
        public string cardId;

        /// <summary>
        /// 数量（叠放张数，可为 1）。
        /// </summary>
        public int count = 1;
    }

    /// <summary>
    /// 卡牌仓库快照。
    /// </summary>
    [Serializable]
    public sealed class CardCollectionData
    {
        /// <summary>
        /// 卡牌列表。
        /// </summary>
        public List<CardEntry> cards = new();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 读取卡牌仓库；若文件不存在则返回空列表并可写入默认测试数据（编辑器下）。
    /// </summary>
    public static CardCollectionData Load()
    {
        lock (FileLock)
        {
            if (!TryGetCurrentDataPath(out var encryptedPath, out var ownerKey))
            {
                _cached = new CardCollectionData();
                _cachedOwnerKey = string.Empty;
                _canSaveCurrent = false;
                Debug.LogWarning("[CardCollectionStore] 当前没有登录账号，跳过卡牌仓库读取。");
                return _cached;
            }

            try
            {
                MigrateLegacyDataIfNeeded(encryptedPath);
                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        return MarkLoadFailed(ownerKey, "卡牌仓库文件为空或格式异常。");
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return MarkLoadFailed(ownerKey, "卡牌仓库解密结果为空。");
                    }

                    var data = JsonUtility.FromJson<CardCollectionData>(json);
                    SetCache(data ?? new CardCollectionData(), ownerKey, true);
                    return _cached;
                }

                SetCache(CreateDefaultIfNeeded(), ownerKey, true);
                return _cached;
            }
            catch (Exception ex)
            {
                return MarkLoadFailed(ownerKey, $"读取卡牌仓库失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存卡牌仓库。
    /// </summary>
    public static bool Save(CardCollectionData data)
    {
        lock (FileLock)
        {
            if (!TryGetCurrentDataPath(out var encryptedPath, out var ownerKey))
            {
                _canSaveCurrent = false;
                Debug.LogWarning("[CardCollectionStore] 当前没有登录账号，跳过卡牌仓库保存。");
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

                _cached = data ?? new CardCollectionData();
                _cachedOwnerKey = ownerKey;
                _canSaveCurrent = true;
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(encryptedPath, encrypted);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save card collection failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 退出前保存当前缓存。
    /// </summary>
    public static bool SaveCurrent()
    {
        if (!_canSaveCurrent)
        {
            Debug.LogError("[CardCollectionStore] 当前卡牌仓库缓存不可保存，已跳过以避免覆盖原存档。");
            return false;
        }

        return Save(_cached ?? new CardCollectionData());
    }

    /// <summary>
    /// 向舰娘仓库增加指定 cardId 的数量；已存在则累加 count，否则新增一条。
    /// </summary>
    public static bool AddCards(string cardId, int count = 1)
    {
        if (string.IsNullOrWhiteSpace(cardId) || count <= 0)
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

            _cached.cards ??= new List<CardEntry>();
            var id = cardId.Trim();
            for (var i = 0; i < _cached.cards.Count; i++)
            {
                var e = _cached.cards[i];
                if (e != null && string.Equals(e.cardId, id, StringComparison.Ordinal))
                {
                    e.count += count;
                    if (SaveCurrent())
                    {
                        return true;
                    }

                    e.count -= count;
                    return false;
                }
            }

            var entry = new CardEntry { cardId = id, count = count };
            _cached.cards.Add(entry);
            if (SaveCurrent())
            {
                return true;
            }

            _cached.cards.Remove(entry);
            return false;
        }
    }

    /// <summary>
    /// 消耗舰娘仓库中指定 cardId 的数量；数量不足或不存在时返回 false，不修改数据。
    /// 扣减后若为 0 则移除该条目。
    /// </summary>
    public static bool TryConsumeCards(string cardId, int count = 1)
    {
        if (string.IsNullOrWhiteSpace(cardId) || count <= 0)
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

            _cached.cards ??= new List<CardEntry>();
            var id = cardId.Trim();
            for (var i = 0; i < _cached.cards.Count; i++)
            {
                var e = _cached.cards[i];
                if (e == null || !string.Equals(e.cardId, id, StringComparison.Ordinal))
                {
                    continue;
                }

                if (e.count < count)
                {
                    return false;
                }

                e.count -= count;
                var removed = e.count <= 0;
                if (removed)
                {
                    _cached.cards.RemoveAt(i);
                }

                if (SaveCurrent())
                {
                    return true;
                }

                if (removed)
                {
                    _cached.cards.Insert(i, e);
                }

                e.count += count;
                return false;
            }

            return false;
        }
    }

    /// <summary>
    /// 从舰娘仓库中删除指定 cardId 的整条记录（不论数量）。
    /// </summary>
    /// <returns>是否删除了已存在的条目。</returns>
    public static bool RemoveCards(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
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

            _cached.cards ??= new List<CardEntry>();
            var id = cardId.Trim();
            for (var i = 0; i < _cached.cards.Count; i++)
            {
                var e = _cached.cards[i];
                if (e != null && string.Equals(e.cardId, id, StringComparison.Ordinal))
                {
                    _cached.cards.RemoveAt(i);
                    if (SaveCurrent())
                    {
                        return true;
                    }

                    _cached.cards.Insert(i, e);
                    return false;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 判断当前卡牌仓库缓存是否可安全写入。
    /// </summary>
    public static bool CanSaveCurrentData()
    {
        lock (FileLock)
        {
            if (!_canSaveCurrent)
            {
                return false;
            }

            return TryGetCurrentDataPath(out _, out var ownerKey) &&
                   string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// 账号切换时清空卡牌仓库缓存，避免旧账号数据被后续自动保存。
    /// </summary>
    public static void ResetCacheForAccountChange()
    {
        lock (FileLock)
        {
            _cached = new CardCollectionData();
            _cachedOwnerKey = string.Empty;
            _canSaveCurrent = false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 仅在编辑器且列表为空时生成几条测试卡牌，便于查看船坞懒加载。
    /// </summary>
    private static CardCollectionData CreateDefaultIfNeeded()
    {
        var data = new CardCollectionData();
#if UNITY_EDITOR
        for (var i = 1; i <= 30; i++)
        {
            data.cards.Add(new CardEntry { cardId = $"card_{i:D3}", count = 1 });
        }
#endif
        return data;
    }

    /// <summary>
    /// 设置卡牌仓库缓存及其写入状态。
    /// </summary>
    private static void SetCache(CardCollectionData data, string ownerKey, bool canSave)
    {
        _cached = data ?? new CardCollectionData();
        _cachedOwnerKey = ownerKey ?? string.Empty;
        _canSaveCurrent = canSave;
    }

    /// <summary>
    /// 记录读档失败并返回安全默认值，但禁止后续覆盖保存。
    /// </summary>
    private static CardCollectionData MarkLoadFailed(string ownerKey, string message)
    {
        SetCache(new CardCollectionData(), ownerKey, false);
        Debug.LogError($"[CardCollectionStore] {message}");
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
            Debug.LogError("[CardCollectionStore] 卡牌仓库缓存所属账号与当前账号不一致，已跳过保存。");
            return false;
        }

        if (!_canSaveCurrent && File.Exists(path))
        {
            Debug.LogError("[CardCollectionStore] 最近一次卡牌仓库读档失败，已跳过保存以避免覆盖原文件。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取当前账号卡牌仓库目录路径。
    /// </summary>
    private static string GetDataFolderPath()
    {
        return LocalUserDataPaths.TryGetCurrentUserDataFolderPath(out var folder, out _)
            ? folder
            : string.Empty;
    }

    /// <summary>
    /// 获取当前账号卡牌仓库文件路径。
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
    /// 首次进入账号目录时迁移早期重制版共享卡牌仓库文件，避免修复后旧进度不可见。
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
        Debug.Log($"[CardCollectionStore] 已迁移旧共享卡牌仓库文件到当前账号目录：{currentPath}");
    }

    #endregion
}
