using System.Collections.Generic;

/// <summary>
/// 单位受击事件：由 BattleUnit.TakeDamage 发布（数据所有权原则：HP 属于单位）。
/// </summary>
public sealed class UnitDamagedEvent : IBattleEvent
{
    /// <summary>受击单位 UnitId。</summary>
    public string UnitId { get; }

    /// <summary>受击单位阵营。</summary>
    public BattleSide Side { get; }

    /// <summary>受击单位卡牌 ID。</summary>
    public string CardId { get; }

    /// <summary>实际扣减的 HP（0 表示被完全格挡）。</summary>
    public int Damage { get; }

    /// <summary>受击后剩余 HP。</summary>
    public int HpAfter { get; }

    /// <summary>
    /// 构造单位受击事件。
    /// </summary>
    public UnitDamagedEvent(string unitId, BattleSide side, string cardId, int damage, int hpAfter)
    {
        UnitId = unitId;
        Side = side;
        CardId = cardId;
        Damage = damage;
        HpAfter = hpAfter;
    }
}

/// <summary>
/// 单位治疗事件：由 BattleUnit.Heal 发布。
/// </summary>
public sealed class UnitHealedEvent : IBattleEvent
{
    /// <summary>治疗单位 UnitId。</summary>
    public string UnitId { get; }

    /// <summary>治疗单位阵营。</summary>
    public BattleSide Side { get; }

    /// <summary>治疗单位卡牌 ID。</summary>
    public string CardId { get; }

    /// <summary>实际回复的 HP。</summary>
    public int Healed { get; }

    /// <summary>治疗后剩余 HP。</summary>
    public int HpAfter { get; }

    /// <summary>
    /// 构造单位治疗事件。
    /// </summary>
    public UnitHealedEvent(string unitId, BattleSide side, string cardId, int healed, int hpAfter)
    {
        UnitId = unitId;
        Side = side;
        CardId = cardId;
        Healed = healed;
        HpAfter = hpAfter;
    }
}

/// <summary>
/// 单位阵亡事件：由 BattleUnit.TakeDamage（致死时）发布。
/// </summary>
public sealed class UnitDiedEvent : IBattleEvent
{
    /// <summary>阵亡单位 UnitId。</summary>
    public string UnitId { get; }

    /// <summary>阵亡单位阵营。</summary>
    public BattleSide Side { get; }

    /// <summary>阵亡单位卡牌 ID。</summary>
    public string CardId { get; }

    /// <summary>
    /// 构造单位阵亡事件。
    /// </summary>
    public UnitDiedEvent(string unitId, BattleSide side, string cardId)
    {
        UnitId = unitId;
        Side = side;
        CardId = cardId;
    }
}

/// <summary>
/// Buff 添加事件：由 BattleUnit.AddBuff 发布。
/// </summary>
public sealed class BuffAddedEvent : IBattleEvent
{
    /// <summary>目标单位 UnitId。</summary>
    public string UnitId { get; }

    /// <summary>Buff 种类。</summary>
    public BuffKind Kind { get; }

    /// <summary>Buff 数值。</summary>
    public int Value { get; }

    /// <summary>持续回合数。</summary>
    public int Duration { get; }

    /// <summary>
    /// 构造 Buff 添加事件。
    /// </summary>
    public BuffAddedEvent(string unitId, BuffKind kind, int value, int duration)
    {
        UnitId = unitId;
        Kind = kind;
        Value = value;
        Duration = duration;
    }
}

/// <summary>
/// Buff 到期事件：由 BattleUnit.TickBuffsEndOfRound（到期时）发布。
/// </summary>
public sealed class BuffExpiredEvent : IBattleEvent
{
    /// <summary>目标单位 UnitId。</summary>
    public string UnitId { get; }

    /// <summary>Buff 种类。</summary>
    public BuffKind Kind { get; }

    /// <summary>
    /// 构造 Buff 到期事件。
    /// </summary>
    public BuffExpiredEvent(string unitId, BuffKind kind)
    {
        UnitId = unitId;
        Kind = kind;
    }
}

/// <summary>
/// 槽位绑定事件：单位上场时由 BattleField 发布。
/// </summary>
public sealed class SlotBoundEvent : IBattleEvent
{
    /// <summary>槽位阵营。</summary>
    public BattleSide Side { get; }

    /// <summary>槽位下标（0 起，固定 2 槽）。</summary>
    public int SlotIndex { get; }

    /// <summary>上场单位 UnitId。</summary>
    public string UnitId { get; }

    /// <summary>上场单位卡牌 ID。</summary>
    public string CardId { get; }

    /// <summary>
    /// 构造槽位绑定事件。
    /// </summary>
    public SlotBoundEvent(BattleSide side, int slotIndex, string unitId, string cardId)
    {
        Side = side;
        SlotIndex = slotIndex;
        UnitId = unitId;
        CardId = cardId;
    }
}

/// <summary>
/// 槽位解绑事件：单位阵亡/离场时由 BattleField 发布。
/// </summary>
public sealed class SlotUnboundEvent : IBattleEvent
{
    /// <summary>槽位阵营。</summary>
    public BattleSide Side { get; }

    /// <summary>槽位下标。</summary>
    public int SlotIndex { get; }

    /// <summary>离场单位 UnitId。</summary>
    public string UnitId { get; }

    /// <summary>
    /// 构造槽位解绑事件。
    /// </summary>
    public SlotUnboundEvent(BattleSide side, int slotIndex, string unitId)
    {
        Side = side;
        SlotIndex = slotIndex;
        UnitId = unitId;
    }
}

/// <summary>
/// 换牌事件：主动换牌时由 BattleField 发布（阵亡替补不触发本事件，走 SlotBound/SlotUnbound）。
/// </summary>
public sealed class UnitSwitchedEvent : IBattleEvent
{
    /// <summary>槽位阵营。</summary>
    public BattleSide Side { get; }

    /// <summary>槽位下标。</summary>
    public int SlotIndex { get; }

    /// <summary>离场单位 UnitId。</summary>
    public string OutUnitId { get; }

    /// <summary>上场单位 UnitId。</summary>
    public string InUnitId { get; }

    /// <summary>
    /// 构造换牌事件。
    /// </summary>
    public UnitSwitchedEvent(BattleSide side, int slotIndex, string outUnitId, string inUnitId)
    {
        Side = side;
        SlotIndex = slotIndex;
        OutUnitId = outUnitId;
        InUnitId = inUnitId;
    }
}

/// <summary>
/// 回合开始事件：由 BattleTurnSystem 发布。
/// </summary>
public sealed class RoundStartedEvent : IBattleEvent
{
    /// <summary>回合数（1 起）。</summary>
    public int RoundNumber { get; }

    /// <summary>
    /// 构造回合开始事件。
    /// </summary>
    public RoundStartedEvent(int roundNumber)
    {
        RoundNumber = roundNumber;
    }
}

/// <summary>
/// 行动阶段切换事件：由 BattleTurnSystem 发布。
/// </summary>
public sealed class TurnPhaseChangedEvent : IBattleEvent
{
    /// <summary>新阶段。</summary>
    public BattleTurnPhase Phase { get; }

    /// <summary>当前行动阵营。</summary>
    public BattleSide Side { get; }

    /// <summary>
    /// 构造阶段切换事件。
    /// </summary>
    public TurnPhaseChangedEvent(BattleTurnPhase phase, BattleSide side)
    {
        Phase = phase;
        Side = side;
    }
}

/// <summary>
/// 槽位行动完成事件：由 BattleTurnSystem 发布（放技能/换牌后，槽位变暗依据）。
/// </summary>
public sealed class SlotActedEvent : IBattleEvent
{
    /// <summary>槽位阵营。</summary>
    public BattleSide Side { get; }

    /// <summary>槽位下标。</summary>
    public int SlotIndex { get; }

    /// <summary>行动单位 UnitId。</summary>
    public string UnitId { get; }

    /// <summary>
    /// 构造槽位行动完成事件。
    /// </summary>
    public SlotActedEvent(BattleSide side, int slotIndex, string unitId)
    {
        Side = side;
        SlotIndex = slotIndex;
        UnitId = unitId;
    }
}

/// <summary>
/// 技能开始释放事件：由技能系统发布。
/// </summary>
public sealed class SkillCastStartedEvent : IBattleEvent
{
    /// <summary>施法单位 UnitId。</summary>
    public string CasterUnitId { get; }

    /// <summary>施法单位阵营。</summary>
    public BattleSide CasterSide { get; }

    /// <summary>技能 ID。</summary>
    public string SkillId { get; }

    /// <summary>技能显示名。</summary>
    public string SkillName { get; }

    /// <summary>
    /// 构造技能开始释放事件。
    /// </summary>
    public SkillCastStartedEvent(string casterUnitId, BattleSide casterSide, string skillId, string skillName)
    {
        CasterUnitId = casterUnitId;
        CasterSide = casterSide;
        SkillId = skillId;
        SkillName = skillName;
    }
}

/// <summary>
/// 技能进入选目标模式事件：需要玩家手动点选目标时由应用层发布，
/// UI 据此高亮合法目标（两段式选目标：先选技能，再点目标）。
/// </summary>
public sealed class SkillTargetSelectingEvent : IBattleEvent
{
    /// <summary>施法单位 UnitId。</summary>
    public string CasterUnitId { get; }

    /// <summary>技能 ID。</summary>
    public string SkillId { get; }

    /// <summary>技能显示名。</summary>
    public string SkillName { get; }

    /// <summary>合法目标 UnitId 列表（空列表 = 无需选目标或无合法目标）。</summary>
    public IReadOnlyList<string> ValidTargetUnitIds { get; }

    /// <summary>
    /// 构造技能选目标事件。
    /// </summary>
    public SkillTargetSelectingEvent(string casterUnitId, string skillId, string skillName,
        IReadOnlyList<string> validTargetUnitIds)
    {
        CasterUnitId = casterUnitId;
        SkillId = skillId;
        SkillName = skillName;
        ValidTargetUnitIds = validTargetUnitIds ?? new List<string>(0);
    }
}

/// <summary>
/// 技能释放结束事件：由技能系统发布（无论成败）。
/// </summary>
public sealed class SkillCastFinishedEvent : IBattleEvent
{
    /// <summary>施法单位 UnitId。</summary>
    public string CasterUnitId { get; }

    /// <summary>技能 ID。</summary>
    public string SkillId { get; }

    /// <summary>是否成功。</summary>
    public bool Success { get; }

    /// <summary>失败原因（成功时为空）。</summary>
    public string FailureReason { get; }

    /// <summary>
    /// 构造技能释放结束事件。
    /// </summary>
    public SkillCastFinishedEvent(string casterUnitId, string skillId, bool success, string failureReason)
    {
        CasterUnitId = casterUnitId;
        SkillId = skillId;
        Success = success;
        FailureReason = failureReason ?? string.Empty;
    }
}

/// <summary>
/// 战斗开始事件：由 BattleSession 发布。UI 通过应用层只读视图取初始数据，本事件仅作流程通知。
/// </summary>
public sealed class BattleStartedEvent : IBattleEvent
{
    /// <summary>
    /// 构造战斗开始事件。
    /// </summary>
    public BattleStartedEvent()
    {
    }
}

/// <summary>
/// 战斗结束事件：由 BattleSession 发布。
/// </summary>
public sealed class BattleEndedEvent : IBattleEvent
{
    /// <summary>战斗结果。</summary>
    public BattleResult Result { get; }

    /// <summary>获胜阵营（平局/撤退时无意义）。</summary>
    public BattleSide WinnerSide { get; }

    /// <summary>结束时回合数。</summary>
    public int RoundNumber { get; }

    /// <summary>结束原因（用于结算面板展示）。</summary>
    public string Reason { get; }

    /// <summary>
    /// 构造战斗结束事件。
    /// </summary>
    public BattleEndedEvent(BattleResult result, BattleSide winnerSide, int roundNumber, string reason)
    {
        Result = result;
        WinnerSide = winnerSide;
        RoundNumber = roundNumber;
        Reason = reason ?? string.Empty;
    }
}
