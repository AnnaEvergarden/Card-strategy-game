using UnityEngine;

/// <summary>
/// 按配置概率立即清空该技能冷却（概率来自 <see cref="InstantEffectEntry.ChancePercent"/>）。
/// </summary>
public sealed class RefreshCooldownChanceEffect : ISkillEffect
{
    #region Fields

    private readonly int _chancePercent;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造带概率的刷新冷却效果。
    /// </summary>
    public RefreshCooldownChanceEffect(int chancePercent)
    {
        _chancePercent = Mathf.Clamp(chancePercent, 0, 100);
    }

    #endregion

    #region Public API

    /// <inheritdoc />
    public bool TryExecute(SkillExecutionContext context, out ISkillEffectRollback rollback)
    {
        rollback = null;
        if (_chancePercent <= 0)
        {
            return true;
        }

        if (context?.Caster == null)
        {
            return false;
        }

        var roll = Random.Range(0, 100);
        if (roll >= _chancePercent)
        {
            return true;
        }

        var caster = context.Caster;
        var hadEntry = caster.TryGetCooldownSnapshot(context.SkillId, out var previousRemaining);
        var cleared = caster.ClearCooldown(context.SkillId);
        context.Outcome.CooldownRefreshed = true;
        rollback = SkillEffectRollbackRecords.TryCreateCooldownRollback(
            caster.UnitId,
            context.SkillId,
            previousRemaining,
            hadEntry,
            cleared);
        return true;
    }

    #endregion
}
