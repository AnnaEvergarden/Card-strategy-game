using UnityEngine;

/// <summary>
/// 伤害效果评分计算器：有效伤害 × DamageWeight + 击杀奖励。
/// </summary>
public sealed class DamageScoreCalculator : IScoreCalculator
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

        var effectiveDamage = Mathf.Max(0, effectValue - unit.Defense);
        var baseScore = effectiveDamage * profile.DamageWeight;
        var killBonus = effectiveDamage >= unit.Hp ? profile.KillBonus : 0f;
        return baseScore + killBonus;
    }
}
