using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// SkillSystem + SkillPipeline 单元测试：校验/冷却/次数/目标选取/效果执行/失败回滚。
/// 回滚是本测试文件的重点：任一效果失败必须逆序撤销已生效效果，且不消耗次数与冷却。
/// </summary>
public class SkillSystemTests
{
    private BattleEventBus _bus;
    private BattleField _field;
    private SkillSystem _skills;

    /// <summary>测试初始化：双方各 3 名单位（防御 0，便于数值断言）、可播种随机源。</summary>
    [SetUp]
    public void SetUp()
    {
        _bus = new BattleEventBus();
        _field = new BattleField(CreateUnits(BattleSide.P1, 3), CreateUnits(BattleSide.P2, 3), _bus);
        _skills = new SkillSystem(_field, _bus, null, null, null, new Random(12345));
    }

    /// <summary>构造一方单位的辅助方法。</summary>
    private List<BattleUnit> CreateUnits(BattleSide side, int count)
    {
        var units = new List<BattleUnit>(count);
        for (var i = 0; i < count; i++)
            units.Add(new BattleUnit(UnitId.Build(side, i), $"card_{side}_{i}", $"舰娘{i}", side, 100, 0, _bus));
        return units;
    }

    /// <summary>构造技能配置的辅助方法。</summary>
    private static SkillConfig CreateSkill(string id, SkillTargetKind kind, params SkillEffectConfig[] effects)
    {
        return new SkillConfig
        {
            SkillId = id,
            DisplayName = id,
            TargetKind = kind,
            Cooldown = 0,
            Effects = new List<SkillEffectConfig>(effects)
        };
    }

    /// <summary>构造效果配置的辅助方法。</summary>
    private static SkillEffectConfig Effect(SkillEffectKind kind, int value, int duration = 0, float chance = 1f)
    {
        return new SkillEffectConfig { Kind = kind, Value = value, Duration = duration, Chance = chance };
    }

    // ───────────────────────── 基本释放 ─────────────────────────

    /// <summary>单体敌方伤害技能：对目标造成伤害并发布开始/结束事件。</summary>
    [Test]
    public void TryCast_DamageSkill_DealsDamageAndPublishesEvents()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 30));
        var started = new List<SkillCastStartedEvent>();
        var finished = new List<SkillCastFinishedEvent>();
        using var _1 = _bus.Subscribe<SkillCastStartedEvent>(started.Add);
        using var _2 = _bus.Subscribe<SkillCastFinishedEvent>(finished.Add);

        var ok = _skills.TryCast("P1_0", skill, "P2_0", out var outcome);

        Assert.IsTrue(ok, outcome.FailureReason);
        Assert.AreEqual(70, _field.GetUnit("P2_0").Hp);
        Assert.AreEqual(1, started.Count);
        Assert.AreEqual(1, finished.Count);
        Assert.IsTrue(finished[0].Success);
    }

    /// <summary>成功释放后按配置进入冷却。</summary>
    [Test]
    public void TryCast_Success_SetsCooldown()
    {
        var skill = CreateSkill("Skill_CD", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 10));
        skill.Cooldown = 2;

        _skills.TryCast("P1_0", skill, "P2_0", out var outcome);

        Assert.IsTrue(outcome.IsSuccess);
        Assert.IsTrue(_field.GetUnit("P1_0").IsSkillOnCooldown("Skill_CD", out var remaining));
        Assert.AreEqual(2, remaining);
    }

    /// <summary>冷却中的技能不能释放。</summary>
    [Test]
    public void TryCast_OnCooldown_Fails()
    {
        var skill = CreateSkill("Skill_CD", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 10));
        skill.Cooldown = 2;

        _skills.TryCast("P1_0", skill, "P2_0", out _);
        var ok = _skills.TryCast("P1_0", skill, "P2_0", out var outcome);

        Assert.IsFalse(ok);
        Assert.AreEqual(SkillCastResult.ValidationFailed, outcome.Result);
        Assert.IsNotEmpty(outcome.FailureReason);
    }

    /// <summary>次数耗尽的技能不能释放（次数在成功后递减）。</summary>
    [Test]
    public void TryCast_UseLimitExhausted_Fails()
    {
        var skill = CreateSkill("Skill_Limit", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 10));
        var caster = _field.GetUnit("P1_0");
        caster.SetSkillRemainingUses("Skill_Limit", 1);

        Assert.IsTrue(_skills.TryCast("P1_0", skill, "P2_0", out _));
        Assert.IsFalse(_skills.TryCast("P1_0", skill, "P2_0", out var outcome));
        Assert.AreEqual(SkillCastResult.ValidationFailed, outcome.Result);
    }

    /// <summary>阵亡施法者不能释放技能。</summary>
    [Test]
    public void TryCast_DeadCaster_Fails()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 10));
        _field.GetUnit("P1_0").TakeDamage(999, out _);

        var ok = _skills.TryCast("P1_0", skill, "P2_0", out var outcome);

        Assert.IsFalse(ok);
        Assert.AreEqual(SkillCastResult.ValidationFailed, outcome.Result);
    }

    /// <summary>不在场上的单位（替补）不能施法。</summary>
    [Test]
    public void TryCast_CasterInReserve_Fails()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 10));

        var ok = _skills.TryCast("P1_2", skill, "P2_0", out var outcome);

        Assert.IsFalse(ok);
        Assert.AreEqual(SkillCastResult.ValidationFailed, outcome.Result);
        Assert.IsNotEmpty(outcome.FailureReason);
    }

    /// <summary>无效果配置的技能校验失败。</summary>
    [Test]
    public void TryCast_SkillWithoutEffects_Fails()
    {
        var skill = CreateSkill("Skill_Empty", SkillTargetKind.SingleEnemy);

        Assert.IsFalse(_skills.TryCast("P1_0", skill, "P2_0", out var outcome));
        Assert.AreEqual(SkillCastResult.ValidationFailed, outcome.Result);
    }

    // ───────────────────────── 目标选取 ─────────────────────────

    /// <summary>需要手动目标的技能未提供目标时失败。</summary>
    [Test]
    public void TryCast_SingleEnemyWithoutManualTarget_Fails()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 10));

        Assert.IsFalse(_skills.TryCast("P1_0", skill, null, out var outcome));
        Assert.AreEqual(SkillCastResult.TargetSelectionFailed, outcome.Result);
    }

    /// <summary>手动目标为友方时校验失败。</summary>
    [Test]
    public void TryCast_SingleEnemyWithAllyTarget_Fails()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 10));

        Assert.IsFalse(_skills.TryCast("P1_0", skill, "P1_1", out var outcome));
        Assert.AreEqual(SkillCastResult.TargetSelectionFailed, outcome.Result);
    }

    /// <summary>全体敌方技能命中全部在场存活敌方（含已阵亡未清理者的过滤）。</summary>
    [Test]
    public void TryCast_AllEnemies_HitsAllAliveActiveEnemies()
    {
        var skill = CreateSkill("Skill_AOE", SkillTargetKind.AllEnemies, Effect(SkillEffectKind.Damage, 20));
        _field.GetUnit("P2_1").TakeDamage(999, out _); // 一个敌方阵亡（未清理，不可作为目标）

        var ok = _skills.TryCast("P1_0", skill, null, out var outcome);

        Assert.IsTrue(ok, outcome.FailureReason);
        Assert.AreEqual(80, _field.GetUnit("P2_0").Hp);
        Assert.AreEqual(0, _field.GetUnit("P2_1").Hp); // 阵亡者不受额外伤害
    }

    /// <summary>治疗技能可作用于自身。</summary>
    [Test]
    public void TryCast_SelfHeal_RestoresOwnHp()
    {
        var skill = CreateSkill("Skill_Heal", SkillTargetKind.Self, Effect(SkillEffectKind.Heal, 25));
        _field.GetUnit("P1_0").TakeDamage(40, out _);

        var ok = _skills.TryCast("P1_0", skill, null, out var outcome);

        Assert.IsTrue(ok, outcome.FailureReason);
        Assert.AreEqual(85, _field.GetUnit("P1_0").Hp);
    }

    /// <summary>全体友方技能包含施法者自身。</summary>
    [Test]
    public void TryCast_AllAllies_IncludesCaster()
    {
        var skill = CreateSkill("Skill_DefAll", SkillTargetKind.AllAllies,
            Effect(SkillEffectKind.DefenseBuff, 15, 2));

        var ok = _skills.TryCast("P1_0", skill, null, out var outcome);

        Assert.IsTrue(ok, outcome.FailureReason);
        Assert.AreEqual(15, _field.GetUnit("P1_0").Defense);
        Assert.AreEqual(15, _field.GetUnit("P1_1").Defense);
    }

    // ───────────────────────── 失败回滚（核心保障） ─────────────────────────

    /// <summary>后续效果失败时：已生效的扣血被回滚、施法者代价被回滚、次数与冷却不结算。</summary>
    [Test]
    public void TryCast_LaterEffectFails_RollsBackEverything()
    {
        // 技能：扣敌方 30 血 → 自损 999（致死，必失败）。
        var skill = CreateSkill("Skill_Combo", SkillTargetKind.SingleEnemy,
            Effect(SkillEffectKind.Damage, 30),
            Effect(SkillEffectKind.SelfHpDrain, 999));
        skill.Cooldown = 2;
        var caster = _field.GetUnit("P1_0");
        var target = _field.GetUnit("P2_0");

        var ok = _skills.TryCast("P1_0", skill, "P2_0", out var outcome);

        Assert.IsFalse(ok);
        Assert.AreEqual(SkillCastResult.PipelineFailed, outcome.Result);

        // 已生效的扣血被回滚。
        Assert.AreEqual(100, target.Hp);
        // 致死代价被回滚（施法者存活）。
        Assert.AreEqual(100, caster.Hp);
        Assert.IsFalse(caster.IsDead);
        // 冷却未设置。
        Assert.IsFalse(caster.IsSkillOnCooldown("Skill_Combo", out _));
    }

    /// <summary>治疗作为前置效果时，后续失败同样被回滚。</summary>
    [Test]
    public void TryCast_HealThenFailingEffect_RollsBackHeal()
    {
        // 技能：治疗友军 20 → 自损 999（必失败）。
        var skill = CreateSkill("Skill_HealCombo", SkillTargetKind.SingleAlly,
            Effect(SkillEffectKind.Heal, 20),
            Effect(SkillEffectKind.SelfHpDrain, 999));
        var ally = _field.GetUnit("P1_1");
        ally.TakeDamage(50, out _); // 50 HP

        var ok = _skills.TryCast("P1_0", skill, "P1_1", out var outcome);

        Assert.IsFalse(ok);
        Assert.AreEqual(SkillCastResult.PipelineFailed, outcome.Result);
        Assert.AreEqual(50, ally.Hp); // 治疗被回滚
    }

    /// <summary>冷却刷新效果作用于施法者自身冷却：概率 1.0 刷新后管线失败回滚恢复原值。</summary>
    [Test]
    public void TryCast_RefreshCooldown_ChanceOne_RollsBackOnFailure()
    {
        // 前置：施法者自身有两个冷却中的技能。
        var caster = _field.GetUnit("P1_0");
        caster.SetSkillCooldown("Skill_A", 3);
        caster.SetSkillCooldown("Skill_B", 2);

        // 技能：刷新自身冷却 → 自损 999（必失败触发回滚）。
        var skill = CreateSkill("Skill_Refresh", SkillTargetKind.AllAllies,
            Effect(SkillEffectKind.RefreshCooldownChance, 0, 0, 1f),
            Effect(SkillEffectKind.SelfHpDrain, 999));

        var ok = _skills.TryCast("P1_0", skill, null, out var outcome);

        Assert.IsFalse(ok);
        // 冷却刷新被回滚：Skill_A / Skill_B 冷却保持原值。
        caster.IsSkillOnCooldown("Skill_A", out var remainingA);
        caster.IsSkillOnCooldown("Skill_B", out var remainingB);
        Assert.AreEqual(3, remainingA);
        Assert.AreEqual(2, remainingB);
    }

    /// <summary>冷却刷新效果：概率 1.0 时成功释放会重置施法者自身冷却。</summary>
    [Test]
    public void TryCast_RefreshCooldown_ChanceOne_ResetsCooldowns()
    {
        var caster = _field.GetUnit("P1_0");
        caster.SetSkillCooldown("Skill_A", 3);

        var skill = CreateSkill("Skill_Refresh", SkillTargetKind.AllAllies,
            Effect(SkillEffectKind.RefreshCooldownChance, 0, 0, 1f));

        var ok = _skills.TryCast("P1_0", skill, null, out var outcome);

        Assert.IsTrue(ok, outcome.FailureReason);
        Assert.IsFalse(caster.IsSkillOnCooldown("Skill_A", out _)); // 冷却被重置
    }
}
