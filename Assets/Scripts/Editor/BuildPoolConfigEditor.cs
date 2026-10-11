using UnityEditor;
using UnityEngine;

/// <summary>
/// BuildPoolConfigSO 的自定义 Inspector 编辑器：在默认绘制下方增加批量补充工具。
/// </summary>
[CustomEditor(typeof(BuildPoolConfigSO))]
public class BuildPoolConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var dropEntriesProp = serializedObject.FindProperty("_dropEntries");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("\u6279\u91cf\u5de5\u5177", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = dropEntriesProp.arraySize > 0;
        if (GUILayout.Button("\u8865\u5145\u5168\u90e8 CardId", GUILayout.Height(24)))
        {
            BatchFillAllCardIds(dropEntriesProp);
        }

        if (GUILayout.Button("\u6e05\u7a7a\u5168\u90e8 CardId", GUILayout.Height(24)))
        {
            BatchClearAllCardIds(dropEntriesProp);
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        // 显示统计信息
        if (dropEntriesProp.arraySize > 0)
        {
            int filled = 0;
            for (int i = 0; i < dropEntriesProp.arraySize; i++)
            {
                var entry = dropEntriesProp.GetArrayElementAtIndex(i);
                var cardConfigProp = entry.FindPropertyRelative("CardConfig");
                var cardIdProp = entry.FindPropertyRelative("CardId");

                if (cardConfigProp.objectReferenceValue != null &&
                    !string.IsNullOrEmpty(cardIdProp.stringValue))
                {
                    filled++;
                }
            }
            EditorGUILayout.LabelField(
                $"\u72b6\u6001\uff1a{filled}/{dropEntriesProp.arraySize} \u6761\u5df2\u5173\u8054 CardConfig \u5e76\u8865\u5145 CardId",
                EditorStyles.miniLabel);
        }
    }

    private void BatchFillAllCardIds(SerializedProperty dropEntriesProp)
    {
        int count = 0;
        serializedObject.Update();
        for (int i = 0; i < dropEntriesProp.arraySize; i++)
        {
            var entry = dropEntriesProp.GetArrayElementAtIndex(i);
            var cardConfigProp = entry.FindPropertyRelative("CardConfig");
            var cardIdProp = entry.FindPropertyRelative("CardId");

            if (cardConfigProp.objectReferenceValue is CardConfigSO cardConfig)
            {
                cardIdProp.stringValue = cardConfig.CardId;
                count++;
            }
        }
        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[BuildPoolConfigEditor] \u5df2\u8865\u5145 {count} \u4e2a\u6761\u76ee\u7684 CardId");
    }

    private void BatchClearAllCardIds(SerializedProperty dropEntriesProp)
    {
        serializedObject.Update();
        for (int i = 0; i < dropEntriesProp.arraySize; i++)
        {
            var entry = dropEntriesProp.GetArrayElementAtIndex(i);
            var cardIdProp = entry.FindPropertyRelative("CardId");
            cardIdProp.stringValue = string.Empty;
        }
        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[BuildPoolConfigEditor] \u5df2\u6e05\u7a7a {dropEntriesProp.arraySize} \u4e2a\u6761\u76ee\u7684 CardId");
    }
}
