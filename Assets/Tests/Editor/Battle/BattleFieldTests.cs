using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// BattleField 单元测试：槽位绑定/替补队列/阵亡补位/换牌/全灭判定。
/// </summary>
public class BattleFieldTests
{
    private BattleEventBus _bus;

    /// <summary>测试初始化。</summary>
    [SetUp]
    public void SetUp()
    {
        _bus = new BattleEventBus();
    }

    /// <summary>构造一方单位的辅助方法：前 2 名为首发，其余替补。</summary>
    private List<BattleUnit> CreateUnits(BattleSide side, int count)
    {
        var units = new List<BattleUnit>(count);
        for (var i = 0; i < count; i++)
            units.Add(new BattleUnit(UnitId.Build(side, i), $"card_{side}_{i}", $"舰娘{i}", side, 100, 10, _bus));
        return units;
    }

    /// <summary>构造战场：双方各 4 名单位。</summary>
    private BattleField CreateField()
    {
        return new BattleField(CreateUnits(BattleSide.P1, 4), CreateUnits(BattleSide.P2, 4), _bus);
    }

    /// <summary>构造时前 2 名绑定槽位，其余进入替补队列。</summary>
    [Test]
    public void Constructor_BindsFirstTwoUnits_AndQueuesRest()
    {
        var field = CreateField();

        Assert.AreEqual("P1_0", field.GetActiveUnitAtSlot(BattleSide.P1, 0).UnitId);
        Assert.AreEqual("P1_1", field.GetActiveUnitAtSlot(BattleSide.P1, 1).UnitId);
        Assert.AreEqual(2, field.GetReserveUnits(BattleSide.P1).Count);
        Assert.AreEqual(8, field.AllUnits.Count);
    }

    /// <summary>构造时每方发布 2 个槽位绑定事件。</summary>
    [Test]
    public void Constructor_PublishesSlotBoundEvents()
    {
        var boundEvents = new List<SlotBoundEvent>();
        using var _ = _bus.Subscribe<SlotBoundEvent>(boundEvents.Add);

        var field = CreateField();

        Assert.AreEqual(4, boundEvents.Count); // 双方各 2 槽
    }

    /// <summary>GetUnit 按 UnitId 跨双方查找。</summary>
    [Test]
    public void GetUnit_FindsByUnitId_AcrossSides()
    {
        var field = CreateField();

        Assert.AreEqual(BattleSide.P2, field.GetUnit("P2_0").Side);
        Assert.IsNull(field.GetUnit("P9_0"));
    }

    /// <summary>TryGetSlotOfUnit 返回单位所在槽位。</summary>
    [Test]
    public void TryGetSlotOfUnit_ReturnsSlotIndex()
    {
        var field = CreateField();

        var ok = field.TryGetSlotOfUnit("P2_1", out var side, out var slotIndex);

        Assert.IsTrue(ok);
        Assert.AreEqual(BattleSide.P2, side);
        Assert.AreEqual(1, slotIndex);
        Assert.IsFalse(field.TryGetSlotOfUnit("P1_2", out _, out _)); // 替补不在槽上
    }

    /// <summary>阵亡后清理：解绑并发 SlotUnboundEvent，替补自动补位并发 SlotBoundEvent。</summary>
    [Test]
    public void RemoveDeadUnitsAndFill_UnbindsDead_AndFillsFromReserve()
    {
        var field = CreateField();
        var unbound = new List<SlotUnboundEvent>();
        var bound = new List<SlotBoundEvent>();
        using var _1 = _bus.Subscribe<SlotUnboundEvent>(unbound.Add);
        using var _2 = _bus.Subscribe<SlotBoundEvent>(bound.Add);

        field.GetUnit("P1_0").TakeDamage(999, out _);
        field.RemoveDeadUnitsAndFill();

        Assert.AreEqual(1, unbound.Count);
        Assert.AreEqual("P1_0", unbound[0].UnitId);
        Assert.AreEqual("P1_2", field.GetActiveUnitAtSlot(BattleSide.P1, 0).UnitId); // 替补队首补位
        Assert.AreEqual("P1_2", bound[bound.Count - 1].UnitId);
    }

    /// <summary>无替补可补时阵亡槽位保持为空。</summary>
    [Test]
    public void RemoveDeadUnitsAndFill_NoReserve_LeavesSlotEmpty()
    {
        var field = new BattleField(CreateUnits(BattleSide.P1, 2), CreateUnits(BattleSide.P2, 2), _bus);

        field.GetUnit("P1_1").TakeDamage(999, out _);
        field.RemoveDeadUnitsAndFill();

        Assert.IsNull(field.GetActiveUnitAtSlot(BattleSide.P1, 1));
    }

    /// <summary>主动换牌：上场者占据槽位，下场者排入替补队尾，其余替补顺序不变。</summary>
    [Test]
    public void TrySwitchActive_SwapsSlots_AndKeepsReserveOrder()
    {
        var field = CreateField();

        var ok = field.TrySwitchActive(BattleSide.P1, "P1_0", "P1_3", out var reason);

        Assert.IsTrue(ok, reason);
        Assert.AreEqual("P1_3", field.GetActiveUnitAtSlot(BattleSide.P1, 0).UnitId);
        var reserve = field.GetReserveUnits(BattleSide.P1);
        Assert.AreEqual(2, reserve.Count);
        Assert.AreEqual("P1_2", reserve[0].UnitId); // 原替补顺序不变
        Assert.AreEqual("P1_0", reserve[1].UnitId); // 下场者排到队尾
    }

    /// <summary>换牌发布 UnitSwitchedEvent。</summary>
    [Test]
    public void TrySwitchActive_PublishesSwitchedEvent()
    {
        var field = CreateField();
        UnitSwitchedEvent received = null;
        using var sub = _bus.Subscribe<UnitSwitchedEvent>(e => received = e);

        field.TrySwitchActive(BattleSide.P1, "P1_0", "P1_2", out _);

        Assert.IsNotNull(received);
        Assert.AreEqual("P1_0", received.OutUnitId);
        Assert.AreEqual("P1_2", received.InUnitId);
    }

    /// <summary>已在场上的单位不能作为上场者。</summary>
    [Test]
    public void TrySwitchActive_IncomingAlreadyOnField_Fails()
    {
        var field = CreateField();

        var ok = field.TrySwitchActive(BattleSide.P1, "P1_0", "P1_1", out var reason);

        Assert.IsFalse(ok);
        Assert.IsNotEmpty(reason);
    }

    /// <summary>阵亡的替补单位不能上场。</summary>
    [Test]
    public void TrySwitchActive_DeadIncoming_Fails()
    {
        var field = CreateField();
        field.GetUnit("P1_2").TakeDamage(999, out _);

        var ok = field.TrySwitchActive(BattleSide.P1, "P1_0", "P1_2", out var reason);

        Assert.IsFalse(ok);
        Assert.IsNotEmpty(reason);
    }

    /// <summary>非本阵营单位不能作为上场者。</summary>
    [Test]
    public void TrySwitchActive_EnemyIncoming_Fails()
    {
        var field = CreateField();

        Assert.IsFalse(field.TrySwitchActive(BattleSide.P1, "P1_0", "P2_2", out _));
    }

    /// <summary>全员阵亡判定：含替补全灭才判负。</summary>
    [Test]
    public void IsSideDefeated_AllUnitsDead_ReturnsTrue()
    {
        var field = CreateField();

        Assert.IsFalse(field.IsSideDefeated(BattleSide.P1));

        foreach (var unit in field.AllUnits)
        {
            if (unit.Side == BattleSide.P1)
                unit.TakeDamage(999, out _);
        }

        Assert.IsTrue(field.IsSideDefeated(BattleSide.P1));
        Assert.IsFalse(field.IsSideDefeated(BattleSide.P2));
    }
}
