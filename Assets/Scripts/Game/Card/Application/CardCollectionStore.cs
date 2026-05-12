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
            if (!PlayerDataPath.TryGetCurrentUserFilePath(DataFileName, out var encryptedPath, out var userKey))
            {
                MarkCache(CreateDefaultIfNeeded(), string.Empty, false);
                return _cached;
            }

            try
            {
                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        Debug.LogWarning($"Load card collection failed: 数据文件无效，已阻止自动覆盖 => {encryptedPath}");
                        MarkCache(CreateDefaultIfNeeded(), userKey, false);
                        return _cached;
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Debug.LogWarning($"Load card collection failed: 数据内容为空，已阻止自动覆盖 => {encryptedPath}");
                        MarkCache(CreateDefaultIfNeeded(), userKey, false);
                        return _cached;
                    }

                    var data = JsonUtility.FromJson<CardCollectionData>(json);
                    MarkCache(data ?? new CardCollectionData(), userKey, true);
                    return _cached;
                }

                MarkCache(CreateDefaultIfNeeded(), userKey, true);
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load card collection failed: {ex.Message}");
                MarkCache(new CardCollectionData(), userKey, false);
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
            if (!PlayerDataPath.TryEnsureCurrentUserFolder(out _, out var userKey) ||
                !PlayerDataPath.TryGetCurrentUserFilePath(DataFileName, out var path, out _))
            {
                Debug.LogWarning("Save card collection skipped: 当前没有登录账号。");
                return;
            }

            try
            {
                var saveData = data ?? new CardCollectionData();
                var json = JsonUtility.ToJson(saveData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
                MarkCache(saveData, userKey, true);
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
        lock (FileLock)
        {
            if (!CanSaveCurrentCache(out var reason))
            {
                Debug.LogWarning($"Save card collection skipped: {reason}");
                return;
            }

            Save(_cached ?? new CardCollectionData());
        }
    }

    /// <summary>
    /// 向舰娘仓库增加指定 cardId 的数量；已存在则累加 count，否则新增一条。
    /// </summary>
    public static void AddCards(string cardId, int count = 1)
    {
        if (string.IsNullOrWhiteSpace(cardId) || count <= 0)
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

            _cached.cards ??= new List<CardEntry>();
            var id = cardId.Trim();
            for (var i = 0; i < _cached.cards.Count; i++)
            {
                var e = _cached.cards[i];
                if (e != null && string.Equals(e.cardId, id, StringComparison.Ordinal))
                {
                    e.count += count;
                    SaveCurrent();
                    return;
                }
            }

            _cached.cards.Add(new CardEntry { cardId = id, count = count });
            SaveCurrent();
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
            if (!_cachedCanSave)
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
            if (!_cachedCanSave)
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
    /// 更新内存缓存及其账号归属。
    /// </summary>
    private static void MarkCache(CardCollectionData data, string userKey, bool canSave)
    {
        _cached = data ?? new CardCollectionData();
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
