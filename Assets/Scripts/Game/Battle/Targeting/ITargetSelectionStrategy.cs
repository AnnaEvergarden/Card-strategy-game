/// <summary>
/// 目标选择策略：单体敌方、无目标等。
/// </summary>
public interface ITargetSelectionStrategy
{
    #region Methods

    /// <summary>
    /// 是否需要玩家在 UI 上点选目标。
    /// </summary>
    bool RequiresManualTarget { get; }

    /// <summary>
    /// 解析目标列表写入 <paramref name="context"/>。
    /// </summary>
    /// <param name="context">技能执行上下文。</param>
    /// <param name="manualTargetUnitId">玩家点选的目标 UnitId（无目标技能可为空）。</param>
    /// <param name="failureReason">失败原因。</param>
    /// <returns>是否成功。</returns>
    bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId, out string failureReason);

    #endregion
}
