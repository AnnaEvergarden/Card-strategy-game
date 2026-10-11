/// <summary>
/// 治疗评分器：回复量越高分越高，满血目标视为浪费（负分惩罚）。
/// </summary>
public sealed class HealScoreCalculator : IScoreCalculator
{
    /// <summary>满血目标的治疗浪费惩罚。</summary>
    private const float WastedPenalty = -15f;

    /// <summary>处理治疗效果。</summary>
    public SkillEffectKind Kind => SkillEffectKind.Heal;

    /// <summary>
    /// 评分：实际回复量 × 权重；无法回复时给负分避免浪费回合。
    /// </summary>
    public float Calculate(BattleUnit caster, BattleUnit target, SkillEffectConfig effect)
    {
        if (target == null || target.IsDead)
            return 0f;

        var missing = target.MaxHp - target.Hp;
        if (missing <= 0)
            return WastedPenalty;

        var healed = effect.Value < missing ? effect.Value : missing;
        return healed * 1.2f;
    }
}
