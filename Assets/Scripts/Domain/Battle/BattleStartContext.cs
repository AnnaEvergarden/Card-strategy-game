/// <summary>
/// 战斗入口上下文：存储从关卡选择到战斗场景的传递参数。
/// 在 LevelStageSelectPanel 中选择关卡时创建，BattleScene 读取。
/// </summary>
public class BattleStartContext
{
    /// <summary>所选关卡的 ID。</summary>
    public string StageId { get; private set; }

    /// <summary>关卡模式（常驻/活动）。</summary>
    public LevelMode Mode { get; private set; }

    /// <summary>选择的编队索引（在 BattleDeckSelectPanel 中设置）。</summary>
    public int FleetIndex { get; set; } = -1;

    /// <summary>首发舰娘索引（在 BattleActivePickPanel 中设置）。</summary>
    public int[] ActiveShipIndices { get; set; }

    /// <summary>创建战斗上下文。</summary>
    public BattleStartContext(string stageId, LevelMode mode)
    {
        StageId = stageId;
        Mode = mode;
    }
}
