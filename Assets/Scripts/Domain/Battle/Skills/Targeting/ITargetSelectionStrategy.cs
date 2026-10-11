using System.Collections.Generic;

/// <summary>
/// 目标选取策略接口：按 SkillTargetKind 选取技能目标（策略模式）。
/// 新增目标规则 = 新策略类 + 注册到 TargetSelector，不修改技能系统。
/// </summary>
public interface ITargetSelectionStrategy
{
    /// <summary>本策略处理的目标类型。</summary>
    SkillTargetKind Kind { get; }

    /// <summary>
    /// 选取目标。
    /// </summary>
    /// <param name="context">技能执行上下文（施法者与战场）</param>
    /// <param name="manualTargetUnitId">手动选择的目标（需要手动选目标的策略必填）</param>
    /// <param name="targets">输出：目标列表</param>
    /// <param name="failureReason">输出：失败原因</param>
    /// <returns>是否选取成功</returns>
    bool TrySelectTargets(SkillExecutionContext context, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason);
}
