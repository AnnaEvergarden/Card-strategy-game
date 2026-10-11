/// <summary>
/// AI 评分器接口：按效果种类为"单位 × 技能效果 × 目标"组合打分（Utility AI）。
/// 新增效果种类时：实现本接口 + 注册到 ScoreCalculatorRegistry，AI 自动获得评估能力。
/// 评分为启发式权重，与效果执行逻辑完全解耦——调整 AI 优先级不需要改战斗规则。
/// </summary>
public interface IScoreCalculator
{
    /// <summary>本评分器处理的效果种类。</summary>
    SkillEffectKind Kind { get; }

    /// <summary>
    /// 计算该效果在当前局势下的价值。
    /// </summary>
    /// <param name="caster">施法单位</param>
    /// <param name="target">评分目标（作用于施法者自身的效果，目标即施法者）</param>
    /// <param name="effect">效果配置</param>
    /// <returns>评分（可为负：代价类效果）</returns>
    float Calculate(BattleUnit caster, BattleUnit target, SkillEffectConfig effect);
}
