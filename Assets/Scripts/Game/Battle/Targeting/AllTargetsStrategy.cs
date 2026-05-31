/// <summary>
/// 全体目标：场上所有存活单位（敌我双方）。
/// </summary>
public sealed class AllTargetsStrategy : ITargetSelectionStrategy
{
    #region Properties

    /// <inheritdoc />
    public bool RequiresManualTarget => false;

    #endregion

    #region Public API

    /// <inheritdoc />
    public bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId, out string failureReason)
    {
        failureReason = null;
        if (context == null)
        {
            failureReason = "技能上下文无效";
            return false;
        }

        BattleTargetCollector.CollectAllAlive(context);
        if (context.Targets.Count == 0)
        {
            failureReason = "场上没有可选目标";
            return false;
        }

        return true;
    }

    #endregion
}
