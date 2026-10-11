/// <summary>
/// Inventory 领域对象 ↔ SaveData DTO 映射器。纯函数，无状态。
/// </summary>
public static class InventoryMapper
{
    /// <summary>SaveData → Domain</summary>
    public static Inventory ToDomain(InventorySaveData dto)
    {
        var inventory = new Inventory();
        if (dto?.Items == null) return inventory;

        for (int i = 0; i < dto.Items.Count; i++)
        {
            var entry = dto.Items[i];
            if (entry != null && !string.IsNullOrWhiteSpace(entry.ItemId) && entry.Quantity > 0)
                inventory.AddItem(entry.ItemId, entry.Quantity);
        }

        return inventory;
    }

    /// <summary>Domain → SaveData</summary>
    public static InventorySaveData ToSaveData(Inventory domain)
    {
        var dto = new InventorySaveData();
        var items = domain.Items;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            dto.Items.Add(new InventorySaveData.ItemEntry
            {
                ItemId = item.ItemId,
                Quantity = item.Quantity
            });
        }
        return dto;
    }
}