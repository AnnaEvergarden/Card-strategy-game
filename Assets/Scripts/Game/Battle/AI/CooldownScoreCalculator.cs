using UnityEngine;

/// <summary>
/// 冷却刷新效果评分计算器：若施法者有任何技能处于冷却，概率 × RefreshCooldownWeight。
/// </summary>
public sealed class CooldownScoreCalculator : IScoreCalculator
{
    public float Calculate(int effectValue, string casterUnitId, string targetUnitId, AIProfileSO profile)
    {
        if (effectValue <= 0 || profile.RefreshCooldownWeight <= 0f || string.IsNullOrWhiteSpace(casterUnitId))
        {
            return 0f;
        }

        var field = BattleContext.Current?.Field;
        if (field == null)
        {
            return 0f;
        }

        var caster = field.GetUnit(casterUnitId);
        if (caster == null)
        {
            return 0f;
        }

        if (!AnySkillOnCooldown(field, caster))
        {
            return 0f;
        }

        return (effectValue / 100f) * profile.RefreshCooldownWeight;
    }

    private static bool AnySkillOnCooldown(BattleFieldState field, BattleUnit caster)
    {
        var skills = new System.Collections.Generic.List<SkillConfigSO>(CardConfigSO.MaxSkillsPerCard);
        var faction = BattleUtility.GetCardFaction(caster.CardId);
        CardSkillQuery.ResolveSkillsForCard(caster.CardId, faction, skills);

        for (var i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill != null && !string.IsNullOrWhiteSpace(skill.SkillId)
                              && caster.GetSkillCooldownRemaining(skill.SkillId) > 0)
            {
                return true;
            }
        }
        return false;
    }
}
