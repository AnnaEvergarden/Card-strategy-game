/// <summary>
/// 冷却刷新评分器：施法者冷却中的技能越多、刷新概率越高分越高。
/// </summary>
public sealed class CooldownRefreshScoreCalculator : IScoreCalculator
{
    /// <summary>处理冷却刷新效果。</summary>
    public SkillEffectKind Kind => SkillEffectKind.RefreshCooldownChance;

    /// <summary>
    /// 评分：期望刷新数 × 单次价值。
    /// </summary>
    public float Calculate(BattleUnit caster, BattleUnit target, SkillEffectConfig effect)
    {
        if (caster == null || caster.IsDead)
            return 0f;

        var coolingCount = 0;
        foreach (var pair in caster.SkillCooldowns)
        {
            if (pair.Value > 0)
                coolingCount++;
        }

        if (coolingCount == 0)
            return -10f; // 没有冷却中的技能时收益为负（不浪费回合）

        return coolingCount * effect.Chance * 10f;
    }
}
