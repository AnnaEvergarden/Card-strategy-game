using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 战斗主界面占位：上方敌方首发（关卡 NPC 配置前 2 名），下方我方出战（玩家所选若干名）。
/// 请将 <see cref="BasePanel.PanelName"/> 设为 <see cref="PanelNames.BattleMainPanel"/>（Awake 会补全）；初始建议隐藏。
/// </summary>
public sealed class BattleMainPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// P2（对手方/敌方）舰娘槽位父节点（横向排列由预制体/布局决定）。
    /// </summary>
    [FormerlySerializedAs("enemyRowRoot")]
    [SerializeField] private RectTransform p2RowRoot;

    /// <summary>
    /// P1（玩家方）舰娘槽位父节点。
    /// </summary>
    [FormerlySerializedAs("playerRowRoot")]
    [SerializeField] private RectTransform p1RowRoot;

    /// <summary>
    /// 单行舰娘展示预制体（挂 <see cref="BattleShipFieldSlotView"/>）。
    /// </summary>
    [SerializeField] private GameObject shipFieldSlotPrefab;

    /// <summary>
    /// 离开战斗返回游戏场景。
    /// </summary>
    [SerializeField] private Button leaveBattleBtn;

    /// <summary>
    /// 结束回合：跳过未行动的我方单位并进入敌方行动阶段。
    /// </summary>
    [SerializeField] private Button endTurnBtn;

    /// <summary>
    /// 标题或提示（可选）。
    /// </summary>
    [SerializeField] private TMP_Text headerText;

    /// <summary>
    /// NPC id 缓冲。
    /// </summary>
    private readonly List<string> _npcIdsBuffer = new(FleetStore.MaxCardsPerFleet);

    /// <summary>
    /// cardId 到 <see cref="CardConfigSO"/> 的映射缓存（通过 <see cref="GameResourceLoader.GetCardConfigMap"/> 加载）。
    /// 非 readonly 以允许 <see cref="LoadConfigMap"/> 替换为全局共享字典引用。
    /// </summary>
    private Dictionary<string, CardConfigSO> _configMap = new();

    /// <summary>
    /// P1 方槽位 UnitId → 视图（回合表现用）。
    /// </summary>
    private readonly Dictionary<string, BattleShipFieldSlotView> _p1SlotViews = new();

    /// <summary>
    /// P2 方槽位 UnitId → 视图（回合表现用）。
    /// </summary>
    private readonly Dictionary<string, BattleShipFieldSlotView> _p2SlotViews = new();

    /// <summary>
    /// 当前选中放大中的 UnitId。
    /// </summary>
    private string _highlightedUnitId = string.Empty;

    /// <summary>
    /// P2 阶段自动结算协程。
    /// </summary>
    private Coroutine _p2PhaseRoutine;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中的战斗主界面实例。
    /// </summary>
    public static BattleMainPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<BattleMainPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.BattleMainPanel;
            }

            return existing;
        }

        return null;
    }

    /// <summary>
    /// 刷新敌我上场行（技能伤害、换牌后调用）。
    /// </summary>
    public void RefreshBattlefield()
    {
        LoadConfigMap();
        RebuildBattlefield();
        RefreshTurnPresentation();
    }

    /// <summary>
    /// 刷新回合标题与各槽位行动态（窗口缩放/禁点）。
    /// </summary>
    public void RefreshTurnPresentation()
    {
        UpdateHeaderTurnText();
        UpdateEndTurnButtonState();
        foreach (var pair in _p1SlotViews)
        {
            pair.Value?.ApplyTurnInteractionState();
        }
        foreach (var pair in _p2SlotViews)
        {
            pair.Value?.ApplyTurnInteractionState();
        }
    }

    /// <summary>
    /// 打开操作菜单时对当前卡牌播放 DOTween 放大。
    /// </summary>
    /// <param name="unitId">舰娘 UnitId。</param>
    public void HighlightSlotForAction(string unitId)
    {
        ClearSlotHighlight();
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return;
        }

        unitId = unitId.Trim();
        _highlightedUnitId = unitId;
        if (_p1SlotViews.TryGetValue(unitId, out var view) && view != null)
        {
            view.PlayActionMenuOpenScale();
        }
    }

    /// <summary>
    /// 取消选中放大（未行动就关闭菜单时调用）。
    /// </summary>
    /// <param name="unitId">舰娘 UnitId。</param>
    public void ClearSlotHighlightIfOwner(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId) ||
            !string.Equals(_highlightedUnitId, unitId.Trim(), System.StringComparison.Ordinal))
        {
            return;
        }

        ClearSlotHighlight();
    }

    /// <summary>
    /// 仅刷新 P1 方上场行（换牌后调用）。
    /// </summary>
    public void RefreshPlayerBattlefield()
    {
        LoadConfigMap();
        var field = BattleContext.Current.Field;
        field.EnsureInitialized();
        _p1SlotViews.Clear();
        ClearRow(p1RowRoot);

        if (shipFieldSlotPrefab == null || p1RowRoot == null)
        {
            return;
        }

        // 直接遍历槽位数组，避免同 cardId 时 FindUnitOnField 返回错误单位
        for (var i = 0; i < field.P1Slots.Length; i++)
        {
            var unit = field.P1Slots[i];
            if (unit != null && !unit.IsDead)
            {
                SpawnFieldSlot(p1RowRoot, unit, BattleSide.P1);
            }
        }

        RefreshTurnPresentation();
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 注册前写入默认面板名。
    /// </summary>
    protected override void Awake()
    {
        if (string.IsNullOrWhiteSpace(PanelName))
        {
            PanelName = PanelNames.BattleMainPanel;
        }

        base.Awake();
    }

    /// <summary>
    /// 启用时订阅并生成敌我占位行。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        BattleContext.Current.Turns.TurnStateChanged += OnTurnStateChanged;
        BattleContext.Current.Field.EnsureInitialized();
        BattleContext.Current.Turns.BeginBattle();
        LoadConfigMap();
        RebuildBattlefield();
        RefreshTurnPresentation();
    }

    /// <summary>
    /// 禁用时取消订阅并清理动态子物体。
    /// </summary>
    protected override void OnDisable()
    {
        BattleContext.Current.Turns.TurnStateChanged -= OnTurnStateChanged;
        if (_p2PhaseRoutine != null)
        {
            StopCoroutine(_p2PhaseRoutine);
            _p2PhaseRoutine = null;
        }

        UnsubscribeButtons();
        _p1SlotViews.Clear();
        _p2SlotViews.Clear();
        ClearRow(p2RowRoot);
        ClearRow(p1RowRoot);
        _highlightedUnitId = string.Empty;
        base.OnDisable();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 订阅按钮。
    /// </summary>
    private void SubscribeButtons()
    {
        if (leaveBattleBtn != null)
        {
            leaveBattleBtn.onClick.AddListener(OnClickLeaveBattle);
        }

        if (endTurnBtn != null)
        {
            endTurnBtn.onClick.AddListener(OnClickEndTurn);
        }
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (leaveBattleBtn != null)
        {
            leaveBattleBtn.onClick.RemoveListener(OnClickLeaveBattle);
        }

        if (endTurnBtn != null)
        {
            endTurnBtn.onClick.RemoveListener(OnClickEndTurn);
        }
    }

    /// <summary>
    /// 离开战斗场景。
    /// </summary>
    private void OnClickLeaveBattle()
    {
        BattleUiFlow.OpenSettlement(BattleSettlementKind.Surrender);
    }

    /// <summary>
    /// 结束我方回合，切换到敌方行动阶段。
    /// </summary>
    private void OnClickEndTurn()
    {
        if (!BattleContext.Current.Turns.TryEndP1Turn())
        {
            return;
        }

        BattleUiSession.ClearPendingSkillCast();
        BattleUiSession.ClearActionOwnerUnit();
        UIPanelRegistry.PopWhileTopIsAny(BattleUiFlow.BattleOverlayPanelNames);
        ClearSlotHighlight();
        RefreshTurnPresentation();
    }

    /// <summary>
    /// 结束回合按钮仅在玩家方可操作阶段可点。
    /// </summary>
    private void UpdateEndTurnButtonState()
    {
        if (endTurnBtn == null)
        {
            return;
        }

        endTurnBtn.interactable = BattleContext.Current.Turns.IsPlayerActionPhase;
    }

    /// <summary>
    /// 生成敌我首发占位。
    /// </summary>
    private void RebuildBattlefield()
    {
        var field = BattleContext.Current.Field;
        field.EnsureInitialized();
        _p1SlotViews.Clear();
        _p2SlotViews.Clear();
        ClearRow(p2RowRoot);
        ClearRow(p1RowRoot);

        UpdateHeaderTurnText();

        if (shipFieldSlotPrefab == null)
        {
            Debug.LogWarning("BattleMainPanel: 未绑定 shipFieldSlotPrefab。");
            return;
        }

        // 直接遍历槽位数组，避免同 cardId 时 FindUnitOnField 返回错误单位
        if (p2RowRoot != null)
        {
            for (var i = 0; i < field.P2Slots.Length; i++)
            {
                var unit = field.P2Slots[i];
                if (unit != null && !unit.IsDead)
                {
                    SpawnFieldSlot(p2RowRoot, unit, BattleSide.P2);
                }
            }
        }

        if (p1RowRoot != null)
        {
            for (var i = 0; i < field.P1Slots.Length; i++)
            {
                var unit = field.P1Slots[i];
                if (unit != null && !unit.IsDead)
                {
                    SpawnFieldSlot(p1RowRoot, unit, BattleSide.P1);
                }
            }
        }
    }

    /// <summary>
    /// 在指定行下实例化一个战场槽位并绑定配置。
    /// </summary>
    private void SpawnFieldSlot(RectTransform row, BattleUnit unit, BattleSide side)
    {
        if (row == null || unit == null)
        {
            return;
        }

        var cardId = unit.CardId;
        var unitId = unit.UnitId;

        var go = Instantiate(shipFieldSlotPrefab, row, false);
        var view = go.GetComponent<BattleShipFieldSlotView>() ??
                   go.GetComponentInChildren<BattleShipFieldSlotView>(true);
        if (view == null)
        {
            Destroy(go);
            Debug.LogError("BattleMainPanel: shipFieldSlotPrefab 缺少 BattleShipFieldSlotView。");
            return;
        }

        _configMap.TryGetValue(cardId, out var cfg);
        view.Bind(cfg, cardId, side, unitId);
        view.SetHp(unit.Hp);

        view.SetActionMenuAvailable(side == BattleSide.P1);
        view.ApplyTurnInteractionState();
        if (side == BattleSide.P1)
        {
            _p1SlotViews[unitId] = view;
        }
        else
        {
            _p2SlotViews[unitId] = view;
        }
    }

    /// <summary>
    /// 更新标题中的关卡名与回合阶段文案。
    /// </summary>
    private void UpdateHeaderTurnText()
    {
        if (headerText == null)
        {
            return;
        }

        var stage = BattleStartContext.CurrentStage;
        var stageName = stage != null && !string.IsNullOrEmpty(stage.DisplayName)
            ? stage.DisplayName
            : "战斗";
        headerText.text = $"{stageName}  ·  {BattleContext.Current.Turns.GetPhaseDisplayText()}";
    }

    /// <summary>
    /// 回合状态变化：刷新槽位；敌方阶段则显示提示并自动占位行动。
    /// </summary>
    private void OnTurnStateChanged()
    {
        // P2PhaseRoutine 协程运行中禁止提前刷新 UI：EndRound 在 PassRemaining 内触发时，
        // 协程尚未完全结束（TryForceEndP2Phase / TryRemoveDeadCards 还在后面），
        // 此时开放 P1 交互会导致玩家误操作或 P2 额外行动。
        if (_p2PhaseRoutine != null)
        {
            return;
        }

        RefreshTurnPresentation();
        if (BattleContext.Current.Turns.Phase != BattleTurnPhase.P2Action)
        {
            return;
        }

        // 显示敌方行动阶段提示
        if (headerText != null)
        {
            headerText.text = BattleContext.Current.Turns.GetPhaseDisplayText() + " — 敌方行动中…";
        }

        _p2PhaseRoutine = StartCoroutine(P2PhaseRoutine());
    }

    /// <summary>
    /// P2 行动阶段主流程：AI 评分 → 逐条执行（播放强调动画）→ 收尾未行动单位。
    /// </summary>
    private IEnumerator P2PhaseRoutine()
    {
        yield return new WaitForSeconds(0.35f);

        // 进入 P2 阶段时先检查结算条件（P1/P2 可能已在上一阶段全灭）
        if (BattleFacade.TryOpenBattleSettlement())
        {
            _p2PhaseRoutine = null;
            yield break;
        }

        var actions = new List<AIAction>(4);
        BattleAIService.EvaluateBestActions(actions, BattleSide.P2);

        if (actions.Count == 0)
        {
            // 无 AI 行动时停留至少 1.5 秒，让玩家看到敌方阶段文字与缩放动画
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            for (var i = 0; i < actions.Count; i++)
            {
                // 播放施法者强调动画，让玩家看到 AI 操作
                if (_p2SlotViews.TryGetValue(actions[i].CasterUnitId, out var casterView) && casterView != null)
                {
                    casterView.PlayActionEmphasis();
                }

                BattleAIService.ExecuteSingleAction(actions[i], BattleSide.P2);
                RefreshBattlefield();
                yield return new WaitForSeconds(0.25f);

                // 每次 AI 行动后检查结算：P1 全灭 → 失败，P2 全灭 → 胜利
                if (BattleFacade.TryOpenBattleSettlement())
                {
                    _p2PhaseRoutine = null;
                    yield break;
                }
            }
        }

        BattleAIService.PassRemaining(BattleSide.P2);
        // P2 全灭后不会触发 IsP2SideRoundComplete → EndRound，需手动强制结束
        BattleContext.Current.Turns.TryForceEndP2Phase();
        // 清理 P2 阵亡卡牌（ExecuteSingleAction 已逐一处理，此处兜底）
        BattleContext.Current.Field.TryRemoveDeadCards(BattleSide.P2);
        RefreshTurnPresentation();
        _p2PhaseRoutine = null;
    }

    /// <summary>
    /// 清除当前选中放大。
    /// </summary>
    private void ClearSlotHighlight()
    {
        if (!string.IsNullOrEmpty(_highlightedUnitId) &&
            _p1SlotViews.TryGetValue(_highlightedUnitId, out var view) &&
            view != null)
        {
            view.ResetActionMenuScale();
        }

        _highlightedUnitId = string.Empty;
    }

    /// <summary>
    /// 清空行下子物体。
    /// </summary>
    private static void ClearRow(RectTransform row)
    {
        if (row == null)
        {
            return;
        }

        for (var i = row.childCount - 1; i >= 0; i--)
        {
            var c = row.GetChild(i);
            if (c != null)
            {
                Destroy(c.gameObject);
            }
        }
    }

    /// <summary>
    /// 加载卡牌配置表映射：将 <see cref="_configMap"/> 替换为 <see cref="GameResourceLoader.GetCardConfigMap"/>
    /// 返回的全局共享字典引用，避免每面板独立查询配置表产生的额外开销。
    /// </summary>
    private void LoadConfigMap()
    {
        _configMap = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
    }

    #endregion
}
