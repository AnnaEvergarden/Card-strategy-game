#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器：校验并同步 <see cref="CardConfigSO"/> 的 skillIds 与 skillRefs。
/// </summary>
public static class CardSkillBindingSyncUtility
{
    #region Nested Types

    /// <summary>
    /// 同步方向。
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// 按 skillIds 查找并写入 skillRefs。
        /// </summary>
        FromSkillIds = 0,

        /// <summary>
        /// 按 skillRefs 写入 skillIds。
        /// </summary>
        FromSkillRefs = 1,

        /// <summary>
        /// 先补全缺失 SO，再补全缺失 Id（不覆盖双方都已填但不一致的组合）。
        /// </summary>
        Bidirectional = 2
    }

    /// <summary>
    /// 单槽问题严重级别。
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// 提示。
        /// </summary>
        Info = 0,

        /// <summary>
        /// 可自动修复。
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 需人工处理。
        /// </summary>
        Error = 2
    }

    /// <summary>
    /// 单槽校验结果。
    /// </summary>
    public sealed class SlotIssue
    {
        /// <summary>
        /// 槽位索引（0 起）。
        /// </summary>
        public int SlotIndex;

        /// <summary>
        /// 说明。
        /// </summary>
        public string Message = string.Empty;

        /// <summary>
        /// 严重级别。
        /// </summary>
        public IssueSeverity Severity = IssueSeverity.Info;
    }

    /// <summary>
    /// 单张卡牌校验报告。
    /// </summary>
    public sealed class CardReport
    {
        /// <summary>
        /// 目标卡牌。
        /// </summary>
        public CardConfigSO Card;

        /// <summary>
        /// 槽位问题列表。
        /// </summary>
        public readonly List<SlotIssue> Issues = new();

        /// <summary>
        /// 是否存在 Error 级问题。
        /// </summary>
        public bool HasError
        {
            get
            {
                for (var i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == IssueSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    /// <summary>
    /// 同步/修复结果。
    /// </summary>
    public sealed class SyncResult
    {
        /// <summary>
        /// 修改的槽位数。
        /// </summary>
        public int SlotsChanged;

        /// <summary>
        /// 仍无法自动修复的说明。
        /// </summary>
        public readonly List<string> RemainingMessages = new();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 扫描项目中全部 <see cref="SkillConfigSO"/>，构建 skillId → SO 映射（重复 id 记入 warnings）。
    /// </summary>
    public static Dictionary<string, SkillConfigSO> BuildGlobalSkillIdMap(List<string> duplicateWarnings)
    {
        duplicateWarnings?.Clear();
        var map = new Dictionary<string, SkillConfigSO>(StringComparer.Ordinal);
        var guids = AssetDatabase.FindAssets("t:SkillConfigSO");
        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var skill = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(path);
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            {
                continue;
            }

            var key = skill.SkillId.Trim();
            if (map.TryGetValue(key, out var existing) && existing != skill)
            {
                duplicateWarnings?.Add(
                    $"重复 SkillId「{key}」: {existing.name} 与 {skill.name}");
                continue;
            }

            map[key] = skill;
        }

        return map;
    }

    /// <summary>
    /// 校验一张卡牌上 skillIds 与 skillRefs 是否一致。
    /// </summary>
    public static CardReport Validate(CardConfigSO card, IReadOnlyDictionary<string, SkillConfigSO> skillMap)
    {
        var report = new CardReport { Card = card };
        if (card == null)
        {
            report.Issues.Add(new SlotIssue
            {
                SlotIndex = -1,
                Message = "卡牌为空",
                Severity = IssueSeverity.Error
            });
            return report;
        }

        var serialized = new SerializedObject(card);
        var idsProp = serialized.FindProperty("skillIds");
        var refsProp = serialized.FindProperty("skillRefs");
        EnsureParallelArraySize(idsProp, refsProp);

        for (var i = 0; i < CardConfigSO.MaxSkillsPerCard; i++)
        {
            var id = GetSkillIdAt(idsProp, i);
            var sk = GetSkillRefAt(refsProp, i);
            ValidateSlot(report, i, id, sk, skillMap);
        }

        return report;
    }

    /// <summary>
    /// 对单张卡牌执行同步。
    /// </summary>
    public static SyncResult ApplySync(
        CardConfigSO card,
        SyncMode mode,
        IReadOnlyDictionary<string, SkillConfigSO> skillMap)
    {
        var result = new SyncResult();
        if (card == null)
        {
            result.RemainingMessages.Add("卡牌为空");
            return result;
        }

        Undo.RecordObject(card, "Sync skill bindings");
        var serialized = new SerializedObject(card);
        var idsProp = serialized.FindProperty("skillIds");
        var refsProp = serialized.FindProperty("skillRefs");
        EnsureParallelArraySize(idsProp, refsProp);

        if (mode == SyncMode.FromSkillIds || mode == SyncMode.Bidirectional)
        {
            result.SlotsChanged += SyncRefsFromIds(idsProp, refsProp, skillMap, result.RemainingMessages);
        }

        if (mode == SyncMode.FromSkillRefs || mode == SyncMode.Bidirectional)
        {
            result.SlotsChanged += SyncIdsFromRefs(idsProp, refsProp, result.RemainingMessages);
        }

        if (result.SlotsChanged > 0)
        {
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(card);
        }

        return result;
    }

    /// <summary>
    /// 收集全部 <see cref="CardConfigDatabaseSO"/> 中的卡牌配置。
    /// </summary>
    public static void CollectAllCardConfigs(List<CardConfigSO> destination)
    {
        destination?.Clear();
        if (destination == null)
        {
            return;
        }

        var dbGuids = AssetDatabase.FindAssets("t:CardConfigDatabaseSO");
        var seen = new HashSet<CardConfigSO>();
        for (var d = 0; d < dbGuids.Length; d++)
        {
            var db = AssetDatabase.LoadAssetAtPath<CardConfigDatabaseSO>(
                AssetDatabase.GUIDToAssetPath(dbGuids[d]));
            if (db?.Cards == null)
            {
                continue;
            }

            var cards = db.Cards;
            for (var i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                if (c != null && seen.Add(c))
                {
                    destination.Add(c);
                }
            }
        }

        var cardGuids = AssetDatabase.FindAssets("t:CardConfigSO");
        for (var i = 0; i < cardGuids.Length; i++)
        {
            var c = AssetDatabase.LoadAssetAtPath<CardConfigSO>(AssetDatabase.GUIDToAssetPath(cardGuids[i]));
            if (c != null && seen.Add(c))
            {
                destination.Add(c);
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 保证 skillIds / skillRefs 数组长度均为 MaxSkillsPerCard。
    /// </summary>
    private static void EnsureParallelArraySize(SerializedProperty idsProp, SerializedProperty refsProp)
    {
        if (idsProp == null || refsProp == null)
        {
            return;
        }

        idsProp.arraySize = CardConfigSO.MaxSkillsPerCard;
        refsProp.arraySize = CardConfigSO.MaxSkillsPerCard;
    }

    /// <summary>
    /// 读取槽位 skillId。
    /// </summary>
    private static string GetSkillIdAt(SerializedProperty idsProp, int index)
    {
        if (idsProp == null || index < 0 || index >= idsProp.arraySize)
        {
            return string.Empty;
        }

        return idsProp.GetArrayElementAtIndex(index).stringValue?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// 读取槽位技能 SO。
    /// </summary>
    private static SkillConfigSO GetSkillRefAt(SerializedProperty refsProp, int index)
    {
        if (refsProp == null || index < 0 || index >= refsProp.arraySize)
        {
            return null;
        }

        return refsProp.GetArrayElementAtIndex(index).objectReferenceValue as SkillConfigSO;
    }

    /// <summary>
    /// 校验单槽。
    /// </summary>
    private static void ValidateSlot(
        CardReport report,
        int index,
        string id,
        SkillConfigSO sk,
        IReadOnlyDictionary<string, SkillConfigSO> skillMap)
    {
        var hasId = !string.IsNullOrWhiteSpace(id);
        var hasRef = sk != null;

        if (!hasId && !hasRef)
        {
            return;
        }

        if (hasId && !hasRef)
        {
            if (skillMap != null && skillMap.ContainsKey(id))
            {
                report.Issues.Add(new SlotIssue
                {
                    SlotIndex = index,
                    Message = $"有 SkillId「{id}」但 SO 为空，可自动查找绑定",
                    Severity = IssueSeverity.Warning
                });
            }
            else
            {
                report.Issues.Add(new SlotIssue
                {
                    SlotIndex = index,
                    Message = $"有 SkillId「{id}」但找不到对应 SkillConfigSO",
                    Severity = IssueSeverity.Error
                });
            }

            return;
        }

        if (!hasId && hasRef)
        {
            var soId = sk.SkillId?.Trim() ?? string.Empty;
            report.Issues.Add(new SlotIssue
            {
                SlotIndex = index,
                Message = string.IsNullOrEmpty(soId)
                    ? $"槽 {index + 1} 已引用 SO「{sk.name}」但 SO 的 SkillId 为空"
                    : $"SO 已填但 SkillId 为空，可自动写入「{soId}」",
                Severity = string.IsNullOrEmpty(soId) ? IssueSeverity.Error : IssueSeverity.Warning
            });
            return;
        }

        var refId = sk.SkillId?.Trim() ?? string.Empty;
        if (!string.Equals(id, refId, StringComparison.Ordinal))
        {
            report.Issues.Add(new SlotIssue
            {
                SlotIndex = index,
                Message = $"不一致：SkillId「{id}」≠ SO.SkillId「{refId}」（{sk.name}）",
                Severity = IssueSeverity.Error
            });
        }
    }

    /// <summary>
    /// 按 SkillId 填充 SO 引用。
    /// </summary>
    private static int SyncRefsFromIds(
        SerializedProperty idsProp,
        SerializedProperty refsProp,
        IReadOnlyDictionary<string, SkillConfigSO> skillMap,
        List<string> remaining)
    {
        var changed = 0;
        for (var i = 0; i < CardConfigSO.MaxSkillsPerCard; i++)
        {
            var id = GetSkillIdAt(idsProp, i);
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var current = GetSkillRefAt(refsProp, i);
            if (current != null && string.Equals(current.SkillId?.Trim(), id, StringComparison.Ordinal))
            {
                continue;
            }

            if (current != null && !string.Equals(current.SkillId?.Trim(), id, StringComparison.Ordinal))
            {
                remaining?.Add($"槽 {i + 1}: SkillId 与已绑 SO 不一致，跳过自动覆盖 SO");
                continue;
            }

            if (skillMap == null || !skillMap.TryGetValue(id, out var found) || found == null)
            {
                remaining?.Add($"槽 {i + 1}: 找不到 SkillId「{id}」对应的 SO");
                continue;
            }

            refsProp.GetArrayElementAtIndex(i).objectReferenceValue = found;
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// 按 SO 写入 SkillId。
    /// </summary>
    private static int SyncIdsFromRefs(
        SerializedProperty idsProp,
        SerializedProperty refsProp,
        List<string> remaining)
    {
        var changed = 0;
        for (var i = 0; i < CardConfigSO.MaxSkillsPerCard; i++)
        {
            var sk = GetSkillRefAt(refsProp, i);
            if (sk == null)
            {
                continue;
            }

            var idFromSo = sk.SkillId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(idFromSo))
            {
                remaining?.Add($"槽 {i + 1}: SO「{sk.name}」的 SkillId 为空");
                continue;
            }

            var currentId = GetSkillIdAt(idsProp, i);
            if (string.Equals(currentId, idFromSo, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(currentId) && !string.Equals(currentId, idFromSo, StringComparison.Ordinal))
            {
                remaining?.Add($"槽 {i + 1}: SkillId 与 SO 不一致，跳过自动覆盖 SkillId");
                continue;
            }

            idsProp.GetArrayElementAtIndex(i).stringValue = idFromSo;
            changed++;
        }

        return changed;
    }

    #endregion
}
#endif
