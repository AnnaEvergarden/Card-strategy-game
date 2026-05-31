using UnityEngine;

/// <summary>
/// 自伤效果评分计算器：-实际自损 × SelfDamagePenalty（纯惩罚项）。
/// </summary>
public sealed class SelfDrainScoreCalculator : IScoreCalculator
{
    public float Calculate(int effectValue, string casterUnitId, string targetUnitId, AIProfileSO profile)
    {
        if (string.IsNullOrWhiteSpace(casterUnitId))
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

        var actualDrain = Mathf.Min(effectValue, caster.Hp);
        return -actualDrain * profile.SelfDamagePenalty;
    }
}
