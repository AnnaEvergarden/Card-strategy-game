using System;
using System.Collections.Generic;
using System.IO;
using Game.Common.Auth;
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
            var user = AccountStore.GetCurrentUser();
            if (!LocalUserDataPaths.TryGetUserDataFilePath(user, DataFileName, out var path))
            {
                _cached = CreateDefaultFromCollection();
                _cachedOwner = string.Empty;
                _canSaveCurrent = false;
                return _cached;
            }

            _cachedOwner = user.Trim();
            var fileExists = File.Exists(path);
            if (TryLoadFromFile(path, out var loaded))
            {
                _cached = loaded;
                Normalize(_cached);
                _canSaveCurrent = true;
                return _cached;
            }

            if (!fileExists && TryLoadFromFile(LocalUserDataPaths.GetLegacySharedDataFilePath(DataFileName), out loaded))
            {
                _cached = loaded;
                Normalize(_cached);
                _canSaveCurrent = true;
                Save(_cached);
                return _cached;
            }

            _cached = fileExists ? new FleetData() : CreateDefaultFromCollection();
            Normalize(_cached);
            _canSaveCurrent = !fileExists;
            if (fileExists)
            {
                Debug.LogWarning("Load fleet data failed: 已阻止自动保存覆盖现有编队文件。");
            }

            return _cached;
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
                var user = AccountStore.GetCurrentUser();
                if (!LocalUserDataPaths.TryGetUserDataFilePath(user, DataFileName, out var filePath))
                {
                    Debug.LogWarning("Save fleet data skipped: 当前没有登录账号。");
                    _canSaveCurrent = false;
                    return;
                }

                var folder = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new FleetData();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(filePath, encrypted);
                _cachedOwner = user.Trim();
                _canSaveCurrent = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save fleet data failed: {ex.Message}");
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
    /// 尝试从指定加密文件读取编队数据。
    /// </summary>
    private static bool TryLoadFromFile(string filePath, out FleetData data)
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

            data = JsonUtility.FromJson<FleetData>(json);
            return data != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Load fleet data failed: {ex.Message}");
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
