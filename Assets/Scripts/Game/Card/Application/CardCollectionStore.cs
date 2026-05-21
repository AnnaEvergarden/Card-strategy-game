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
            if (!LocalUserDataPaths.TryGetCurrentUserProgressFilePath(DataFileName, out var encryptedPath, out var ownerKey))
            {
                SetCache(CreateDefaultIfNeeded(), string.Empty, false);
                return _cached;
            }

            try
            {
                if (TryLoadFromFile(encryptedPath, out var data))
                {
                    SetCache(data, ownerKey, true);
                    return _cached;
                }

                if (!File.Exists(encryptedPath) &&
                    LocalUserDataPaths.TryGetLegacySharedFilePath(DataFileName, out var legacyPath) &&
                    TryLoadFromFile(legacyPath, out data))
                {
                    SetCache(data, ownerKey, true);
                    Save(_cached);
                    return _cached;
                }

                SetCache(CreateDefaultIfNeeded(), ownerKey, !File.Exists(encryptedPath));
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load card collection failed: {ex.Message}");
                SetCache(new CardCollectionData(), ownerKey, false);
                return _cached;
            }
        }
    }

    /// <summary>
    /// 保存卡牌仓库。
    /// </summary>
    public static void Save(CardCollectionData data)
    {
        lock (FileLock)
        {
            if (!LocalUserDataPaths.TryGetCurrentUserProgressFilePath(DataFileName, out var encryptedPath, out var ownerKey))
            {
                Debug.LogWarning("Save card collection skipped: 当前没有登录账号。");
                return;
            }

            try
            {
                LocalUserDataPaths.EnsureParentDirectory(encryptedPath);

                SetCache(data ?? new CardCollectionData(), ownerKey, true);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(encryptedPath, encrypted);
            }
            catch (Exception ex)
            {
                _canSaveCurrent = false;
                Debug.LogError($"Save card collection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出前保存当前缓存。
    /// </summary>
    /// <returns>缓存成功写入当前账号进度时返回 true。</returns>
    public static bool SaveCurrent()
    {
        if (!_canSaveCurrent || !LocalUserDataPaths.IsCurrentUserKey(_cachedOwnerKey))
        {
            Debug.LogWarning("Save card collection skipped: 缓存不属于当前登录账号。");
            return false;
        }

        Save(_cached ?? new CardCollectionData());
        return _canSaveCurrent && LocalUserDataPaths.IsCurrentUserKey(_cachedOwnerKey);
    }

    /// <summary>
    /// 向舰娘仓库增加指定 cardId 的数量；已存在则累加 count，否则新增一条。
    /// </summary>
    /// <returns>成功写入当前账号仓库时返回 true。</returns>
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
                    return SaveCurrent();
                }
            }

            _cached.cards.Add(new CardEntry { cardId = id, count = count });
            return SaveCurrent();
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
                if (e.count <= 0)
                {
                    _cached.cards.RemoveAt(i);
                }

                return SaveCurrent();
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
                    return SaveCurrent();
                }
            }

            return false;
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
    /// 尝试从指定路径读取卡牌仓库数据。
    /// </summary>
    private static bool TryLoadFromFile(string encryptedPath, out CardCollectionData data)
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

        data = JsonUtility.FromJson<CardCollectionData>(json) ?? new CardCollectionData();
        data.cards ??= new List<CardEntry>();
        return true;
    }

    /// <summary>
    /// 写入缓存状态与归属信息。
    /// </summary>
    private static void SetCache(CardCollectionData data, string ownerKey, bool canSave)
    {
        _cached = data ?? new CardCollectionData();
        _cached.cards ??= new List<CardEntry>();
        _cachedOwnerKey = ownerKey ?? string.Empty;
        _canSaveCurrent = canSave;
    }

    #endregion
}
