/// <summary>
/// 状态变化事件：单位受到伤害（由 <see cref="BattleUnit"/> 发布；技能流水线内暂存至成功后批量发布）。
/// </summary>
public sealed class DamageEvent : IBattleEvent
{
    #region Fields

    /// <summary>
    /// 受伤单位 cardId。
    /// </summary>
    public readonly string TargetCardId;

    /// <summary>
    /// 实际扣除的生命值。
    /// </summary>
    public readonly int AppliedDamage;

    /// <summary>
    /// 受伤后剩余 HP。
    /// </summary>
    public readonly int RemainingHp;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造伤害事件。
    /// </summary>
    public DamageEvent(string targetCardId, int appliedDamage, int remainingHp)
    {
        TargetCardId = targetCardId ?? string.Empty;
        AppliedDamage = appliedDamage;
        RemainingHp = remainingHp;
    }

    #endregion
}
