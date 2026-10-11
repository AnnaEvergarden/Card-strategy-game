/// <summary>
/// 伤害评分器：期望伤害越高分越高，可击杀时大幅加分。
/// </summary>
public sealed class DamageScoreCalculator : IScoreCalculator
{
    /// <summary>击杀额外加分（优先终结残血单位）。</summary>
    private const float KillBonus = 50f;

    /// <summary>处理伤害效果。</summary>
    public SkillEffectKind Kind => SkillEffectKind.Damage;

    /// <summary>
    /// 评分：经防御后实际伤害 + 击杀加成。
    /// </summary>
    public float Calculate(BattleUnit caster, BattleUnit target, SkillEffectConfig effect)
    {
        if (target == null || target.IsDead)
            return 0f;

        var effective = effect.Value - target.Defense;
        if (effective < 0)
            effective = 0;

        var applied = effective < target.Hp ? effective : target.Hp;

        var score = applied * 1f;
        if (effective >= target.Hp && target.Hp > 0)
            score += KillBonus; // 本次可击杀

        return score;
    }
}
