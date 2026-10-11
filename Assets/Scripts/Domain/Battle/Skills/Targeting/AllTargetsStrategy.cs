using System.Collections.Generic;

/// <summary>
/// 全体目标策略：自动选取场上全部存活单位（双方，无需手动选择）。
/// </summary>
public sealed class AllTargetsStrategy : ITargetSelectionStrategy
{
    /// <summary>处理全体目标类型。</summary>
    public SkillTargetKind Kind => SkillTargetKind.AllTargets;

    /// <summary>
    /// 自动选取场上全部存活单位（双方合计）。
    /// </summary>
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason)
    {
        var alive = new List<BattleUnit>();
        alive.AddRange(AllEnemiesTargetStrategy.FilterAlive(context.Field.GetActiveUnits(BattleSide.P1)));
        alive.AddRange(AllEnemiesTargetStrategy.FilterAlive(context.Field.GetActiveUnits(BattleSide.P2)));

        targets = alive;
        failureReason = string.Empty;

        if (alive.Count == 0)
        {
            failureReason = "场上没有存活单位。";
            return false;
        }

        return true;
    }
}
