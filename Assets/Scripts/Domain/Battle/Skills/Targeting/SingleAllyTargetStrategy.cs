using System.Collections.Generic;

/// <summary>
/// 单体友方目标策略：手动选择 1 名存活且在场上的友方单位（含施法者自身）。
/// </summary>
public sealed class SingleAllyTargetStrategy : ITargetSelectionStrategy
{
    /// <summary>处理单体友方目标类型。</summary>
    public SkillTargetKind Kind => SkillTargetKind.SingleAlly;

    /// <summary>
    /// 选取手动指定的友方目标。
    /// </summary>
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason)
    {
        targets = null;
        failureReason = string.Empty;

        if (string.IsNullOrEmpty(manualTargetUnitId))
        {
            failureReason = "请选择友方目标。";
            return false;
        }

        var target = context.Field.GetUnit(manualTargetUnitId);
        if (target == null || target.Side != context.Caster.Side)
        {
            failureReason = "目标必须为友方单位。";
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
