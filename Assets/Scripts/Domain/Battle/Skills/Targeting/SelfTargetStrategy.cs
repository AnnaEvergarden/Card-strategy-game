using System.Collections.Generic;

/// <summary>
/// 自身目标策略：目标固定为施法者（无需手动选择）。
/// </summary>
public sealed class SelfTargetStrategy : ITargetSelectionStrategy
{
    /// <summary>处理自身目标类型。</summary>
    public SkillTargetKind Kind => SkillTargetKind.Self;

    /// <summary>
    /// 选取施法者自身。
    /// </summary>
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason)
    {
        targets = new List<BattleUnit> { context.Caster };
        failureReason = string.Empty;
        return true;
    }
}
