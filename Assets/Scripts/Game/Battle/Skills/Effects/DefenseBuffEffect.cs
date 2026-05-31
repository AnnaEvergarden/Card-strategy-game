using UnityEngine;

/// <summary>
/// 防御增益：对流水线目标（无目标时对施法者）写入 <see cref="BattleBuffState"/> 并提升运行时防御。
/// </summary>
public sealed class DefenseBuffEffect : ISkillEffect
{
    #region Fields

    /// <summary>
    /// 防御加成。
    /// </summary>
    private readonly int _defenseBonus;

    /// <summary>
    /// 持续回合数。
    /// </summary>
    private readonly int _durationTurns;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造防御增益 Effect。
    /// </summary>
    public DefenseBuffEffect(int defenseBonus, int durationTurns)
    {
        _defenseBonus = Mathf.Max(0, defenseBonus);
        _durationTurns = Mathf.Max(1, durationTurns);
    }

    #endregion

    #region Public API

    /// <inheritdoc />
    public bool TryExecute(SkillExecutionContext context, out ISkillEffectRollback rollback)
    {
        rollback = null;
        if (_defenseBonus <= 0)
        {
            return true;
        }

        if (context == null)
        {
            return false;
        }

        var buffRollback = SkillEffectRollbackRecords.CreateDefenseBuffRollback();
        var hasChange = false;

        if (context.Targets.Count > 0)
        {
            for (var i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                if (target == null)
                {
                    continue;
                }

                var buff = BattleBuffState.ApplyBuffAndGet(
                    target.CardId,
                    SkillBuffKind.DefenseBuff,
                    _defenseBonus,
                    _durationTurns);
                if (buff != null)
                {
                    buffRollback.Record(target.CardId, buff);
                    hasChange = true;
                }
            }
        }
        else if (context.Caster != null)
        {
            var buff = BattleBuffState.ApplyBuffAndGet(
                context.Caster.CardId,
                SkillBuffKind.DefenseBuff,
                _defenseBonus,
                _durationTurns);
            if (buff != null)
            {
                buffRollback.Record(context.Caster.CardId, buff);
                hasChange = true;
            }
        }

        if (hasChange)
        {
            rollback = buffRollback;
        }

        return true;
    }

    #endregion
}
