/// <summary>
/// 全体敌方：场上所有存活敌方单位。
/// </summary>
public sealed class AllEnemiesTargetStrategy : ITargetSelectionStrategy
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

        BattleTargetCollector.CollectAllEnemies(context);
        if (context.Targets.Count == 0)
        {
            failureReason = "没有可选敌方目标";
            return false;
        }

        return true;
    }

    #endregion
}
