using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗回合与行动权：每方每槽每回合可行动一次；换牌与放技能均消耗该次行动。
/// 基于槽位而非卡牌追踪——换牌后新上场卡牌继承槽位的"已行动"状态，无法再次行动。
/// </summary>
public sealed class BattleTurnSystem
{
    #region Fields

    private readonly BattleFieldState _field;
    private int _roundNumber = 1;
    private BattleTurnPhase _phase = BattleTurnPhase.P1Action;

    /// <summary>
    /// P1 各槽位本回合是否已行动。
    /// </summary>
    private readonly bool[] _p1SlotActed = new bool[BattleFieldState.SlotCount];

    /// <summary>
    /// P2 各槽位本回合是否已行动。
    /// </summary>
    private readonly bool[] _p2SlotActed = new bool[BattleFieldState.SlotCount];

    private readonly List<string> _p2CardIdsBuffer = new(4);

    #endregion

    #region Properties

    public int RoundNumber => _roundNumber;
    public BattleTurnPhase Phase => _phase;

    /// <summary>
    /// 是否处于 P1 方可操作阶段（玩家阶段）。
    /// </summary>
    public bool IsPlayerActionPhase => _phase == BattleTurnPhase.P1Action;

    #endregion

    #region Events

    public event Action TurnStateChanged;

    #endregion

    #region Constructors

    public BattleTurnSystem(BattleFieldState field)
    {
        _field = field ?? throw new ArgumentNullException(nameof(field));
    }

    #endregion

    #region Public API

    public void BeginBattle()
    {
        Reset();
        _phase = BattleTurnPhase.P1Action;
        NotifyChanged();
    }

    public void Reset()
    {
        _roundNumber = 1;
        _phase = BattleTurnPhase.P1Action;
        Array.Clear(_p1SlotActed, 0, _p1SlotActed.Length);
        Array.Clear(_p2SlotActed, 0, _p2SlotActed.Length);
        _p2CardIdsBuffer.Clear();
    }

    /// <summary>
    /// 查询指定阵营当前是否为行动阶段。
    /// </summary>
    public bool IsSideActionPhase(BattleSide side)
    {
        return side == BattleSide.P1
            ? _phase == BattleTurnPhase.P1Action
            : _phase == BattleTurnPhase.P2Action;
    }

    /// <summary>
    /// P1 方单位是否可点击打开操作菜单（存活、未行动、玩家阶段、当前上场）。
    /// </summary>
    public bool CanOpenActionMenu(string unitId)
    {
        if (!IsPlayerActionPhase)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        unitId = unitId.Trim();
        var slotInfo = _field.FindSlot(unitId);
        if (slotInfo == null || slotInfo.Value.side != BattleSide.P1)
        {
            return false;
        }

        return !_p1SlotActed[slotInfo.Value.slotIndex] && _field.IsAlive(unitId);
    }

    /// <summary>
    /// 尝试选中单位以打开操作菜单。
    /// </summary>
    public bool TrySelectCardForAction(string unitId, out string failureReason)
    {
        failureReason = null;
        if (!CanOpenActionMenu(unitId))
        {
            failureReason = IsPlayerActionPhase ? "该舰娘本回合已行动或无法操作" : "当前为敌方行动阶段";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 本回合是否已消耗行动（含换牌、放技能），自动推断阵营。
    /// </summary>
    public bool HasActedThisRound(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        unitId = unitId.Trim();
        var slotInfo = _field.FindSlot(unitId);
        if (slotInfo == null)
        {
            return false;
        }

        return GetSlotActed(slotInfo.Value.slotIndex, slotInfo.Value.side);
    }

    /// <summary>
    /// 按阵营 + slot 查询本回合是否已行动。
    /// </summary>
    public bool HasActedThisRound(string unitId, BattleSide side)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        unitId = unitId.Trim();
        var slotInfo = _field.FindSlot(unitId);
        if (slotInfo == null || slotInfo.Value.side != side)
        {
            return false;
        }

        return GetSlotActed(slotInfo.Value.slotIndex, side);
    }

    /// <summary>
    /// 直接按槽位索引查询本回合是否已行动。
    /// </summary>
    public bool HasActedThisRound(int slotIndex, BattleSide side)
    {
        return GetSlotActed(slotIndex, side);
    }

    /// <summary>
    /// 标记单位本回合行动完成。
    /// </summary>
    public bool TryCompleteCardAction(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        unitId = unitId.Trim();
        var slotInfo = _field.FindSlot(unitId);
        if (slotInfo == null)
        {
            return false;
        }

        return TryCompleteCardAction(slotInfo.Value.slotIndex, slotInfo.Value.side);
    }

    /// <summary>
    /// 按 UnitId + 阵营标记行动完成。
    /// </summary>
    public bool TryCompleteCardAction(string unitId, BattleSide side)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        unitId = unitId.Trim();
        var slotInfo = _field.FindSlot(unitId);
        if (slotInfo == null || slotInfo.Value.side != side)
        {
            return false;
        }

        return TryCompleteCardAction(slotInfo.Value.slotIndex, side);
    }

    /// <summary>
    /// 按槽位索引标记行动完成（换牌等场景预先知道槽位时使用）。
    /// </summary>
    public bool TryCompleteCardAction(int slotIndex, BattleSide side)
    {
        if (side == BattleSide.P1)
        {
            if (!IsPlayerActionPhase || slotIndex < 0 || slotIndex >= _p1SlotActed.Length || _p1SlotActed[slotIndex])
            {
                return false;
            }

            _p1SlotActed[slotIndex] = true;
            if (IsP1SideRoundComplete())
            {
                EnterP2Phase();
            }
            else
            {
                NotifyChanged();
            }

            return true;
        }

        if (_phase != BattleTurnPhase.P2Action || slotIndex < 0 || slotIndex >= _p2SlotActed.Length || _p2SlotActed[slotIndex])
        {
            return false;
        }

        _p2SlotActed[slotIndex] = true;
        if (IsP2SideRoundComplete())
        {
            EndRound();
        }
        else
        {
            NotifyChanged();
        }

        return true;
    }

    /// <summary>
    /// 复制指定阵营上场存活 UnitId。
    /// </summary>
    public void CopyFieldUnitIds(BattleSide side, List<string> dest)
    {
        if (dest == null)
        {
            return;
        }

        _field.EnsureInitialized();
        _field.CopyFieldSlotUnitIds(side, dest);
    }

    /// <summary>
    /// 复制当前 P2 上场存活 UnitId。
    /// </summary>
    public void CopyP2FieldUnitIds(List<string> dest)
    {
        CopyFieldUnitIds(BattleSide.P2, dest);
    }

    /// <summary>
    /// 供 UI 显示的回合/阶段文案。
    /// </summary>
    public string GetPhaseDisplayText()
    {
        var phaseText = _phase == BattleTurnPhase.P1Action ? "我方行动" : "敌方行动";
        return $"第 {_roundNumber} 回合 · {phaseText}";
    }

    /// <summary>
    /// 若 P2 阶段无存活单位，强制结束 P2 阶段并进入下一回合。
    /// </summary>
    public bool TryForceEndP2Phase()
    {
        if (_phase != BattleTurnPhase.P2Action)
        {
            return false;
        }

        CopyP2FieldUnitIds(_p2CardIdsBuffer);
        if (_p2CardIdsBuffer.Count > 0)
        {
            return false;
        }

        EndRound();
        return true;
    }

    /// <summary>
    /// P1 方主动结束本回合：所有槽位标记为已行动，进入 P2 阶段。
    /// </summary>
    public bool TryEndP1Turn()
    {
        if (!IsPlayerActionPhase)
        {
            return false;
        }

        for (var i = 0; i < _p1SlotActed.Length; i++)
        {
            _p1SlotActed[i] = true;
        }

        EnterP2Phase();
        return true;
    }

    #endregion

    #region Private Methods

    private bool GetSlotActed(int slotIndex, BattleSide side)
    {
        var arr = side == BattleSide.P1 ? _p1SlotActed : _p2SlotActed;
        return slotIndex >= 0 && slotIndex < arr.Length && arr[slotIndex];
    }

    private void EnterP2Phase()
    {
        _phase = BattleTurnPhase.P2Action;
        NotifyChanged();
    }

    private void EndRound()
    {
        BattleBuffState.TickTurnEnd();
        _field.TickAllSkillCooldownsEndOfRound();

        _roundNumber++;
        Array.Clear(_p1SlotActed, 0, _p1SlotActed.Length);
        Array.Clear(_p2SlotActed, 0, _p2SlotActed.Length);
        _phase = BattleTurnPhase.P1Action;
        NotifyChanged();
        Debug.Log($"BattleTurnSystem: 进入第 {_roundNumber} 回合（玩家方行动）");
    }

    private bool IsP1SideRoundComplete()
    {
        return IsSideRoundComplete(BattleSide.P1);
    }

    private bool IsP2SideRoundComplete()
    {
        return IsSideRoundComplete(BattleSide.P2);
    }

    private bool IsSideRoundComplete(BattleSide side)
    {
        var slots = side == BattleSide.P1 ? _field.P1Slots : _field.P2Slots;
        var acted = side == BattleSide.P1 ? _p1SlotActed : _p2SlotActed;
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !slots[i]!.IsDead && !acted[i])
            {
                return false;
            }
        }

        return true;
    }

    private void NotifyChanged()
    {
        TurnStateChanged?.Invoke();
    }

    #endregion
}
