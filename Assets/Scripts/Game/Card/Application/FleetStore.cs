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
    /// 数据目录名（与账号、卡牌仓库一致）。
    /// </summary>
    private const string DataFolderName = "UserData";

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
    /// 是否已经从磁盘加载过或通过保存建立过可信缓存。
    /// </summary>
    private static bool _hasCachedData;

    /// <summary>
    /// 最近一次加载是否失败；失败后禁止自动保存覆盖原文件。
    /// </summary>
    private static bool _saveBlockedByLoadFailure;

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
                var encryptedPath = GetEncryptedFilePath();
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
                            MarkCacheReadyForSave();
                            return _cached;
                        }
                    }

                    Debug.LogWarning($"Load fleet data failed: encrypted file is invalid => {encryptedPath}");
                    _cached = CreateDefaultFromCollection();
                    MarkLoadFailure();
                    return _cached;
                }

                _cached = CreateDefaultFromCollection();
                MarkCacheReadyForSave();
                return _cached;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Load fleet data failed: {ex.Message}");
                _cached = CreateDefaultFromCollection();
                MarkLoadFailure();
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
                if (_saveBlockedByLoadFailure)
                {
                    Debug.LogError("Save fleet data blocked: last load failed, refusing to overwrite existing data.");
                    return;
                }

                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new FleetData();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(GetEncryptedFilePath(), encrypted);
                MarkCacheReadyForSave();
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
            if (!_hasCachedData)
            {
                // 自动保存前先加载磁盘数据，避免用静态初始空对象覆盖真实编队。
                Load();
            }

            if (_saveBlockedByLoadFailure)
            {
                Debug.LogError("Save current fleet data skipped: cached data came from a failed load.");
                return;
            }
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
    /// 标记当前缓存已经可信，可被后续自动保存写回。
    /// </summary>
    private static void MarkCacheReadyForSave()
    {
        _hasCachedData = true;
        _saveBlockedByLoadFailure = false;
    }

    /// <summary>
    /// 标记加载失败，后续自动保存必须跳过以保护原始文件。
    /// </summary>
    private static void MarkLoadFailure()
    {
        _hasCachedData = true;
        _saveBlockedByLoadFailure = true;
    }

    /// <summary>
    /// 获取编队数据目录（游戏根目录/UserData）。
    /// </summary>
    private static string GetDataFolderPath()
    {
        var dataPath = Application.dataPath;
        var gameRoot = Directory.GetParent(dataPath)?.FullName;
        if (string.IsNullOrEmpty(gameRoot))
        {
            gameRoot = dataPath;
        }

        return Path.Combine(gameRoot, DataFolderName);
    }

    /// <summary>
    /// 获取编队加密文件完整路径。
    /// </summary>
    private static string GetEncryptedFilePath() => Path.Combine(GetDataFolderPath(), DataFileName);

    #endregion
}
