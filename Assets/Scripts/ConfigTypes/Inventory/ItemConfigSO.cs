using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Inventory/Item Config", fileName = "ItemConfig_")]
public class ItemConfigSO : ScriptableObject, IConfigData
{
    [SerializeField] private string _itemId;
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private int _iconResourceId;
    [SerializeField] private int _maxStack = 99;

    public string Id => ItemId;
    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public int IconResourceId => _iconResourceId;
    public int MaxStack => _maxStack;

    public ItemConfig ToDomain() => new ItemConfig
    {
        ItemId = _itemId, DisplayName = _displayName, Description = _description,
        IconResourceId = _iconResourceId, MaxStack = _maxStack
    };
}




