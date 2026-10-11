using System.Collections.Generic;

/// <summary>
/// 道具存档 DTO。只用于序列化，无行为。保持与 Domain/Save/InventorySaveData.cs 相同结构。
/// </summary>
[System.Serializable]
public class InventorySaveData
{
    [System.Serializable]
    public class ItemEntry
    {
        public string ItemId;
        public int Quantity;
    }

    public List<ItemEntry> Items = new List<ItemEntry>();
}