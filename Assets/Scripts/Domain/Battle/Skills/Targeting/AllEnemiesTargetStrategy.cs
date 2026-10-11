using System.Collections.Generic;

/// <summary>
/// 全体敌方目标策略：自动选取全部存活且在场上的敌方单位（无需手动选择）。
/// </summary>
public sealed class AllEnemiesTargetStrategy : ITargetSelectionStrategy
{
    /// <summary>处理全体敌方目标类型。</summary>
    public SkillTargetKind Kind => SkillTargetKind.AllEnemies;

    /// <summary>
    /// 自动选取全部在场存活的敌方单位。
    /// </summary>
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason)
    {
        targets = FilterAlive(context.Field.GetActiveUnits(GetEnemySide(context.Caster.Side)));
        failureReason = string.Empty;

        if (targets.Count == 0)
        {
            failureReason = "场上没有存活敌方单位。";
            return false;
        }

        return true;
    }

    /// <summary>过滤出存活单位（阵亡但尚未清理的单位不可作为目标）。</summary>
    internal static List<BattleUnit> FilterAlive(IReadOnlyList<BattleUnit> units)
    {
        var alive = new List<BattleUnit>(units.Count);
        for (var i = 0; i < units.Count; i++)
        {
            if (!units[i].IsDead)
                alive.Add(units[i]);
        }
        return alive;
    }

    /// <summary>获取对立阵营。</summary>
    internal static BattleSide GetEnemySide(BattleSide side)
    {
        return side == BattleSide.P1 ? BattleSide.P2 : BattleSide.P1;
    }
}
