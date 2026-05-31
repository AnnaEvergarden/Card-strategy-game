using System;
using UnityEngine;

/// <summary>
/// 即时效果配置：释放技能时立即执行一次。
/// </summary>
[Serializable]
public sealed class InstantEffectEntry : BattleEffectEntry
{
    #region Fields

    /// <summary>
    /// 即时效果类型。
    /// </summary>
    [SerializeField] private SkillInstantKind instantKind = SkillInstantKind.None;

    /// <summary>
    /// 通用数值（伤害、治疗、自损等）。
    /// </summary>
    [SerializeField] private int value;

    /// <summary>
    /// 概率（0～100），用于刷新冷却等。
    /// </summary>
    [Range(0, 100)]
    [SerializeField] private int chancePercent;

    #endregion

    #region Properties

    /// <inheritdoc />
    public override SkillEffectCategory Category => SkillEffectCategory.Instant;

    /// <inheritdoc />
    public override bool IsValid => instantKind != SkillInstantKind.None;

    /// <summary>
    /// 即时效果类型。
    /// </summary>
    public SkillInstantKind InstantKind => instantKind;

    /// <summary>
    /// 数值。
    /// </summary>
    public int Value => value;

    /// <summary>
    /// 概率（0～100）。
    /// </summary>
    public int ChancePercent => Mathf.Clamp(chancePercent, 0, 100);

    #endregion
}
