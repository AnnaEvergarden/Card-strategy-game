using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Save;
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
            if (!PlayerDataPath.TryGetCurrentUserFilePath(DataFileName, out var encryptedPath, out var userKey))
            {
                MarkCache(CreateDefaultFromCollection(), string.Empty, false);
                return _cached;
            }

            try
            {
                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes != null && bytes.Length > 16)
                    {
                        var json = LocalDataCrypto.DecryptToUtf8(bytes);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var data = JsonUtility.FromJson<FleetData>(json);
                            data ??= new FleetData();
                            Normalize(data);
                            MarkCache(data, userKey, true);
                            return _cached;
                        }
                    }

                    Debug.LogWarning($"Load fleet data failed: 数据文件无效，已阻止自动覆盖 => {encryptedPath}");
                    MarkCache(CreateDefaultFromCollection(), userKey, false);
                    return _cached;
                }

                MarkCache(CreateDefaultFromCollection(), userKey, true);
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load fleet data failed: {ex.Message}");
                MarkCache(CreateDefaultFromCollection(), userKey, false);
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
            if (!PlayerDataPath.TryEnsureCurrentUserFolder(out _, out var userKey) ||
                !PlayerDataPath.TryGetCurrentUserFilePath(DataFileName, out var path, out _))
            {
                Debug.LogWarning("Save fleet data skipped: 当前没有登录账号。");
                return;
            }

            try
            {
                var saveData = data ?? new FleetData();
                Normalize(saveData);
                var json = JsonUtility.ToJson(saveData, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(path, encrypted);
                MarkCache(saveData, userKey, true);
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
        lock (FileLock)
        {
            if (!CanSaveCurrentCache(out var reason))
            {
                Debug.LogWarning($"Save fleet data skipped: {reason}");
                return;
            }

            Save(_cached ?? new FleetData());
        }
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
    /// 更新内存缓存及其账号归属。
    /// </summary>
    private static void MarkCache(FleetData data, string userKey, bool canSave)
    {
        _cached = data ?? new FleetData();
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
