using UnityEngine;

/// <summary>
/// 防御 Buff 评分计算器：防御值 × DefenseWeight，低血量目标触发 SurvivalMultiplier。
/// </summary>
public sealed class DefenseBuffScoreCalculator : IScoreCalculator
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

        var targetMaxHp = BattleUtility.ResolveMaxHp(unit.CardId);
        if (targetMaxHp <= 0)
        {
            return effectValue * profile.DefenseWeight;
        }

        var baseScore = effectValue * profile.DefenseWeight;
        var hpRatio = (float)unit.Hp / targetMaxHp;

        if (hpRatio < profile.SurvivalThreshold)
        {
            baseScore *= profile.SurvivalMultiplier;
        }

        return baseScore;
    }
}
