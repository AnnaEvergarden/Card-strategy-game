using UnityEditor;
using UnityEngine;

/// <summary>
/// LevelStageConfigSO 的自定义 Inspector：在默认字段下方增加使用 LevelAreaConfigSO 引用自动补充 AreaId 的工具。
/// </summary>
[CustomEditor(typeof(LevelStageConfigSO))]
public class LevelStageConfigEditor : Editor
{
    private SerializedProperty _areaIdProp;
    private SerializedProperty _areaRefProp;

    private void OnEnable()
    {
        _areaIdProp = serializedObject.FindProperty("_areaId");
        _areaRefProp = serializedObject.FindProperty("_areaRef");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 手动绘制所有字段（排除 _areaRef，由下方补充工具处理）
        DrawPropertiesExcluding(serializedObject, "_areaRef");

        serializedObject.ApplyModifiedProperties();

        // 补充工具区域
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("批量工具", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        float origLabel = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 100f;
        EditorGUILayout.PropertyField(_areaRefProp, new GUIContent("区域配置"));
        EditorGUIUtility.labelWidth = origLabel;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = _areaRefProp.objectReferenceValue != null;
        if (GUILayout.Button("补充 AreaId", GUILayout.Height(22)))
        {
            FillAreaId();
        }
        if (GUILayout.Button("清空 AreaId", GUILayout.Height(22)))
        {
            _areaIdProp.stringValue = string.Empty;
            serializedObject.ApplyModifiedProperties();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // 状态提示
        if (_areaRefProp.objectReferenceValue != null && !string.IsNullOrEmpty(_areaIdProp.stringValue))
        {
            EditorGUILayout.LabelField(
                string.Format("状态：已关联 {0} → {1}",
                    _areaRefProp.objectReferenceValue.name, _areaIdProp.stringValue),
                EditorStyles.miniLabel);
        }
    }

    private void FillAreaId()
    {
        if (_areaRefProp.objectReferenceValue is LevelAreaConfigSO areaConfig)
        {
            _areaIdProp.stringValue = areaConfig.AreaId;
            serializedObject.ApplyModifiedProperties();
            Debug.Log(string.Format("[LevelStageConfigEditor] 已补充 AreaId: {0}", areaConfig.AreaId));
        }
    }
}
