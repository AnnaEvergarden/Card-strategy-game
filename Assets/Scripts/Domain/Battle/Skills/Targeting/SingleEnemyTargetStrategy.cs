using System.Collections.Generic;

/// <summary>
/// 单体敌方目标策略：手动选择 1 名存活且在场上的敌方单位。
/// </summary>
public sealed class SingleEnemyTargetStrategy : ITargetSelectionStrategy
{
    /// <summary>处理单体敌方目标类型。</summary>
    public SkillTargetKind Kind => SkillTargetKind.SingleEnemy;

    /// <summary>
    /// 选取手动指定的敌方目标。
    /// </summary>
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason)
    {
        targets = null;
        failureReason = string.Empty;

        if (string.IsNullOrEmpty(manualTargetUnitId))
        {
            failureReason = "请选择攻击目标。";
            return false;
        }

        var target = context.Field.GetUnit(manualTargetUnitId);
        if (target == null || target.Side == context.Caster.Side)
        {
            failureReason = "目标必须为敌方单位。";
            return false;
        }

        if (target.IsDead)
        {
            failureReason = "目标已阵亡。";
            return false;
        }

        if (!context.Field.TryGetSlotOfUnit(manualTargetUnitId, out _, out _))
        {
            failureReason = "目标不在场上。";
            return false;
        }

        targets = new List<BattleUnit> { target };
        return true;
    }
}
