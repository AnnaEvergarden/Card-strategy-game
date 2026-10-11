/// <summary>
/// 防御强化评分器：目标越残血、增益总量越大分越高；满血目标价值低。
/// </summary>
public sealed class DefenseBuffScoreCalculator : IScoreCalculator
{
    /// <summary>处理防御强化效果。</summary>
    public SkillEffectKind Kind => SkillEffectKind.DefenseBuff;

    /// <summary>
    /// 评分：按目标损血比例加权（残血承伤位更值得强化），满血目标收益趋近 0。
    /// </summary>
    public float Calculate(BattleUnit caster, BattleUnit target, SkillEffectConfig effect)
    {
        if (target == null || target.IsDead)
            return 0f;

        var missingRatio = 1f - (float)target.Hp / target.MaxHp; // 0 满血 → 1 濒死
        return effect.Value * effect.Duration * missingRatio * 0.8f;
    }
}
