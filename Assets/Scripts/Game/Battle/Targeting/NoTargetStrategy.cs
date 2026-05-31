/// <summary>
/// 无目标策略：效果仅作用于施法者或全局，不填充 Targets。
/// </summary>
public sealed class NoTargetStrategy : ITargetSelectionStrategy
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
        context?.ClearTargets();
        return context != null;
    }

    #endregion
}
