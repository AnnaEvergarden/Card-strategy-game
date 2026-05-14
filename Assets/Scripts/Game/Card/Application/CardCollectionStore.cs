using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Auth;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 卡牌仓库持久化数据：记录玩家拥有的卡牌列表（加密存储）。
/// </summary>
public static class CardCollectionStore
{
    #region Fields

    /// <summary>
    /// 旧版共享卡牌仓库数据文件夹名，当前版本仅用于单账号旧档迁移。
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
    /// 当前缓存是否允许写回磁盘；读档失败后禁止自动保存以保护原文件。
    /// </summary>
    private static bool _canSaveCached = true;

    /// <summary>
    /// 当前缓存所属账号存储键，用于账号切换时重载数据。
    /// </summary>
    private static string _cachedProfileKey = string.Empty;

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
            var profileKey = AccountStore.GetCurrentUserStorageKey();
            try
            {
                var encryptedPath = GetLoadFilePath();

                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes == null || bytes.Length <= 16)
                    {
                        throw new InvalidDataException("卡牌仓库文件为空或长度无效");
                    }

                    var json = LocalDataCrypto.DecryptToUtf8(bytes);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidDataException("卡牌仓库解密结果为空");
                    }

                    var data = JsonUtility.FromJson<CardCollectionData>(json);
                    _cached = data ?? new CardCollectionData();
                    MarkLoadSucceeded(profileKey);
                    return _cached;
                }

                _cached = CreateDefaultIfNeeded();
                MarkLoadSucceeded(profileKey);
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load card collection failed: {ex.Message}");
                _cached = new CardCollectionData();
                MarkLoadFailed(profileKey);
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
            var profileKey = AccountStore.GetCurrentUserStorageKey();
            if (!_canSaveCached && string.Equals(_cachedProfileKey, profileKey, StringComparison.Ordinal))
            {
                Debug.LogWarning("Save card collection skipped because the last load failed; keeping existing file untouched.");
                return;
            }

            try
            {
                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new CardCollectionData();
                _cachedProfileKey = profileKey;
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(GetEncryptedFilePath(), encrypted);
                _canSaveCached = true;
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
            if (!IsCacheForCurrentProfile())
            {
                Load();
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
            if (!_canSaveCached)
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
            if (!_canSaveCached)
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
            if (!_canSaveCached)
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
    /// 标记读档成功，并把缓存绑定到当前账号。
    /// </summary>
    private static void MarkLoadSucceeded(string profileKey)
    {
        _cachedProfileKey = profileKey;
        _canSaveCached = true;
    }

    /// <summary>
    /// 标记读档失败，后续自动保存会跳过以保护原文件。
    /// </summary>
    private static void MarkLoadFailed(string profileKey)
    {
        _cachedProfileKey = profileKey;
        _canSaveCached = false;
    }

    /// <summary>
    /// 判断当前缓存是否属于当前登录账号。
    /// </summary>
    private static bool IsCacheForCurrentProfile()
    {
        return string.Equals(_cachedProfileKey, AccountStore.GetCurrentUserStorageKey(), StringComparison.Ordinal);
    }

    /// <summary>
    /// 获取当前账号的数据目录。
    /// </summary>
    private static string GetDataFolderPath()
    {
        return AccountStore.GetCurrentUserDataFolderPath();
    }

    /// <summary>
    /// 获取旧版共享数据目录，用于单账号项目升级时兼容旧档。
    /// </summary>
    private static string GetLegacyDataFolderPath()
    {
        var dataPath = Application.dataPath;
        var gameRoot = Directory.GetParent(dataPath)?.FullName;
        if (string.IsNullOrEmpty(gameRoot)) gameRoot = dataPath;
        return Path.Combine(gameRoot, DataFolderName);
    }

    /// <summary>
    /// 获取当前账号卡牌仓库数据文件完整路径。
    /// </summary>
    private static string GetEncryptedFilePath() => Path.Combine(GetDataFolderPath(), DataFileName);

    /// <summary>
    /// 获取本次读取应使用的文件路径；当前账号无档且仅有单账号时允许读取旧共享档。
    /// </summary>
    private static string GetLoadFilePath()
    {
        var currentPath = GetEncryptedFilePath();
        if (File.Exists(currentPath))
        {
            return currentPath;
        }

        var legacyPath = Path.Combine(GetLegacyDataFolderPath(), DataFileName);
        return AccountStore.CanMigrateLegacyUserData() && File.Exists(legacyPath) ? legacyPath : currentPath;
    }

    #endregion
}
