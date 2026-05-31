/// <summary>
/// 状态变化事件：单位 HP 归零（由 <see cref="BattleUnit"/> 发布；技能流水线内暂存至成功后批量发布）。
/// </summary>
public sealed class DeadEvent : IBattleEvent
{
    #region Fields

    /// <summary>
    /// 死亡单位 cardId。
    /// </summary>
    public readonly string CardId;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造死亡事件。
    /// </summary>
    public DeadEvent(string cardId)
    {
        CardId = cardId ?? string.Empty;
    }

    #endregion
}
