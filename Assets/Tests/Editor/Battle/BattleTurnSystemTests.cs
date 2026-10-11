using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// BattleTurnSystem 单元测试：阶段推进、槽位行动标记、回合结束 Tick、换牌保留行动标记。
/// </summary>
public class BattleTurnSystemTests
{
    private BattleEventBus _bus;
    private BattleField _field;
    private BattleTurnSystem _turns;

    /// <summary>测试初始化：双方各 4 名单位的战场与回合系统。</summary>
    [SetUp]
    public void SetUp()
    {
        _bus = new BattleEventBus();
        _field = new BattleField(CreateUnits(BattleSide.P1, 4), CreateUnits(BattleSide.P2, 4), _bus);
        _turns = new BattleTurnSystem(_field, _bus);
    }

    /// <summary>构造一方单位的辅助方法。</summary>
    private List<BattleUnit> CreateUnits(BattleSide side, int count)
    {
        var units = new List<BattleUnit>(count);
        for (var i = 0; i < count; i++)
            units.Add(new BattleUnit(UnitId.Build(side, i), $"card_{side}_{i}", $"舰娘{i}", side, 100, 10, _bus));
        return units;
    }

    /// <summary>标记指定阵营全部槽位已行动。</summary>
    private void ActAllSlots(BattleSide side)
    {
        for (var i = 0; i < BattleField.ActiveSlotCount; i++)
            _turns.TryCompleteSlotAction(side, i);
    }

    /// <summary>初始状态：第 1 回合、P1 行动阶段、P2 AI 默认开启。</summary>
    [Test]
    public void InitialState_RoundOne_P1Phase_P2AiOn()
    {
        Assert.AreEqual(1, _turns.RoundNumber);
        Assert.AreEqual(BattleTurnPhase.P1Action, _turns.Phase);
        Assert.IsTrue(_turns.IsSideActionPhase(BattleSide.P1));
        Assert.IsFalse(_turns.IsSideActionPhase(BattleSide.P2));
        Assert.IsTrue(_turns.IsAiEnabled(BattleSide.P2));
        Assert.IsFalse(_turns.IsAiEnabled(BattleSide.P1));
    }

    /// <summary>开战发布首回合与首阶段事件。</summary>
    [Test]
    public void Start_PublishesRoundAndPhaseEvents()
    {
        var rounds = new List<RoundStartedEvent>();
        var phases = new List<TurnPhaseChangedEvent>();
        using var _1 = _bus.Subscribe<RoundStartedEvent>(rounds.Add);
        using var _2 = _bus.Subscribe<TurnPhaseChangedEvent>(phases.Add);

        _turns.Start();

        Assert.AreEqual(1, rounds.Count);
        Assert.AreEqual(1, rounds[0].RoundNumber);
        Assert.AreEqual(1, phases.Count);
        Assert.AreEqual(BattleSide.P1, phases[0].Side);
    }

    /// <summary>槽位行动标记：成功后发布事件，重复行动被拒绝。</summary>
    [Test]
    public void TryCompleteSlotAction_MarksSlot_PublishesEvent_RejectsRepeat()
    {
        var acted = new List<SlotActedEvent>();
        using var _ = _bus.Subscribe<SlotActedEvent>(acted.Add);

        var ok = _turns.TryCompleteSlotAction(BattleSide.P1, 0);

        Assert.IsTrue(ok);
        Assert.AreEqual(1, acted.Count);
        Assert.AreEqual("P1_0", acted[0].UnitId);
        Assert.IsTrue(_turns.HasSlotActed(BattleSide.P1, 0));
        Assert.IsFalse(_turns.TryCompleteSlotAction(BattleSide.P1, 0)); // 不能重复行动
    }

    /// <summary>非行动阶段的阵营不能标记槽位行动。</summary>
    [Test]
    public void TryCompleteSlotAction_WrongPhase_Fails()
    {
        Assert.IsFalse(_turns.TryCompleteSlotAction(BattleSide.P2, 0)); // 当前是 P1 阶段
    }

    /// <summary>有槽位未行动时不能结束该阵营回合。</summary>
    [Test]
    public void TryEndSideTurn_BeforeAllSlotsActed_Fails()
    {
        _turns.TryCompleteSlotAction(BattleSide.P1, 0);

        Assert.IsFalse(_turns.TryEndSideTurn(BattleSide.P1));
    }

    /// <summary>全部行动后结束 P1 回合 → 进入 P2 阶段。</summary>
    [Test]
    public void TryEndSideTurn_P1Complete_TransitionsToP2()
    {
        ActAllSlots(BattleSide.P1);

        var ok = _turns.TryEndSideTurn(BattleSide.P1);

        Assert.IsTrue(ok);
        Assert.AreEqual(BattleTurnPhase.P2Action, _turns.Phase);
        Assert.IsTrue(_turns.IsSideActionPhase(BattleSide.P2));
    }

    /// <summary>双方回合各结束一次 → 回合数 +1、行动标记重置、冷却 Tick。</summary>
    [Test]
    public void TryEndSideTurn_BothSidesComplete_IncrementsRoundAndTicksCooldowns()
    {
        _field.GetUnit("P1_0").SetSkillCooldown("Skill_0001", 2);

        ActAllSlots(BattleSide.P1);
        Assert.IsTrue(_turns.TryEndSideTurn(BattleSide.P1));
        ActAllSlots(BattleSide.P2);
        Assert.IsTrue(_turns.TryEndSideTurn(BattleSide.P2));

        Assert.AreEqual(2, _turns.RoundNumber);
        Assert.AreEqual(BattleTurnPhase.P1Action, _turns.Phase);
        Assert.IsFalse(_turns.HasSlotActed(BattleSide.P1, 0)); // 标记重置
        _field.GetUnit("P1_0").IsSkillOnCooldown("Skill_0001", out var remaining);
        Assert.AreEqual(1, remaining); // 2 - 1 = 1
    }

    /// <summary>回合结束 Tick 全员 Buff：1 回合 Buff 到期移除。</summary>
    [Test]
    public void EndRound_TicksBuffs_OnAllUnits()
    {
        _field.GetUnit("P2_0").AddBuff(new Buff(BuffKind.DefenseBuff, 20, 1));

        ActAllSlots(BattleSide.P1);
        _turns.TryEndSideTurn(BattleSide.P1);
        ActAllSlots(BattleSide.P2);
        _turns.TryEndSideTurn(BattleSide.P2);

        Assert.AreEqual(10, _field.GetUnit("P2_0").Defense); // Buff 到期，回到基础防御
    }

    /// <summary>换牌不清除槽位行动标记（行动权绑定槽位）。</summary>
    [Test]
    public void SwitchActive_KeepsSlotActedFlag()
    {
        _turns.TryCompleteSlotAction(BattleSide.P1, 0);

        var ok = _field.TrySwitchActive(BattleSide.P1, "P1_0", "P1_2", out var reason);

        Assert.IsTrue(ok, reason);
        Assert.IsTrue(_turns.HasSlotActed(BattleSide.P1, 0));
        Assert.IsFalse(_turns.CanUnitAct("P1_2")); // 新上场者继承槽位的已行动状态
    }

    /// <summary>CanUnitAct 综合阶段/行动标记/存活判定。</summary>
    [Test]
    public void CanUnitAct_RespectsPhaseActedAndAlive()
    {
        Assert.IsTrue(_turns.CanUnitAct("P1_0"));
        Assert.IsFalse(_turns.CanUnitAct("P2_0")); // P1 阶段，敌方不可行动
        Assert.IsFalse(_turns.CanUnitAct("P1_2")); // 替补不可行动

        _turns.TryCompleteSlotAction(BattleSide.P1, 0);
        Assert.IsFalse(_turns.CanUnitAct("P1_0")); // 已行动

        _field.GetUnit("P1_1").TakeDamage(999, out _);
        Assert.IsFalse(_turns.CanUnitAct("P1_1")); // 阵亡
    }

    /// <summary>PassRemainingSideActions 一次跳过全部未行动槽位并可结束回合。</summary>
    [Test]
    public void PassRemainingSideActions_MarksAll_AndAllowsTurnEnd()
    {
        _turns.PassRemainingSideActions(BattleSide.P1);

        Assert.IsTrue(_turns.IsSideRoundComplete(BattleSide.P1));
        Assert.IsTrue(_turns.TryEndSideTurn(BattleSide.P1));
    }

    /// <summary>MarkEnded 后回合冻结，不再接受结束回合调用。</summary>
    [Test]
    public void MarkEnded_FreezesTurnSystem()
    {
        _turns.MarkEnded();

        Assert.AreEqual(BattleTurnPhase.Ended, _turns.Phase);
        Assert.IsFalse(_turns.TryEndSideTurn(BattleSide.P1));
    }
}
