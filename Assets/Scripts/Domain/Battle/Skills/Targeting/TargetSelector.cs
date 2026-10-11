using System.Collections.Generic;

/// <summary>
/// 目标选择器：按技能的 SkillTargetKind 分发到对应策略（策略模式入口）。
/// 新增目标规则 = 新策略类 + 在构造时注册，不修改本类与技能系统。
/// </summary>
public sealed class TargetSelector
{
    private readonly Dictionary<SkillTargetKind, ITargetSelectionStrategy> _strategies =
        new Dictionary<SkillTargetKind, ITargetSelectionStrategy>();

    /// <summary>
    /// 构造目标选择器并注册默认策略。
    /// </summary>
    public TargetSelector()
    {
        Register(new SingleEnemyTargetStrategy());
        Register(new SingleAllyTargetStrategy());
        Register(new AllEnemiesTargetStrategy());
        Register(new AllAlliesTargetStrategy());
        Register(new SelfTargetStrategy());
        Register(new AllTargetsStrategy());
        Register(new NoTargetStrategy());
    }

    /// <summary>
    /// 注册/替换目标策略。
    /// </summary>
    public void Register(ITargetSelectionStrategy strategy)
    {
        if (strategy != null)
            _strategies[strategy.Kind] = strategy;
    }

    /// <summary>
    /// 是否需要玩家手动选择目标（两段式选目标 UI 依据：先选技能 → 高亮合法目标 → 点选确认）。
    /// </summary>
    public bool RequiresManualTarget(SkillConfig skill)
    {
        return skill != null
            && (skill.TargetKind == SkillTargetKind.SingleEnemy || skill.TargetKind == SkillTargetKind.SingleAlly);
    }

    /// <summary>
    /// 选取技能目标。
    /// </summary>
    /// <param name="context">技能执行上下文</param>
    /// <param name="skill">技能配置</param>
    /// <param name="manualTargetUnitId">手动目标（无需手动选择的策略忽略）</param>
    /// <param name="targets">输出：目标列表</param>
    /// <param name="failureReason">输出：失败原因</param>
    public bool TrySelectTargets(SkillExecutionContext context, SkillConfig skill, string manualTargetUnitId,
        out IReadOnlyList<BattleUnit> targets, out string failureReason)
    {
        targets = null;
        failureReason = string.Empty;

        if (skill == null || !_strategies.TryGetValue(skill.TargetKind, out var strategy))
        {
            failureReason = $"未知目标类型：{skill?.TargetKind}。";
            return false;
        }

        return strategy.TrySelectTargets(context, manualTargetUnitId, out targets, out failureReason);
    }
}
