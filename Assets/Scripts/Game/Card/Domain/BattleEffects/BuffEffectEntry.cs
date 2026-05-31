using System;
using UnityEngine;

/// <summary>
/// 持续 Buff 配置：挂载后跨回合生效（具体 Tick 由 Buff 系统消费）。
/// </summary>
[Serializable]
public sealed class BuffEffectEntry : BattleEffectEntry
{
    #region Fields

    /// <summary>
    /// Buff 类型。
    /// </summary>
    [SerializeField] private SkillBuffKind buffKind = SkillBuffKind.None;

    /// <summary>
    /// 数值（如防御加成）。
    /// </summary>
    [SerializeField] private int value;

    /// <summary>
    /// 持续回合数。
    /// </summary>
    [SerializeField] private int durationTurns = 1;

    #endregion

    #region Properties

    /// <inheritdoc />
    public override SkillEffectCategory Category => SkillEffectCategory.Buff;

    /// <inheritdoc />
    public override bool IsValid => buffKind != SkillBuffKind.None;

    /// <summary>
    /// Buff 类型。
    /// </summary>
    public SkillBuffKind BuffKind => buffKind;

    /// <summary>
    /// 数值。
    /// </summary>
    public int Value => value;

    /// <summary>
    /// 持续回合。
    /// </summary>
    public int DurationTurns => Mathf.Max(1, durationTurns);

    #endregion
}
