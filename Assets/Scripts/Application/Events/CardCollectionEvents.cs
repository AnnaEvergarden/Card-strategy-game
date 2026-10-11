using System.Collections.Generic;

/// <summary>
/// 卡牌收藏变更事件。
/// </summary>
public sealed class CardCollectionChangedEvent : IDomainEvent
{
    /// <summary>变更后的完整收藏列表。</summary>
    public IReadOnlyList<CardInstance> Cards { get; }

    /// <summary>发生变更的卡牌实例 ID（null 表示批量变更）。</summary>
    public string ChangedInstanceId { get; }

    /// <summary>变更类型。</summary>
    public ChangeType Type { get; }

    public enum ChangeType { Added, Removed, Updated, Unknown }

    public CardCollectionChangedEvent(IReadOnlyList<CardInstance> cards, string changedInstanceId = null, ChangeType type = ChangeType.Unknown)
    {
        Cards = cards;
        ChangedInstanceId = changedInstanceId;
        Type = type;
    }
}