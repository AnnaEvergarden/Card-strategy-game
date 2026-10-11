using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// BattleSession 单元测试：会话组装、技能次数初始化、阵亡自动终局、撤退。
/// </summary>
public class BattleSessionTests
{
    private BattleEventBus _bus;
    private readonly Dictionary<string, SkillConfig> _skillTable = new Dictionary<string, SkillConfig>();

    /// <summary>测试初始化。</summary>
    [SetUp]
    public void SetUp()
    {
        _bus = new BattleEventBus();
        _skillTable.Clear();
    }

    /// <summary>技能解析器（运行时由应用层接 ConfigService）。</summary>
    private SkillConfig ResolveSkill(string skillId)
    {
        return _skillTable.TryGetValue(skillId, out var skill) ? skill : null;
    }

    /// <summary>构造一方单位的辅助方法。</summary>
    private List<BattleUnit> CreateUnits(BattleSide side, int count, params string[] skillIds)
    {
        var units = new List<BattleUnit>(count);
        for (var i = 0; i < count; i++)
        {
            var skills = skillIds.Length > 0 ? new List<string>(skillIds) : new List<string>(0);
            units.Add(new BattleUnit(UnitId.Build(side, i), $"card_{side}_{i}", $"舰娘{i}",
                side, 100, 0, _bus, skills));
        }
        return units;
    }

    /// <summary>构建带使用次数限制的技能。</summary>
    private SkillConfig RegisterLimitedSkill(string skillId, int useLimit)
    {
        var skill = new SkillConfig
        {
            SkillId = skillId,
            DisplayName = skillId,
            TargetKind = SkillTargetKind.SingleEnemy,
            Cooldown = 1,
            UseLimit = useLimit,
            Effects = new List<SkillEffectConfig>
            {
                new SkillEffectConfig { Kind = SkillEffectKind.Damage, Value = 40 }
            }
        };
        _skillTable[skillId] = skill;
        return skill;
    }

    /// <summary>构造会话。</summary>
    private BattleSession CreateSession(List<BattleUnit> p1, List<BattleUnit> p2)
    {
        return new BattleSession(p1, p2, ResolveSkill, _bus, new Random(12345));
    }

    /// <summary>会话按配置初始化技能本场次数（UseLimit &gt; 0）。</summary>
    [Test]
    public void Constructor_InitializesSkillUseLimits()
    {
        var skill = RegisterLimitedSkill("Skill_Limit", 2);
        var p1 = CreateUnits(BattleSide.P1, 2, skill.SkillId);

        var session = CreateSession(p1, CreateUnits(BattleSide.P2, 2));

        p1[0].GetSkillRemainingUses("Skill_Limit", out var remaining);
        Assert.AreEqual(2, remaining);
        Assert.IsNull(session.Outcome);
    }

    /// <summary>开战发布战斗开始 + 首回合 + 首阶段事件。</summary>
    [Test]
    public void Start_PublishesBattleStartedRoundAndPhaseEvents()
    {
        var battleStarted = new List<BattleStartedEvent>();
        var rounds = new List<RoundStartedEvent>();
        var phases = new List<TurnPhaseChangedEvent>();
        using var _1 = _bus.Subscribe<BattleStartedEvent>(battleStarted.Add);
        using var _2 = _bus.Subscribe<RoundStartedEvent>(rounds.Add);
        using var _3 = _bus.Subscribe<TurnPhaseChangedEvent>(phases.Add);

        var session = CreateSession(CreateUnits(BattleSide.P1, 3), CreateUnits(BattleSide.P2, 3));
        session.Start();

        Assert.AreEqual(1, battleStarted.Count);
        Assert.AreEqual(1, rounds.Count);
        Assert.AreEqual(1, phases.Count);
        Assert.AreEqual(BattleSide.P1, phases[0].Side);
    }

    /// <summary>敌方全灭 → 阵亡事件自动触发终局（P1 胜利 + 结束事件）。</summary>
    [Test]
    public void EnemyAnnihilation_AutoEndsWithP1Victory()
    {
        BattleEndedEvent ended = null;
        using var sub = _bus.Subscribe<BattleEndedEvent>(e => ended = e);

        var p2 = CreateUnits(BattleSide.P2, 2);
        var session = CreateSession(CreateUnits(BattleSide.P1, 2), p2);
        session.Start();

        foreach (var unit in p2)
            unit.TakeDamage(999, out _);

        Assert.IsTrue(session.IsEnded);
        Assert.AreEqual(BattleResult.P1Victory, session.Outcome.Result);
        Assert.IsNotNull(ended);
        Assert.AreEqual(BattleResult.P1Victory, ended.Result);
    }

    /// <summary>我方全灭 → 自动判负。</summary>
    [Test]
    public void PlayerAnnihilation_AutoEndsWithP2Victory()
    {
        var session = CreateSession(CreateUnits(BattleSide.P1, 2), CreateUnits(BattleSide.P2, 3));
        session.Start();

        session.Field.GetUnit("P1_0").TakeDamage(999, out _);
        session.Field.GetUnit("P1_1").TakeDamage(999, out _);

        Assert.AreEqual(BattleResult.P2Victory, session.Outcome.Result);
    }

    /// <summary>撤退立即认负结束。</summary>
    [Test]
    public void Retreat_EndsWithP1RetreatResult()
    {
        var session = CreateSession(CreateUnits(BattleSide.P1, 2), CreateUnits(BattleSide.P2, 2));
        session.Start();

        session.Retreat();

        Assert.IsTrue(session.IsEnded);
        Assert.AreEqual(BattleResult.P1Retreat, session.Outcome.Result);
        Assert.AreEqual(BattleTurnPhase.Ended, session.Turns.Phase);
    }

    /// <summary>战斗结束后回合冻结，不再响应阶段推进。</summary>
    [Test]
    public void EndedSession_FreezesTurnSystem()
    {
        var session = CreateSession(CreateUnits(BattleSide.P1, 2), CreateUnits(BattleSide.P2, 2));
        session.Start();
        session.Retreat();

        Assert.IsFalse(session.Turns.TryEndSideTurn(BattleSide.P1));
    }

    /// <summary>Dispose 后不再自动响应阵亡事件（会话生命周期结束）。</summary>
    [Test]
    public void Dispose_UnsubscribesDeathMonitor()
    {
        var p2 = CreateUnits(BattleSide.P2, 2);
        var session = CreateSession(CreateUnits(BattleSide.P1, 2), p2);
        session.Start();
        session.Dispose();

        foreach (var unit in p2)
            unit.TakeDamage(999, out _);

        Assert.IsFalse(session.IsEnded); // 已释放的会话不再自动终局
    }

    /// <summary>阵亡后替补自动补位，且补位槽位本回合标记为已行动（补位者不可立即行动）。</summary>
    [Test]
    public void UnitDied_RefillsFromReserve_AndMarksSlotActed()
    {
        var p1 = CreateUnits(BattleSide.P1, 3); // 2 名首发 + 1 名替补
        var session = CreateSession(p1, CreateUnits(BattleSide.P2, 2));
        session.Start();

        session.Field.GetUnit("P1_0").TakeDamage(999, out _);

        Assert.AreEqual("P1_2", session.Field.GetActiveUnitAtSlot(BattleSide.P1, 0).UnitId); // 替补补位
        Assert.IsTrue(session.Turns.HasSlotActed(BattleSide.P1, 0));
        Assert.IsFalse(session.Turns.CanUnitAct("P1_2")); // 本回合不可行动
    }

    /// <summary>端到端：技能释放命中敌方但未全灭时战斗继续；全灭后自动判定胜利。</summary>
    [Test]
    public void FullFlow_CastSkillThenAnnihilation_EndsBattle()
    {
        var skill = RegisterLimitedSkill("Skill_Dmg", -1); // 无限制
        var p1 = CreateUnits(BattleSide.P1, 2, skill.SkillId);
        var p2 = CreateUnits(BattleSide.P2, 2);
        var session = CreateSession(p1, p2);
        session.Start();

        var ok1 = session.Skills.TryCast("P1_0", skill, "P2_0", out var outcome1);
        var ok2 = session.Skills.TryCast("P1_1", skill, "P2_1", out var outcome2);

        Assert.IsTrue(ok1, outcome1.FailureReason);
        Assert.IsTrue(ok2, outcome2.FailureReason);
        Assert.AreEqual(60, p2[0].Hp); // 100 - 40
        Assert.IsFalse(session.IsEnded); // 未全灭，战斗继续

        foreach (var unit in p2)
            unit.TakeDamage(999, out _);

        Assert.IsTrue(session.IsEnded);
        Assert.AreEqual(BattleResult.P1Victory, session.Outcome.Result);
    }
}
