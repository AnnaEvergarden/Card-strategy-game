using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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
    /// 玩家可拥有的编队（卡组）套数上限（后续若调整仅改此常量）。
    /// </summary>
    public const int MaxFleetGroups = 6;

    /// <summary>
    /// 进入战斗时单套卡组最少舰娘数量（与 <see cref="TryValidateBattleFleetGroup"/> 一致）。
    /// </summary>
    public const int MinCardsPerBattleFleet = 1;

    /// <summary>
    /// 单局出战舰娘最少数量（上阵挑选面板确认逻辑一致）。
    /// </summary>
    public const int MinActivesPerBattleFleet = 1;

    /// <summary>
    /// 单局出战舰娘最多数量（上阵挑选面板确认逻辑一致）。
    /// </summary>
    public const int MaxActivesPerBattleFleet = 2;

    /// <summary>
    /// 内存缓存。
    /// </summary>
    private static FleetData _cached = new();

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
                            return _cached;
                        }
                    }
                }

                _cached = CreateDefaultFromCollection();
                return _cached;
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"Load fleet data failed (IO): {ex.Message}");
                _cached = CreateDefaultFromCollection();
                return _cached;
            }
            catch (CryptographicException ex)
            {
                Debug.LogWarning($"Load fleet data failed (crypto): {ex.Message}");
                _cached = CreateDefaultFromCollection();
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
                var folder = GetDataFolderPath();
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _cached = data ?? new FleetData();
                Normalize(_cached);
                var json = JsonUtility.ToJson(_cached, true);
                var encrypted = LocalDataCrypto.EncryptUtf8(json);
                StoreUtil.AtomicWrite(GetEncryptedFilePath(), encrypted);
            }
            catch (IOException ex)
            {
                Debug.LogError($"Save fleet data failed (IO): {ex.Message}");
            }
            catch (CryptographicException ex)
            {
                Debug.LogError($"Save fleet data failed (crypto): {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 退出前保存当前缓存；若内存为空但磁盘已有有效存档，则不覆盖（防止 Play 结束域重载后空缓存写盘）。
    /// </summary>
    public static void SaveCurrent()
    {
        lock (FileLock)
        {
            if (ShouldSkipSaveEmptyCache())
            {
                return;
            }

            Save(_cached ?? new FleetData());
        }
    }
    #endregion

    #region Validation

    /// <summary>
    /// 校验单套卡组是否满足进入战斗的舰娘数量：至少 <see cref="MinCardsPerBattleFleet"/> 艘、至多 <see cref="MaxCardsPerFleet"/> 艘，非空 cardId 互不重复。
    /// </summary>
    /// <param name="group">卡组数据。</param>
    /// <param name="failureReason">失败原因（中文短句，成功时为 null）。</param>
    /// <returns>是否通过。</returns>
    public static bool TryValidateBattleFleetGroup(FleetGroupData group, out string failureReason)
    {
        failureReason = null;
        if (group == null)
        {
            failureReason = "卡组数据无效";
            return false;
        }

        group.cardIds ??= new List<string>();
        var seen = new HashSet<string>();
        var count = 0;
        for (var i = 0; i < group.cardIds.Count; i++)
        {
            var raw = group.cardIds[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var id = raw.Trim();
            if (!seen.Add(id))
            {
                failureReason = "卡组中存在重复舰娘";
                return false;
            }

            count++;
        }

        if (count < MinCardsPerBattleFleet)
        {
            failureReason = $"编队至少需要 {MinCardsPerBattleFleet} 艘舰娘（当前 {count} 艘）";
            return false;
        }

        if (count > MaxCardsPerFleet)
        {
            failureReason = $"编队超过 {MaxCardsPerFleet} 艘";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 校验本局出战舰娘列表：至少 <see cref="MinActivesPerBattleFleet"/>、至多 <see cref="MaxActivesPerBattleFleet"/> 名，且均来自所选卡组、互不重复。
    /// </summary>
    /// <param name="actives">出战 cardId 列表（上阵顺序）。</param>
    /// <param name="deckCardIds">当前所选卡组 cardId 列表。</param>
    /// <param name="failureReason">失败原因（中文短句，成功时为 null）。</param>
    /// <returns>是否通过。</returns>
    public static bool TryValidateBattleActives(
        IReadOnlyList<string> actives,
        IReadOnlyList<string> deckCardIds,
        out string failureReason)
    {
        failureReason = null;
        if (actives == null)
        {
            failureReason = "未选择出战舰娘";
            return false;
        }

        var deckSet = new HashSet<string>();
        if (deckCardIds != null)
        {
            for (var i = 0; i < deckCardIds.Count; i++)
            {
                var deckId = deckCardIds[i];
                if (!string.IsNullOrWhiteSpace(deckId))
                {
                    deckSet.Add(deckId.Trim());
                }
            }
        }

        var seen = new HashSet<string>();
        var count = 0;
        for (var i = 0; i < actives.Count; i++)
        {
            var raw = actives[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var id = raw.Trim();
            if (!seen.Add(id))
            {
                failureReason = "出战列表中存在重复舰娘";
                return false;
            }

            if (deckSet.Count > 0 && !deckSet.Contains(id))
            {
                failureReason = "出战舰娘必须来自当前所选卡组";
                return false;
            }

            count++;
        }

        if (count < MinActivesPerBattleFleet)
        {
            failureReason = $"请至少选择 {MinActivesPerBattleFleet} 名出战舰娘（当前 {count} 名）";
            return false;
        }

        if (count > MaxActivesPerBattleFleet)
        {
            failureReason = $"出战最多选择 {MaxActivesPerBattleFleet} 名舰娘（当前 {count} 名）";
            return false;
        }

        return true;
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
    /// 内存缓存为空且磁盘上已有有效存档时，跳过保存以免覆盖。
    /// </summary>
    private static bool ShouldSkipSaveEmptyCache()
    {
        if (_cached?.groups != null && _cached.groups.Count > 0)
        {
            return false;
        }

        return StoreUtil.HasValidDataOnDisk(GetEncryptedFilePath());
    }

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

    private static string GetEncryptedFilePath() => Path.Combine(GetDataFolderPath(), DataFileName);

    #endregion
}