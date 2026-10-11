using System.Collections.Generic;

/// <summary>
/// 技能管线：按配置顺序执行技能的全部效果。
/// 全部成功才算释放成功；任一效果失败 → 逆序回滚已生效效果 → 整体失败。
/// 技能次数与冷却是管线成功后的结算（SkillSystem 负责），因此失败时不会"扣了次数但效果不完整"。
/// </summary>
public sealed class SkillPipeline
{
    private readonly SkillEffectRegistry _effectRegistry;

    /// <summary>
    /// 构造技能管线。
    /// </summary>
    /// <param name="effectRegistry">效果注册表</param>
    public SkillPipeline(SkillEffectRegistry effectRegistry)
    {
        _effectRegistry = effectRegistry ?? new SkillEffectRegistry();
    }

    /// <summary>
    /// 执行效果列表。
    /// </summary>
    /// <param name="context">技能执行上下文（成功效果已在其中登记回滚记录）</param>
    /// <param name="effects">效果配置列表（按序执行）</param>
    /// <param name="failureReason">输出：失败原因</param>
    /// <returns>是否全部成功</returns>
    public bool TryExecute(SkillExecutionContext context, IReadOnlyList<SkillEffectConfig> effects,
        out string failureReason)
    {
        failureReason = string.Empty;

        for (var i = 0; i < effects.Count; i++)
        {
            var config = effects[i];
            if (config == null || config.Kind == SkillEffectKind.None)
                continue;

            var effect = _effectRegistry.TryGet(config.Kind);
            if (effect == null)
            {
                failureReason = $"效果 {config.Kind} 未注册。";
                context.RollbackAll();
                return false;
            }

            if (effect.Execute(config, context, out var effectFailure))
                continue;

            // 任一效果失败：逆序回滚已生效效果后整体失败。
            failureReason = effectFailure;
            context.RollbackAll();
            return false;
        }

        return true;
    }
}
