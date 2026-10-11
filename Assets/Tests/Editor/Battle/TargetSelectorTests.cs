using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 目标选取策略单元测试：各 SkillTargetKind 的选取规则与手动目标校验。
/// </summary>
public class TargetSelectorTests
{
    private BattleEventBus _bus;
    private BattleField _field;
    private TargetSelector _selector;

    /// <summary>测试初始化：双方各 3 名单位、施法者为 P1_0。</summary>
    [SetUp]
    public void SetUp()
    {
        _bus = new BattleEventBus();
        _field = new BattleField(CreateUnits(BattleSide.P1, 3), CreateUnits(BattleSide.P2, 3), _bus);
        _selector = new TargetSelector();
        _context = new SkillExecutionContext(_field.GetUnit("P1_0"), null, null, _field, new Random(1));
    }

    private SkillExecutionContext _context;

    /// <summary>构造一方单位的辅助方法。</summary>
    private List<BattleUnit> CreateUnits(BattleSide side, int count)
    {
        var units = new List<BattleUnit>(count);
        for (var i = 0; i < count; i++)
            units.Add(new BattleUnit(UnitId.Build(side, i), $"card_{side}_{i}", $"舰娘{i}", side, 100, 0, _bus));
        return units;
    }

    /// <summary>构造技能配置的辅助方法。</summary>
    private static SkillConfig CreateSkill(SkillTargetKind kind)
    {
        return new SkillConfig { SkillId = "Skill_T", DisplayName = "测试技能", TargetKind = kind };
    }

    /// <summary>单体敌方：合法手动目标选取成功。</summary>
    [Test]
    public void SingleEnemy_ValidManualTarget_Succeeds()
    {
        var ok = _selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.SingleEnemy),
            "P2_0", out var targets, out _);

        Assert.IsTrue(ok);
        Assert.AreEqual(1, targets.Count);
        Assert.AreEqual("P2_0", targets[0].UnitId);
    }

    /// <summary>单体敌方：未提供手动目标失败（提示选择目标）。</summary>
    [Test]
    public void SingleEnemy_WithoutManualTarget_Fails()
    {
        Assert.IsFalse(_selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.SingleEnemy),
            null, out _, out var reason));
        Assert.IsNotEmpty(reason);
    }

    /// <summary>单体敌方：友方目标被拒绝。</summary>
    [Test]
    public void SingleEnemy_AllyTarget_Fails()
    {
        Assert.IsFalse(_selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.SingleEnemy),
            "P1_1", out _, out _));
    }

    /// <summary>单体敌方：替补（不在场上）目标被拒绝。</summary>
    [Test]
    public void SingleEnemy_ReserveTarget_Fails()
    {
        Assert.IsFalse(_selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.SingleEnemy),
            "P2_2", out _, out _));
    }

    /// <summary>单体友方：可选施法者自身（治疗类技能）。</summary>
    [Test]
    public void SingleAlly_CasterSelf_Succeeds()
    {
        var ok = _selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.SingleAlly),
            "P1_0", out var targets, out _);

        Assert.IsTrue(ok);
        Assert.AreEqual("P1_0", targets[0].UnitId);
    }

    /// <summary>全体敌方：仅选取在场存活敌方（阵亡未清理者被过滤）。</summary>
    [Test]
    public void AllEnemies_FiltersDeadBoundUnits()
    {
        _field.GetUnit("P2_0").TakeDamage(999, out _); // 阵亡但未清理

        var ok = _selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.AllEnemies),
            null, out var targets, out _);

        Assert.IsTrue(ok);
        Assert.AreEqual(1, targets.Count); // 只剩 P2_1
        Assert.AreEqual("P2_1", targets[0].UnitId);
    }

    /// <summary>全体友方：包含施法者与场上队友。</summary>
    [Test]
    public void AllAllies_IncludesCasterAndTeammates()
    {
        var ok = _selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.AllAllies),
            null, out var targets, out _);

        Assert.IsTrue(ok);
        Assert.AreEqual(2, targets.Count);
    }

    /// <summary>自身目标：恒为施法者。</summary>
    [Test]
    public void Self_AlwaysCaster()
    {
        var ok = _selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.Self),
            null, out var targets, out _);

        Assert.IsTrue(ok);
        Assert.AreEqual("P1_0", targets[0].UnitId);
    }

    /// <summary>全体目标：双方在场存活单位合计。</summary>
    [Test]
    public void AllTargets_BothSides()
    {
        var ok = _selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.AllTargets),
            null, out var targets, out _);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, targets.Count);
    }

    /// <summary>无目标类型：返回空列表且成功。</summary>
    [Test]
    public void None_EmptyTargets_Succeeds()
    {
        var ok = _selector.TrySelectTargets(_context, CreateSkill(SkillTargetKind.None),
            null, out var targets, out _);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, targets.Count);
    }

    /// <summary>RequiresManualTarget：仅单体敌方/友方需要手动选择。</summary>
    [Test]
    public void RequiresManualTarget_OnlySingleKinds()
    {
        Assert.IsTrue(_selector.RequiresManualTarget(CreateSkill(SkillTargetKind.SingleEnemy)));
        Assert.IsTrue(_selector.RequiresManualTarget(CreateSkill(SkillTargetKind.SingleAlly)));
        Assert.IsFalse(_selector.RequiresManualTarget(CreateSkill(SkillTargetKind.AllEnemies)));
        Assert.IsFalse(_selector.RequiresManualTarget(CreateSkill(SkillTargetKind.Self)));
        Assert.IsFalse(_selector.RequiresManualTarget(CreateSkill(SkillTargetKind.None)));
    }
}
