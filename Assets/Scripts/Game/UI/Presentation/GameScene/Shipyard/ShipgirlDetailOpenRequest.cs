/// <summary>
/// 舰娘详情打开请求：船坞点击槽位时写入待展示的 cardId，详情面板 <see cref="ShipgirlDetailPanel"/> 在 OnEnable 消费。
/// </summary>
public static class ShipgirlDetailOpenRequest
{
    #region Fields

    /// <summary>
    /// 待展示舰娘 cardId（null 表示无请求）。
    /// </summary>
    private static string _pendingCardId;

    #endregion

    #region Public API

    /// <summary>
    /// 当前是否有待消费的打开请求。
    /// </summary>
    public static bool HasPending => !string.IsNullOrWhiteSpace(_pendingCardId);

    /// <summary>
    /// 设置即将打开的舰娘 cardId（覆盖上一次未消费的请求）。
    /// </summary>
    /// <param name="cardId">收藏条目中的 cardId。</param>
    public static void SetPending(string cardId)
    {
        _pendingCardId = string.IsNullOrWhiteSpace(cardId) ? null : cardId.Trim();
    }

    /// <summary>
    /// 读取并清空待展示的 cardId（详情面板启用时调用一次）。
    /// </summary>
    /// <returns>cardId；若无则返回 null。</returns>
    public static string ConsumePending()
    {
        var v = _pendingCardId;
        _pendingCardId = null;
        return v;
    }

    #endregion
}
