using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗主界面：双方上场槽位、回合指示、操作菜单（技能/换牌）、两段式选目标、
/// 事件驱动的受击/治疗/飘字表现与结算入口。
/// 表现层只订阅事件与调用 IBattleService，绝不直接修改战斗数据。
/// 场景控件经 [SerializeField] 绑定（BattleScene 已接好引用），缺失时按子物体名解析兜底。
/// </summary>
public sealed class BattleMainPanel : PanelBase
{
    [Header("场景控件（留空则按子物体名运行时解析）")]
    [SerializeField]
    private RectTransform _enemyRowRoot;
    [SerializeField]
    private RectTransform _playerRowRoot;
    [SerializeField]
    private TMP_Text _roundText;
    [SerializeField]
    private Button _endTurnBtn;
    [SerializeField]
    private Button _leaveBattleBtn;
    [Tooltip("场上槽位预制体（BattleShipFieldSlotView.prefab）；留空则动态构建")]
    [SerializeField]
    private GameObject _slotPrefab;

    [Header("面板 ID")]
    [SerializeField]
    private string _settlementPanelId = "BattleSettlement";

    private IBattleService _battleService;
    private BattleEntryService _entryService;
    private IResourceService _resourceService;
    private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

    private RectTransform _boardRoot;
    private readonly Dictionary<string, BattleUnitViewData> _unitDataMap = new Dictionary<string, BattleUnitViewData>();
    private readonly BattleShipFieldSlotView[] _p1Slots = new BattleShipFieldSlotView[2];
    private readonly BattleShipFieldSlotView[] _p2Slots = new BattleShipFieldSlotView[2];
    private readonly Dictionary<string, BattleShipFieldSlotView> _viewByUnit = new Dictionary<string, BattleShipFieldSlotView>();

    private RectTransform _floatTextRoot;
    private readonly List<TMP_Text> _floatTextPool = new List<TMP_Text>();
    private GameObject _cancelTargetObject;

    private bool _selectingTarget;
    private bool _battleEnded;

    /// <summary>当前放大强调的操作槽位 UnitId（对应历史 _highlightedUnitId）。</summary>
    private string _highlightedUnitId = string.Empty;

    /// <summary>战斗浮层面板 ID（与场景面板的 _panelId 一致；[0] 为槽位操作菜单）。</summary>
    private static readonly string[] OverlayPanelIds =
    {
        "BattleSlotActionMenu",
        "BattleSkillSelect",
        "BattleCardSwitch",
        "BattleEmoji",
    };

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.SetAsLastSibling(); // 场景实例打开时置顶，保证层级覆盖
        _battleService = ServiceContainer?.TryResolve<IBattleService>();
        _entryService = ServiceContainer?.TryResolve<BattleEntryService>();
        _resourceService = ServiceContainer?.TryResolve<IResourceService>();

        EnsureBoardStructure();
        SubscribeBattleEvents();
        StartBattleAsync().Forget();
    }

    protected override void OnDisable()
    {
        UnsubscribeBattleEvents();
        CloseOverlayPanels();
        base.OnDisable();
    }

    // ───────────────────────── 开局 ─────────────────────────

    /// <summary>开局：无会话则从入口上下文构建，已有会话则恢复视图。</summary>
    private async UniTaskVoid StartBattleAsync()
    {
        if (_battleService == null)
            return;

        if (!_battleService.HasActiveSession)
        {
            var context = _entryService?.CurrentContext;
            var ok = await _battleService.StartBattleAsync(context);
            if (!ok)
            {
                // 关卡/编队配置缺失：仅记录日志，界面保持默认状态（对应历史无状态提示的做法）
                UnityEngine.Debug.LogWarning("[BattleMainPanel] 战斗启动失败：请检查关卡与编队配置。");
                return;
            }
        }

        await RefreshAllSlotsAsync();
        UpdateRoundText();
    }

    // ───────────────────────── 事件订阅与处理 ─────────────────────────

    /// <summary>订阅战斗事件（表现层只消费事件，不发布战斗数据事件）。</summary>
    private void SubscribeBattleEvents()
    {
        if (_battleService?.EventBus == null)
            return;

        var bus = _battleService.EventBus;
        _subscriptions.Add(bus.Subscribe<BattleStartedEvent>(_ =>
        {
            RefreshAllSlotsAsync().Forget();
            UpdateRoundText();
        }));
        _subscriptions.Add(bus.Subscribe<RoundStartedEvent>(_ => UpdateRoundText()));
        _subscriptions.Add(bus.Subscribe<TurnPhaseChangedEvent>(_ =>
        {
            CloseOverlayPanels();
            RefreshDimStates();
            UpdateRoundText();
        }));
        _subscriptions.Add(bus.Subscribe<SlotActedEvent>(e =>
        {
            if (_viewByUnit.TryGetValue(e.UnitId, out var view))
                view.SetDimmed(true);
        }));
        _subscriptions.Add(bus.Subscribe<UnitDamagedEvent>(e =>
        {
            if (_viewByUnit.TryGetValue(e.UnitId, out var view))
            {
                view.PlayHitFx(e.Damage);
                if (_unitDataMap.TryGetValue(e.UnitId, out var data))
                {
                    data.Hp = e.HpAfter;
                    view.UpdateHp(e.HpAfter, data.MaxHp);
                }
            }

            ShowFloatText(e.UnitId, e.Damage > 0 ? $"-{e.Damage}" : "格挡", e.Damage > 0 ? Color.red : Color.gray);
        }));
        _subscriptions.Add(bus.Subscribe<UnitHealedEvent>(e =>
        {
            if (_viewByUnit.TryGetValue(e.UnitId, out var view))
            {
                view.PlayHealFx();
                if (_unitDataMap.TryGetValue(e.UnitId, out var data))
                {
                    data.Hp = e.HpAfter;
                    view.UpdateHp(e.HpAfter, data.MaxHp);
                }
            }

            ShowFloatText(e.UnitId, $"+{e.Healed}", new Color(0.4f, 1f, 0.5f));
        }));
        _subscriptions.Add(bus.Subscribe<UnitDiedEvent>(e =>
        {
            ShowFloatText(e.UnitId, "击破！", new Color(1f, 0.5f, 0.2f));
            if (_viewByUnit.TryGetValue(e.UnitId, out var view))
                view.SetDimmed(true);
        }));
        _subscriptions.Add(bus.Subscribe<SlotUnboundEvent>(_ => RefreshAllSlotsAsync().Forget()));
        _subscriptions.Add(bus.Subscribe<SlotBoundEvent>(_ => RefreshAllSlotsAsync().Forget()));
        _subscriptions.Add(bus.Subscribe<UnitSwitchedEvent>(_ => RefreshAllSlotsAsync().Forget()));
        _subscriptions.Add(bus.Subscribe<SkillCastStartedEvent>(e =>
        {
            // 敌方施法时播放施法者强调动画（对应历史 P2 阶段的 PlayActionEmphasis）。
            if (e.CasterSide == BattleSide.P2
                && _viewByUnit.TryGetValue(e.CasterUnitId, out var casterView) && casterView != null)
            {
                casterView.PlayActionEmphasis();
            }
        }));
        _subscriptions.Add(bus.Subscribe<SkillTargetSelectingEvent>(e =>
        {
            _selectingTarget = true;
            HighlightTargets(e.ValidTargetUnitIds);
            if (_cancelTargetObject != null)
                _cancelTargetObject.SetActive(true);
            UpdateActionButtonsState();
        }));
        _subscriptions.Add(bus.Subscribe<SkillCastFinishedEvent>(_ => ExitTargetMode()));
        _subscriptions.Add(bus.Subscribe<BattleEndedEvent>(_ =>
        {
            _battleEnded = true;
            CloseOverlayPanels();
            ExitTargetMode();
            OpenSettlementAsync().Forget();
        }));
    }

    /// <summary>退订全部战斗事件。</summary>
    private void UnsubscribeBattleEvents()
    {
        for (var i = 0; i < _subscriptions.Count; i++)
            _subscriptions[i].Dispose();
        _subscriptions.Clear();
        ExitTargetMode();
    }

    // ───────────────────────── 槽位交互 ─────────────────────────

    /// <summary>槽位点击：选目标模式确认目标 / 我方槽位打开操作菜单。</summary>
    private void OnSlotClicked(BattleShipFieldSlotView view)
    {
        if (_battleEnded || string.IsNullOrEmpty(view.UnitId))
            return;

        // 选目标模式：点击任意高亮槽位即确认目标（合法性由服务层校验）。
        if (_selectingTarget)
        {
            if (_battleService.ConfirmCastTarget(view.UnitId, out _))
                ExitTargetMode();
            return;
        }

        // 常规模式：点击我方可行动单位打开操作菜单。
        if (view.Side != BattleSide.P1)
            return;

        // 对应历史 ApplyTurnInteractionState：仅我方行动阶段且未行动的槽位可操作（不可点则忽略）。
        if (!view.CanOpenActionMenu)
            return;

        // 高亮当前操作槽位（对应历史 HighlightSlotForAction），再打开操作菜单面板。
        HighlightActionSlot(view.UnitId);
        OpenOverlayPanel(OverlayPanelIds[0], view.UnitId);
    }

    /// <summary>打开操作菜单时放大当前槽位（对应历史 HighlightSlotForAction）。</summary>
    private void HighlightActionSlot(string unitId)
    {
        ClearSlotHighlight();

        if (string.IsNullOrEmpty(unitId) || !_viewByUnit.TryGetValue(unitId, out var view) || view == null)
            return;

        _highlightedUnitId = unitId;
        view.PlayActionMenuOpenScale();
    }

    /// <summary>清空当前操作槽位放大（对应历史 ClearSlotHighlight / ClearSlotHighlightIfOwner）。</summary>
    private void ClearSlotHighlight()
    {
        if (!string.IsNullOrEmpty(_highlightedUnitId)
            && _viewByUnit.TryGetValue(_highlightedUnitId, out var view) && view != null)
        {
            view.ResetActionMenuScale();
        }

        _highlightedUnitId = string.Empty;
    }

    /// <summary>打开战斗浮层面板（携带可选参数，经 UIService 统一管理）。</summary>
    private async void OpenOverlayPanel(string panelId, object param)
    {
        if (UIService == null || string.IsNullOrEmpty(panelId))
            return;

        await UIService.OpenAsync<PanelBase>(panelId, param);
    }

    /// <summary>关闭全部战斗浮层面板并清除操作槽位放大（阶段切换/战斗结束/退出时调用）。</summary>
    private void CloseOverlayPanels()
    {
        ClearSlotHighlight();

        if (UIService == null)
            return;

        for (var i = 0; i < OverlayPanelIds.Length; i++)
            UIService.CloseAsync(OverlayPanelIds[i]).Forget();
    }

    /// <summary>取消等待中的目标选择。</summary>
    private void OnCancelTargetClicked()
    {
        _battleService?.CancelPendingCast();
        ExitTargetMode();
    }

    /// <summary>退出选目标模式：清理高亮、隐藏取消按钮。</summary>
    private void ExitTargetMode()
    {
        _selectingTarget = false;
        HighlightTargets(null);
        if (_cancelTargetObject != null)
            _cancelTargetObject.SetActive(false);
        UpdateActionButtonsState();
    }

    /// <summary>高亮/清理合法目标边框。</summary>
    private void HighlightTargets(IReadOnlyList<string> unitIds)
    {
        foreach (var pair in _viewByUnit)
            pair.Value.SetHighlight(false);

        if (unitIds == null)
            return;

        for (var i = 0; i < unitIds.Count; i++)
        {
            if (_viewByUnit.TryGetValue(unitIds[i], out var view))
                view.SetHighlight(true);
        }
    }

    // ───────────────────────── 按钮事件（场景绑定） ─────────────────────────

    /// <summary>结束我方回合。</summary>
    public void OnEndTurnClicked()
    {
        if (_battleService == null)
            return;

        if (_selectingTarget)
        {
            OnCancelTargetClicked();
            return;
        }

        _battleService.TryEndPlayerTurn(out _);
    }

    /// <summary>离开战斗（对应历史 leaveBattleBtn：以放弃战斗结束并进入结算）。</summary>
    public async void OnClickLeaveBattle()
    {
        if (_battleService == null || _battleEnded)
            return;

        _battleService.TryRetreat();
        await UniTask.CompletedTask;
    }

    /// <summary>战斗结束后打开结算面板。</summary>
    private async UniTaskVoid OpenSettlementAsync()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

        // await 后重新校验：延时期间面板可能已被销毁（如撤退后场景已切换）。
        if (this == null)
            return;

        if (UIService != null && !string.IsNullOrEmpty(_settlementPanelId))
            await UIService.OpenAsync<PanelBase>(_settlementPanelId);
    }

    // ───────────────────────── 视图刷新 ─────────────────────────

    /// <summary>按最新只读视图重建双方槽位绑定（结构变化/入场时调用）。</summary>
    private async UniTask RefreshAllSlotsAsync()
    {
        if (_battleService == null)
            return;

        var view = _battleService.GetBattleView();
        if (view == null)
            return;

        _unitDataMap.Clear();
        _viewByUnit.Clear();

        for (var i = 0; i < view.P1Slots.Count; i++)
        {
            await BindSlotAsync(_p1Slots[i], view.P1Slots[i], i);
        }

        for (var i = 0; i < view.P2Slots.Count; i++)
        {
            await BindSlotAsync(_p2Slots[i], view.P2Slots[i], i);
        }

        CollectReserveData(view.P1Reserve);
        CollectReserveData(view.P2Reserve);
        RefreshDimStates();
    }

    /// <summary>绑定单个槽位视图。</summary>
    private async UniTask BindSlotAsync(BattleShipFieldSlotView slotView, BattleSlotViewData slotData, int index)
    {
        if (slotView == null)
            return;

        if (slotData?.Unit == null)
        {
            slotView.MarkEmpty();
            return;
        }

        await slotView.BindAsync(slotData.Unit, _resourceService);
        _unitDataMap[slotData.Unit.UnitId] = slotData.Unit;
        _viewByUnit[slotData.Unit.UnitId] = slotView;
    }

    /// <summary>登记替补数据（供事件定位飘字/表现）。</summary>
    private void CollectReserveData(IReadOnlyList<BattleUnitViewData> reserves)
    {
        for (var i = 0; i < reserves.Count; i++)
            _unitDataMap[reserves[i].UnitId] = reserves[i];
    }

    /// <summary>按行动/阶段状态刷新全部槽位变暗遮罩。</summary>
    private void RefreshDimStates()
    {
        var view = _battleService?.GetBattleView();
        if (view == null)
            return;

        for (var i = 0; i < view.P1Slots.Count && i < _p1Slots.Length; i++)
        {
            var unit = view.P1Slots[i]?.Unit;
            _p1Slots[i].SetDimmed(unit != null && unit.HasActed);
            // 对应历史 SetActionMenuAvailable + ApplyTurnInteractionState：仅我方行动阶段且未行动的槽位可操作。
            _p1Slots[i].SetActionMenuAvailable(view.Phase == BattleTurnPhase.P1Action
                && unit != null && !unit.HasActed && !unit.IsDead);
        }

        for (var i = 0; i < view.P2Slots.Count && i < _p2Slots.Length; i++)
        {
            var slot = view.P2Slots[i];
            _p2Slots[i].SetDimmed(slot?.Unit != null && slot.Unit.HasActed);
            _p2Slots[i].SetActionMenuAvailable(false); // 敌方槽位不提供操作菜单（对应历史）
        }
    }

    /// <summary>更新回合标题：关卡名 · 阶段文案（对应历史 UpdateHeaderTurnText）。</summary>
    private void UpdateRoundText()
    {
        var view = _battleService?.GetBattleView();
        if (_roundText != null && view != null)
        {
            var phaseText = view.Phase == BattleTurnPhase.P1Action ? "我方行动阶段"
                : view.Phase == BattleTurnPhase.P2Action ? "敌方行动阶段" : "战斗结束";
            var stageName = string.IsNullOrEmpty(view.StageDisplayName) ? "战斗" : view.StageDisplayName;
            _roundText.text = view.Phase == BattleTurnPhase.P2Action
                ? $"{stageName}  ·  {phaseText} — 敌方行动中…"
                : $"{stageName}  ·  {phaseText}";
        }

        UpdateActionButtonsState();
    }

    /// <summary>刷新操作按钮可用性（对应历史 UpdateEndTurnButtonState）：非我方阶段/选目标中/已结束禁用结束回合。</summary>
    private void UpdateActionButtonsState()
    {
        var view = _battleService?.GetBattleView();
        var canEndTurn = view != null && !_battleEnded && !_selectingTarget
            && view.Phase == BattleTurnPhase.P1Action;

        if (_endTurnBtn != null)
            _endTurnBtn.interactable = canEndTurn;
        if (_leaveBattleBtn != null)
            _leaveBattleBtn.interactable = !_battleEnded;
    }

    // ───────────────────────── 动态构建 ─────────────────────────

    /// <summary>确保棋盘结构存在（槽位锚、按钮、文本缺失时动态创建）。</summary>
    private void EnsureBoardStructure()
    {
        var panelRect = (RectTransform)transform;

        if (_boardRoot == null)
        {
            var boardObject = new GameObject("BattleBoard", typeof(RectTransform));
            _boardRoot = (RectTransform)boardObject.transform;
            _boardRoot.SetParent(panelRect, false);
            _boardRoot.anchorMin = Vector2.zero;
            _boardRoot.anchorMax = Vector2.one;
            _boardRoot.offsetMin = Vector2.zero;
            _boardRoot.offsetMax = Vector2.zero;
        }

        // 回合/阶段标题：优先复用场景现成 Header_Text，缺失时动态创建。
        if (_roundText == null)
            _roundText = PanelWidgetUtil.FindChild<TMP_Text>(transform, "Header_Text");
        if (_roundText == null)
        {
            var roundObject = new GameObject("Header_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var roundRect = (RectTransform)roundObject.transform;
            roundRect.SetParent(_boardRoot, false);
            roundRect.anchorMin = new Vector2(0.5f, 1f);
            roundRect.anchorMax = new Vector2(0.5f, 1f);
            roundRect.pivot = new Vector2(0.5f, 1f);
            roundRect.anchoredPosition = new Vector2(0f, -14f);
            roundRect.sizeDelta = new Vector2(360f, 30f);
            _roundText = roundObject.GetComponent<TMP_Text>();
            _roundText.fontSize = 20;
            _roundText.alignment = TextAlignmentOptions.Center;
            _roundText.color = new Color(1f, 0.95f, 0.7f);
            _roundText.raycastTarget = false;
        }

        // 敌方槽位行（上方）与我方槽位行（下方）：序列化引用优先，其次按名解析，最后动态创建。
        var enemyRow = ResolveRow(_enemyRowRoot, "EnemyRowRoot", "EnemyRow", 0.72f);
        var playerRow = ResolveRow(_playerRowRoot, "PlayerRowRoot", "PlayerRow", 0.30f);
        for (var i = 0; i < 2; i++)
        {
            if (_p2Slots[i] == null)
            {
                _p2Slots[i] = CreateSlotView(enemyRow, BattleSide.P2, i);
                PositionSlot(_p2Slots[i], i);
            }
            if (_p1Slots[i] == null)
            {
                _p1Slots[i] = CreateSlotView(playerRow, BattleSide.P1, i);
                PositionSlot(_p1Slots[i], i);
            }
        }

        // 结束回合按钮：优先复用场景 EndTurn_Button，缺失时动态创建。
        if (_endTurnBtn == null)
            _endTurnBtn = PanelWidgetUtil.FindChild<Button>(transform, "EndTurn_Button");
        if (_endTurnBtn == null)
            _endTurnBtn = CreateButton(_boardRoot, "EndTurnBtn", "结束回合", new Vector2(0.5f, 0f),
                new Vector2(0f, 60f), new Vector2(180f, 44f));
        _endTurnBtn.onClick.RemoveListener(OnEndTurnClicked);
        _endTurnBtn.onClick.AddListener(OnEndTurnClicked);

        // 离开战斗按钮：优先复用场景 GiveUp_Button，缺失时动态创建。
        if (_leaveBattleBtn == null)
            _leaveBattleBtn = PanelWidgetUtil.FindChild<Button>(transform, "GiveUp_Button");
        if (_leaveBattleBtn == null)
            _leaveBattleBtn = CreateButton(_boardRoot, "LeaveBattleBtn", "放弃战斗", new Vector2(1f, 0f),
                new Vector2(-90f, 60f), new Vector2(150f, 40f));
        _leaveBattleBtn.onClick.RemoveListener(OnClickLeaveBattle);
        _leaveBattleBtn.onClick.AddListener(OnClickLeaveBattle);

        // 选目标取消按钮（隐藏，选目标时显示）。
        EnsureCancelTargetButton();

        // 飘字层（最顶层，穿透）。
        if (_floatTextRoot == null)
        {
            var floatObject = new GameObject("FloatTextRoot", typeof(RectTransform));
            _floatTextRoot = (RectTransform)floatObject.transform;
            _floatTextRoot.SetParent(_boardRoot, false);
            _floatTextRoot.anchorMin = Vector2.zero;
            _floatTextRoot.anchorMax = Vector2.one;
            _floatTextRoot.offsetMin = Vector2.zero;
            _floatTextRoot.offsetMax = Vector2.zero;
        }
    }

    /// <summary>创建槽位视图：优先实例化预制体（BattleShipFieldSlotView.prefab），缺失时动态构建。</summary>
    private BattleShipFieldSlotView CreateSlotView(RectTransform row, BattleSide side, int index)
    {
        if (_slotPrefab != null && row != null)
        {
            var slotObject = Instantiate(_slotPrefab, row, false);
            slotObject.name = $"SlotView_{side}_{index}";
            var view = slotObject.GetComponent<BattleShipFieldSlotView>()
                       ?? slotObject.GetComponentInChildren<BattleShipFieldSlotView>(true);
            if (view != null)
            {
                view.SetSide(side);
                return view;
            }

            Destroy(slotObject);
        }

        return BattleShipFieldSlotView.Create(row, side, index);
    }

    /// <summary>解析场景行容器：序列化引用 → 子物体名解析 → 动态创建兜底。</summary>
    private RectTransform ResolveRow(RectTransform serializedRow, string sceneChildName,
        string fallbackName, float verticalAnchor)
    {
        if (serializedRow != null)
            return serializedRow;

        var sceneRow = PanelWidgetUtil.FindChild<RectTransform>(transform, sceneChildName);
        return sceneRow != null ? sceneRow : EnsureRow(fallbackName, verticalAnchor);
    }

    /// <summary>确保一行槽位容器存在。</summary>
    private RectTransform EnsureRow(string rowName, float verticalAnchor)
    {
        var existing = _boardRoot.Find(rowName);
        if (existing != null)
            return (RectTransform)existing;

        var rowObject = new GameObject(rowName, typeof(RectTransform));
        var rowRect = (RectTransform)rowObject.transform;
        rowRect.SetParent(_boardRoot, false);
        rowRect.anchorMin = new Vector2(0.5f, verticalAnchor);
        rowRect.anchorMax = new Vector2(0.5f, verticalAnchor);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = Vector2.zero;
        rowRect.sizeDelta = new Vector2(420f, 240f);
        return rowRect;
    }

    /// <summary>槽位左右排布。</summary>
    private static void PositionSlot(BattleShipFieldSlotView view, int index)
    {
        var rect = (RectTransform)view.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(index == 0 ? -110f : 110f, 0f);
    }

    /// <summary>选目标模式的取消按钮。</summary>
    private void EnsureCancelTargetButton()
    {
        var cancelObject = new GameObject("CancelTargetBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        var cancelRect = (RectTransform)cancelObject.transform;
        cancelRect.SetParent(_boardRoot, false);
        cancelRect.anchorMin = new Vector2(0.5f, 1f);
        cancelRect.anchorMax = new Vector2(0.5f, 1f);
        cancelRect.pivot = new Vector2(0.5f, 1f);
        cancelRect.anchoredPosition = new Vector2(0f, -50f);
        cancelRect.sizeDelta = new Vector2(160f, 36f);

        var image = cancelObject.GetComponent<Image>();
        image.color = new Color(0.8f, 0.3f, 0.3f, 0.9f);

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var textRect = (RectTransform)textObject.transform;
        textRect.SetParent(cancelRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var label = textObject.GetComponent<TMP_Text>();
        label.fontSize = 15;
        label.alignment = TextAlignmentOptions.Center;
        label.text = "取消选择";
        label.raycastTarget = false;

        var button = cancelObject.GetComponent<Button>();
        button.onClick.AddListener(OnCancelTargetClicked);
        cancelObject.SetActive(false);
        _cancelTargetObject = cancelObject;
    }

    /// <summary>创建通用按钮。</summary>
    private static Button CreateButton(RectTransform parent, string name, string text,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)buttonObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.35f, 0.55f, 0.95f);

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var label = textObject.GetComponent<TMP_Text>();
        label.fontSize = 16;
        label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        label.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }

    // ───────────────────────── 飘字（对象池） ─────────────────────────

    /// <summary>在指定单位位置播放飘字（池化复用，DOTween 上浮淡出）。</summary>
    private void ShowFloatText(string unitId, string content, Color color)
    {
        if (_floatTextRoot == null)
            return;

        TMP_Text floatText = null;
        for (var i = 0; i < _floatTextPool.Count; i++)
        {
            if (!_floatTextPool[i].gameObject.activeSelf)
            {
                floatText = _floatTextPool[i];
                break;
            }
        }

        if (floatText == null)
        {
            var textObject = new GameObject("FloatText", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(_floatTextRoot, false);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(200f, 40f);
            floatText = textObject.GetComponent<TMP_Text>();
            floatText.fontSize = 24;
            floatText.alignment = TextAlignmentOptions.Center;
            floatText.raycastTarget = false;
            _floatTextPool.Add(floatText);
        }

        // 定位到受击单位的世界坐标（Canvas 空间换算）。
        var anchored = Vector2.zero;
        if (_viewByUnit.TryGetValue(unitId, out var view) && view != null)
        {
            var viewRect = (RectTransform)view.transform;
            var rootRect = _floatTextRoot;
            var localPoint = rootRect.InverseTransformPoint(viewRect.position);
            anchored = ((Vector2)localPoint) + new Vector2(0f, 70f);
        }

        var rectTransform = (RectTransform)floatText.transform;
        rectTransform.anchoredPosition = anchored;
        floatText.text = content;
        floatText.color = color;
        floatText.gameObject.SetActive(true);

        var canvasGroup = floatText.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = floatText.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        canvasGroup.DOKill();
        rectTransform.DOAnchorPosY(anchored.y + 60f, 0.8f).SetEase(Ease.OutCubic);
        canvasGroup.DOFade(0f, 0.8f).OnComplete(() => floatText.gameObject.SetActive(false));
    }
}
