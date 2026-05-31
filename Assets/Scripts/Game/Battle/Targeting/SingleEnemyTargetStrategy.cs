/// <summary>
/// 单体敌方目标策略：目标须存在、存活且与施法者不同阵营。
/// 使用 UnitId 唯一标识，不依赖 cardId+side 组合定位。
/// </summary>
public sealed class SingleEnemyTargetStrategy : ITargetSelectionStrategy
{
    #region Properties

    /// <inheritdoc />
    public bool RequiresManualTarget => true;

    #endregion

    #region Public API

    /// <inheritdoc />
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId, out string failureReason)
    {
        failureReason = null;
        context?.ClearTargets();

        if (context == null)
        {
            failureReason = "技能上下文无效";
            return false;
        }

        if (string.IsNullOrWhiteSpace(manualTargetUnitId))
        {
            failureReason = "未选择目标";
            return false;
        }

        var battle = context.Battle ?? BattleContext.Current;
        var target = battle.Field.GetUnit(manualTargetUnitId);
        if (target == null || target.IsDead)
        {
            failureReason = "目标状态无效";
            return false;
        }

        if (target.Side == context.Caster?.Side)
        {
            failureReason = "不能选择己方单位";
            return false;
        }

        context.AddTarget(target);
        return true;
    }

    #endregion
}
