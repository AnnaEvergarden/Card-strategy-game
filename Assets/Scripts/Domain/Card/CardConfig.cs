using System.Collections.Generic;

/// <summary>
/// 卡牌/舰娘静态配置数据。
/// 由 ConfigService 从导出的 JSON 加载，运行时只读。
/// </summary>
[System.Serializable]
public class CardConfig : IConfigData
{
    public string Id => CardId;
    public string CardId;
    public string DisplayName;
    public string EnglishName;
    public ShipFaction Faction;
    public CardRarity Rarity;
    public int BaseHP;
    public int BaseAttack;
    public int BaseDefense;
    public List<string> SkillIds = new List<string>();
    public int IconResourceId;
    public int PortraitResourceId;
}

/// <summary>
/// 卡牌配置表容器。
/// </summary>
[System.Serializable]
public class CardConfigTable
{
    public List<CardConfig> Items = new List<CardConfig>();
}

