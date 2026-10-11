using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 战斗应用服务：编排"开局 → 玩家操作 → AI 阶段 → 结算"的完整战斗用例。
/// - 开局：编队实例 → 卡牌配置 → 战斗单位 → BattleSession；
/// - 两段式选目标：TryBeginCast（进模式）→ ConfirmCastTarget（释放）；
/// - AI 阶段：阶段切换事件驱动，按 350ms 节奏逐个执行评分最高的行动；
/// - 表现层只经此服务操作战斗，绝不直接触碰领域对象。
/// </summary>
public sealed class BattleService : IBattleService
{
    /// <summary>进入 AI 阶段的起始停顿（毫秒，对应历史 0.35s）。</summary>
    private const int AiPhaseEnterDelayMs = 350;

    /// <summary>AI 连续行动之间的间隔（毫秒，对应历史 0.25s）。</summary>
    private const int AiStepIntervalMs = 250;

    /// <summary>AI 无行动时的停留时长（毫秒，对应历史 1.5s，让玩家看到敌方阶段）。</summary>
    private const int AiNoActionDwellMs = 1500;

    private readonly IConfigService _configService;
    private readonly IFleetService _fleetService;
    private readonly ICardCollectionService _collectionService;

    private BattleSession _session;
    private IDisposable _phaseSubscription;
    private string _pendingCasterUnitId;
    private string _pendingSkillId;
    private string _stageDisplayName = "战斗";
    private int _autoRunGuard;

    /// <summary>
    /// 构造战斗服务。
    /// </summary>
    public BattleService(IConfigService configService, IFleetService fleetService,
        ICardCollectionService collectionService)
    {
        _configService = configService;
        _fleetService = fleetService;
        _collectionService = collectionService;
    }

    /// <summary>当前会话的事件总线。</summary>
    public IBattleEventBus EventBus => _session?.EventBus;

    /// <summary>是否存在进行中的会话。</summary>
    public bool HasActiveSession => _session != null && !_session.IsEnded;

    /// <summary>是否正在等待玩家选择目标。</summary>
    public bool IsAwaitingTarget => _pendingCasterUnitId != null;

    /// <summary>
    /// 开始战斗：构建 P1（编队）与 P2（关卡配置）单位并启动会话。
    /// </summary>
    public async UniTask<bool> StartBattleAsync(BattleStartContext context)
    {
        ClearSession();

        if (context == null || string.IsNullOrEmpty(context.StageId))
        {
            UnityEngine.Debug.LogWarning("[BattleService] 战斗上下文无效，无法开局。");
            return false;
        }

        var stage = _configService.Get<LevelStageConfig>(context.StageId);
        if (stage == null)
        {
            UnityEngine.Debug.LogWarning($"[BattleService] 关卡配置不存在：{context.StageId}");
            return false;
        }

        _stageDisplayName = string.IsNullOrEmpty(stage.DisplayName) ? "战斗" : stage.DisplayName;

        // 0. 本局共享的事件总线（单位/战场/技能/回合全部经此发布，UI 经服务订阅）。
        var bus = new BattleEventBus();

        // 1. P2：关卡敌方卡组 → 卡牌配置 → 战斗单位。
        var p2Units = BuildEnemyUnits(stage, bus);
        if (p2Units.Count == 0)
        {
            UnityEngine.Debug.LogWarning($"[BattleService] 关卡 {context.StageId} 未配置敌方卡组。");
            return false;
        }

        // 2. P1：编队实例 ID → 实例 → 卡牌配置 → 战斗单位（首发置前）。
        var p1Units = await BuildPlayerUnitsAsync(context, bus);
        if (p1Units.Count == 0)
        {
            UnityEngine.Debug.LogWarning("[BattleService] 编队为空，无法开局。");
            return false;
        }

        // 3. 组装会话并启动。
        _session = new BattleSession(p1Units, p2Units, ResolveSkill, bus, new Random());
        _phaseSubscription = bus.Subscribe<TurnPhaseChangedEvent>(OnTurnPhaseChanged);
        _session.Start();

        UnityEngine.Debug.Log($"[BattleService] 战斗开始：Stage={context.StageId}，" +
            $"我方 {p1Units.Count} 名 / 敌方 {p2Units.Count} 名");
        return true;
    }

    /// <summary>
    /// 获取指定单位的技能菜单条目。
    /// </summary>
    public IReadOnlyList<SkillMenuEntry> GetSkillMenuEntries(string unitId)
    {
        var entries = new List<SkillMenuEntry>();
        if (_session == null)
            return entries;

        var unit = _session.Field.GetUnit(unitId);
        if (unit == null || unit.IsDead)
            return entries;

        for (var i = 0; i < unit.SkillIds.Count; i++)
        {
            var skillId = unit.SkillIds[i];
            var skill = ResolveSkill(skillId);
            if (skill == null)
                continue;

            unit.IsSkillOnCooldown(skillId, out var cooldownRemaining);
            unit.GetSkillRemainingUses(skillId, out var remainingUses);
            var usable = unit.CanUseSkill(skillId);

            entries.Add(new SkillMenuEntry
            {
                SkillId = skillId,
                DisplayName = skill.DisplayName,
                Description = skill.Description,
                IconResourceId = skill.IconResourceId,
                CooldownRemaining = cooldownRemaining,
                RemainingUses = remainingUses,
                IsUsable = usable,
                BlockReason = usable
                    ? string.Empty
                    : cooldownRemaining > 0 ? $"冷却中（剩 {cooldownRemaining} 回合）" : "次数已耗尽",
            });
        }

        return entries;
    }

    /// <summary>
    /// 获取编队成员预览（战斗前的选卡界面展示）。
    /// </summary>
    public async UniTask<IReadOnlyList<FleetCardPreview>> GetFleetPreviewAsync(int fleetIndex)
    {
        var previews = new List<FleetCardPreview>();

        var fleetInstanceIds = await _fleetService.LoadGroupAsync(fleetIndex);
        if (fleetInstanceIds == null || fleetInstanceIds.Count == 0)
            return previews;

        var owned = await _collectionService.GetCardsAsync();
        var instanceById = new Dictionary<string, CardInstance>(owned.Count);
        for (var i = 0; i < owned.Count; i++)
            instanceById[owned[i].InstanceId] = owned[i];

        for (var i = 0; i < fleetInstanceIds.Count && previews.Count < 6; i++)
        {
            if (!instanceById.TryGetValue(fleetInstanceIds[i], out var instance))
                continue;

            var card = _configService.Get<CardConfig>(instance.CardId);
            if (card == null)
                continue;

            previews.Add(new FleetCardPreview
            {
                SlotIndex = previews.Count,
                CardId = card.CardId,
                DisplayName = card.DisplayName,
                IconResourceId = card.IconResourceId,
                MaxHp = card.BaseHP,
                BaseDefense = card.BaseDefense,
            });
        }

        return previews;
    }

    /// <summary>
    /// 获取战斗结果。
    /// </summary>
    public BattleOutcome GetOutcome()
    {
        return _session?.Outcome;
    }

    /// <summary>
    /// 获取战斗只读视图。
    /// </summary>
    public BattleViewData GetBattleView()
    {
        if (_session == null)
            return null;

        var field = _session.Field;
        var turns = _session.Turns;

        return new BattleViewData
        {
            RoundNumber = turns.RoundNumber,
            Phase = turns.Phase,
            StageDisplayName = _stageDisplayName,
            P1AutoBattle = turns.P1AiEnabled,
            P1Slots = BuildSlots(BattleSide.P1, field, turns),
            P2Slots = BuildSlots(BattleSide.P2, field, turns),
            P1Reserve = BuildReserveViews(field.GetReserveUnits(BattleSide.P1)),
            P2Reserve = BuildReserveViews(field.GetReserveUnits(BattleSide.P2)),
        };
    }

    /// <summary>
    /// 获取技能的合法手动目标列表。
    /// </summary>
    public IReadOnlyList<string> GetValidTargets(string casterUnitId, string skillId)
    {
        var result = new List<string>();
        var session = _session;
        if (session == null)
            return result;

        var caster = session.Field.GetUnit(casterUnitId);
        var skill = ResolveSkill(skillId);
        if (caster == null || skill == null)
            return result;

        var poolSide = skill.TargetKind == SkillTargetKind.SingleEnemy
            ? (caster.Side == BattleSide.P1 ? BattleSide.P2 : BattleSide.P1)
            : caster.Side;

        var pool = session.Field.GetActiveUnits(poolSide);
        for (var i = 0; i < pool.Count; i++)
        {
            if (!pool[i].IsDead)
                result.Add(pool[i].UnitId);
        }
        return result;
    }

    /// <summary>
    /// 发起技能释放（两段式第一段）。
    /// </summary>
    public bool TryBeginCast(string casterUnitId, string skillId, out string failureReason)
    {
        failureReason = string.Empty;
        CancelPendingCast();

        if (_session == null || _session.IsEnded)
        {
            failureReason = "战斗未开始或已结束。";
            return false;
        }

        var skill = ResolveSkill(skillId);
        if (skill == null)
        {
            failureReason = $"技能配置不存在：{skillId}";
            return false;
        }

        var caster = _session.Field.GetUnit(casterUnitId);
        if (caster == null)
        {
            failureReason = "施法单位不存在。";
            return false;
        }

        if (!_session.Turns.IsSideActionPhase(caster.Side))
        {
            failureReason = caster.Side == BattleSide.P1 ? "当前不是我方行动阶段。" : "当前不是敌方行动阶段。";
            return false;
        }

        if (!_session.Turns.CanUnitAct(casterUnitId))
        {
            failureReason = "该单位本回合已行动或不可行动。";
            return false;
        }

        if (_session.Skills.RequiresManualTarget(skill))
        {
            // 进入两段式选目标模式：UI 高亮合法目标，等待 ConfirmCastTarget。
            var validTargets = GetValidTargets(casterUnitId, skillId);
            if (validTargets.Count == 0)
            {
                failureReason = "没有可选目标。";
                return false;
            }

            _pendingCasterUnitId = casterUnitId;
            _pendingSkillId = skillId;
            _session.EventBus.Publish(new SkillTargetSelectingEvent(casterUnitId, skillId,
                skill.DisplayName, validTargets));
            return true; // 已进入选目标模式（尚未释放）
        }

        // 自动目标技能：立即释放。
        var outcome = CastNow(casterUnitId, skill, null);
        failureReason = outcome?.FailureReason ?? string.Empty;
        return outcome != null && outcome.IsSuccess;
    }

    /// <summary>
    /// 确认技能目标（两段式第二段）。
    /// </summary>
    public bool ConfirmCastTarget(string targetUnitId, out string failureReason)
    {
        failureReason = string.Empty;

        if (_pendingCasterUnitId == null)
        {
            failureReason = "当前没有等待确认的技能。";
            return false;
        }

        var skill = ResolveSkill(_pendingSkillId);
        var outcome = CastNow(_pendingCasterUnitId, skill, targetUnitId);

        // 无论成败都退出选目标模式。
        _pendingCasterUnitId = null;
        _pendingSkillId = null;

        failureReason = outcome?.FailureReason ?? string.Empty;
        return outcome != null && outcome.IsSuccess;
    }

    /// <summary>
    /// 取消等待中的技能释放。
    /// </summary>
    public void CancelPendingCast()
    {
        _pendingCasterUnitId = null;
        _pendingSkillId = null;
    }

    /// <summary>
    /// 换牌：消耗该槽位行动权。
    /// </summary>
    public bool TrySwitchUnit(string outgoingUnitId, string incomingUnitId, out string failureReason)
    {
        failureReason = string.Empty;
        CancelPendingCast();

        if (_session == null || _session.IsEnded)
        {
            failureReason = "战斗未开始或已结束。";
            return false;
        }

        if (!_session.Field.TrySwitchActive(BattleSide.P1, outgoingUnitId, incomingUnitId, out failureReason))
            return false;

        if (_session.Field.TryGetSlotOfUnit(incomingUnitId, out _, out var slotIndex))
            _session.Turns.TryCompleteSlotAction(BattleSide.P1, slotIndex);

        TryAutoEndPlayerTurn();
        return true;
    }

    /// <summary>
    /// 结束玩家回合。
    /// </summary>
    public bool TryEndPlayerTurn(out string failureReason)
    {
        failureReason = string.Empty;
        CancelPendingCast();

        if (_session == null || _session.IsEnded)
        {
            failureReason = "战斗未开始或已结束。";
            return false;
        }

        _session.Turns.PassRemainingSideActions(BattleSide.P1);
        if (!_session.Turns.TryEndSideTurn(BattleSide.P1))
        {
            failureReason = "结束回合失败。";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 开关 P1 自动战斗；开启时若正处于 P1 阶段则立即托管。
    /// </summary>
    public void SetAutoBattle(bool enabled)
    {
        if (_session == null)
            return;

        _session.Turns.SetAiEnabled(BattleSide.P1, enabled);
        if (enabled && _session.Turns.IsSideActionPhase(BattleSide.P1))
            RunAiPhaseAsync(BattleSide.P1).Forget();
    }

    /// <summary>
    /// 撤退认负。
    /// </summary>
    public bool TryRetreat()
    {
        if (_session == null || _session.IsEnded)
            return false;

        _session.Retreat();
        return true;
    }

    /// <summary>
    /// 清理会话（重复调用安全）。
    /// </summary>
    public void ClearSession()
    {
        CancelPendingCast();
        _phaseSubscription?.Dispose();
        _phaseSubscription = null;
        _session?.Dispose();
        _session = null;
    }

    // ───────────────────────── 内部实现 ─────────────────────────

    /// <summary>技能配置解析器（注入到领域层）。</summary>
    private SkillConfig ResolveSkill(string skillId)
    {
        return _configService.Get<SkillConfig>(skillId);
    }

    /// <summary>立即释放技能并消耗行动权，自动推进回合收尾。</summary>
    private SkillCastOutcome CastNow(string casterUnitId, SkillConfig skill, string targetUnitId)
    {
        if (_session == null || skill == null)
            return SkillCastOutcome.Fail(SkillCastResult.ValidationFailed, "战斗未开始或技能不存在。");

        var ok = _session.Skills.TryCast(casterUnitId, skill, targetUnitId, out var outcome);

        if (ok && _session.Field.TryGetSlotOfUnit(casterUnitId, out var side, out var slotIndex))
        {
            _session.Turns.TryCompleteSlotAction(side, slotIndex);

            // 我方行动完毕且托管开启时，自动交棒，保证自动战斗节奏连续。
            if (side == BattleSide.P1)
                TryAutoEndPlayerTurn();
        }

        return outcome;
    }

    /// <summary>自动战斗开启时，我方全部行动完毕则自动结束回合。</summary>
    private void TryAutoEndPlayerTurn()
    {
        if (_session == null || _session.IsEnded)
            return;

        if (!_session.Turns.P1AiEnabled || !_session.Turns.IsSideActionPhase(BattleSide.P1))
            return;

        if (_session.Turns.IsSideRoundComplete(BattleSide.P1))
            _session.Turns.TryEndSideTurn(BattleSide.P1);
    }

    /// <summary>阶段切换：进入 AI 阵营的行动阶段时自动驱动。</summary>
    private void OnTurnPhaseChanged(TurnPhaseChangedEvent e)
    {
        if (_session == null || _session.IsEnded)
            return;

        var isAiPhase = e.Phase == BattleTurnPhase.P1Action && _session.Turns.P1AiEnabled
                     || e.Phase == BattleTurnPhase.P2Action && _session.Turns.P2AiEnabled;
        if (isAiPhase)
            RunAiPhaseAsync(e.Phase == BattleTurnPhase.P1Action ? BattleSide.P1 : BattleSide.P2).Forget();
    }

    /// <summary>
    /// AI 阶段驱动循环：每 350ms 评估并执行一个最优行动，
    /// 行动枯竭则跳过剩余并结束该阵营回合（首步行动前先等待，给表现层留出阶段切换动画时间）。
    /// </summary>
    private async UniTaskVoid RunAiPhaseAsync(BattleSide side)
    {
        // 防重入：同一时刻只允许一个 AI 驱动循环。
        var runId = ++_autoRunGuard;

        var actions = new List<AIAction>();
        var isFirstStep = true;
        while (_session != null && !_session.IsEnded
            && _session.Turns.IsSideActionPhase(side) && runId == _autoRunGuard)
        {
            // 对应历史节奏：进入 AI 阶段先停 0.35s；其后每个行动间隔 0.25s。
            await UniTask.Delay(isFirstStep ? AiPhaseEnterDelayMs : AiStepIntervalMs);
            isFirstStep = false;

            if (_session == null || _session.IsEnded
                || !_session.Turns.IsSideActionPhase(side) || runId != _autoRunGuard)
                return;

            actions.Clear();
            _session.AI.EvaluateBestActions(side, actions);

            if (actions.Count == 0)
            {
                // 无 AI 行动时停留 1.5s，让玩家看到敌方阶段文字与动画（对应历史行为）。
                await UniTask.Delay(AiNoActionDwellMs);
                if (_session == null || _session.IsEnded
                    || !_session.Turns.IsSideActionPhase(side) || runId != _autoRunGuard)
                    return;

                _session.AI.PassRemainingUnits(side);
                _session.Turns.TryEndSideTurn(side);
                return;
            }

            // 评分最高的候选若执行失败（如目标已死），退而求其次依次尝试。
            var acted = false;
            for (var i = 0; i < actions.Count && !acted; i++)
                acted = _session.AI.ExecuteSingleAction(actions[i]);

            if (!acted)
            {
                _session.AI.PassRemainingUnits(side);
                _session.Turns.TryEndSideTurn(side);
                return;
            }

            // 行动后若本回合完成则立即交棒，避免多等一轮节奏。
            if (_session.Turns.IsSideRoundComplete(side))
                _session.Turns.TryEndSideTurn(side);
        }
    }

    /// <summary>由关卡敌方卡组构建 P2 单位（前 2 名首发），共享会话事件总线。</summary>
    private List<BattleUnit> BuildEnemyUnits(LevelStageConfig stage, IBattleEventBus bus)
    {
        var units = new List<BattleUnit>();
        var index = 0;
        for (var i = 0; i < stage.EnemyCardIds.Count && units.Count < 6; i++)
        {
            var card = _configService.Get<CardConfig>(stage.EnemyCardIds[i]);
            if (card == null)
            {
                UnityEngine.Debug.LogWarning($"[BattleService] 敌方卡牌配置缺失：{stage.EnemyCardIds[i]}");
                continue;
            }

            units.Add(BuildUnit(BattleSide.P2, index++, card, bus));
        }

        return units;
    }

    /// <summary>
    /// 由玩家编队构建 P1 单位：首发（ActiveShipIndices 选中者）置前，其余按编队顺序替补，
    /// 共享会话事件总线。
    /// </summary>
    private async UniTask<List<BattleUnit>> BuildPlayerUnitsAsync(BattleStartContext context, IBattleEventBus bus)
    {
        var units = new List<BattleUnit>();

        var fleetInstanceIds = await _fleetService.LoadGroupAsync(context.FleetIndex);
        if (fleetInstanceIds == null || fleetInstanceIds.Count == 0)
            return units;

        // 实例 ID → 实例映射（编队存实例 ID，卡牌数值经 CardConfig 查询）。
        var owned = await _collectionService.GetCardsAsync();
        var instanceById = new Dictionary<string, CardInstance>(owned.Count);
        for (var i = 0; i < owned.Count; i++)
            instanceById[owned[i].InstanceId] = owned[i];

        // 先取卡牌配置（校验存在），再按"首发在前"重排。
        var cardsInFleet = new List<CardConfig>(fleetInstanceIds.Count);
        for (var i = 0; i < fleetInstanceIds.Count && cardsInFleet.Count < 6; i++)
        {
            if (!instanceById.TryGetValue(fleetInstanceIds[i], out var instance))
                continue;
            var card = _configService.Get<CardConfig>(instance.CardId);
            if (card != null)
                cardsInFleet.Add(card);
        }

        var ordered = OrderActivesFirst(cardsInFleet, context.ActiveShipIndices);
        for (var i = 0; i < ordered.Count; i++)
            units.Add(BuildUnit(BattleSide.P1, i, ordered[i], bus));

        return units;
    }

    /// <summary>按玩家点选的首发索引重排卡牌顺序（未选中者保持原序替补）。</summary>
    private static List<CardConfig> OrderActivesFirst(List<CardConfig> cards, int[] activeIndices)
    {
        var ordered = new List<CardConfig>(cards.Count);
        if (activeIndices == null || activeIndices.Length == 0)
        {
            ordered.AddRange(cards);
            return ordered;
        }

        for (var i = 0; i < activeIndices.Length; i++)
        {
            var idx = activeIndices[i];
            if (idx >= 0 && idx < cards.Count && !ordered.Contains(cards[idx]))
                ordered.Add(cards[idx]);
        }

        for (var i = 0; i < cards.Count; i++)
        {
            if (!ordered.Contains(cards[i]))
                ordered.Add(cards[i]);
        }

        return ordered;
    }

    /// <summary>由卡牌配置构建战斗单位。</summary>
    private static BattleUnit BuildUnit(BattleSide side, int index, CardConfig card, IBattleEventBus bus)
    {
        return new BattleUnit(UnitId.Build(side, index), card.CardId, card.DisplayName,
            side, card.BaseHP, card.BaseDefense, bus, new List<string>(card.SkillIds));
    }

    /// <summary>构建槽位视图。</summary>
    private List<BattleSlotViewData> BuildSlots(BattleSide side, BattleField field, BattleTurnSystem turns)
    {
        var slots = new List<BattleSlotViewData>(BattleField.ActiveSlotCount);
        for (var i = 0; i < BattleField.ActiveSlotCount; i++)
        {
            var unit = field.GetActiveUnitAtSlot(side, i);
            slots.Add(new BattleSlotViewData
            {
                Side = side,
                SlotIndex = i,
                Unit = unit != null ? BuildUnitView(unit, turns.HasSlotActed(side, i)) : null,
            });
        }
        return slots;
    }

    /// <summary>构建替补视图。</summary>
    private List<BattleUnitViewData> BuildReserveViews(IReadOnlyList<BattleUnit> units)
    {
        var views = new List<BattleUnitViewData>(units.Count);
        for (var i = 0; i < units.Count; i++)
            views.Add(BuildUnitView(units[i], false));
        return views;
    }

    /// <summary>由领域单位构建只读视图（头像资源经卡牌配置查询）。</summary>
    private BattleUnitViewData BuildUnitView(BattleUnit unit, bool hasActed)
    {
        var card = _configService.Get<CardConfig>(unit.CardId);
        return new BattleUnitViewData
        {
            UnitId = unit.UnitId,
            CardId = unit.CardId,
            DisplayName = unit.DisplayName,
            IconResourceId = card != null ? card.IconResourceId : 0,
            MaxHp = unit.MaxHp,
            Hp = unit.Hp,
            Defense = unit.Defense,
            Side = unit.Side,
            IsDead = unit.IsDead,
            HasActed = hasActed,
            SkillIds = unit.SkillIds,
        };
    }
}
