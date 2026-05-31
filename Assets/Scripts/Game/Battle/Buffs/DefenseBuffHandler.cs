using UnityEngine;

/// <summary>
/// 防御 Buff：施加时增加运行时防御，到期时扣回。
/// </summary>
public sealed class DefenseBuffHandler : IBattleBuffHandler
{
    #region Public API

    /// <inheritdoc />
    public void OnApplied(string cardId, BattleBuffState.RuntimeBuff buff)
    {
        if (buff == null || buff.Value <= 0)
        {
            return;
        }

        var side = ResolveSide(cardId);
        BattleContext.Current.Field.FindUnitOnField(cardId, side)?.AddDefense(buff.Value);
    }

    /// <inheritdoc />
    public void OnExpired(string cardId, BattleBuffState.RuntimeBuff buff)
    {
        if (buff == null || buff.Value <= 0)
        {
            return;
        }

        var side = ResolveSide(cardId);
        BattleContext.Current.Field.FindUnitOnField(cardId, side)?.AddDefense(-buff.Value);
    }

    /// <summary>
    /// 从 <see cref="BattleFieldState"/> 推断卡牌所属阵营。
    /// 使用 <see cref="BattleFieldState.FindUnitOnField"/> 通过 cardId 定位。
    /// 双方同 cardId 时优先 P1（兜底行为）。
    /// </summary>
    private static BattleSide ResolveSide(string cardId)
    {
        var field = BattleContext.Current?.Field;
        if (field == null)
        {
            return BattleSide.P1;
        }

        var onP2 = field.FindUnitOnField(cardId, BattleSide.P2) != null;
        if (onP2)
        {
            return BattleSide.P2;
        }

        return BattleSide.P1;
    }

    #endregion
}
