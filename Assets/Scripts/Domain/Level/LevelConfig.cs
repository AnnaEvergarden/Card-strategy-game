using System.Collections.Generic;

/// <summary>
/// 关卡模式枚举。
/// </summary>
/// <summary>
/// 关卡静态配置数据。
/// </summary>
[System.Serializable]
public class LevelStageConfig : IConfigData
{
    public string Id => StageId;
    public string StageId;
    public string AreaId;
    public string DisplayName;
    public int StageIndexInArea;
    public bool IsUnlocked = true;

    /// <summary>敌方上阵卡组（CardId 列表，最多 6 张：前 2 名首发、其余替补）。</summary>
    public List<string> EnemyCardIds = new List<string>();
}

/// <summary>
/// 关卡配置表容器（独立查询用）。
/// </summary>
[System.Serializable]
public class LevelStageConfigTable
{
    public List<LevelStageConfig> Items = new List<LevelStageConfig>();
}

/// <summary>
/// 区域配置数据：包含该区域内的所有关卡配置。
/// </summary>
[System.Serializable]
public class LevelAreaConfig : IConfigData
{
    public string Id => AreaId;
    public string AreaId;
    public string DisplayName;
    public string Description;
    public ShipFaction Faction;
    public LevelAreaCategory Category;
    public bool IsUnlocked = true;

    /// <summary>该区域下的关卡列表（嵌入，非 ID 引用）。</summary>
    public List<LevelStageConfig> Stages = new List<LevelStageConfig>();
}

/// <summary>
/// 区域配置表容器。
/// </summary>
[System.Serializable]
public class LevelAreaConfigTable
{
    public List<LevelAreaConfig> Items = new List<LevelAreaConfig>();
}