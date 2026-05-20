using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Auth;
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
            var user = AccountStore.GetCurrentUser();
            if (!LocalUserDataPaths.TryGetUserDataFilePath(user, DataFileName, out var path))
            {
                _cached = CreateDefaultIfNeeded();
                _cachedOwner = string.Empty;
                _canSaveCurrent = false;
                return _cached;
            }

            _cachedOwner = user.Trim();
            var fileExists = File.Exists(path);
            if (TryLoadFromFile(path, out var loaded))
            {
                _cached = loaded;
                _canSaveCurrent = true;
                return _cached;
            }

            if (!fileExists && TryLoadFromFile(LocalUserDataPaths.GetLegacySharedDataFilePath(DataFileName), out loaded))
            {
                _cached = loaded;
                _canSaveCurrent = true;
                Save(_cached);
                return _cached;
            }

            _cached = fileExists ? new CardCollectionData() : CreateDefaultIfNeeded();
            _canSaveCurrent = !fileExists;
            if (fileExists)
            {
                Debug.LogWarning("Load card collection failed: 已阻止自动保存覆盖现有卡牌仓库文件。");
            }

            return _cached;
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
                var user = AccountStore.GetCurrentUser();
                if (!LocalUserDataPaths.TryGetUserDataFilePath(user, DataFileName, out var filePath))
                {
                    Debug.LogWarning("Save card collection skipped: 当前没有登录账号。");
                    _canSaveCurrent = false;
                    return;
                }

                var folder = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new CardCollectionData();
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(filePath, encrypted);
                _cachedOwner = user.Trim();
                _canSaveCurrent = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save card collection failed: {ex.Message}");
                _canSaveCurrent = false;
            }
        }
    }

    /// <summary>
    /// 退出前保存当前缓存。
    /// </summary>
    public static void SaveCurrent()
    {
        if (!CanSaveCurrentForActiveUser())
        {
            return;
        }

        Save(_cached ?? new CardCollectionData());
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
    /// 尝试从指定加密文件读取卡牌仓库数据。
    /// </summary>
    private static bool TryLoadFromFile(string filePath, out CardCollectionData data)
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

            data = JsonUtility.FromJson<CardCollectionData>(json);
            return data != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Load card collection failed: {ex.Message}");
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
