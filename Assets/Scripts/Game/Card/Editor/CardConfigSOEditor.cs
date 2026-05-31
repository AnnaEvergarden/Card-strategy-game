#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="CardConfigSO"/> Inspector：提供 skillIds 与 skillRefs 同步按钮。
/// </summary>
[CustomEditor(typeof(CardConfigSO))]
public sealed class CardConfigSOEditor : Editor
{
    #region Fields

    /// <summary>
    /// 重复 SkillId 缓冲。
    /// </summary>
    private readonly System.Collections.Generic.List<string> _duplicateWarnings = new();

    #endregion

    #region Unity Lifecycle

    /// <inheritdoc />
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        var card = (CardConfigSO)target;
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("技能绑定同步", EditorStyles.boldLabel);

        var skillMap = CardSkillBindingSyncUtility.BuildGlobalSkillIdMap(_duplicateWarnings);
        var report = CardSkillBindingSyncUtility.Validate(card, skillMap);

        if (report.Issues.Count == 0)
        {
            EditorGUILayout.HelpBox("skillIds 与 skillRefs 一致。", MessageType.None);
        }
        else
        {
            for (var i = 0; i < report.Issues.Count; i++)
            {
                var issue = report.Issues[i];
                var type = issue.Severity == CardSkillBindingSyncUtility.IssueSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox($"槽 {issue.SlotIndex + 1}: {issue.Message}", type);
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("按 SkillId 补 SO"))
        {
            ApplySync(card, CardSkillBindingSyncUtility.SyncMode.FromSkillIds, skillMap);
        }

        if (GUILayout.Button("按 SO 补 SkillId"))
        {
            ApplySync(card, CardSkillBindingSyncUtility.SyncMode.FromSkillRefs, skillMap);
        }

        if (GUILayout.Button("双向补全"))
        {
            ApplySync(card, CardSkillBindingSyncUtility.SyncMode.Bidirectional, skillMap);
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("打开批量同步工具…"))
        {
            CardSkillSyncEditorWindow.Open();
        }

        serializedObject.ApplyModifiedProperties();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 执行同步并刷新 Inspector。
    /// </summary>
    private static void ApplySync(
        CardConfigSO card,
        CardSkillBindingSyncUtility.SyncMode mode,
        System.Collections.Generic.IReadOnlyDictionary<string, SkillConfigSO> skillMap)
    {
        var result = CardSkillBindingSyncUtility.ApplySync(card, mode, skillMap);
        for (var i = 0; i < result.RemainingMessages.Count; i++)
        {
            Debug.LogWarning($"[CardSkillSync] {card.name}: {result.RemainingMessages[i]}", card);
        }

        Debug.Log($"[CardSkillSync] {card.name} 同步完成，修改 {result.SlotsChanged} 个槽位。", card);
    }

    #endregion
}
#endif
