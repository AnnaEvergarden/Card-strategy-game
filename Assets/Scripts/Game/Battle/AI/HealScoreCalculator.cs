using UnityEngine;

/// <summary>
/// 治疗效果评分计算器：有效治疗量 × HealWeight，低血量目标触发 SurvivalMultiplier。
/// </summary>
public sealed class HealScoreCalculator : IScoreCalculator
{
    public float Calculate(int effectValue, string casterUnitId, string targetUnitId, AIProfileSO profile)
    {
        if (string.IsNullOrWhiteSpace(targetUnitId))
        {
            return 0f;
        }

        var field = BattleContext.Current?.Field;
        if (field == null)
        {
            return 0f;
        }

        var unit = field.GetUnit(targetUnitId);
        if (unit == null)
        {
            return 0f;
        }

        // 用 cardId 读取配置的 MaxHp（仅 AI 评分阶段需要，后续执行阶段从 BattleUnit 实例读）
        var targetMaxHp = BattleUtility.ResolveMaxHp(unit.CardId);
        if (targetMaxHp <= 0)
        {
            return 0f;
        }

        var missingHp = Mathf.Max(0, targetMaxHp - unit.Hp);
        if (missingHp <= 0)
        {
            return 0f;
        }

        var effectiveHeal = Mathf.Min(effectValue, missingHp);
        var baseScore = effectiveHeal * profile.HealWeight;
        var hpRatio = (float)unit.Hp / targetMaxHp;

        if (hpRatio < profile.SurvivalThreshold)
        {
            baseScore *= profile.SurvivalMultiplier;
        }

        return baseScore;
    }
}
