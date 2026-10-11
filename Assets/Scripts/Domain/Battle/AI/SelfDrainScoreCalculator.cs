/// <summary>
/// 自我损耗评分器：固定负分（技能代价），损耗越重惩罚越大。
/// 与伤害/治疗分共同决定"高收益但带代价"的技能是否值得释放。
/// </summary>
public sealed class SelfDrainScoreCalculator : IScoreCalculator
{
    /// <summary>损耗权重（每点 HP 代价）。</summary>
    private const float DrainWeight = 1.5f;

    /// <summary>处理自我损耗效果。</summary>
    public SkillEffectKind Kind => SkillEffectKind.SelfHpDrain;

    /// <summary>
    /// 评分：固定负分（目标参数为施法者自身）。
    /// </summary>
    public float Calculate(BattleUnit caster, BattleUnit target, SkillEffectConfig effect)
    {
        if (caster == null || caster.IsDead)
            return 0f;

        // 损耗会致死时直接否决（AI 不会自杀）。
        if (effect.Value >= caster.Hp)
            return -1000f;

        return -effect.Value * DrainWeight;
    }
}
