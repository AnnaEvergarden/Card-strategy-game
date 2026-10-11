using System.Collections.Generic;

/// <summary>
/// 无目标策略：目标列表为空（效果自行决定作用对象，如仅影响冷却的技能）。
/// </summary>
public sealed class NoTargetStrategy : ITargetSelectionStrategy
{
    /// <summary>处理无目标类型。</summary>
    public SkillTargetKind Kind => SkillTargetKind.None;

    /// <summary>
    /// 返回空目标列表（恒成功）。
    /// </summary>
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason)
    {
        targets = new List<BattleUnit>(0);
        failureReason = string.Empty;
        return true;
    }
}
