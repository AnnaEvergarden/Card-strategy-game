using System.Collections.Generic;

/// <summary>
/// 技能定义：技能 = 目标策略 + Effect 流水线配置（非 per-skillId 巨型类）。
/// </summary>
public sealed class SkillDefinition
{
    #region Fields

    /// <summary>
    /// 目标选择策略。
    /// </summary>
    private readonly ITargetSelectionStrategy _targetStrategy;

    /// <summary>
    /// 效果列表。
    /// </summary>
    private readonly IReadOnlyList<ISkillEffect> _effects;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造技能定义。
    /// </summary>
    public SkillDefinition(
        string skillId,
        ITargetSelectionStrategy targetStrategy,
        IReadOnlyList<ISkillEffect> effects)
    {
        SkillId = skillId ?? string.Empty;
        _targetStrategy = targetStrategy;
        _effects = effects ?? System.Array.Empty<ISkillEffect>();
    }

    #endregion

    #region Properties

    /// <summary>
    /// 技能 id（与 <see cref="SkillConfigSO.SkillId"/> 一致）。
    /// </summary>
    public string SkillId { get; }

    /// <summary>
    /// 目标策略。
    /// </summary>
    public ITargetSelectionStrategy TargetStrategy => _targetStrategy;

    /// <summary>
    /// 是否需要玩家点选目标。
    /// </summary>
    public bool RequiresManualTarget => _targetStrategy != null && _targetStrategy.RequiresManualTarget;

    /// <summary>
    /// Effect 流水线。
    /// </summary>
    public IReadOnlyList<ISkillEffect> Effects => _effects;

    #endregion
}
