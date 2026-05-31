/// <summary>
/// 全体己方：当前上场所有存活友方。
/// </summary>
public sealed class AllAlliesTargetStrategy : ITargetSelectionStrategy
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

        BattleTargetCollector.CollectAllAllies(context);
        if (context.Targets.Count == 0)
        {
            failureReason = "没有可选友方目标";
            return false;
        }

        return true;
    }

    #endregion
}
