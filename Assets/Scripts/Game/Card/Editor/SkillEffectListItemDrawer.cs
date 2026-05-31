#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 技能效果列表项 Inspector：先选 Instant/Buff，再选子类型与参数。
/// </summary>
[CustomPropertyDrawer(typeof(SkillEffectListItem))]
public sealed class SkillEffectListItemDrawer : PropertyDrawer
{
    #region Constants

    /// <summary>
    /// 单行高度。
    /// </summary>
    private const float LineHeight = 18f;

    /// <summary>
    /// 行间距。
    /// </summary>
    private const float VerticalGap = 2f;

    #endregion

    #region Public API

    /// <inheritdoc />
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var categoryProp = property.FindPropertyRelative("category");
        var instantProp = property.FindPropertyRelative("instant");
        var buffProp = property.FindPropertyRelative("buff");

        var y = position.y;
        var line = new Rect(position.x, y, position.width, LineHeight);
        EditorGUI.PropertyField(line, categoryProp, new GUIContent("效果类型"));
        y += LineHeight + VerticalGap;

        var category = (SkillEffectCategory)categoryProp.enumValueIndex;
        if (category == SkillEffectCategory.Instant)
        {
            y = DrawInstantEffect(position.x, position.width, y, instantProp);
        }
        else
        {
            y = DrawBuffEffect(position.x, position.width, y, buffProp);
        }

        EditorGUI.EndProperty();
    }

    /// <inheritdoc />
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var categoryProp = property.FindPropertyRelative("category");
        var category = (SkillEffectCategory)categoryProp.enumValueIndex;
        if (category == SkillEffectCategory.Instant)
        {
            var kindProp = property.FindPropertyRelative("instant.instantKind");
            return GetInstantHeight((SkillInstantKind)kindProp.enumValueIndex);
        }

        return GetBuffHeight();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 绘制即时效果字段。
    /// </summary>
    private static float DrawInstantEffect(float x, float width, float y, SerializedProperty instantProp)
    {
        var kindProp = instantProp.FindPropertyRelative("instantKind");
        var valueProp = instantProp.FindPropertyRelative("value");
        var chanceProp = instantProp.FindPropertyRelative("chancePercent");

        var line = new Rect(x, y, width, LineHeight);
        EditorGUI.PropertyField(line, kindProp, new GUIContent("即时效果"));
        y += LineHeight + VerticalGap;

        var kind = (SkillInstantKind)kindProp.enumValueIndex;
        switch (kind)
        {
            case SkillInstantKind.Damage:
            case SkillInstantKind.Heal:
            case SkillInstantKind.SelfHpDrain:
                line = new Rect(x, y, width, LineHeight);
                EditorGUI.PropertyField(line, valueProp, new GUIContent(GetInstantValueLabel(kind)));
                y += LineHeight + VerticalGap;
                break;
            case SkillInstantKind.RefreshCooldown:
                line = new Rect(x, y, width, LineHeight);
                EditorGUI.PropertyField(line, chanceProp, new GUIContent("刷新概率 (%)"));
                y += LineHeight + VerticalGap;
                break;
        }

        return y;
    }

    /// <summary>
    /// 绘制 Buff 效果字段。
    /// </summary>
    private static float DrawBuffEffect(float x, float width, float y, SerializedProperty buffProp)
    {
        var kindProp = buffProp.FindPropertyRelative("buffKind");
        var valueProp = buffProp.FindPropertyRelative("value");
        var durationProp = buffProp.FindPropertyRelative("durationTurns");

        var line = new Rect(x, y, width, LineHeight);
        EditorGUI.PropertyField(line, kindProp, new GUIContent("Buff 类型"));
        y += LineHeight + VerticalGap;

        var kind = (SkillBuffKind)kindProp.enumValueIndex;
        if (kind == SkillBuffKind.DefenseBuff)
        {
            line = new Rect(x, y, width, LineHeight);
            EditorGUI.PropertyField(line, valueProp, new GUIContent("防御加成"));
            y += LineHeight + VerticalGap;
            line = new Rect(x, y, width, LineHeight);
            EditorGUI.PropertyField(line, durationProp, new GUIContent("持续回合"));
            y += LineHeight + VerticalGap;
        }

        return y;
    }

    /// <summary>
    /// 即时效果区块高度。
    /// </summary>
    private static float GetInstantHeight(SkillInstantKind kind)
    {
        var lines = 2;
        switch (kind)
        {
            case SkillInstantKind.RefreshCooldown:
            case SkillInstantKind.Damage:
            case SkillInstantKind.Heal:
            case SkillInstantKind.SelfHpDrain:
                lines = 3;
                break;
        }

        return lines * LineHeight + (lines - 1) * VerticalGap + 4f;
    }

    /// <summary>
    /// Buff 区块高度。
    /// </summary>
    private static float GetBuffHeight()
    {
        const int lines = 4;
        return lines * LineHeight + (lines - 1) * VerticalGap + 4f;
    }

    /// <summary>
    /// 即时效果 Value 标签。
    /// </summary>
    private static string GetInstantValueLabel(SkillInstantKind kind)
    {
        return kind switch
        {
            SkillInstantKind.Damage => "伤害值",
            SkillInstantKind.Heal => "治疗量",
            SkillInstantKind.SelfHpDrain => "扣除生命",
            _ => "数值"
        };
    }

    #endregion
}
#endif
