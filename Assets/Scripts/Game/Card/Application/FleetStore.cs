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
    /// 当前缓存所属账号目录键。
    /// </summary>
    private static string _cachedOwnerKey = string.Empty;

    /// <summary>
    /// 当前缓存是否允许保存；读档失败或未登录时为 false。
    /// </summary>
    private static bool _canSaveCurrent;

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
            if (!TryGetCurrentDataPath(out var encryptedPath, out var ownerKey))
            {
                _cached = new FleetData();
                _cachedOwnerKey = string.Empty;
                _canSaveCurrent = false;
                Debug.LogWarning("[FleetStore] 当前没有登录账号，跳过编队读取。");
                return _cached;
            }

            try
            {
                MigrateLegacyDataIfNeeded(encryptedPath);
                if (File.Exists(encryptedPath))
                {
                    var bytes = File.ReadAllBytes(encryptedPath);
                    if (bytes != null && bytes.Length > 16)
                    {
                        var json = LocalDataCrypto.DecryptToUtf8(bytes);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var data = JsonUtility.FromJson<FleetData>(json);
                            SetCache(data ?? new FleetData(), ownerKey, true);
                            Normalize(_cached);
                            return _cached;
                        }
                    }

                    return MarkLoadFailed(ownerKey, "编队文件为空或格式异常。");
                }

                SetCache(CreateDefaultFromCollection(), ownerKey, true);
                return _cached;
            }
            catch (Exception ex)
            {
                return MarkLoadFailed(ownerKey, $"读取编队失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存编队数据。
    /// </summary>
    public static bool Save(FleetData data)
    {
        lock (FileLock)
        {
            if (!TryGetCurrentDataPath(out var encryptedPath, out var ownerKey))
            {
                _canSaveCurrent = false;
                Debug.LogWarning("[FleetStore] 当前没有登录账号，跳过编队保存。");
                return false;
            }

            if (!CanWriteForOwner(ownerKey, encryptedPath))
            {
                return false;
            }

            try
            {
                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new FleetData();
                _cachedOwnerKey = ownerKey;
                _canSaveCurrent = true;
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                File.WriteAllBytes(encryptedPath, encrypted);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save fleet data failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 退出前保存当前缓存。
    /// </summary>
    public static bool SaveCurrent()
    {
        if (!_canSaveCurrent)
        {
            Debug.LogError("[FleetStore] 当前编队缓存不可保存，已跳过以避免覆盖原存档。");
            return false;
        }

        return Save(_cached ?? new FleetData());
    }

    /// <summary>
    /// 账号切换时清空编队缓存，避免旧账号数据被后续自动保存。
    /// </summary>
    public static void ResetCacheForAccountChange()
    {
        lock (FileLock)
        {
            _cached = new FleetData();
            _cachedOwnerKey = string.Empty;
            _canSaveCurrent = false;
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
    /// 设置编队缓存及其写入状态。
    /// </summary>
    private static void SetCache(FleetData data, string ownerKey, bool canSave)
    {
        _cached = data ?? new FleetData();
        _cachedOwnerKey = ownerKey ?? string.Empty;
        _canSaveCurrent = canSave;
    }

    /// <summary>
    /// 记录读档失败并返回安全默认值，但禁止后续覆盖保存。
    /// </summary>
    private static FleetData MarkLoadFailed(string ownerKey, string message)
    {
        SetCache(new FleetData(), ownerKey, false);
        Debug.LogError($"[FleetStore] {message}");
        return _cached;
    }

    /// <summary>
    /// 判断当前缓存是否允许写入指定账号目录。
    /// </summary>
    private static bool CanWriteForOwner(string ownerKey, string path)
    {
        if (!string.IsNullOrEmpty(_cachedOwnerKey) &&
            !string.Equals(_cachedOwnerKey, ownerKey, StringComparison.Ordinal))
        {
            Debug.LogError("[FleetStore] 编队缓存所属账号与当前账号不一致，已跳过保存。");
            return false;
        }

        if (!_canSaveCurrent && File.Exists(path))
        {
            Debug.LogError("[FleetStore] 最近一次编队读档失败，已跳过保存以避免覆盖原文件。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取当前账号编队目录路径。
    /// </summary>
    private static string GetDataFolderPath()
    {
        return LocalUserDataPaths.TryGetCurrentUserDataFolderPath(out var folder, out _)
            ? folder
            : string.Empty;
    }

    /// <summary>
    /// 获取当前账号编队文件路径。
    /// </summary>
    private static bool TryGetCurrentDataPath(out string path, out string ownerKey)
    {
        if (!LocalUserDataPaths.TryGetCurrentUserDataFolderPath(out var folder, out ownerKey))
        {
            path = string.Empty;
            return false;
        }

        path = Path.Combine(folder, DataFileName);
        return true;
    }

    /// <summary>
    /// 首次进入账号目录时迁移早期重制版共享编队文件，避免修复后旧进度不可见。
    /// </summary>
    private static void MigrateLegacyDataIfNeeded(string currentPath)
    {
        if (File.Exists(currentPath))
        {
            return;
        }

        var legacyPath = LocalUserDataPaths.GetLegacySharedDataFilePath(DataFileName);
        if (!File.Exists(legacyPath))
        {
            return;
        }

        var folder = Path.GetDirectoryName(currentPath);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.Copy(legacyPath, currentPath, false);
        Debug.Log($"[FleetStore] 已迁移旧共享编队文件到当前账号目录：{currentPath}");
    }

    #endregion
}
