using System.Collections.Generic;

/// <summary>
/// 全体友方目标策略：自动选取全部存活且在场上的友方单位（含施法者自身，无需手动选择）。
/// </summary>
public sealed class AllAlliesTargetStrategy : ITargetSelectionStrategy
{
    /// <summary>处理全体友方目标类型。</summary>
    public SkillTargetKind Kind => SkillTargetKind.AllAllies;

    /// <summary>
    /// 自动选取全部在场存活的友方单位。
    /// </summary>
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason)
    {
        targets = AllEnemiesTargetStrategy.FilterAlive(context.Field.GetActiveUnits(context.Caster.Side));
        failureReason = string.Empty;

        if (targets.Count == 0)
        {
            failureReason = "场上没有存活友方单位。";
            return false;
        }

        return true;
    }
}
