using UnityEngine;


/// <summary>
/// 治疗效果：对上下文目标恢复固定生命值。
/// </summary>
public sealed class HealEffect : ISkillEffect
{
    #region Fields

    private readonly int _healAmount;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造治疗 Effect。
    /// </summary>
    public HealEffect(int healAmount)
    {
        _healAmount = Mathf.Max(0, healAmount);
    }

    #endregion

    #region Public API

    /// <inheritdoc />
    public bool TryExecute(SkillExecutionContext context, out ISkillEffectRollback rollback)
    {
        rollback = null;
        if (context == null || _healAmount <= 0)
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

            var applied = target.Heal(_healAmount);
            if (applied > 0)
            {
                hpRollback.Record(target.UnitId, applied);
                hasChange = true;
            }
        }

        if (hasChange)
        {
            rollback = hpRollback;
        }

        return true;
    }

    #endregion
}
