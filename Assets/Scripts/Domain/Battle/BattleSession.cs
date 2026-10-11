using System;
using System.Collections.Generic;

/// <summary>
/// 战斗会话（领域组合根）：一场战斗的全部运行时组件的组装与生命周期。
/// 由 BattleService（应用层）构建单位列表后创建；持有战场、回合、技能系统与 AI，
/// 自动监听阵亡事件判定胜负，对外只暴露只读属性与少量流程方法。
/// </summary>
public sealed class BattleSession : IDisposable
{
    private readonly Func<string, SkillConfig> _skillResolver;
    private readonly IDisposable _deathSubscription;

    /// <summary>战斗事件总线（本局独有，UI 经应用层订阅）。</summary>
    public IBattleEventBus EventBus { get; }

    /// <summary>战场。</summary>
    public BattleField Field { get; }

    /// <summary>回合系统。</summary>
    public BattleTurnSystem Turns { get; }

    /// <summary>技能系统。</summary>
    public SkillSystem Skills { get; }

    /// <summary>AI 决策服务（P1 托管与 P2 敌方共用）。</summary>
    public BattleAIService AI { get; }

    /// <summary>战斗结果；未结束时为 null。</summary>
    public BattleOutcome Outcome { get; private set; }

    /// <summary>战斗是否已结束。</summary>
    public bool IsEnded => Outcome != null;

    /// <summary>
    /// 构建战斗会话：组装战场、回合、技能与 AI，并按配置初始化技能使用次数。
    /// </summary>
    /// <param name="p1Units">P1 全部单位（前 2 名为首发）</param>
    /// <param name="p2Units">P2 全部单位（前 2 名为首发）</param>
    /// <param name="skillResolver">技能 ID → 配置解析器（应用层注入 ConfigService 查询）</param>
    /// <param name="eventBus">战斗事件总线实例</param>
    /// <param name="random">随机源</param>
    public BattleSession(IReadOnlyList<BattleUnit> p1Units, IReadOnlyList<BattleUnit> p2Units,
        Func<string, SkillConfig> skillResolver, IBattleEventBus eventBus, Random random)
    {
        _skillResolver = skillResolver ?? (_ => null);
        EventBus = eventBus ?? new BattleEventBus();

        Field = new BattleField(p1Units, p2Units, EventBus);
        Turns = new BattleTurnSystem(Field, EventBus);

        var effectRegistry = new SkillEffectRegistry();
        Skills = new SkillSystem(Field, EventBus, null, null, new SkillPipeline(effectRegistry), random);

        AI = new BattleAIService(Field, Turns, Skills, _skillResolver, null, null, random);

        InitializeSkillUseLimits(p1Units, _skillResolver);
        InitializeSkillUseLimits(p2Units, _skillResolver);

        // 数据所有权：任一单位阵亡即触发"解绑补位 + 行动权标记 + 终局检查"
        //（补位者本回合不可行动；应用层无需在各用例里重复处理）。
        _deathSubscription = EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
    }

    /// <summary>
    /// 阵亡处理：解绑阵亡者并尝试补位，补位槽位本回合标记为已行动，随后判定胜负。
    /// </summary>
    private void OnUnitDied(UnitDiedEvent e)
    {
        if (IsEnded)
            return;

        for (var s = 0; s < 2; s++)
        {
            var side = (BattleSide)s;
            for (var i = 0; i < BattleField.ActiveSlotCount; i++)
            {
                var unit = Field.GetActiveUnitAtSlot(side, i);
                if (unit == null || !unit.IsDead)
                    continue;

                if (Field.UnbindDeadAndFill(side, i))
                    Turns.TryCompleteSlotAction(side, i); // 补位者本回合不可行动
            }
        }

        CheckForEnd();
    }

    /// <summary>
    /// 开战：发布战斗开始事件与首回合事件（UI 须在调用前完成事件订阅）。
    /// </summary>
    public void Start()
    {
        if (IsEnded)
            return;

        EventBus.Publish(new BattleStartedEvent());
        Turns.Start();
    }

    /// <summary>
    /// 终局检查：一方全员阵亡即判定胜负并发布战斗结束事件。
    /// </summary>
    public void CheckForEnd()
    {
        if (IsEnded)
            return;

        if (Field.IsSideDefeated(BattleSide.P2))
        {
            End(BattleOutcome.P1Won(Turns.RoundNumber, "敌方全员击破"));
        }
        else if (Field.IsSideDefeated(BattleSide.P1))
        {
            End(BattleOutcome.P2Won(Turns.RoundNumber, "我方舰队全灭"));
        }
    }

    /// <summary>
    /// 撤退：立即以 P1 认负结束战斗。
    /// </summary>
    public void Retreat()
    {
        if (IsEnded)
            return;

        End(BattleOutcome.P1Retreated(Turns.RoundNumber, "我方选择撤退"));
    }

    /// <summary>
    /// 结束战斗：记录结果、冻结回合系统、发布结束事件。
    /// </summary>
    private void End(BattleOutcome outcome)
    {
        Outcome = outcome;
        Turns.MarkEnded();
        EventBus.Publish(new BattleEndedEvent(outcome.Result, outcome.WinnerSide,
            outcome.RoundNumber, outcome.Reason));
    }

    /// <summary>
    /// 按技能配置初始化本场使用次数（UseLimit &gt; 0 时才登记；无限制不占条目）。
    /// </summary>
    private static void InitializeSkillUseLimits(IReadOnlyList<BattleUnit> units,
        Func<string, SkillConfig> skillResolver)
    {
        for (var i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            for (var s = 0; s < unit.SkillIds.Count; s++)
            {
                var skillId = unit.SkillIds[s];
                var skill = skillResolver(skillId);
                if (skill != null && skill.UseLimit > 0)
                    unit.SetSkillRemainingUses(skillId, skill.UseLimit);
            }
        }
    }

    /// <summary>
    /// 释放会话：退订阵亡监听（战场/单位数据随 GC 回收，无 Unity 资源需释放）。
    /// </summary>
    public void Dispose()
    {
        _deathSubscription?.Dispose();
    }
}
