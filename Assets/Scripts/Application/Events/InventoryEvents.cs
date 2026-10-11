using System.Collections.Generic;

/// <summary>
/// 道具变更事件：在 Inventory 发生增删时发布，供 UI 或其他模块监听刷新。
/// </summary>
public sealed class InventoryChangedEvent : IDomainEvent
{
    /// <summary>变更后的完整道具列表。</summary>
    public IReadOnlyList<InventoryItem> Items { get; }

    /// <summary>发生变更的道具 ID（null 表示批量变更）。</summary>
    public string ChangedItemId { get; }

    /// <summary>变更数量（正数=增加，负数=减少，0=未知）。</summary>
    public int DeltaQuantity { get; }

    public InventoryChangedEvent(IReadOnlyList<InventoryItem> items, string changedItemId = null, int deltaQuantity = 0)
    {
        Items = items;
        ChangedItemId = changedItemId;
        DeltaQuantity = deltaQuantity;
    }
}