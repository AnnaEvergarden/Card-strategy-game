/// <summary>
/// 目标选择入口：按技能定义中的策略解析目标列表。
/// </summary>
public static class TargetSelector
{
    #region Public API

    /// <summary>
    /// 执行目标选择并写入上下文。
    /// </summary>
    public static bool TrySelect(
        ITargetSelectionStrategy strategy,
        SkillExecutionContext context,
        string manualTargetUnitId,
        out string failureReason)
    {
        failureReason = null;
        if (strategy == null || context == null)
        {
            failureReason = "目标策略无效";
            return false;
        }

        return strategy.TrySelectTargets(context, manualTargetUnitId, out failureReason);
    }

    #endregion
}
