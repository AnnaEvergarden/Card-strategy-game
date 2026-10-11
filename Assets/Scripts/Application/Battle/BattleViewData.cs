using System.Collections.Generic;

/// <summary>
/// 战斗单位只读视图：UI 初始化与刷新的数据快照（不含任何可变状态引用）。
/// </summary>
public sealed class BattleUnitViewData
{
    /// <summary>战斗内唯一标识。</summary>
    public string UnitId { get; set; }

    /// <summary>配置表卡牌 ID。</summary>
    public string CardId { get; set; }

    /// <summary>显示名称。</summary>
    public string DisplayName { get; set; }

    /// <summary>头像资源 ID（ResourceService 寻址，0 = 占位图）。</summary>
    public int IconResourceId { get; set; }

    /// <summary>最大生命。</summary>
    public int MaxHp { get; set; }

    /// <summary>当前生命。</summary>
    public int Hp { get; set; }

    /// <summary>当前防御。</summary>
    public int Defense { get; set; }

    /// <summary>阵营。</summary>
    public BattleSide Side { get; set; }

    /// <summary>是否阵亡。</summary>
    public bool IsDead { get; set; }

    /// <summary>所在槽位本回合是否已行动（行动后变暗）。</summary>
    public bool HasActed { get; set; }

    /// <summary>技能 ID 列表。</summary>
    public IReadOnlyList<string> SkillIds { get; set; }
}

/// <summary>
/// 上场槽位视图：槽位与其绑定单位（空槽 Unit 为 null）。
/// </summary>
public sealed class BattleSlotViewData
{
    /// <summary>槽位阵营。</summary>
    public BattleSide Side { get; set; }

    /// <summary>槽位下标。</summary>
    public int SlotIndex { get; set; }

    /// <summary>槽位上的单位视图（空槽为 null）。</summary>
    public BattleUnitViewData Unit { get; set; }
}

/// <summary>
/// 战斗整体只读视图：回合状态 + 双方槽位与替补快照。
/// </summary>
public sealed class BattleViewData
{
    /// <summary>当前回合数。</summary>
    public int RoundNumber { get; set; }

    /// <summary>当前阶段。</summary>
    public BattleTurnPhase Phase { get; set; }

    /// <summary>关卡显示名（战斗标题栏使用；对应历史 headerText 的关卡名）。</summary>
    public string StageDisplayName { get; set; }

    /// <summary>P1 自动战斗是否开启。</summary>
    public bool P1AutoBattle { get; set; }

    /// <summary>P1（2 槽位）。</summary>
    public IReadOnlyList<BattleSlotViewData> P1Slots { get; set; }

    /// <summary>P2（2 槽位）。</summary>
    public IReadOnlyList<BattleSlotViewData> P2Slots { get; set; }

    /// <summary>P1 替补（按上场顺序）。</summary>
    public IReadOnlyList<BattleUnitViewData> P1Reserve { get; set; }

    /// <summary>P2 替补（按上场顺序）。</summary>
    public IReadOnlyList<BattleUnitViewData> P2Reserve { get; set; }
}
