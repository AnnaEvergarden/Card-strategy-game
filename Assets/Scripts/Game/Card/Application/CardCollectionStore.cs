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
    /// 当前缓存所属的账号目录键，用于账号切换时阻止串档保存。
    /// </summary>
    private static string _cachedOwnerKey = string.Empty;

    /// <summary>
    /// 当前缓存是否允许自动保存；读档失败时保持 false。
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
            if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out var encryptedPath))
            {
                ResetCacheForOwner(string.Empty);
                Debug.LogWarning("[CardCollectionStore] 当前没有登录账号，跳过卡牌仓库读档。");
                return _cached;
            }

            ResetCacheForOwner(ownerKey);
            try
            {
                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        _cached = CreateDefaultIfNeeded();
                        _canSaveCurrent = false;
                        return _cached;
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _cached = CreateDefaultIfNeeded();
                        _canSaveCurrent = false;
                        return _cached;
                    }

                    var data = JsonUtility.FromJson<CardCollectionData>(json);
                    _cached = data ?? new CardCollectionData();
                    _canSaveCurrent = true;
                    return _cached;
                }

                _cached = CreateDefaultIfNeeded();
                _canSaveCurrent = true;
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load card collection failed: {ex.Message}");
                _cached = new CardCollectionData();
                _canSaveCurrent = false;
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
            if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out var encryptedPath))
            {
                Debug.LogWarning("[CardCollectionStore] 当前没有登录账号，跳过卡牌仓库保存。");
                return;
            }

            ResetCacheForOwner(ownerKey);
            try
            {
                LocalUserDataPaths.EnsureParentDirectory(encryptedPath);
                _cached = data ?? new CardCollectionData();
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(encryptedPath, encrypted);
                _canSaveCurrent = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save card collection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出前保存当前缓存。
    /// </summary>
    public static void SaveCurrent()
    {
        if (!LocalUserDataPaths.TryGetCurrentUserDataFilePath(DataFileName, out var ownerKey, out _))
        {
            Debug.LogWarning("[CardCollectionStore] 当前没有登录账号，跳过卡牌仓库自动保存。");
            return;
        }

        if (!string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal) || !_canSaveCurrent)
        {
            Debug.LogWarning("[CardCollectionStore] 卡牌仓库缓存未成功加载或账号已切换，跳过自动保存以避免覆盖存档。");
            return;
        }

        Save(_cached ?? new CardCollectionData());
    }

    /// <summary>
    /// 向舰娘仓库增加指定 cardId 的数量；已存在则累加 count，否则新增一条。
    /// </summary>
    /// <returns>卡牌仓库可写且加入成功时为 true。</returns>
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
                Debug.LogWarning("[CardCollectionStore] 卡牌仓库读档失败，拒绝增加卡牌以避免覆盖原存档。");
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
                    SaveCurrent();
                    return true;
                }
            }

            _cached.cards.Add(new CardEntry { cardId = id, count = count });
            SaveCurrent();
            return true;
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
                Debug.LogWarning("[CardCollectionStore] 卡牌仓库读档失败，拒绝消耗卡牌以避免产生无法保存的状态。");
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

                SaveCurrent();
                return true;
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
                Debug.LogWarning("[CardCollectionStore] 卡牌仓库读档失败，拒绝删除卡牌以避免覆盖原存档。");
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
    /// 账号切换时重置卡牌仓库缓存状态，避免旧账号数据写入新账号目录。
    /// </summary>
    private static void ResetCacheForOwner(string ownerKey)
    {
        ownerKey ??= string.Empty;
        if (string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal))
        {
            return;
        }

        _cachedOwnerKey = ownerKey;
        _cached = CreateDefaultIfNeeded();
        _canSaveCurrent = false;
    }

    #endregion
}
