using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Security;
using UnityEngine;

/// <summary>
/// 编队持久化数据：维护玩家多套卡组，每组最多 6 张卡牌（加密存储）。
/// </summary>
public static class FleetStore
{
    #region Fields

    /// <summary>
    /// 编队数据文件名。
    /// </summary>
    private const string DataFileName = "fleet_data.dat";

    /// <summary>
    /// 单套卡组的最大卡牌数量。
    /// </summary>
    public const int MaxCardsPerFleet = 6;

    /// <summary>
    /// 内存缓存。
    /// </summary>
    private static FleetData _cached = new();

    /// <summary>
    /// 当前缓存所属账号；用于避免账号切换后保存到错误目录。
    /// </summary>
    private static string _cachedUser = string.Empty;

    /// <summary>
    /// 文件读写锁。
    /// </summary>
    private static readonly object FileLock = new();

    #endregion

    #region Nested Models

    /// <summary>
    /// 单套编队数据。
    /// </summary>
    [Serializable]
    public sealed class FleetGroupData
    {
        /// <summary>
        /// 卡组显示名称。
        /// </summary>
        public string groupName = "卡组";

        /// <summary>
        /// 卡牌配置 id 列表。
        /// </summary>
        public List<string> cardIds = new();
    }

    /// <summary>
    /// 编队数据快照。
    /// </summary>
    [Serializable]
    public sealed class FleetData
    {
        /// <summary>
        /// 多套卡组。
        /// </summary>
        public List<FleetGroupData> groups = new();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 读取编队数据；若文件不存在则基于玩家卡牌仓库自动生成默认第一套卡组。
    /// </summary>
    public static FleetData Load()
    {
        lock (FileLock)
        {
            try
            {
                if (!TryGetActiveFilePath(out var encryptedPath, out var currentUser))
                {
                    _cachedUser = string.Empty;
                    _cached = new FleetData();
                    Normalize(_cached);
                    return _cached;
                }

                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes != null && bytes.Length > 16)
                    {
                        var json = LocalDataCrypto.DecryptToUtf8(bytes);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var data = JsonUtility.FromJson<FleetData>(json);
                            _cached = data ?? new FleetData();
                            Normalize(_cached);
                            _cachedUser = currentUser;
                            return _cached;
                        }
                    }
                }

                if (TryLoadLegacyData(out var legacyData))
                {
                    _cached = legacyData;
                    _cachedUser = currentUser;
                    Save(_cached);
                    return _cached;
                }

                _cached = CreateDefaultFromCollection();
                _cachedUser = currentUser;
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load fleet data failed: {ex.Message}");
                _cached = CreateDefaultFromCollection();
                _cachedUser = string.Empty;
                return _cached;
            }
        }
    }

    /// <summary>
    /// 保存编队数据。
    /// </summary>
    public static void Save(FleetData data)
    {
        lock (FileLock)
        {
            try
            {
                if (!TryGetActiveFilePath(out var path, out var currentUser))
                {
                    Debug.LogWarning("Save fleet data skipped: no logged-in user.");
                    return;
                }

                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new FleetData();
                _cachedUser = currentUser;
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save fleet data failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出前保存当前缓存。
    /// </summary>
    public static void SaveCurrent()
    {
        var currentUser = UserDataPathService.GetCurrentUser();
        if (string.IsNullOrWhiteSpace(currentUser))
        {
            Debug.Log("Save fleet data skipped: no logged-in user.");
            return;
        }

        if (!string.Equals(_cachedUser, currentUser, StringComparison.Ordinal))
        {
            Debug.Log("Save fleet data skipped: cached data belongs to another user or has not been loaded.");
            return;
        }

        Save(_cached ?? new FleetData());
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 规范化数据：保证至少一套卡组，且每套不超过 6 张。
    /// </summary>
    private static void Normalize(FleetData data)
    {
        data.groups ??= new List<FleetGroupData>();
        if (data.groups.Count == 0)
        {
            data.groups.Add(new FleetGroupData { groupName = "卡组 1" });
        }

        for (var i = 0; i < data.groups.Count; i++)
        {
            var g = data.groups[i] ?? new FleetGroupData();
            if (data.groups[i] == null)
            {
                data.groups[i] = g;
            }

            if (string.IsNullOrWhiteSpace(g.groupName))
            {
                g.groupName = $"卡组 {i + 1}";
            }

            g.cardIds ??= new List<string>();
            if (g.cardIds.Count > MaxCardsPerFleet)
            {
                g.cardIds.RemoveRange(MaxCardsPerFleet, g.cardIds.Count - MaxCardsPerFleet);
            }
        }
    }

    /// <summary>
    /// 首次无编队文件时，从玩家卡牌仓库抽取前 6 张生成默认第一套卡组。
    /// </summary>
    private static FleetData CreateDefaultFromCollection()
    {
        var data = new FleetData();
        var group = new FleetGroupData { groupName = "卡组 1" };
        data.groups.Add(group);

        var collection = CardCollectionStore.Load();
        var cards = collection != null ? collection.cards : null;
        if (cards == null)
        {
            return data;
        }

        for (var i = 0; i < cards.Count && group.cardIds.Count < MaxCardsPerFleet; i++)
        {
            var entry = cards[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.cardId))
            {
                continue;
            }

            group.cardIds.Add(entry.cardId.Trim());
        }

        return data;
    }

    /// <summary>
    /// 尝试获取当前账号的编队文件路径。
    /// </summary>
    private static bool TryGetActiveFilePath(out string filePath, out string currentUser)
    {
        currentUser = UserDataPathService.GetCurrentUser();
        if (string.IsNullOrWhiteSpace(currentUser))
        {
            filePath = string.Empty;
            return false;
        }

        return UserDataPathService.TryGetCurrentUserDataFilePath(DataFileName, out filePath);
    }

    /// <summary>
    /// 尝试读取旧版共享编队文件，用于首次迁移到当前账号目录。
    /// </summary>
    private static bool TryLoadLegacyData(out FleetData data)
    {
        data = null;
        var legacyPath = UserDataPathService.GetLegacySharedDataFilePath(DataFileName);
        if (!File.Exists(legacyPath))
        {
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(legacyPath);
            if (bytes == null || bytes.Length <= 16)
            {
                return false;
            }

            var json = LocalDataCrypto.DecryptToUtf8(bytes);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            data = JsonUtility.FromJson<FleetData>(json) ?? new FleetData();
            Normalize(data);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Load legacy fleet data failed: {ex.Message}");
            return false;
        }
    }

    #endregion
}
