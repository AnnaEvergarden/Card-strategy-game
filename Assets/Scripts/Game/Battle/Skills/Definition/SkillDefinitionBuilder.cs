/// <summary>
/// 从 <see cref="SkillConfigSO"/> 效果列表构建 <see cref="SkillDefinition"/>。
/// </summary>
public static class SkillDefinitionBuilder
{
    #region Public API

    /// <summary>
    /// 根据技能配置 SO 构建可注册定义；无有效效果时返回 null。
    /// </summary>
    public static SkillDefinition TryBuild(SkillConfigSO config)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.SkillId) || !config.HasExecutableEffects)
        {
            return null;
        }

        var effects = SkillEffectFactory.BuildPipelineFromEffectList(config.EffectList);
        if (effects == null || effects.Length == 0)
        {
            return null;
        }

        return new SkillDefinition(
            config.SkillId,
            SkillTargetStrategyFactory.Get(config.TargetKind),
            effects);
    }

    #endregion
}
