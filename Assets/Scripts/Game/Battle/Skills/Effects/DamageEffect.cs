using UnityEngine;

/// <summary>
/// 伤害效果：对目标造成固定数值伤害（<see cref="InstantEffectEntry.Value"/>）。
/// </summary>
public sealed class DamageEffect : ISkillEffect
{
    #region Fields

    /// <summary>
    /// 伤害数值。
    /// </summary>
    private readonly int _damage;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造固定伤害 Effect。
    /// </summary>
    public DamageEffect(int damage)
    {
        _damage = Mathf.Max(0, damage);
    }

    #endregion

    #region Public API

    /// <inheritdoc />
    public bool TryExecute(SkillExecutionContext context, out ISkillEffectRollback rollback)
    {
        rollback = null;
        if (context == null || _damage <= 0 || context.Targets == null)
        {
            return context != null;
        }

        var hpRollback = SkillEffectRollbackRecords.CreateHpRollback();
        var hasChange = false;

        for (var i = 0; i < context.Targets.Count; i++)
        {
            var target = context.Targets[i];
            if (target == null || target.IsDead)
            {
                continue;
            }

            var applied = target.TakeDamage(_damage);
            if (applied > 0)
            {
                hpRollback.Record(target.UnitId, -applied);
                hasChange = true;
            }

            context.Outcome.TotalDamageDealt += applied;
        }

        if (hasChange)
        {
            rollback = hpRollback;
        }

        return true;
    }

    #endregion
}
