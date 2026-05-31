using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 卡牌仓库持久化数据：记录玩家拥有的卡牌列表（加密存储）。
/// </summary>
public static class CardCollectionStore
{
    #region Fields

    /// <summary>
    /// 数据目录名（与账号、道具一致，位于游戏根目录下）。
    /// </summary>
    private const string DataFolderName = "UserData";

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
    /// 读取卡牌仓库；无存档时返回空列表（不再注入假 card_001 数据，避免覆盖真实存档观感）。
    /// </summary>
    /// <param name="forceReload">为 true 时忽略内存缓存，强制从磁盘重读。</param>
    public static CardCollectionData Load(bool forceReload = false)
    {
        lock (FileLock)
        {
            if (!forceReload && _cached?.cards != null && _cached.cards.Count > 0)
            {
                return _cached;
            }

            try
            {
                var encryptedPath = GetEncryptedFilePath();

                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        Debug.LogWarning("CardCollectionStore: 存档文件过短或损坏，已按空仓库处理。");
                        _cached = new CardCollectionData();
                        return _cached;
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Debug.LogWarning("CardCollectionStore: 解密后内容为空，已按空仓库处理。");
                        _cached = new CardCollectionData();
                        return _cached;
                    }

                    var data = JsonUtility.FromJson<CardCollectionData>(json);
                    _cached = data ?? new CardCollectionData();
                    _cached.cards ??= new List<CardEntry>();
                    return _cached;
                }

                _cached = new CardCollectionData();
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load card collection failed: {ex.Message}");
                if (_cached == null || _cached.cards == null || _cached.cards.Count == 0)
                {
                    _cached = new CardCollectionData();
                }

                return _cached;
            }
        }
    }

    /// <summary>
    /// 清空内存缓存（下次 <see cref="Load"/> 将从磁盘读取）。
    /// </summary>
    public static void InvalidateCache()
    {
        lock (FileLock)
        {
            _cached = new CardCollectionData();
        }
    }

    /// <summary>
    /// 保存卡牌仓库。
    /// </summary>
    public static void Save(CardCollectionData data)
    {
        lock (FileLock)
        {
            try
            {
                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new CardCollectionData();
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                StoreUtil.AtomicWrite(GetEncryptedFilePath(), encrypted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save card collection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出前保存当前缓存；若内存为空但磁盘已有存档，则不覆盖（防止 Play 结束域重载后空缓存写盘）。
    /// </summary>
    public static void SaveCurrent()
    {
        lock (FileLock)
        {
            if (ShouldSkipSaveEmptyCache())
            {
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
    /// 内存仓库为空且磁盘上已有有效存档时，跳过保存以免覆盖。
    /// </summary>
    private static bool ShouldSkipSaveEmptyCache()
    {
        if (_cached?.cards != null && _cached.cards.Count > 0)
        {
            return false;
        }

        return StoreUtil.HasValidDataOnDisk(GetEncryptedFilePath());
    }

    private static string GetDataFolderPath()
    {
        var dataPath = Application.dataPath;
        var gameRoot = Directory.GetParent(dataPath)?.FullName;
        if (string.IsNullOrEmpty(gameRoot)) gameRoot = dataPath;
        return Path.Combine(gameRoot, DataFolderName);
    }

    private static string GetEncryptedFilePath() => Path.Combine(GetDataFolderPath(), DataFileName);

    #endregion
}
