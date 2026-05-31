#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 菜单工具：批量检查并同步舰娘 skillIds 与 skillRefs。
/// </summary>
public sealed class CardSkillSyncEditorWindow : EditorWindow
{
    #region Fields

    /// <summary>
    /// 滚动位置。
    /// </summary>
    private Vector2 _scroll;

    /// <summary>
    /// 全部卡牌。
    /// </summary>
    private readonly List<CardConfigSO> _cards = new();

    /// <summary>
    /// 校验报告。
    /// </summary>
    private readonly List<CardSkillBindingSyncUtility.CardReport> _reports = new();

    /// <summary>
    /// 全局 skillId 映射。
    /// </summary>
    private Dictionary<string, SkillConfigSO> _skillMap = new();

    /// <summary>
    /// 重复 SkillId 警告。
    /// </summary>
    private readonly List<string> _duplicateSkillWarnings = new();

    /// <summary>
    /// 上次操作日志。
    /// </summary>
    private string _lastLog = string.Empty;

    #endregion

    #region Public API

    /// <summary>
    /// 打开工具窗口。
    /// </summary>
    [MenuItem("Game/Card/技能 ID 与 SO 同步工具")]
    public static void Open()
    {
        var win = GetWindow<CardSkillSyncEditorWindow>(false, "Card Skill Sync", true);
        win.minSize = new Vector2(480f, 360f);
        win.RefreshAll();
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 绘制窗口 UI。
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField("舰娘 SkillIds ↔ SkillConfigSO 同步", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "按 SkillId 补全 SO：有字符串、缺引用时从全项目 SkillConfigSO 查找。\n" +
            "按 SO 补全 SkillId：有引用、字符串为空时写入 SO.SkillId。\n" +
            "双向：只补缺失项，双方都已填但不一致时需手动改或使用单向覆盖。",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新列表", GUILayout.Height(24f)))
        {
            RefreshAll();
        }

        if (GUILayout.Button("检查全部", GUILayout.Height(24f)))
        {
            RunValidateAll();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全部：按 SkillId 补 SO", GUILayout.Height(26f)))
        {
            RunSyncAll(CardSkillBindingSyncUtility.SyncMode.FromSkillIds);
        }

        if (GUILayout.Button("全部：按 SO 补 SkillId", GUILayout.Height(26f)))
        {
            RunSyncAll(CardSkillBindingSyncUtility.SyncMode.FromSkillRefs);
        }

        if (GUILayout.Button("全部：双向补全", GUILayout.Height(26f)))
        {
            RunSyncAll(CardSkillBindingSyncUtility.SyncMode.Bidirectional);
        }

        EditorGUILayout.EndHorizontal();

        if (_duplicateSkillWarnings.Count > 0)
        {
            EditorGUILayout.HelpBox(
                $"全局重复 SkillId {_duplicateSkillWarnings.Count} 条（见 Console）",
                MessageType.Warning);
        }

        if (!string.IsNullOrEmpty(_lastLog))
        {
            EditorGUILayout.LabelField("上次结果", EditorStyles.miniBoldLabel);
            EditorGUILayout.TextArea(_lastLog, GUILayout.MaxHeight(60f));
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"卡牌 {_cards.Count} · 有问题 {_reports.Count}", EditorStyles.miniLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (var i = 0; i < _reports.Count; i++)
        {
            DrawReport(_reports[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 重建技能映射与卡牌列表。
    /// </summary>
    private void RefreshAll()
    {
        _skillMap = CardSkillBindingSyncUtility.BuildGlobalSkillIdMap(_duplicateSkillWarnings);
        for (var i = 0; i < _duplicateSkillWarnings.Count; i++)
        {
            Debug.LogWarning($"[CardSkillSync] {_duplicateSkillWarnings[i]}");
        }

        CardSkillBindingSyncUtility.CollectAllCardConfigs(_cards);
        RunValidateAll();
    }

    /// <summary>
    /// 校验全部卡牌。
    /// </summary>
    private void RunValidateAll()
    {
        _reports.Clear();
        for (var i = 0; i < _cards.Count; i++)
        {
            var report = CardSkillBindingSyncUtility.Validate(_cards[i], _skillMap);
            if (report.Issues.Count > 0)
            {
                _reports.Add(report);
            }
        }

        _lastLog = $"检查完成：{_cards.Count} 张卡，{_reports.Count} 张存在问题。";
        Repaint();
    }

    /// <summary>
    /// 对全部卡牌执行同步。
    /// </summary>
    private void RunSyncAll(CardSkillBindingSyncUtility.SyncMode mode)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Sync all card skill bindings");
        var totalSlots = 0;
        var remaining = 0;
        for (var i = 0; i < _cards.Count; i++)
        {
            var result = CardSkillBindingSyncUtility.ApplySync(_cards[i], mode, _skillMap);
            totalSlots += result.SlotsChanged;
            remaining += result.RemainingMessages.Count;
            for (var r = 0; r < result.RemainingMessages.Count; r++)
            {
                Debug.LogWarning($"[CardSkillSync] {_cards[i].name}: {result.RemainingMessages[r]}", _cards[i]);
            }
        }

        AssetDatabase.SaveAssets();
        RunValidateAll();
        _lastLog = $"同步({mode})：修改 {totalSlots} 个槽位，仍有 {remaining} 条需人工处理（见 Console）。";
    }

    /// <summary>
    /// 绘制单卡报告与快捷按钮。
    /// </summary>
    private void DrawReport(CardSkillBindingSyncUtility.CardReport report)
    {
        if (report?.Card == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.ObjectField(report.Card, typeof(CardConfigSO), false);
        for (var i = 0; i < report.Issues.Count; i++)
        {
            var issue = report.Issues[i];
            var prefix = issue.Severity switch
            {
                CardSkillBindingSyncUtility.IssueSeverity.Error => "错误",
                CardSkillBindingSyncUtility.IssueSeverity.Warning => "可修",
                _ => "提示"
            };
            EditorGUILayout.LabelField($"[{prefix}] 槽 {issue.SlotIndex + 1}: {issue.Message}", EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("按 Id 补 SO", GUILayout.Width(100f)))
        {
            ApplyOne(report.Card, CardSkillBindingSyncUtility.SyncMode.FromSkillIds);
        }

        if (GUILayout.Button("按 SO 补 Id", GUILayout.Width(100f)))
        {
            ApplyOne(report.Card, CardSkillBindingSyncUtility.SyncMode.FromSkillRefs);
        }

        if (GUILayout.Button("双向", GUILayout.Width(60f)))
        {
            ApplyOne(report.Card, CardSkillBindingSyncUtility.SyncMode.Bidirectional);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 同步单张卡并刷新报告。
    /// </summary>
    private void ApplyOne(CardConfigSO card, CardSkillBindingSyncUtility.SyncMode mode)
    {
        Undo.RecordObject(card, "Sync skill bindings");
        var result = CardSkillBindingSyncUtility.ApplySync(card, mode, _skillMap);
        EditorUtility.SetDirty(card);
        AssetDatabase.SaveAssets();
        for (var r = 0; r < result.RemainingMessages.Count; r++)
        {
            Debug.LogWarning($"[CardSkillSync] {card.name}: {result.RemainingMessages[r]}", card);
        }

        _lastLog = $"{card.name}：修改 {result.SlotsChanged} 槽，剩余 {result.RemainingMessages.Count} 条。";
        RunValidateAll();
    }

    #endregion
}
#endif
