using System.Collections.Generic;

/// <summary>
/// 道具静态配置数据。
/// </summary>
[System.Serializable]
public class ItemConfig : IConfigData
{
    public string Id => ItemId;
    public string ItemId;
    public string DisplayName;
    public string Description;
    public int IconResourceId;
    public int MaxStack = 99;
}

/// <summary>
/// 道具配置表容器。
/// </summary>
[System.Serializable]
public class ItemConfigTable
{
    public List<ItemConfig> Items = new List<ItemConfig>();
}

