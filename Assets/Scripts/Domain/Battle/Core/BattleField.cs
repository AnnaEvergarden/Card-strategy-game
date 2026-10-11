using System;
using System.Collections.Generic;

/// <summary>
/// 战场：双方全部战斗单位 + 固定 2 个上场槽位/阵营 + 替补队列的统一管理。
/// 槽位与替补模型取代旧的 activeCardIds 列表：上场、阵亡替补、主动换牌均以槽位为锚点，
/// UnitId 是唯一寻址方式。纯 C# 领域对象，不依赖 Unity。
/// </summary>
public sealed class BattleField
{
    /// <summary>每方固定上场槽位数。</summary>
    public const int ActiveSlotCount = 2;

    /// <summary>
    /// 获取指定阵营的槽位数组（含空槽引用，供回合系统判断空槽不阻塞回合；外部不应修改数组内容）。
    /// </summary>
    internal BattleUnit[] GetSlotsInternal(BattleSide side)
    {
        return _slots[(int)side];
    }

    private readonly List<BattleUnit> _allUnits = new List<BattleUnit>();
    private readonly BattleUnit[][] _slots = new BattleUnit[2][];
    private readonly Queue<BattleUnit>[] _reserveQueues = new Queue<BattleUnit>[2];
    private readonly IBattleEventBus _eventBus;

    /// <summary>
    /// 构造战场：按传入顺序绑定前 2 个单位到上场槽位，其余进入替补队列。
    /// </summary>
    /// <param name="p1Units">P1 全部单位（前 2 个为首发）</param>
    /// <param name="p2Units">P2 全部单位（前 2 个为首发）</param>
    /// <param name="eventBus">战斗事件总线</param>
    public BattleField(IReadOnlyList<BattleUnit> p1Units, IReadOnlyList<BattleUnit> p2Units,
        IBattleEventBus eventBus)
    {
        if (p1Units == null || p1Units.Count == 0)
            throw new ArgumentException("P1 单位列表不能为空。", nameof(p1Units));
        if (p2Units == null || p2Units.Count == 0)
            throw new ArgumentException("P2 单位列表不能为空。", nameof(p2Units));

        _eventBus = eventBus;

        for (var side = 0; side < 2; side++)
        {
            _slots[side] = new BattleUnit[ActiveSlotCount];
            _reserveQueues[side] = new Queue<BattleUnit>();
        }

        SetupSide(p1Units, BattleSide.P1);
        SetupSide(p2Units, BattleSide.P2);
    }

    /// <summary>全部单位（双方合计，只读）。</summary>
    public IReadOnlyList<BattleUnit> AllUnits => _allUnits;

    /// <summary>
    /// 初始化一方：登记全部单位，前 2 名绑定槽位，其余入替补队列。
    /// </summary>
    private void SetupSide(IReadOnlyList<BattleUnit> units, BattleSide side)
    {
        var slotIndex = (int)side;

        for (var i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit == null)
                continue;

            if (unit.Side != side)
                throw new ArgumentException($"单位 {unit.UnitId} 阵营不符：期望 {side}，实际 {unit.Side}。");

            _allUnits.Add(unit);

            if (i < ActiveSlotCount)
            {
                _slots[slotIndex][i] = unit;
                _eventBus?.Publish(new SlotBoundEvent(side, i, unit.UnitId, unit.CardId));
            }
            else
            {
                _reserveQueues[slotIndex].Enqueue(unit);
            }
        }
    }

    /// <summary>
    /// 按 UnitId 查找单位（跨双方）。
    /// </summary>
    /// <returns>单位实例；不存在返回 null</returns>
    public BattleUnit GetUnit(string unitId)
    {
        for (var i = 0; i < _allUnits.Count; i++)
        {
            if (_allUnits[i].UnitId == unitId)
                return _allUnits[i];
        }
        return null;
    }

    /// <summary>
    /// 获取指定阵营当前上场（槽位绑定）的单位列表（只读快照）。
    /// </summary>
    public IReadOnlyList<BattleUnit> GetActiveUnits(BattleSide side)
    {
        var result = new List<BattleUnit>(ActiveSlotCount);
        var slots = _slots[(int)side];
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                result.Add(slots[i]);
        }
        return result;
    }

    /// <summary>
    /// 获取指定槽位上绑定的单位。
    /// </summary>
    /// <returns>槽位单位；空槽返回 null</returns>
    public BattleUnit GetActiveUnitAtSlot(BattleSide side, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= ActiveSlotCount)
            return null;
        return _slots[(int)side][slotIndex];
    }

    /// <summary>
    /// 查询单位所在槽位。
    /// </summary>
    /// <param name="unitId">单位 UnitId</param>
    /// <param name="side">输出：所在阵营</param>
    /// <param name="slotIndex">输出：槽位下标</param>
    /// <returns>是否在场上（已绑定槽位）</returns>
    public bool TryGetSlotOfUnit(string unitId, out BattleSide side, out int slotIndex)
    {
        side = default;
        slotIndex = -1;

        for (var s = 0; s < _slots.Length; s++)
        {
            var slots = _slots[s];
            for (var i = 0; i < slots.Length; i++)
            {
                var unit = slots[i];
                if (unit != null && unit.UnitId == unitId)
                {
                    side = (BattleSide)s;
                    slotIndex = i;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 获取指定阵营的替补队列快照（按上场顺序）。
    /// </summary>
    public IReadOnlyList<BattleUnit> GetReserveUnits(BattleSide side)
    {
        return new List<BattleUnit>(_reserveQueues[(int)side]);
    }

    /// <summary>
    /// 单位是否在替补队列中（未上场）。
    /// </summary>
    public bool IsUnitInReserve(string unitId)
    {
        for (var s = 0; s < _reserveQueues.Length; s++)
        {
            foreach (var unit in _reserveQueues[s])
            {
                if (unit.UnitId == unitId)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 指定阵营是否全员阵亡（含替补）。
    /// </summary>
    public bool IsSideDefeated(BattleSide side)
    {
        for (var i = 0; i < _allUnits.Count; i++)
        {
            var unit = _allUnits[i];
            if (unit.Side == side && !unit.IsDead)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 清理阵亡单位：遍历双方槽位，阵亡者解绑（发 SlotUnboundEvent）并尝试从替补补位。
    /// </summary>
    public void RemoveDeadUnitsAndFill()
    {
        for (var s = 0; s < _slots.Length; s++)
        {
            var side = (BattleSide)s;
            var slots = _slots[s];
            for (var i = 0; i < slots.Length; i++)
            {
                var unit = slots[i];
                if (unit == null || !unit.IsDead)
                    continue;

                UnbindDeadAndFill(side, i);
            }
        }
    }

    /// <summary>
    /// 解绑指定槽位的阵亡单位（发布 SlotUnboundEvent）并从替补补位（发布 SlotBoundEvent）。
    /// </summary>
    /// <returns>是否发生了阵亡解绑（无论是否成功补位）</returns>
    public bool UnbindDeadAndFill(BattleSide side, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= ActiveSlotCount)
            return false;

        var unit = _slots[(int)side][slotIndex];
        if (unit == null || !unit.IsDead)
            return false;

        _slots[(int)side][slotIndex] = null;
        _eventBus?.Publish(new SlotUnboundEvent(side, slotIndex, unit.UnitId));
        TryFillSlotFromReserve(side, slotIndex);
        return true;
    }

    /// <summary>
    /// 从替补队首补位到空槽（跳过队首已阵亡单位）。
    /// </summary>
    /// <returns>是否成功补位</returns>
    public bool TryFillSlotFromReserve(BattleSide side, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= ActiveSlotCount)
            return false;

        if (_slots[(int)side][slotIndex] != null)
            return false;

        var queue = _reserveQueues[(int)side];
        while (queue.Count > 0)
        {
            var candidate = queue.Dequeue();
            if (candidate.IsDead)
                continue;

            _slots[(int)side][slotIndex] = candidate;
            _eventBus?.Publish(new SlotBoundEvent(side, slotIndex, candidate.UnitId, candidate.CardId));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 主动换牌：场上单位下场入替补队尾，替补单位顶替其槽位。
    /// 槽位本回合的已行动标记不清除（由回合系统按槽位追踪，换牌不刷新行动权）。
    /// </summary>
    /// <param name="side">阵营</param>
    /// <param name="outgoingUnitId">下场单位</param>
    /// <param name="incomingUnitId">上场单位</param>
    /// <param name="failureReason">输出：失败原因</param>
    public bool TrySwitchActive(BattleSide side, string outgoingUnitId, string incomingUnitId,
        out string failureReason)
    {
        failureReason = string.Empty;

        if (!TryGetSlotOfUnit(outgoingUnitId, out var outSide, out var slotIndex))
        {
            failureReason = "下场单位不在场上。";
            return false;
        }

        if (outSide != side)
        {
            failureReason = "下场单位不属于该阵营。";
            return false;
        }

        var outgoing = GetUnit(outgoingUnitId);
        if (outgoing == null || outgoing.IsDead)
        {
            failureReason = "下场单位已阵亡。";
            return false;
        }

        var incoming = GetUnit(incomingUnitId);
        if (incoming == null || incoming.Side != side)
        {
            failureReason = "上场单位不存在或阵营不符。";
            return false;
        }

        if (incoming.IsDead)
        {
            failureReason = "上场单位已阵亡。";
            return false;
        }

        if (!IsUnitInReserve(incomingUnitId))
        {
            failureReason = "上场单位不在替补队列。";
            return false;
        }

        // 从替补队列中移除上场者（保持其余顺序），下场的单位排入队尾。
        RemoveFromReserveQueue(incomingUnitId);
        _reserveQueues[(int)side].Enqueue(outgoing);

        _slots[(int)side][slotIndex] = incoming;
        _eventBus?.Publish(new UnitSwitchedEvent(side, slotIndex, outgoingUnitId, incomingUnitId));
        return true;
    }

    /// <summary>
    /// 从替补队列中移除指定单位（重建队列，保持其余顺序）。
    /// </summary>
    private void RemoveFromReserveQueue(string unitId)
    {
        for (var s = 0; s < _reserveQueues.Length; s++)
        {
            var queue = _reserveQueues[s];
            if (!ContainsUnit(queue, unitId))
                continue;

            var rebuilt = new Queue<BattleUnit>(queue.Count);
            foreach (var unit in queue)
            {
                if (unit.UnitId != unitId)
                    rebuilt.Enqueue(unit);
            }

            _reserveQueues[s] = rebuilt;
            return;
        }
    }

    /// <summary>队列中是否包含指定单位。</summary>
    private static bool ContainsUnit(Queue<BattleUnit> queue, string unitId)
    {
        foreach (var unit in queue)
        {
            if (unit.UnitId == unitId)
                return true;
        }
        return false;
    }
}
