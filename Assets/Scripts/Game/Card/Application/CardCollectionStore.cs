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
    /// 加密后的卡牌仓库文件名。
    /// </summary>
    private const string DataFileName = "card_collection.dat";

    /// <summary>
    /// 内存缓存，供退出保存使用。
    /// </summary>
    private static CardCollectionData _cached = new();

    /// <summary>
    /// 上一次读取是否失败；失败时禁止保存默认卡牌仓库覆盖原文件。
    /// </summary>
    private static bool _saveBlocked;

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
    /// 读取当前账号卡牌仓库；文件不存在则返回空列表并可写入默认测试数据（编辑器下）。
    /// </summary>
    public static CardCollectionData Load()
    {
        lock (FileLock)
        {
            try
            {
                if (!TryGetEncryptedFilePath(out var encryptedPath))
                {
                    return MarkLoadFailed(new CardCollectionData(), "Load card collection skipped: 当前没有登录账号。");
                }

                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        return MarkLoadFailed(new CardCollectionData(), "Load card collection failed: 卡牌仓库存档文件过短或为空。");
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return MarkLoadFailed(new CardCollectionData(), "Load card collection failed: 卡牌仓库存档解密结果为空。");
                    }

                    var data = JsonUtility.FromJson<CardCollectionData>(json);
                    _cached = data ?? new CardCollectionData();
                    _cached.cards ??= new List<CardEntry>();
                    _saveBlocked = false;
                    return _cached;
                }

                _cached = CreateDefaultIfNeeded();
                _saveBlocked = false;
                return _cached;
            }
            catch (Exception ex)
            {
                return MarkLoadFailed(new CardCollectionData(), $"Load card collection failed: {ex.Message}");
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
            try
            {
                if (_saveBlocked)
                {
                    Debug.LogError("Save card collection skipped: 上一次读取卡牌仓库存档失败，禁止覆盖原文件。");
                    return;
                }

                if (!TryGetDataFolderPath(out var folder))
                {
                    Debug.LogWarning("Save card collection skipped: 当前没有登录账号。");
                    return;
                }

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new CardCollectionData();
                _cached.cards ??= new List<CardEntry>();
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                if (TryGetEncryptedFilePath(out var filePath))
                {
                    File.WriteAllBytes(filePath, encrypted);
                }
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
        Save(_cached ?? new CardCollectionData());
    }

    /// <summary>
    /// 清理运行时缓存；账号切换后必须先清理，避免旧账号卡牌写入新账号目录。
    /// </summary>
    public static void ClearCache()
    {
        lock (FileLock)
        {
            _cached = new CardCollectionData();
            _saveBlocked = true;
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
            if (_saveBlocked)
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
    /// 消耗舰娘仓库中指定 cardId 的数量；数量不足、不存在或读档失败时返回 false，不修改数据。
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
            if (_saveBlocked)
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
            if (_saveBlocked)
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
    /// 标记读取失败并返回安全默认值；后续保存会被阻止，避免数据丢失。
    /// </summary>
    /// <param name="fallback">返回给调用方的默认数据。</param>
    /// <param name="message">失败原因。</param>
    private static CardCollectionData MarkLoadFailed(CardCollectionData fallback, string message)
    {
        Debug.LogWarning(message);
        _cached = fallback;
        _cached.cards ??= new List<CardEntry>();
        _saveBlocked = true;
        return _cached;
    }

    /// <summary>
    /// 尝试获取当前账号的卡牌仓库目录。
    /// </summary>
    private static bool TryGetDataFolderPath(out string folderPath)
    {
        return LocalSavePath.TryGetCurrentAccountDataFolderPath(out folderPath);
    }

    /// <summary>
    /// 尝试获取当前账号的卡牌仓库文件路径。
    /// </summary>
    private static bool TryGetEncryptedFilePath(out string filePath)
    {
        return LocalSavePath.TryGetCurrentAccountDataFilePath(DataFileName, out filePath);
    }

    #endregion
}
