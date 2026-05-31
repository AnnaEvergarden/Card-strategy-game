#nullable enable
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本局战斗场上状态：以固定 2 槽 + 独立 BattleUnit 阵列维护双方上场与替补数据。
/// 不再使用 <see cref="CardRuntime"/> 字典——HP/防御/技能状态均挂载在 <see cref="BattleUnit"/> 上。
/// </summary>
public sealed class BattleFieldState
{
    #region Constants

    /// <summary>
    /// 每方上场槽位数。
    /// </summary>
    public const int SlotCount = FleetStore.MaxActivesPerBattleFleet; // 2

    /// <summary>
    /// 每方最大单位数（卡组上限）。
    /// </summary>
    public const int MaxUnitsPerSide = FleetStore.MaxCardsPerFleet; // 6

    #endregion

    #region Fields

    private bool _initialized;

    /// <summary>
    /// P1（玩家方）全部 6 个战斗单位。
    /// </summary>
    private readonly List<BattleUnit> _p1Units = new(MaxUnitsPerSide);

    /// <summary>
    /// P2（对手方）全部战斗单位（来自关卡配置，最多 6 个）。
    /// </summary>
    private readonly List<BattleUnit> _p2Units = new(MaxUnitsPerSide);

    /// <summary>
    /// P1 上场槽位（引用 <see cref="_p1Units"/> 中的元素，null=空槽）。
    /// </summary>
    private readonly BattleUnit?[] _p1Slots = new BattleUnit?[SlotCount];

    /// <summary>
    /// P2 上场槽位（引用 <see cref="_p2Units"/> 中的元素，null=空槽）。
    /// </summary>
    private readonly BattleUnit?[] _p2Slots = new BattleUnit?[SlotCount];

    /// <summary>
    /// P1 替补队列（UnitId 按序上场）。
    /// </summary>
    private readonly Queue<string> _p1ReserveQueue = new();

    /// <summary>
    /// P2 替补队列（UnitId 按序上场）。
    /// </summary>
    private readonly Queue<string> _p2ReserveQueue = new();

    /// <summary>
    /// 初始化技能次数时解析技能列表缓冲。
    /// </summary>
    private readonly List<SkillConfigSO> _skillsInitBuffer = new(CardConfigSO.MaxSkillsPerCard);

    /// <summary>
    /// 关卡 NPC id 初始化缓冲。
    /// </summary>
    private readonly List<string> _npcInitBuffer = new(MaxUnitsPerSide);

    #endregion

    #region Properties

    /// <summary>
    /// P1 全部单位（只读）。
    /// </summary>
    public IReadOnlyList<BattleUnit> P1Units => _p1Units;

    /// <summary>
    /// P2 全部单位（只读）。
    /// </summary>
    public IReadOnlyList<BattleUnit> P2Units => _p2Units;

    /// <summary>
    /// P1 上场槽位（只读）。使用 <see cref="GetP1Slot"/> / <see cref="GetP2Slot"/> 获取单个槽位。
    /// </summary>
    public BattleUnit?[] P1Slots => _p1Slots;

    /// <summary>
    /// P2 上场槽位（只读）。
    /// </summary>
    public BattleUnit?[] P2Slots => _p2Slots;

    /// <summary>
    /// P1 当前上场存活单位的 UnitId 列表。
    /// </summary>
    public IReadOnlyList<string> P1ActiveUnitIds
    {
        get
        {
            EnsureInitialized();
            var result = new List<string>(SlotCount);
            for (var i = 0; i < SlotCount; i++)
            {
                if (_p1Slots[i] != null && !_p1Slots[i]!.IsDead)
                {
                    result.Add(_p1Slots[i]!.UnitId);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// P2 当前上场存活单位的 UnitId 列表。
    /// </summary>
    public IReadOnlyList<string> P2ActiveUnitIds
    {
        get
        {
            EnsureInitialized();
            var result = new List<string>(SlotCount);
            for (var i = 0; i < SlotCount; i++)
            {
                if (_p2Slots[i] != null && !_p2Slots[i]!.IsDead)
                {
                    result.Add(_p2Slots[i]!.UnitId);
                }
            }
            return result;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 清空本局战斗场上数据。
    /// </summary>
    public void ClearState()
    {
        _initialized = false;
        _p1Units.Clear();
        _p2Units.Clear();
        for (var i = 0; i < SlotCount; i++)
        {
            _p1Slots[i] = null;
            _p2Slots[i] = null;
        }
        _p1ReserveQueue.Clear();
        _p2ReserveQueue.Clear();
        _skillsInitBuffer.Clear();
        _npcInitBuffer.Clear();
    }

    /// <summary>
    /// 规范化 cardId。
    /// </summary>
    public string NormalizeCardId(string cardId)
    {
        return string.IsNullOrWhiteSpace(cardId) ? string.Empty : cardId.Trim();
    }

    /// <summary>
    /// 若尚未初始化，则从 <see cref="BattleStartContext"/> 加载所有数据并创建 BattleUnit。
    /// </summary>
    public void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        ClearState();
        var configMap = GameResourceLoader.GetCardConfigMap(logOnMissing: false);

        // ── P1 初始化 ──
        var deckIds = new List<string>(MaxUnitsPerSide);
        BattleStartContext.CopySelectedDeckCardIds(deckIds);
        var p1Active = BattleStartContext.PlayerActiveCardIds;

        for (var i = 0; i < deckIds.Count; i++)
        {
            var cardId = NormalizeCardId(deckIds[i]);
            if (string.IsNullOrEmpty(cardId))
            {
                continue;
            }

            configMap.TryGetValue(cardId, out var cfg);
            var unitId = $"P1_{i}";
            var unit = new BattleUnit(BattleContext.Current, unitId, cardId, BattleSide.P1,
                cfg?.HP ?? 0);

            // 初始化技能次数
            InitializeUnitSkills(unit);

            _p1Units.Add(unit);

            // 前 N 名上场（与 P1ActiveCardIds 顺序对齐）
            if (i < p1Active.Count && p1Active[i] == cardId)
            {
                _p1Slots[i] = unit;
            }
            else if (i < SlotCount)
            {
                // 不在出场名单中，尝试用出场名单后续卡牌填充
                _p1Slots[i] = unit;
            }
            else
            {
                _p1ReserveQueue.Enqueue(unit.UnitId);
            }
        }

        // ── P2 初始化 ──
        InitializeP2Units(configMap);

        // 同步上场名单到 BattleStartContext（P1）
        SyncP1ActivesToContext();

        _initialized = true;
    }

    /// <summary>
    /// 按 UnitId 查找任意阵营的战斗单位。
    /// </summary>
    public BattleUnit? GetUnit(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return null;
        }
        unitId = unitId.Trim();

        // P1 单位
        for (var i = 0; i < _p1Units.Count; i++)
        {
            if (_p1Units[i]?.UnitId == unitId)
            {
                return _p1Units[i];
            }
        }

        // P2 单位
        for (var i = 0; i < _p2Units.Count; i++)
        {
            if (_p2Units[i]?.UnitId == unitId)
            {
                return _p2Units[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 按 CardId + Side 查找当前上场的战斗单位。
    /// </summary>
    public BattleUnit? FindUnitOnField(string cardId, BattleSide side)
    {
        cardId = NormalizeCardId(cardId);
        if (string.IsNullOrEmpty(cardId))
        {
            return null;
        }

        var slots = side == BattleSide.P1 ? _p1Slots : _p2Slots;
        for (var i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null && slots[i]!.CardId == cardId && !slots[i]!.IsDead)
            {
                return slots[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 查找某个 UnitId 绑定的槽位信息。
    /// </summary>
    public (BattleSide side, int slotIndex)? FindSlot(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return null;
        }
        unitId = unitId.Trim();

        for (var i = 0; i < SlotCount; i++)
        {
            if (_p1Slots[i]?.UnitId == unitId)
            {
                return (BattleSide.P1, i);
            }
        }

        for (var i = 0; i < SlotCount; i++)
        {
            if (_p2Slots[i]?.UnitId == unitId)
            {
                return (BattleSide.P2, i);
            }
        }

        return null;
    }

    /// <summary>
    /// 获取指定阵营的槽位数组。
    /// </summary>
    public BattleUnit?[] GetSlots(BattleSide side)
    {
        return side == BattleSide.P1 ? _p1Slots : _p2Slots;
    }

    /// <summary>
    /// 绑定单位到槽位。
    /// </summary>
    public void BindSlot(BattleSide side, int slotIndex, BattleUnit unit)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
        {
            Debug.LogError($"BattleFieldState: 槽位索引 {slotIndex} 超出范围");
            return;
        }
        if (side == BattleSide.P1)
        {
            _p1Slots[slotIndex] = unit;
        }
        else
        {
            _p2Slots[slotIndex] = unit;
        }
    }

    /// <summary>
    /// 解绑槽位（阵亡下场）。
    /// </summary>
    public void UnbindSlot(BattleSide side, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        if (side == BattleSide.P1)
        {
            _p1Slots[slotIndex] = null;
        }
        else
        {
            _p2Slots[slotIndex] = null;
        }
    }

    /// <summary>
    /// 将场上 <paramref name="focusUnitId"/> 替换为替补 <paramref name="incomingUnitId"/>，
    /// 并同步到 <see cref="BattleStartContext"/>（仅 P1 方）。
    /// </summary>
    public bool TrySwitchActive(string focusUnitId, string incomingUnitId, out string? failureReason)
    {
        failureReason = null;
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(focusUnitId))
        {
            failureReason = "未指定被替换的场上舰娘";
            return false;
        }
        if (string.IsNullOrWhiteSpace(incomingUnitId))
        {
            failureReason = "请选择要换上场的舰娘";
            return false;
        }

        var slotInfo = FindSlot(focusUnitId);
        if (slotInfo == null)
        {
            failureReason = "被替换舰娘不在场上";
            return false;
        }

        var (side, slotIndex) = slotInfo.Value;
        if (side != BattleSide.P1)
        {
            failureReason = "当前仅支持替换我方上场舰娘";
            return false;
        }

        var incoming = GetUnit(incomingUnitId);
        if (incoming == null)
        {
            failureReason = "替补舰娘不存在";
            return false;
        }
        if (incoming.IsDead)
        {
            failureReason = "已阵亡的舰娘无法上场";
            return false;
        }
        if (FindSlot(incomingUnitId) != null)
        {
            failureReason = "该舰娘已在场上";
            return false;
        }

        // 执行替换
        _p1Slots[slotIndex] = incoming;
        SyncP1ActivesToContext();
        return true;
    }

    /// <summary>
    /// 移除指定阵营中已阵亡的卡牌，从替补队列按序填充空槽。
    /// </summary>
    public bool TryRemoveDeadCards(BattleSide side)
    {
        EnsureInitialized();
        var slots = side == BattleSide.P1 ? _p1Slots : _p2Slots;
        var reserveQueue = side == BattleSide.P1 ? _p1ReserveQueue : _p2ReserveQueue;
        var changed = false;

        for (var i = 0; i < SlotCount; i++)
        {
            if (slots[i] == null)
            {
                // 空槽：尝试填充
                if (TryDequeueReserve(side, reserveQueue, out var reserve))
                {
                    slots[i] = reserve;
                    changed = true;
                    Debug.Log($"BattleFieldState: 替补 {reserve.UnitId}({reserve.CardId}) 自动上场至 {side} 槽位 {i}");
                }
                continue;
            }

            if (!slots[i]!.IsDead)
            {
                continue;
            }

            changed = true;
            Debug.Log($"BattleFieldState: {slots[i]!.CardId}({slots[i]!.UnitId}) 被击败，从 {side} 上场列表移除");

            // 尝试从替补队列填充
            if (TryDequeueReserve(side, reserveQueue, out var reserveUnit))
            {
                slots[i] = reserveUnit;
                Debug.Log($"BattleFieldState: 替补 {reserveUnit.CardId}({reserveUnit.UnitId}) 自动换入 {side} 槽位 {i}");
            }
            else
            {
                slots[i] = null;
            }
        }

        if (changed && side == BattleSide.P1)
        {
            SyncP1ActivesToContext();
        }

        return changed;
    }

    /// <summary>
    /// 复制指定阵营当前上场存活单位的 UnitId 到目标列表。
    /// </summary>
    public void CopyFieldSlotUnitIds(BattleSide side, List<string> dest)
    {
        if (dest == null) return;
        dest.Clear();
        EnsureInitialized();
        var slots = side == BattleSide.P1 ? _p1Slots : _p2Slots;
        for (var i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null && !slots[i]!.IsDead)
            {
                dest.Add(slots[i]!.UnitId);
            }
        }
    }

    /// <summary>
    /// 复制指定阵营的替补 UnitId（未上场且存活）。
    /// </summary>
    public void CopyReserveUnitIds(BattleSide side, List<string> dest)
    {
        if (dest == null) return;
        dest.Clear();
        EnsureInitialized();

        var units = side == BattleSide.P1 ? _p1Units : _p2Units;
        var slots = side == BattleSide.P1 ? _p1Slots : _p2Slots;

        for (var i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            // 不在槽位上 = 替补
            var onField = false;
            for (var s = 0; s < SlotCount; s++)
            {
                if (slots[s] == unit)
                {
                    onField = true;
                    break;
                }
            }

            if (!onField)
            {
                dest.Add(unit.UnitId);
            }
        }
    }

    /// <summary>
    /// 该方法已由 BattleUnit.CanUseSkill 替代——查场上 UnitId 对应的单位。
    /// </summary>
    public bool CanUseSkill(string unitId, SkillConfigSO skill)
    {
        return GetUnit(unitId)?.CanUseSkill(skill) ?? false;
    }

    /// <summary>
    /// 该方法已由 BattleUnit.ConsumeSkillUse 替代。
    /// </summary>
    public bool TryConsumeSkillUse(string unitId, string skillId)
    {
        return GetUnit(unitId)?.ConsumeSkillUse(skillId) ?? false;
    }

    /// <summary>
    /// 该方法已由 BattleUnit.SetCooldown 替代。
    /// </summary>
    public void SetSkillCooldown(string unitId, string skillId, int cooldownTurns)
    {
        GetUnit(unitId)?.SetCooldown(skillId, cooldownTurns);
    }

    /// <summary>
    /// 该方法已由 BattleUnit.GetSkillCooldownRemaining 替代。
    /// </summary>
    public int GetSkillCooldownRemaining(string unitId, string skillId)
    {
        return GetUnit(unitId)?.GetSkillCooldownRemaining(skillId) ?? 0;
    }

    /// <summary>
    /// 该方法已由 BattleUnit.ClearCooldown 替代。
    /// </summary>
    public bool ClearSkillCooldown(string unitId, string skillId)
    {
        return GetUnit(unitId)?.ClearCooldown(skillId) ?? false;
    }

    /// <summary>
    /// 查上场单位是否存在且存活。
    /// </summary>
    public bool IsAlive(string unitId)
    {
        var unit = GetUnit(unitId);
        return unit != null && !unit.IsDead;
    }

    /// <summary>
    /// 指定 UnitId 是否在场上指定阵营的槽位上。
    /// </summary>
    public bool IsOnSide(string unitId, BattleSide side)
    {
        var unit = GetUnit(unitId);
        return unit != null && unit.Side == side;
    }

    /// <summary>
    /// 指定 UnitId 是否在场上（任一阵营）。
    /// </summary>
    public bool IsOnField(string unitId)
    {
        return GetUnit(unitId) != null;
    }

    /// <summary>
    /// 指定 UnitId 是否为我方（P1）当前上场槽位。
    /// </summary>
    public bool IsPlayerActive(string unitId) => IsOnSide(unitId, BattleSide.P1);

    /// <summary>
    /// 返回 UnitId 所属阵营。
    /// </summary>
    public BattleSide GetSide(string unitId)
    {
        var unit = GetUnit(unitId);
        return unit?.Side ?? BattleSide.P1;
    }

    /// <summary>
    /// 是否可部署（存活）。
    /// </summary>
    public bool CanDeploy(string unitId) => IsAlive(unitId);

    /// <summary>
    /// 对所有 BattleUnit 执行回合结束冷却缩减。
    /// </summary>
    public void TickAllSkillCooldownsEndOfRound()
    {
        for (var i = 0; i < _p1Units.Count; i++)
        {
            _p1Units[i]?.TickCooldowns();
        }
        for (var i = 0; i < _p2Units.Count; i++)
        {
            _p2Units[i]?.TickCooldowns();
        }
    }

    #endregion

    #region Private Methods

    private void InitializeP2Units(Dictionary<string, CardConfigSO> configMap)
    {
        var stage = BattleStartContext.CurrentStage;
        if (stage == null)
        {
            return;
        }

        _npcInitBuffer.Clear();
        stage.CopyNpcCardIdsNonNull(_npcInitBuffer);

        for (var i = 0; i < _npcInitBuffer.Count; i++)
        {
            var cardId = NormalizeCardId(_npcInitBuffer[i]);
            if (string.IsNullOrEmpty(cardId))
            {
                continue;
            }

            configMap.TryGetValue(cardId, out var cfg);
            var unitId = $"P2_{i}";
            var unit = new BattleUnit(BattleContext.Current, unitId, cardId, BattleSide.P2,
                cfg?.HP ?? 0);

            InitializeUnitSkills(unit);
            _p2Units.Add(unit);

            if (i < SlotCount)
            {
                _p2Slots[i] = unit;
            }
            else
            {
                _p2ReserveQueue.Enqueue(unit.UnitId);
            }
        }
    }

    private void InitializeUnitSkills(BattleUnit unit)
    {
        if (unit == null || string.IsNullOrEmpty(unit.CardId))
        {
            return;
        }

        _skillsInitBuffer.Clear();
        var faction = ResolveCardFaction(unit.CardId);
        CardSkillQuery.ResolveSkillsForCard(unit.CardId, faction, _skillsInitBuffer);
        unit.InitializeSkillUses(_skillsInitBuffer);
    }

    /// <summary>
    /// 从替补队列中取下一个未阵亡的单位。
    /// </summary>
    private static bool TryDequeueReserve(BattleSide side, Queue<string> reserveQueue, out BattleUnit? result)
    {
        result = null;
        while (reserveQueue.Count > 0)
        {
            var unitId = reserveQueue.Dequeue();
            var unit = BattleContext.Current?.Field?.GetUnit(unitId);
            if (unit != null && !unit.IsDead)
            {
                result = unit;
                return true;
            }
            // 已阵亡的跳过，继续取下一个
        }
        return false;
    }

    private void SyncP1ActivesToContext()
    {
        var activeIds = new List<string>(SlotCount);
        for (var i = 0; i < SlotCount; i++)
        {
            if (_p1Slots[i] != null && !_p1Slots[i]!.IsDead)
            {
                activeIds.Add(_p1Slots[i]!.CardId);
            }
        }
        BattleStartContext.SetPlayerActives(activeIds);
    }

    private static ShipFaction ResolveCardFaction(string cardId)
    {
        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (map != null && !string.IsNullOrWhiteSpace(cardId) && map.TryGetValue(cardId.Trim(), out var cfg) && cfg != null)
        {
            return cfg.Faction;
        }
        return ShipFaction.Other;
    }

    #endregion
}
