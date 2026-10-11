using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// BattleAIService 单元测试：候选评估（击杀优先/治疗优先/跳过不可用技能）与行动执行。
/// </summary>
public class BattleAIServiceTests
{
    private BattleEventBus _bus;
    private BattleField _field;
    private BattleTurnSystem _turns;
    private SkillSystem _skills;
    private BattleAIService _ai;
    private readonly Dictionary<string, SkillConfig> _skillTable = new Dictionary<string, SkillConfig>();

    /// <summary>测试初始化：双方各 3 名单位、技能表、AI 服务（技能解析走本地字典）。</summary>
    [SetUp]
    public void SetUp()
    {
        _bus = new BattleEventBus();
        _skillTable.Clear();
        _field = new BattleField(CreateUnits(BattleSide.P1, 3), CreateUnits(BattleSide.P2, 3), _bus);
        _turns = new BattleTurnSystem(_field, _bus);
        _skills = new SkillSystem(_field, _bus, null, null, null, new Random(12345));
        _ai = new BattleAIService(_field, _turns, _skills, ResolveSkill, null, null, new Random(12345));
    }

    /// <summary>技能解析器：AI 经注入的委托查询技能配置（运行时由应用层接 ConfigService）。</summary>
    private SkillConfig ResolveSkill(string skillId)
    {
        return _skillTable.TryGetValue(skillId, out var skill) ? skill : null;
    }

    /// <summary>构造一方单位的辅助方法：默认携带全部测试技能。</summary>
    private List<BattleUnit> CreateUnits(BattleSide side, int count, params string[] skillIds)
    {
        var units = new List<BattleUnit>(count);
        for (var i = 0; i < count; i++)
        {
            var skills = skillIds.Length > 0
                ? new List<string>(skillIds)
                : new List<string>(_skillTable.Keys);
            units.Add(new BattleUnit(UnitId.Build(side, i), $"card_{side}_{i}", $"舰娘{i}",
                side, 100, 0, _bus, skills));
        }
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

    /// <summary>伤害技能优先选择可击杀的残血目标（击杀加成）。</summary>
    [Test]
    public void EvaluateBestActions_DamageSkill_PrefersKillableTarget()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 30));
        _skillTable[skill.SkillId] = skill;
        var field = new BattleField(CreateUnits(BattleSide.P1, 2, skill.SkillId),
            CreateUnits(BattleSide.P2, 3, skill.SkillId), _bus);
        var turns = new BattleTurnSystem(field, _bus);
        var skillsSys = new SkillSystem(field, _bus, null, null, null, new Random(1));
        var ai = new BattleAIService(field, turns, skillsSys, ResolveSkill, null, null, new Random(1));

        field.GetUnit("P2_0").TakeDamage(80, out _); // 残血（20 HP，30 伤害可击杀）

        var results = new List<AIAction>();
        ai.EvaluateBestActions(BattleSide.P1, results);

        Assert.IsNotEmpty(results);
        Assert.AreEqual("P2_0", results[0].TargetUnitId); // 优先击杀残血
        Assert.Greater(results[0].Score, 0f);
    }

    /// <summary>治疗技能优先选择损血最多的友方。</summary>
    [Test]
    public void EvaluateBestActions_HealSkill_PrefersMostDamagedAlly()
    {
        var skill = CreateSkill("Skill_Heal", SkillTargetKind.SingleAlly, Effect(SkillEffectKind.Heal, 40));
        _skillTable[skill.SkillId] = skill;
        var field = new BattleField(CreateUnits(BattleSide.P1, 3, skill.SkillId),
            CreateUnits(BattleSide.P2, 2, skill.SkillId), _bus);
        var turns = new BattleTurnSystem(field, _bus);
        var skillsSys = new SkillSystem(field, _bus, null, null, null, new Random(1));
        var ai = new BattleAIService(field, turns, skillsSys, ResolveSkill, null, null, new Random(1));

        field.GetUnit("P1_1").TakeDamage(60, out _); // 大损
        field.GetUnit("P1_2").TakeDamage(20, out _); // 小损

        var results = new List<AIAction>();
        ai.EvaluateBestActions(BattleSide.P1, results);

        Assert.IsNotEmpty(results);
        Assert.AreEqual("P1_1", results[0].TargetUnitId);
    }

    /// <summary>冷却中的技能不产生候选。</summary>
    [Test]
    public void EvaluateBestActions_SkipsCooldownSkills()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 30));
        skill.Cooldown = 2;
        _skillTable[skill.SkillId] = skill;
        var field = new BattleField(CreateUnits(BattleSide.P1, 2, skill.SkillId),
            CreateUnits(BattleSide.P2, 2, skill.SkillId), _bus);
        var turns = new BattleTurnSystem(field, _bus);
        var skillsSys = new SkillSystem(field, _bus, null, null, null, new Random(1));
        var ai = new BattleAIService(field, turns, skillsSys, ResolveSkill, null, null, new Random(1));

        field.GetUnit("P1_0").SetSkillCooldown(skill.SkillId, 2); // 一人冷却中
        field.GetUnit("P1_1").SetSkillCooldown(skill.SkillId, 2); // 两人都冷却中

        var results = new List<AIAction>();
        ai.EvaluateBestActions(BattleSide.P1, results);

        Assert.IsEmpty(results);
    }

    /// <summary>结果按评分降序排列。</summary>
    [Test]
    public void EvaluateBestActions_SortsByScoreDescending()
    {
        var dmg = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 25));
        var heal = CreateSkill("Skill_Heal", SkillTargetKind.Self, Effect(SkillEffectKind.Heal, 10));
        _skillTable[dmg.SkillId] = dmg;
        _skillTable[heal.SkillId] = heal;
        var field = new BattleField(CreateUnits(BattleSide.P1, 2, dmg.SkillId, heal.SkillId),
            CreateUnits(BattleSide.P2, 2, dmg.SkillId, heal.SkillId), _bus);
        var turns = new BattleTurnSystem(field, _bus);
        var skillsSys = new SkillSystem(field, _bus, null, null, null, new Random(1));
        var ai = new BattleAIService(field, turns, skillsSys, ResolveSkill, null, null, new Random(1));

        var results = new List<AIAction>();
        ai.EvaluateBestActions(BattleSide.P1, results);

        Assert.IsNotEmpty(results);
        for (var i = 1; i < results.Count; i++)
            Assert.GreaterOrEqual(results[i - 1].Score, results[i].Score);
    }

    /// <summary>ExecuteSingleAction 走技能管道释放并消耗槽位行动权。</summary>
    [Test]
    public void ExecuteSingleAction_CastsSkill_AndCompletesSlotAction()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 20));
        _skillTable[skill.SkillId] = skill;
        var field = new BattleField(CreateUnits(BattleSide.P1, 2, skill.SkillId),
            CreateUnits(BattleSide.P2, 2, skill.SkillId), _bus);
        var turns = new BattleTurnSystem(field, _bus);
        var skillsSys = new SkillSystem(field, _bus, null, null, null, new Random(1));
        var ai = new BattleAIService(field, turns, skillsSys, ResolveSkill, null, null, new Random(1));

        var action = new AIAction("P1_0", skill, "P2_0", 100f);
        var ok = ai.ExecuteSingleAction(action);

        Assert.IsTrue(ok);
        Assert.AreEqual(80, field.GetUnit("P2_0").Hp);
        Assert.IsTrue(turns.HasSlotActed(BattleSide.P1, 0));
    }

    /// <summary>非法行动（目标已阵亡）执行失败，不消耗行动权。</summary>
    [Test]
    public void ExecuteSingleAction_InvalidTarget_FailsWithoutConsumingAction()
    {
        var skill = CreateSkill("Skill_Dmg", SkillTargetKind.SingleEnemy, Effect(SkillEffectKind.Damage, 20));
        _skillTable[skill.SkillId] = skill;
        var field = new BattleField(CreateUnits(BattleSide.P1, 2, skill.SkillId),
            CreateUnits(BattleSide.P2, 2, skill.SkillId), _bus);
        var turns = new BattleTurnSystem(field, _bus);
        var skillsSys = new SkillSystem(field, _bus, null, null, null, new Random(1));
        var ai = new BattleAIService(field, turns, skillsSys, ResolveSkill, null, null, new Random(1));

        field.GetUnit("P2_0").TakeDamage(999, out _); // 目标已死
        var action = new AIAction("P1_0", skill, "P2_0", 100f);

        var ok = ai.ExecuteSingleAction(action);

        Assert.IsFalse(ok);
        Assert.IsFalse(turns.HasSlotActed(BattleSide.P1, 0));
    }

    /// <summary>AI 不会选择会让自己阵亡的自我损耗技能。</summary>
    [Test]
    public void EvaluateBestActions_SelfDrainSkill_NeverScoresPositive_WhenLethal()
    {
        var skill = CreateSkill("Skill_Suicide", SkillTargetKind.Self,
            Effect(SkillEffectKind.Damage, 10),
            Effect(SkillEffectKind.SelfHpDrain, 999)); // 致死代价
        _skillTable[skill.SkillId] = skill;
        var field = new BattleField(CreateUnits(BattleSide.P1, 2, skill.SkillId),
            CreateUnits(BattleSide.P2, 2, skill.SkillId), _bus);
        var turns = new BattleTurnSystem(field, _bus);
        var skillsSys = new SkillSystem(field, _bus, null, null, null, new Random(1));
        var ai = new BattleAIService(field, turns, skillsSys, ResolveSkill, null, null, new Random(1));

        var results = new List<AIAction>();
        ai.EvaluateBestActions(BattleSide.P1, results);

        Assert.IsNotEmpty(results);
        Assert.Less(results[0].Score, 0f); // 唯一候选但为强负分（AI 宁愿跳过也不自杀）
    }

    /// <summary>PassRemainingUnits 跳过全部未行动槽位，使回合可以结束。</summary>
    [Test]
    public void PassRemainingUnits_MarksAllUnactedSlots()
    {
        _ai.PassRemainingUnits(BattleSide.P1);

        Assert.IsTrue(_turns.IsSideRoundComplete(BattleSide.P1));
    }
}
