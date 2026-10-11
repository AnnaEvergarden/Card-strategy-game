using System.Collections.Generic;

/// <summary>
/// 回合系统：回合数、行动阶段与"按槽位"的行动权管理。
/// 规则（定稿）：
/// - P1 先手；每槽位每回合行动一次（放技能或换牌消耗该槽位行动权）；
/// - 双方全部行动完毕 → 回合结束：全员 Buff Tick + 技能冷却 Tick → 下一回合；
/// - 换牌不清除槽位已行动标记（行动权绑定槽位而非单位）；
/// - 空槽（阵亡且无替补）不阻塞回合结束；
/// - AI 开关按阵营独立（P1 自动战斗 / P2 敌方 AI）。
/// 纯 C# 领域对象，不依赖 Unity。
/// </summary>
public sealed class BattleTurnSystem
{
    private readonly BattleField _field;
    private readonly IBattleEventBus _eventBus;
    private readonly bool[][] _slotActed = new bool[2][];
    private bool _started;

    /// <summary>构造回合系统。</summary>
    /// <param name="field">战场</param>
    /// <param name="eventBus">战斗事件总线</param>
    public BattleTurnSystem(BattleField field, IBattleEventBus eventBus)
    {
        _field = field;
        _eventBus = eventBus;

        for (var side = 0; side < 2; side++)
            _slotActed[side] = new bool[BattleField.ActiveSlotCount];

        P1AiEnabled = false;
        P2AiEnabled = true;
    }

    /// <summary>当前回合数（1 起）。</summary>
    public int RoundNumber { get; private set; } = 1;

    /// <summary>当前行动阶段。</summary>
    public BattleTurnPhase Phase { get; private set; } = BattleTurnPhase.P1Action;

    /// <summary>P1 自动战斗开关（玩家托管）。</summary>
    public bool P1AiEnabled { get; private set; }

    /// <summary>P2 AI 开关（敌方，默认开启）。</summary>
    public bool P2AiEnabled { get; private set; }

    /// <summary>
    /// 开战：发布首回合与首阶段事件。战斗会话在初始化完成后调用一次。
    /// </summary>
    public void Start()
    {
        if (_started)
            return;
        _started = true;

        _eventBus?.Publish(new RoundStartedEvent(RoundNumber));
        _eventBus?.Publish(new TurnPhaseChangedEvent(Phase, BattleSide.P1));
    }

    /// <summary>
    /// 指定阵营的 AI 是否启用。
    /// </summary>
    public bool IsAiEnabled(BattleSide side)
    {
        return side == BattleSide.P1 ? P1AiEnabled : P2AiEnabled;
    }

    /// <summary>
    /// 设置指定阵营 AI 开关。
    /// </summary>
    public void SetAiEnabled(BattleSide side, bool enabled)
    {
        if (side == BattleSide.P1)
            P1AiEnabled = enabled;
        else
            P2AiEnabled = enabled;
    }

    /// <summary>
    /// 指定阵营是否处于行动阶段。
    /// </summary>
    public bool IsSideActionPhase(BattleSide side)
    {
        return Phase == BattleTurnPhase.P1Action && side == BattleSide.P1
            || Phase == BattleTurnPhase.P2Action && side == BattleSide.P2;
    }

    /// <summary>
    /// 查询槽位是否已行动（UI 变暗依据）。
    /// </summary>
    public bool HasSlotActed(BattleSide side, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= BattleField.ActiveSlotCount)
            return true;
        return _slotActed[(int)side][slotIndex];
    }

    /// <summary>
    /// 指定阵营本回合是否全部行动完毕（空槽不计数）。
    /// </summary>
    public bool IsSideRoundComplete(BattleSide side)
    {
        var acted = _slotActed[(int)side];
        var slots = GetSlots(side);
        for (var i = 0; i < acted.Length; i++)
        {
            // 空槽视为已行动；占位且未行动则回合未完成。
            if (slots[i] == null)
                continue;
            if (!acted[i])
                return false;
        }
        return true;
    }

    /// <summary>
    /// 单位当前是否可行动：存活 + 已绑槽位 + 槽位未行动 + 该阵营处于行动阶段。
    /// </summary>
    public bool CanUnitAct(string unitId)
    {
        if (!_field.TryGetSlotOfUnit(unitId, out var side, out var slotIndex))
            return false;

        var unit = _field.GetUnit(unitId);
        if (unit == null || unit.IsDead)
            return false;

        return IsSideActionPhase(side) && !HasSlotActed(side, slotIndex);
    }

    /// <summary>
    /// 标记槽位行动完成：发布 SlotActedEvent（放技能/换牌成功后调用）。
    /// 仅在该阵营行动阶段、槽位有存活单位且尚未行动时成功。
    /// </summary>
    public bool TryCompleteSlotAction(BattleSide side, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= BattleField.ActiveSlotCount)
            return false;

        if (Phase == BattleTurnPhase.Ended || !IsSideActionPhase(side))
            return false;

        var unit = _field.GetActiveUnitAtSlot(side, slotIndex);
        if (unit == null || unit.IsDead)
            return false;

        if (HasSlotActed(side, slotIndex))
            return false;

        _slotActed[(int)side][slotIndex] = true;
        _eventBus?.Publish(new SlotActedEvent(side, slotIndex, unit.UnitId));
        return true;
    }

    /// <summary>
    /// 跳过指定阵营所有未行动槽位（"结束回合"按钮：无需逐个点击即可交棒）。
    /// </summary>
    public void PassRemainingSideActions(BattleSide side)
    {
        for (var i = 0; i < BattleField.ActiveSlotCount; i++)
        {
            if (!HasSlotActed(side, i))
                TryCompleteSlotAction(side, i);
        }
    }

    /// <summary>
    /// 结束指定阵营的行动阶段：
    /// - P1 结束 → 进入 P2 行动阶段；
    /// - P2 结束 → 回合结束：全员 Buff/冷却 Tick → 回合数 +1 → 重置行动标记 → 回到 P1。
    /// 未行动完毕（或非该阵营阶段）返回 false。
    /// </summary>
    public bool TryEndSideTurn(BattleSide side)
    {
        if (Phase == BattleTurnPhase.Ended)
            return false;

        if (!IsSideActionPhase(side))
            return false;

        if (!IsSideRoundComplete(side))
            return false;

        if (side == BattleSide.P1)
        {
            Phase = BattleTurnPhase.P2Action;
            _eventBus?.Publish(new TurnPhaseChangedEvent(Phase, BattleSide.P2));
            return true;
        }

        EndRound();
        return true;
    }

    /// <summary>
    /// 回合结束流程：Tick 全员 Buff 与技能冷却、重置双方行动标记、回合数 +1、回到 P1 行动阶段。
    /// </summary>
    private void EndRound()
    {
        RoundNumber++;

        var units = _field.AllUnits;
        for (var i = 0; i < units.Count; i++)
        {
            units[i].TickBuffsEndOfRound();
            units[i].TickSkillCooldowns();
        }

        for (var side = 0; side < 2; side++)
        {
            for (var i = 0; i < BattleField.ActiveSlotCount; i++)
                _slotActed[side][i] = false;
        }

        Phase = BattleTurnPhase.P1Action;
        _eventBus?.Publish(new RoundStartedEvent(RoundNumber));
        _eventBus?.Publish(new TurnPhaseChangedEvent(Phase, BattleSide.P1));
    }

    /// <summary>
    /// 标记战斗结束（冻结回合推进，BattleSession 判定胜负后调用）。
    /// </summary>
    public void MarkEnded()
    {
        Phase = BattleTurnPhase.Ended;
        _eventBus?.Publish(new TurnPhaseChangedEvent(Phase, BattleSide.P1));
    }

    /// <summary>
    /// 获取指定阵营的槽位数组引用（内部使用，避免暴露二维结构）。
    /// </summary>
    private BattleUnit[] GetSlots(BattleSide side)
    {
        // 通过 GetActiveUnitAtSlot 无法拿到空槽信息，此处按内部结构访问。
        return _field.GetSlotsInternal(side);
    }
}
