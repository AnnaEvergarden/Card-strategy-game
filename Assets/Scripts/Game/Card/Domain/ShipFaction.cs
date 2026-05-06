/// <summary>
/// 舰娘与建造卡池所属阵营。
/// 注释格式：中文（English），与游戏内势力展示一致。
/// </summary>
public enum ShipFaction
{
    /// <summary>
    /// 白鹰（Eagle Union）。
    /// </summary>
    EagleUnion = 0,

    /// <summary>
    /// 皇家（Royal Navy）。
    /// </summary>
    RoyalNavy = 1,

    /// <summary>
    /// 重樱（Sakura Empire）。
    /// </summary>
    SakuraEmpire = 2,

    /// <summary>
    /// 铁血（Iron Blood）。
    /// </summary>
    IronBlood = 3,

    /// <summary>
    /// 东煌（Dragon Empery）。
    /// </summary>
    DragonEmpery = 4,

    /// <summary>
    /// 其他（Other）。混池/全阵营卡池使用；掉落中可配置任意阵营舰娘。
    /// </summary>
    Other = 5
}
