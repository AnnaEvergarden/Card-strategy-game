#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="CardConfigDatabaseSO"/> Inspector：一键收录全部 CardConfigSO。
/// </summary>
[CustomEditor(typeof(CardConfigDatabaseSO))]
public sealed class CardConfigDatabaseSOEditor : Editor
{
    #region Unity Lifecycle

    /// <inheritdoc />
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var db = (CardConfigDatabaseSO)target;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("数据库同步", EditorStyles.boldLabel);
        if (GUILayout.Button("一键收录全部 CardConfigSO"))
        {
            Undo.RecordObject(db, "Sync all cards into database");
            var report = ConfigDatabaseSyncUtility.SyncCardsIntoDatabase(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ConfigDatabaseSync] {report.Messages[0]}", db);
        }
    }

    #endregion
}
#endif
