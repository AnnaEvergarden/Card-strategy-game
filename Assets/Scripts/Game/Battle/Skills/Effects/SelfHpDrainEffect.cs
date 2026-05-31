using UnityEngine;

/// <summary>
/// 扣除施法者自身生命值（经 <see cref="BattleUnit.TakeDamage"/> 修改 HP；事件在流水线成功后发布）。
/// </summary>
public sealed class SelfHpDrainEffect : ISkillEffect
{
    #region Fields

    /// <summary>
    /// 扣除生命量。
    /// </summary>
    private readonly int _hpCost;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造自损生命 Effect。
    /// </summary>
    public SelfHpDrainEffect(int hpCost)
    {
        _hpCost = Mathf.Max(0, hpCost);
    }

    #endregion

    #region Public API

    /// <inheritdoc />
    public bool TryExecute(SkillExecutionContext context, out ISkillEffectRollback rollback)
    {
        rollback = null;
        if (_hpCost <= 0)
        {
            return true;
        }

        if (context?.Caster == null)
        {
            return false;
        }

        var applied = context.Caster.TakeDamage(_hpCost);
        if (applied > 0)
        {
            var hpRollback = SkillEffectRollbackRecords.CreateHpRollback();
            hpRollback.Record(context.Caster.UnitId, -applied);
            rollback = hpRollback;
        }

        return true;
    }

    #endregion
}
