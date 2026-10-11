using System;

/// <summary>
/// 技能系统：技能释放总入口（校验 → 目标选取 → 效果管线 → 次数/冷却结算）。
/// 只负责调度释放流程，不直接修改任何单位状态——状态修改由各 Effect 与 BattleUnit 完成。
/// 玩家与 AI 共用同一释放管道，保证规则完全一致。
/// </summary>
public sealed class SkillSystem
{
    private readonly BattleField _field;
    private readonly IBattleEventBus _eventBus;
    private readonly SkillValidator _validator;
    private readonly TargetSelector _targetSelector;
    private readonly SkillPipeline _pipeline;
    private readonly Random _random;

    /// <summary>
    /// 构造技能系统。
    /// </summary>
    /// <param name="field">战场</param>
    /// <param name="eventBus">战斗事件总线</param>
    /// <param name="validator">合法性校验器</param>
    /// <param name="targetSelector">目标选择器</param>
    /// <param name="pipeline">效果管线</param>
    /// <param name="random">随机源（可播种，供测试确定性）</param>
    public SkillSystem(BattleField field, IBattleEventBus eventBus, SkillValidator validator,
        TargetSelector targetSelector, SkillPipeline pipeline, Random random)
    {
        _field = field;
        _eventBus = eventBus;
        _validator = validator ?? new SkillValidator();
        _targetSelector = targetSelector ?? new TargetSelector();
        _pipeline = pipeline ?? new SkillPipeline(new SkillEffectRegistry());
        _random = random ?? new Random();
    }

    /// <summary>
    /// 该技能是否需要手动选择目标（供应用层决定是否进入选目标模式）。
    /// </summary>
    public bool RequiresManualTarget(SkillConfig skill)
    {
        return _targetSelector.RequiresManualTarget(skill);
    }

    /// <summary>
    /// 尝试释放技能：完整的"校验 → 发布开始 → 选目标 → 管线 → 结算"流程。
    /// 失败时已生效效果会被逆序回滚，次数与冷却不消耗。
    /// </summary>
    /// <param name="casterUnitId">施法单位 UnitId</param>
    /// <param name="skill">技能配置</param>
    /// <param name="manualTargetUnitId">手动目标（需要手动选目标的技能必填）</param>
    /// <param name="outcome">输出：释放结果</param>
    /// <returns>是否成功</returns>
    public bool TryCast(string casterUnitId, SkillConfig skill, string manualTargetUnitId,
        out SkillCastOutcome outcome)
    {
        outcome = null;

        var caster = _field.GetUnit(casterUnitId);
        if (caster == null)
        {
            outcome = SkillCastOutcome.Fail(SkillCastResult.ValidationFailed, "施法者不存在。");
            PublishFinished(casterUnitId, skill, false, outcome.FailureReason);
            return false;
        }

        // 1. 前置校验。
        if (!_validator.Validate(caster, skill, out var validationFailure))
        {
            outcome = SkillCastOutcome.Fail(SkillCastResult.ValidationFailed, validationFailure);
            PublishFinished(casterUnitId, skill, false, outcome.FailureReason);
            return false;
        }

        // 2. 施法者必须绑在场上槽位（替补与已阵亡者不能施法）。
        if (!_field.TryGetSlotOfUnit(casterUnitId, out _, out _))
        {
            outcome = SkillCastOutcome.Fail(SkillCastResult.ValidationFailed, "施法者不在场上。");
            PublishFinished(casterUnitId, skill, false, outcome.FailureReason);
            return false;
        }

        // 3. 发布开始事件（表现层播放起手动画）。
        _eventBus?.Publish(new SkillCastStartedEvent(casterUnitId, caster.Side,
            skill.SkillId, skill.DisplayName));

        // 4. 选取目标。
        var context = new SkillExecutionContext(caster, null, skill, _field, _random);
        if (!_targetSelector.TrySelectTargets(context, skill, manualTargetUnitId,
                out var targets, out var targetFailure))
        {
            outcome = SkillCastOutcome.Fail(SkillCastResult.TargetSelectionFailed, targetFailure);
            PublishFinished(casterUnitId, skill, false, outcome.FailureReason);
            return false;
        }

        // 5. 执行效果管线。
        var pipelineContext = new SkillExecutionContext(caster, targets, skill, _field, _random);
        if (!_pipeline.TryExecute(pipelineContext, skill.Effects, out var pipelineFailure))
        {
            outcome = SkillCastOutcome.Fail(SkillCastResult.PipelineFailed, pipelineFailure);
            PublishFinished(casterUnitId, skill, false, outcome.FailureReason);
            return false;
        }

        // 6. 结算次数与冷却（管线成功后才扣，失败不扣）。
        caster.TryConsumeSkillUse(skill.SkillId);
        if (skill.Cooldown > 0)
            caster.SetSkillCooldown(skill.SkillId, skill.Cooldown);

        outcome = SkillCastOutcome.Ok();
        PublishFinished(casterUnitId, skill, true, string.Empty);
        return true;
    }

    /// <summary>
    /// 发布技能结束事件。
    /// </summary>
    private void PublishFinished(string casterUnitId, SkillConfig skill, bool success, string failureReason)
    {
        _eventBus?.Publish(new SkillCastFinishedEvent(casterUnitId, skill?.SkillId ?? string.Empty,
            success, failureReason));
    }
}
