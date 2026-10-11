using System.Collections.Generic;

/// <summary>
/// 单次建造消耗数据结构。
/// </summary>
[System.Serializable]
public class BuildCostData
{
    public int Gold;
    public int Diamond;
    public int ShipTicket;
}

/// <summary>
/// 单条掉落项数据结构。
/// </summary>
[System.Serializable]
public class BuildDropEntryData
{
    public string CardId;
    public int Weight = 1;
}

/// <summary>
/// 建造卡池静态配置数据。
/// </summary>
[System.Serializable]
public class BuildPoolConfig : IConfigData
{
    public string Id => PoolId;
    public string PoolId;
    public string DisplayName;
    public ShipFaction PoolFaction;
    public int BannerResourceId;
    public int ButtonIconResourceId;
    public bool IsOpen = true;
    public BuildCostData BuildOneCost = new BuildCostData();
    public BuildCostData BuildTenCost = new BuildCostData();
    public List<BuildDropEntryData> DropEntries = new List<BuildDropEntryData>();
}

/// <summary>
/// 卡池配置表容器。
/// </summary>
[System.Serializable]
public class BuildPoolConfigTable
{
    public List<BuildPoolConfig> Items = new List<BuildPoolConfig>();
}