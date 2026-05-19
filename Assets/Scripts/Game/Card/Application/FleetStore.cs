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
    /// 上一次读取是否失败；失败时禁止保存默认编队覆盖原文件。
    /// </summary>
    private static bool _saveBlocked;

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
    /// 读取当前账号编队数据；若文件不存在则基于玩家卡牌仓库自动生成默认第一套卡组。
    /// </summary>
    public static FleetData Load()
    {
        lock (FileLock)
        {
            try
            {
                if (!TryGetEncryptedFilePath(out var encryptedPath))
                {
                    return MarkLoadFailed(new FleetData(), "Load fleet data skipped: 当前没有登录账号。");
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
                            _saveBlocked = false;
                            return _cached;
                        }
                    }

                    return MarkLoadFailed(CreateDefaultFromCollection(), "Load fleet data failed: 编队存档文件无效或解密为空。");
                }

                _cached = CreateDefaultFromCollection();
                _saveBlocked = false;
                return _cached;
            }
            catch (Exception ex)
            {
                return MarkLoadFailed(CreateDefaultFromCollection(), $"Load fleet data failed: {ex.Message}");
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
                if (_saveBlocked)
                {
                    Debug.LogError("Save fleet data skipped: 上一次读取编队存档失败，禁止覆盖原文件。");
                    return;
                }

                if (!TryGetDataFolderPath(out var folder))
                {
                    Debug.LogWarning("Save fleet data skipped: 当前没有登录账号。");
                    return;
                }

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new FleetData();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                if (TryGetEncryptedFilePath(out var filePath))
                {
                    File.WriteAllBytes(filePath, encrypted);
                }
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
        Save(_cached ?? new FleetData());
    }

    /// <summary>
    /// 清理运行时缓存；账号切换后必须先清理，避免旧账号编队写入新账号目录。
    /// </summary>
    public static void ClearCache()
    {
        lock (FileLock)
        {
            _cached = new FleetData();
            _saveBlocked = true;
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
    /// 标记读取失败并返回安全默认值；后续保存会被阻止，避免数据丢失。
    /// </summary>
    /// <param name="fallback">返回给调用方的默认数据。</param>
    /// <param name="message">失败原因。</param>
    private static FleetData MarkLoadFailed(FleetData fallback, string message)
    {
        Debug.LogWarning(message);
        _cached = fallback ?? new FleetData();
        Normalize(_cached);
        _saveBlocked = true;
        return _cached;
    }

    /// <summary>
    /// 尝试获取当前账号的编队数据目录。
    /// </summary>
    private static bool TryGetDataFolderPath(out string folderPath)
    {
        return LocalSavePath.TryGetCurrentAccountDataFolderPath(out folderPath);
    }

    /// <summary>
    /// 尝试获取当前账号的编队数据文件路径。
    /// </summary>
    private static bool TryGetEncryptedFilePath(out string filePath)
    {
        return LocalSavePath.TryGetCurrentAccountDataFilePath(DataFileName, out filePath);
    }

    #endregion
}
