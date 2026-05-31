#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="SkillConfigDatabaseSO"/> Inspector：一键收录同阵营 SkillConfigSO。
/// </summary>
[CustomEditor(typeof(SkillConfigDatabaseSO))]
public sealed class SkillConfigDatabaseSOEditor : Editor
{
    #region Unity Lifecycle

    /// <inheritdoc />
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var db = (SkillConfigDatabaseSO)target;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("数据库同步", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            $"将扫描全部 SkillConfigSO，把 ShipFaction = {db.DatabaseFaction} 的技能加入本库。",
            MessageType.None);

        if (GUILayout.Button($"一键收录（阵营 {db.DatabaseFaction}）"))
        {
            SyncFactionSkills(db);
        }

        if (GUILayout.Button("打开配置表同步工具…"))
        {
            ConfigDatabaseSyncEditorWindow.Open();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 仅同步与本库阵营一致的技能。
    /// </summary>
    private static void SyncFactionSkills(SkillConfigDatabaseSO database)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Sync faction skills into database");
        var report = new ConfigDatabaseSyncUtility.SyncReport();
        var guids = AssetDatabase.FindAssets("t:SkillConfigSO");
        for (var i = 0; i < guids.Length; i++)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(
                AssetDatabase.GUIDToAssetPath(guids[i]));
            if (skill == null || skill.ShipFaction != database.DatabaseFaction)
            {
                continue;
            }

            var single = ConfigDatabaseSyncUtility.SyncSkillIntoDatabase(database, skill);
            report.Added += single.Added;
            report.SkippedPresent += single.SkippedPresent;
            report.SkippedDuplicateId += single.SkippedDuplicateId;
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[ConfigDatabaseSync] {database.name}：新增 {report.Added}，已存在 {report.SkippedPresent}。",
            database);
    }

    #endregion
}
#endif
