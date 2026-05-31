/// <summary>
/// 技能系统核心调度：校验 → 流程事件 → 选目标 → 构建上下文 → 执行 Pipeline（不扣次数、不播特效）。
/// </summary>
public static class SkillSystem
{
    #region Public API

    /// <summary>
    /// 释放技能（Application 传入施法者与可选的手动目标 UnitId）。
    /// </summary>
    public static bool TryCast(
        string casterUnitId,
        SkillConfigSO skillConfig,
        string manualTargetUnitId,
        out SkillCastOutcome outcome)
    {
        outcome = new SkillCastOutcome();
        var battle = BattleContext.Current;
        battle.EnsureReady();

        var caster = battle.Field.GetUnit(casterUnitId);
        if (caster == null || caster.IsDead)
        {
            outcome.Message = "施法者状态无效";
            return false;
        }

        var casterCardId = caster.CardId;
        var field = BattleContext.Current.Field;
        var skillId = field.NormalizeCardId(skillConfig?.SkillId);
        if (!SkillValidator.Validate(caster, skillConfig, out var validateReason))
        {
            outcome.Message = validateReason ?? "技能无法释放";
            return false;
        }

        // 通过 SkillConfigSO 的 SkillId 读取 SkillDefinitionRegistry 里的 注册表
        // 来体现和 Skill Blueprint 的 SkillId 对应关系。
        if (!SkillDefinitionRegistry.TryGet(skillId, out var definition))
        {
            outcome.Message = $"未注册技能定义 skillId={skillId}";
            return false;
        }

        battle.Events.Publish(new SkillCastStartEvent(caster.CardId, skillId));

        var execCtx = new SkillExecutionContext
        {
            Caster = caster,
            SkillConfig = skillConfig,
            SkillId = skillId,
            Battle = battle
        };

        if (!TargetSelector.TrySelect(
                definition.TargetStrategy,
                execCtx,
                manualTargetUnitId,
                out var targetReason))
        {
            outcome.Message = targetReason ?? "目标无效";
            battle.Events.Publish(new SkillCastFinishEvent(caster.CardId, skillId, false, outcome.Message));
            return false;
        }

        if (!SkillPipeline.TryExecute(execCtx, definition.Effects))
        {
            outcome.Message = "技能效果执行失败";
            battle.Events.Publish(new SkillCastFinishEvent(caster.CardId, skillId, false, outcome.Message));
            return false;
        }

        outcome.Success = true;
        outcome.CooldownRefreshed = execCtx.Outcome.CooldownRefreshed;
        outcome.TotalDamageDealt = execCtx.Outcome.TotalDamageDealt;
        outcome.Message = BuildSuccessMessage(outcome);

        battle.Events.Publish(new SkillCastFinishEvent(caster.CardId, skillId, true, outcome.Message));
        return true;
    }

    /// <summary>
    /// 技能是否需要玩家点选目标（供 UI 与门面层使用）。
    /// </summary>
    public static bool RequiresManualTarget(string skillId)
    {
        skillId = BattleContext.Current.Field.NormalizeCardId(skillId);
        if (!SkillDefinitionRegistry.TryGet(skillId, out var definition))
        {
            return false;
        }

        return definition.RequiresManualTarget;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 根据流水线结果生成提示文案。
    /// </summary>
    private static string BuildSuccessMessage(SkillCastOutcome outcome)
    {
        if (outcome == null)
        {
            return string.Empty;
        }

        if (outcome.TotalDamageDealt > 0 && outcome.CooldownRefreshed)
        {
            return $"造成 {outcome.TotalDamageDealt} 点伤害，冷却已刷新";
        }

        if (outcome.TotalDamageDealt > 0)
        {
            return $"造成 {outcome.TotalDamageDealt} 点伤害";
        }

        return outcome.CooldownRefreshed ? "冷却已刷新" : "技能已生效";
    }

    #endregion
}
