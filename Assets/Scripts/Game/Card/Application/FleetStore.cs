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
            if (!LocalUserDataPaths.TryGetCurrentUserProgressFilePath(DataFileName, out var encryptedPath, out var ownerKey))
            {
                SetCache(CreateDefaultFromCollection(), string.Empty, false);
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

                SetCache(CreateDefaultFromCollection(), ownerKey, !File.Exists(encryptedPath));
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load fleet data failed: {ex.Message}");
                SetCache(CreateDefaultFromCollection(), ownerKey, false);
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
            if (!LocalUserDataPaths.TryGetCurrentUserProgressFilePath(DataFileName, out var encryptedPath, out var ownerKey))
            {
                Debug.LogWarning("Save fleet data skipped: 当前没有登录账号。");
                return;
            }

            try
            {
                LocalUserDataPaths.EnsureParentDirectory(encryptedPath);

                SetCache(data ?? new FleetData(), ownerKey, true);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(encryptedPath, encrypted);
            }
            catch (Exception ex)
            {
                _canSaveCurrent = false;
                Debug.LogError($"Save fleet data failed: {ex.Message}");
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
            Debug.LogWarning("Save fleet data skipped: 缓存不属于当前登录账号。");
            return false;
        }

        Save(_cached ?? new FleetData());
        return _canSaveCurrent && LocalUserDataPaths.IsCurrentUserKey(_cachedOwnerKey);
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
    /// 尝试从指定路径读取编队数据。
    /// </summary>
    private static bool TryLoadFromFile(string encryptedPath, out FleetData data)
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

        data = JsonUtility.FromJson<FleetData>(json) ?? new FleetData();
        Normalize(data);
        return true;
    }

    /// <summary>
    /// 写入缓存状态与归属信息。
    /// </summary>
    private static void SetCache(FleetData data, string ownerKey, bool canSave)
    {
        _cached = data ?? new FleetData();
        Normalize(_cached);
        _cachedOwnerKey = ownerKey ?? string.Empty;
        _canSaveCurrent = canSave;
    }

    #endregion
}
