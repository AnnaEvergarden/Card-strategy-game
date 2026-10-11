using System;

/// <summary>
/// 单条道具：不可变值对象。
/// 数量变更时由 Inventory 创建新实例替换，不直接修改此对象。
/// </summary>
public sealed class InventoryItem
{
    public string ItemId { get; }
    public int Quantity { get; }

    public InventoryItem(string itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("ItemId cannot be null or empty", nameof(itemId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        ItemId = itemId;
        Quantity = quantity;
    }
}