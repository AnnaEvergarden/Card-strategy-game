#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器：扫描项目内 CardConfigSO / SkillConfigSO 并合并进对应 Database 资产。
/// </summary>
public static class ConfigDatabaseSyncUtility
{
    #region Nested Types

    /// <summary>
    /// 同步结果摘要。
    /// </summary>
    public sealed class SyncReport
    {
        /// <summary>
        /// 新加入条数。
        /// </summary>
        public int Added;

        /// <summary>
        /// 已存在而跳过。
        /// </summary>
        public int SkippedPresent;

        /// <summary>
        /// 移除的空引用数。
        /// </summary>
        public int RemovedNulls;

        /// <summary>
        /// 因阵营不匹配移除的条数。
        /// </summary>
        public int RemovedFactionMismatch;

        /// <summary>
        /// 因重复 skillId / cardId 跳过的条数。
        /// </summary>
        public int SkippedDuplicateId;

        /// <summary>
        /// 无匹配数据库的技能数（仅技能同步）。
        /// </summary>
        public int OrphanSkills;

        /// <summary>
        /// 警告与说明。
        /// </summary>
        public readonly List<string> Messages = new();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 查找默认卡牌数据库资产（Resources 路径优先）。
    /// </summary>
    public static CardConfigDatabaseSO FindDefaultCardDatabase()
    {
        var resourcesPath = $"Assets/Resources/{GameResourcePaths.CardConfigDatabaseLegacy}.asset";
        var db = AssetDatabase.LoadAssetAtPath<CardConfigDatabaseSO>(resourcesPath);
        if (db != null)
        {
            return db;
        }

        var guids = AssetDatabase.FindAssets("t:CardConfigDatabaseSO");
        return guids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<CardConfigDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]))
            : null;
    }

    /// <summary>
    /// 收集项目中全部卡牌库资产。
    /// </summary>
    public static void CollectCardDatabases(List<CardConfigDatabaseSO> destination)
    {
        destination?.Clear();
        if (destination == null)
        {
            return;
        }

        var guids = AssetDatabase.FindAssets("t:CardConfigDatabaseSO");
        for (var i = 0; i < guids.Length; i++)
        {
            var db = AssetDatabase.LoadAssetAtPath<CardConfigDatabaseSO>(
                AssetDatabase.GUIDToAssetPath(guids[i]));
            if (db != null)
            {
                destination.Add(db);
            }
        }
    }

    /// <summary>
    /// 收集项目中全部技能库资产。
    /// </summary>
    public static void CollectSkillDatabases(List<SkillConfigDatabaseSO> destination)
    {
        destination?.Clear();
        if (destination == null)
        {
            return;
        }

        var guids = AssetDatabase.FindAssets("t:SkillConfigDatabaseSO");
        for (var i = 0; i < guids.Length; i++)
        {
            var db = AssetDatabase.LoadAssetAtPath<SkillConfigDatabaseSO>(
                AssetDatabase.GUIDToAssetPath(guids[i]));
            if (db != null)
            {
                destination.Add(db);
            }
        }
    }

    /// <summary>
    /// 扫描全部 <see cref="CardConfigSO"/>，按 <see cref="CardConfigSO.Faction"/> 写入对应 <see cref="CardConfigDatabaseSO"/>。
    /// </summary>
    public static SyncReport SyncCardsIntoAllDatabases(IReadOnlyList<CardConfigDatabaseSO> databases)
    {
        var report = new SyncReport();
        if (databases == null || databases.Count == 0)
        {
            report.Messages.Add("未找到任何 CardConfigDatabaseSO");
            return report;
        }

        var dbByFaction = new Dictionary<ShipFaction, CardConfigDatabaseSO>();
        CardConfigDatabaseSO fallbackOther = null;
        for (var i = 0; i < databases.Count; i++)
        {
            var db = databases[i];
            if (db == null)
            {
                continue;
            }

            if (db.DatabaseFaction == ShipFaction.Other)
            {
                fallbackOther ??= db;
            }
            else if (!dbByFaction.ContainsKey(db.DatabaseFaction))
            {
                dbByFaction[db.DatabaseFaction] = db;
            }
        }

        var cardGuids = AssetDatabase.FindAssets("t:CardConfigSO");

        // 清理：从各数据库移除阵营不匹配的卡牌（DatabaseFaction == Other 的库跳过）
        for (var i = 0; i < databases.Count; i++)
        {
            var db = databases[i];
            if (db == null || db.DatabaseFaction == ShipFaction.Other)
            {
                continue;
            }

            var serialized = new SerializedObject(db);
            var listProp = serialized.FindProperty("cards");
            if (listProp == null)
            {
                continue;
            }

            var removed = 0;
            for (var j = listProp.arraySize - 1; j >= 0; j--)
            {
                var card = listProp.GetArrayElementAtIndex(j).objectReferenceValue as CardConfigSO;
                if (card != null && card.Faction != db.DatabaseFaction)
                {
                    var cardId = card.CardId ?? card.name;
                    listProp.DeleteArrayElementAtIndex(j);
                    removed++;
                    report.Messages.Add($"从 {db.name} 移除阵营不匹配卡牌 {cardId}（{card.Faction} → {db.DatabaseFaction}）");
                }
            }

            if (removed > 0)
            {
                report.RemovedFactionMismatch += removed;
                serialized.ApplyModifiedProperties();
            }
        }

        for (var i = 0; i < cardGuids.Length; i++)
        {
            var card = AssetDatabase.LoadAssetAtPath<CardConfigSO>(
                AssetDatabase.GUIDToAssetPath(cardGuids[i]));
            if (card == null)
            {
                continue;
            }

            var faction = card.Faction;
            if (!dbByFaction.TryGetValue(faction, out var targetDb))
            {
                targetDb = fallbackOther;
            }

            if (targetDb == null)
            {
                report.OrphanSkills++;
                report.Messages.Add(
                    $"无匹配卡牌库：{card.name}（阵营 {faction}，CardId={card.CardId}）");
                continue;
            }

            var single = SyncCardsIntoDatabase(targetDb, card);
            report.Added += single.Added;
            report.SkippedPresent += single.SkippedPresent;
            report.SkippedDuplicateId += single.SkippedDuplicateId;
            report.RemovedNulls += single.RemovedNulls;
        }

        for (var i = 0; i < databases.Count; i++)
        {
            if (databases[i] != null)
            {
                EditorUtility.SetDirty(databases[i]);
            }
        }

        report.Messages.Insert(0,
            $"卡牌同步完成：新增 {report.Added}，已存在 {report.SkippedPresent}，清理阵营不匹配 {report.RemovedFactionMismatch}，无库 {report.OrphanSkills}。");
        return report;
    }

    /// <summary>
    /// 将单张卡牌并入指定卡牌库（若尚未存在）。
    /// </summary>
    public static SyncReport SyncCardsIntoDatabase(CardConfigDatabaseSO database, CardConfigSO card)
    {
        var report = new SyncReport();
        if (database == null || card == null)
        {
            return report;
        }

        Undo.RecordObject(database, "Sync card into database");
        var serialized = new SerializedObject(database);
        var listProp = serialized.FindProperty("cards");
        if (listProp == null)
        {
            return report;
        }

        var existing = BuildCardSet(listProp, report);
        RemoveNulls(listProp, report);

        if (existing.Contains(card))
        {
            report.SkippedPresent++;
            serialized.ApplyModifiedProperties();
            return report;
        }

        var cardId = card.CardId?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(cardId) && existing.CardIds.Contains(cardId))
        {
            report.SkippedDuplicateId++;
            serialized.ApplyModifiedProperties();
            return report;
        }

        var idx = listProp.arraySize;
        listProp.InsertArrayElementAtIndex(idx);
        listProp.GetArrayElementAtIndex(idx).objectReferenceValue = card;
        report.Added++;

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        return report;
    }

    /// <summary>
    /// 扫描全部 <see cref="CardConfigSO"/> 并入指定卡牌库（去重、移除空项）。
    /// </summary>
    public static SyncReport SyncCardsIntoDatabase(CardConfigDatabaseSO database)
    {
        var report = new SyncReport();
        if (database == null)
        {
            report.Messages.Add("卡牌数据库为空");
            return report;
        }

        Undo.RecordObject(database, "Sync cards into database");
        var serialized = new SerializedObject(database);
        var listProp = serialized.FindProperty("cards");
        if (listProp == null)
        {
            report.Messages.Add("找不到 cards 列表字段");
            return report;
        }

        var existing = BuildCardSet(listProp, report);
        RemoveNulls(listProp, report);

        var guids = AssetDatabase.FindAssets("t:CardConfigSO");
        var toAdd = new List<CardConfigSO>(guids.Length);
        for (var i = 0; i < guids.Length; i++)
        {
            var card = AssetDatabase.LoadAssetAtPath<CardConfigSO>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (card == null)
            {
                continue;
            }

            if (existing.Contains(card))
            {
                report.SkippedPresent++;
                continue;
            }

            var cardId = card.CardId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(cardId) && existing.CardIds.Contains(cardId))
            {
                report.SkippedDuplicateId++;
                report.Messages.Add($"跳过重复 CardId「{cardId}」: {card.name}");
                continue;
            }

            toAdd.Add(card);
            existing.Add(card, cardId);
        }

        toAdd.Sort((a, b) => string.Compare(a?.CardId, b?.CardId, StringComparison.Ordinal));
        for (var i = 0; i < toAdd.Count; i++)
        {
            var idx = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(idx);
            listProp.GetArrayElementAtIndex(idx).objectReferenceValue = toAdd[i];
            report.Added++;
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        report.Messages.Insert(0, $"卡牌库 {database.name}：新增 {report.Added}，已存在 {report.SkippedPresent}。");
        return report;
    }

    /// <summary>
    /// 扫描全部 <see cref="SkillConfigSO"/>，按 <see cref="SkillConfigSO.ShipFaction"/> 写入对应 <see cref="SkillConfigDatabaseSO"/>。
    /// </summary>
    public static SyncReport SyncSkillsIntoAllDatabases(IReadOnlyList<SkillConfigDatabaseSO> databases)
    {
        var report = new SyncReport();
        if (databases == null || databases.Count == 0)
        {
            report.Messages.Add("未找到任何 SkillConfigDatabaseSO");
            return report;
        }

        var dbByFaction = new Dictionary<ShipFaction, SkillConfigDatabaseSO>();
        SkillConfigDatabaseSO fallbackOther = null;
        for (var i = 0; i < databases.Count; i++)
        {
            var db = databases[i];
            if (db == null)
            {
                continue;
            }

            if (db.DatabaseFaction == ShipFaction.Other)
            {
                fallbackOther ??= db;
            }
            else if (!dbByFaction.ContainsKey(db.DatabaseFaction))
            {
                dbByFaction[db.DatabaseFaction] = db;
            }
        }

        var skillGuids = AssetDatabase.FindAssets("t:SkillConfigSO");
        for (var i = 0; i < skillGuids.Length; i++)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(
                AssetDatabase.GUIDToAssetPath(skillGuids[i]));
            if (skill == null)
            {
                continue;
            }

            var faction = skill.ShipFaction;
            if (!dbByFaction.TryGetValue(faction, out var targetDb))
            {
                targetDb = fallbackOther;
            }

            if (targetDb == null)
            {
                report.OrphanSkills++;
                report.Messages.Add(
                    $"无匹配技能库：{skill.name}（阵营 {faction}，SkillId={skill.SkillId}）");
                continue;
            }

            var single = SyncSkillIntoDatabase(targetDb, skill);
            report.Added += single.Added;
            report.SkippedPresent += single.SkippedPresent;
            report.SkippedDuplicateId += single.SkippedDuplicateId;
            report.RemovedNulls += single.RemovedNulls;
        }

        for (var i = 0; i < databases.Count; i++)
        {
            if (databases[i] != null)
            {
                EditorUtility.SetDirty(databases[i]);
            }
        }

        report.Messages.Insert(0,
            $"技能同步完成：新增 {report.Added}，已存在 {report.SkippedPresent}，无库 {report.OrphanSkills}。");
        return report;
    }

    /// <summary>
    /// 将单条技能并入指定技能库（若尚未存在）。
    /// </summary>
    public static SyncReport SyncSkillIntoDatabase(SkillConfigDatabaseSO database, SkillConfigSO skill)
    {
        var report = new SyncReport();
        if (database == null || skill == null)
        {
            return report;
        }

        Undo.RecordObject(database, "Sync skill into database");
        var serialized = new SerializedObject(database);
        var listProp = serialized.FindProperty("skills");
        if (listProp == null)
        {
            return report;
        }

        var existing = BuildSkillSet(listProp, report);
        RemoveNulls(listProp, report);

        if (existing.Contains(skill))
        {
            report.SkippedPresent++;
            serialized.ApplyModifiedProperties();
            return report;
        }

        var skillId = skill.SkillId?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(skillId) && existing.SkillIds.Contains(skillId))
        {
            report.SkippedDuplicateId++;
            serialized.ApplyModifiedProperties();
            return report;
        }

        var idx = listProp.arraySize;
        listProp.InsertArrayElementAtIndex(idx);
        listProp.GetArrayElementAtIndex(idx).objectReferenceValue = skill;
        report.Added++;

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        return report;
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// 卡牌去重集合。
    /// </summary>
    private sealed class CardEntrySet
    {
        /// <summary>
        /// 已收录的 CardId。
        /// </summary>
        public readonly HashSet<string> CardIds = new(StringComparer.Ordinal);

        /// <summary>
        /// 已收录的资产引用。
        /// </summary>
        private readonly HashSet<CardConfigSO> _refs = new();

        /// <summary>
        /// 是否已包含该资产。
        /// </summary>
        public bool Contains(CardConfigSO card) => card != null && _refs.Contains(card);

        /// <summary>
        /// 登记卡牌。
        /// </summary>
        public void Add(CardConfigSO card, string cardId)
        {
            if (card != null)
            {
                _refs.Add(card);
            }

            if (!string.IsNullOrEmpty(cardId))
            {
                CardIds.Add(cardId);
            }
        }
    }

    /// <summary>
    /// 技能去重集合。
    /// </summary>
    private sealed class SkillEntrySet
    {
        /// <summary>
        /// 已收录的 SkillId。
        /// </summary>
        public readonly HashSet<string> SkillIds = new(StringComparer.Ordinal);

        /// <summary>
        /// 已收录的资产引用。
        /// </summary>
        private readonly HashSet<SkillConfigSO> _refs = new();

        /// <summary>
        /// 是否已包含该资产。
        /// </summary>
        public bool Contains(SkillConfigSO skill) => skill != null && _refs.Contains(skill);

        /// <summary>
        /// 登记技能。
        /// </summary>
        public void Add(SkillConfigSO skill, string skillId)
        {
            if (skill != null)
            {
                _refs.Add(skill);
            }

            if (!string.IsNullOrEmpty(skillId))
            {
                SkillIds.Add(skillId);
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 从序列化列表构建卡牌集合。
    /// </summary>
    private static CardEntrySet BuildCardSet(SerializedProperty listProp, SyncReport report)
    {
        var set = new CardEntrySet();
        for (var i = 0; i < listProp.arraySize; i++)
        {
            var card = listProp.GetArrayElementAtIndex(i).objectReferenceValue as CardConfigSO;
            if (card == null)
            {
                continue;
            }

            set.Add(card, card.CardId?.Trim() ?? string.Empty);
        }

        return set;
    }

    /// <summary>
    /// 从序列化列表构建技能集合。
    /// </summary>
    private static SkillEntrySet BuildSkillSet(SerializedProperty listProp, SyncReport report)
    {
        var set = new SkillEntrySet();
        for (var i = 0; i < listProp.arraySize; i++)
        {
            var skill = listProp.GetArrayElementAtIndex(i).objectReferenceValue as SkillConfigSO;
            if (skill == null)
            {
                continue;
            }

            set.Add(skill, skill.SkillId?.Trim() ?? string.Empty);
        }

        return set;
    }

    /// <summary>
    /// 移除列表中的空引用。
    /// </summary>
    private static void RemoveNulls(SerializedProperty listProp, SyncReport report)
    {
        for (var i = listProp.arraySize - 1; i >= 0; i--)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                listProp.DeleteArrayElementAtIndex(i);
                report.RemovedNulls++;
            }
        }
    }

    #endregion
}
#endif
