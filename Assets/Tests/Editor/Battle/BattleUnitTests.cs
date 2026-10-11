using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// BattleUnit 单元测试：伤害/防御/治疗/死亡/Buff/技能冷却与次数，以及状态变化事件的发布。
/// EditMode 纯逻辑测试：无需启动 Play 模式、无需场景。
/// </summary>
public class BattleUnitTests
{
    private BattleEventBus _bus;
    private BattleUnit _unit;

    /// <summary>测试初始化：每个用例独立的总线与单位。</summary>
    [SetUp]
    public void SetUp()
    {
        _bus = new BattleEventBus();
        _unit = CreateUnit("P1_0", maxHp: 100, baseDefense: 10);
    }

    /// <summary>构造测试用战斗单位的辅助方法。</summary>
    private BattleUnit CreateUnit(string unitId, int maxHp, int baseDefense)
    {
        return new BattleUnit(unitId, "card_001", "测试舰娘", BattleSide.P1, maxHp, baseDefense, _bus);
    }

    // ───────────────────────── 伤害与防御 ─────────────────────────

    /// <summary>无 Buff 时受击：实际扣血 = 原始伤害 - 基础防御。</summary>
    [Test]
    public void TakeDamage_WithoutBuff_AppliesDamageMinusBaseDefense()
    {
        _unit.TakeDamage(30, out var applied);

        Assert.AreEqual(20, applied);
        Assert.AreEqual(80, _unit.Hp);
        Assert.IsFalse(_unit.IsDead);
    }

    /// <summary>防御高于伤害：实际扣血为 0，HP 不变。</summary>
    [Test]
    public void TakeDamage_DefenseHigherThanDamage_AppliesZero()
    {
        _unit.TakeDamage(5, out var applied);

        Assert.AreEqual(0, applied);
        Assert.AreEqual(100, _unit.Hp);
    }

    /// <summary>伤害超过当前 HP：实际扣血下限为当前 HP，HP 不为负。</summary>
    [Test]
    public void TakeDamage_DamageExceedsHp_ClampsToCurrentHp()
    {
        _unit.TakeDamage(999, out var applied);

        Assert.AreEqual(100, applied);
        Assert.AreEqual(0, _unit.Hp);
        Assert.IsTrue(_unit.IsDead);
    }

    /// <summary>防御 Buff 生效后，实际防御 = 基础防御 + Buff 数值。</summary>
    [Test]
    public void TakeDamage_WithDefenseBuff_AppliesDamageMinusTotalDefense()
    {
        _unit.AddBuff(new Buff(BuffKind.DefenseBuff, 40, 2));

        _unit.TakeDamage(50, out var applied);

        Assert.AreEqual(0, applied); // 50 - (10+40) = 0
        Assert.AreEqual(100, _unit.Hp);
    }

    /// <summary>已阵亡单位不受后续伤害。</summary>
    [Test]
    public void TakeDamage_OnDeadUnit_DoesNothing()
    {
        _unit.TakeDamage(999, out _);

        _unit.TakeDamage(50, out var applied);

        Assert.AreEqual(0, applied);
        Assert.AreEqual(0, _unit.Hp);
    }

    /// <summary>受击发布 UnitDamagedEvent，携带实际伤害与受击后 HP。</summary>
    [Test]
    public void TakeDamage_PublishesUnitDamagedEvent()
    {
        UnitDamagedEvent received = null;
        using var sub = _bus.Subscribe<UnitDamagedEvent>(e => received = e);

        _unit.TakeDamage(30, out _);

        Assert.IsNotNull(received);
        Assert.AreEqual("P1_0", received.UnitId);
        Assert.AreEqual(20, received.Damage);
        Assert.AreEqual(80, received.HpAfter);
    }

    /// <summary>伤害被完全格挡时仍发布 0 伤害事件（供 UI 播放格挡表现）。</summary>
    [Test]
    public void TakeDamage_FullyBlocked_PublishesZeroDamageEvent()
    {
        var events = new List<UnitDamagedEvent>();
        using var sub = _bus.Subscribe<UnitDamagedEvent>(events.Add);

        _unit.TakeDamage(5, out _);

        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(0, events[0].Damage);
        Assert.AreEqual(100, events[0].HpAfter);
    }

    /// <summary>致死伤害依次发布 UnitDamagedEvent 与 UnitDiedEvent。</summary>
    [Test]
    public void TakeDamage_LethalDamage_PublishesDiedEvent()
    {
        UnitDiedEvent died = null;
        using var sub = _bus.Subscribe<UnitDiedEvent>(e => died = e);

        _unit.TakeDamage(999, out _);

        Assert.IsNotNull(died);
        Assert.AreEqual("P1_0", died.UnitId);
    }

    // ───────────────────────── 治疗 ─────────────────────────

    /// <summary>治疗回复不超过最大生命。</summary>
    [Test]
    public void Heal_ClampsAtMaxHp()
    {
        _unit.TakeDamage(30, out _); // 基础防御 10：实际扣 20，HP 80

        _unit.Heal(50, out var healed); // 缺失 20 < 治疗 50：回复量被钳制

        Assert.AreEqual(20, healed);
        Assert.AreEqual(100, _unit.Hp);
    }

    /// <summary>已阵亡单位不可治疗。</summary>
    [Test]
    public void Heal_OnDeadUnit_DoesNothing()
    {
        _unit.TakeDamage(999, out _);

        _unit.Heal(50, out var healed);

        Assert.AreEqual(0, healed);
        Assert.IsTrue(_unit.IsDead);
    }

    /// <summary>治疗发布 UnitHealedEvent。</summary>
    [Test]
    public void Heal_PublishesUnitHealedEvent()
    {
        UnitHealedEvent received = null;
        using var sub = _bus.Subscribe<UnitHealedEvent>(e => received = e);

        _unit.TakeDamage(40, out _); // 基础防御 10：实际扣 30，HP 70
        _unit.Heal(25, out _);       // HP 70 + 25 = 95

        Assert.IsNotNull(received);
        Assert.AreEqual(25, received.Healed);
        Assert.AreEqual(95, received.HpAfter);
    }

    // ───────────────────────── Buff ─────────────────────────

    /// <summary>同种类 Buff 不叠加：后到者整体替换。</summary>
    [Test]
    public void AddBuff_SameKind_ReplacesOld()
    {
        _unit.AddBuff(new Buff(BuffKind.DefenseBuff, 10, 1));
        _unit.AddBuff(new Buff(BuffKind.DefenseBuff, 30, 3));

        Assert.AreEqual(1, _unit.Buffs.Count);
        Assert.AreEqual(30, _unit.GetBuffTotalValue(BuffKind.DefenseBuff));
    }

    /// <summary>回合结束 Tick：到期 Buff 被移除并发布 BuffExpiredEvent。</summary>
    [Test]
    public void TickBuffsEndOfRound_ExpiredBuff_RemovedAndEventPublished()
    {
        var expired = new List<BuffExpiredEvent>();
        using var _ = _bus.Subscribe<BuffExpiredEvent>(expired.Add);
        _unit.AddBuff(new Buff(BuffKind.DefenseBuff, 20, 1));

        _unit.TickBuffsEndOfRound();

        Assert.AreEqual(0, _unit.Buffs.Count);
        Assert.AreEqual(10, _unit.Defense); // 回到基础防御
        Assert.AreEqual(1, expired.Count);
        Assert.AreEqual(BuffKind.DefenseBuff, expired[0].Kind);
    }

    /// <summary>回合结束 Tick：多回合 Buff 仅减一回合、不移除。</summary>
    [Test]
    public void TickBuffsEndOfRound_MultiTurnBuff_DecrementsButRemains()
    {
        _unit.AddBuff(new Buff(BuffKind.DefenseBuff, 20, 3));

        _unit.TickBuffsEndOfRound();

        Assert.AreEqual(1, _unit.Buffs.Count);
        Assert.AreEqual(30, _unit.Defense); // 10 基础 + 20 Buff
    }

    // ───────────────────────── 技能冷却与次数 ─────────────────────────

    /// <summary>技能冷却：设置后阻止释放，回合 Tick 后归零解除。</summary>
    [Test]
    public void SkillCooldown_BlocksThenReleasesAfterTick()
    {
        _unit.SetSkillCooldown("Skill_0001", 2);

        Assert.IsTrue(_unit.IsSkillOnCooldown("Skill_0001", out var remaining));
        Assert.AreEqual(2, remaining);

        _unit.TickSkillCooldowns();
        _unit.IsSkillOnCooldown("Skill_0001", out var remainingAfterFirstTick);
        Assert.AreEqual(1, remainingAfterFirstTick);

        _unit.TickSkillCooldowns();
        Assert.IsFalse(_unit.IsSkillOnCooldown("Skill_0001", out _));
        Assert.IsTrue(_unit.CanUseSkill("Skill_0001"));
    }

    /// <summary>未配置次数限制的技能：恒可消耗（无限制）。</summary>
    [Test]
    public void TryConsumeSkillUse_WithoutLimit_AlwaysSucceeds()
    {
        Assert.IsTrue(_unit.TryConsumeSkillUse("Skill_0001"));
        Assert.IsTrue(_unit.TryConsumeSkillUse("Skill_0001"));

        _unit.GetSkillRemainingUses("Skill_0001", out var remaining);
        Assert.AreEqual(-1, remaining); // -1 = 无限制
    }

    /// <summary>配置次数限制的技能：递减耗尽后拒绝。</summary>
    [Test]
    public void TryConsumeSkillUse_WithLimit_DecrementsThenBlocks()
    {
        _unit.SetSkillRemainingUses("Skill_0001", 2);

        Assert.IsTrue(_unit.TryConsumeSkillUse("Skill_0001"));
        Assert.IsTrue(_unit.TryConsumeSkillUse("Skill_0001"));
        Assert.IsFalse(_unit.TryConsumeSkillUse("Skill_0001"));
        Assert.IsFalse(_unit.CanUseSkill("Skill_0001"));
    }

    /// <summary>阵亡单位不可释放技能。</summary>
    [Test]
    public void CanUseSkill_OnDeadUnit_ReturnsFalse()
    {
        _unit.TakeDamage(999, out _);

        Assert.IsFalse(_unit.CanUseSkill("Skill_0001"));
    }
}
