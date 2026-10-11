using UnityEditor;
using UnityEngine;

/// <summary>
/// BuildDropEntrySO 的自定义绘制器：支持通过 CardConfigSO 对象字段拖拽/选择后自动补充 CardId，并提供手动补充按钮。
/// </summary>
[CustomPropertyDrawer(typeof(BuildPoolConfigSO.BuildDropEntrySO))]
public class BuildDropEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var cardConfigProp = property.FindPropertyRelative("CardConfig");
        var cardIdProp = property.FindPropertyRelative("CardId");
        var weightProp = property.FindPropertyRelative("Weight");

        float lh = EditorGUIUtility.singleLineHeight;
        float sp = 2f;

        // 第一行：标签 | CardConfig 对象字段 | 补充按钮
        float lw = position.width * 0.22f;
        float bw = 50f;
        float cw = position.width - lw - bw - 4f;

        GUI.Label(new Rect(position.x, position.y, lw, lh), label, EditorStyles.label);

        Rect configRect = new Rect(position.x + lw + 2, position.y, cw - 2, lh);
        EditorGUI.BeginChangeCheck();
        EditorGUI.ObjectField(configRect, cardConfigProp, typeof(CardConfigSO), GUIContent.none);
        if (EditorGUI.EndChangeCheck()) AutoFillCardId(cardConfigProp, cardIdProp);

        if (GUI.Button(new Rect(position.x + position.width - bw, position.y, bw, lh), "\u8865\u5145"))
            AutoFillCardId(cardConfigProp, cardIdProp);

        // 第二行：CardId
        EditorGUI.PropertyField(
            new Rect(position.x, position.y + lh + sp, position.width, lh),
            cardIdProp, new GUIContent("CardId"));

        // 第三行：Weight
        EditorGUI.PropertyField(
            new Rect(position.x, position.y + (lh + sp) * 2, position.width, lh),
            weightProp, new GUIContent("Weight"));

        EditorGUI.EndProperty();
    }

    private static void AutoFillCardId(SerializedProperty cardConfigProp, SerializedProperty cardIdProp)
    {
        if (cardConfigProp.objectReferenceValue is CardConfigSO cardConfig)
        {
            cardIdProp.stringValue = cardConfig.CardId;
            cardIdProp.serializedObject.ApplyModifiedProperties();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 3 + 4f;
    }
}
