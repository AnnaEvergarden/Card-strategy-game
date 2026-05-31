using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗换牌面板：列出未上场替补舰娘，选择后确认替换当前焦点场上舰娘并刷新主界面。
/// 使用 UnitId 追踪替补单位，支撑换牌后 slot 行动态继承。
/// </summary>
public sealed class BattleCardSwitchPanel : BattleOverlayPanelBase
{
    #region Fields

    /// <summary>
    /// 替补槽位父节点。
    /// </summary>
    [SerializeField] private RectTransform reservesRoot;

    /// <summary>
    /// 替补槽位预制体（挂 <see cref="BattleCardSwitchReserveSlotView"/>）。
    /// </summary>
    [SerializeField] private GameObject reserveSlotPrefab;

    /// <summary>
    /// 确认换牌按钮。
    /// </summary>
    [SerializeField] private Button confirmBtn;

    /// <summary>
    /// 已生成的替补槽实例。
    /// </summary>
    private readonly List<GameObject> _spawnedSlots = new(FleetStore.MaxCardsPerFleet);

    /// <summary>
    /// 替补槽视图缓存。
    /// </summary>
    private readonly List<BattleCardSwitchReserveSlotView> _slotViews = new(FleetStore.MaxCardsPerFleet);

    /// <summary>
    /// 替补 UnitId 列表缓冲。
    /// </summary>
    private readonly List<string> _reserveUnitIds = new(FleetStore.MaxCardsPerFleet);

    /// <summary>
    /// cardId 到 <see cref="CardConfigSO"/> 的映射缓存（通过 <see cref="GameResourceLoader.GetCardConfigMap"/> 加载）。
    /// 非 readonly 以允许 <see cref="LoadConfigMap"/> 替换为全局共享字典引用。
    /// </summary>
    private Dictionary<string, CardConfigSO> _configMap = new();

    /// <summary>
    /// 当前选中的替补 UnitId。
    /// </summary>
    private string _selectedReserveUnitId = string.Empty;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景实例。
    /// </summary>
    public static BattleCardSwitchPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<BattleCardSwitchPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.BattleCardSwitchPanel;
            }

            return existing;
        }

        Debug.LogWarning("BattleCardSwitchPanel: 场景中未找到该面板，请在 BattleScene UI 下挂载。");
        return null;
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 补全面板名。
    /// </summary>
    protected override void Awake()
    {
        if (string.IsNullOrWhiteSpace(PanelName))
        {
            PanelName = PanelNames.BattleCardSwitchPanel;
        }

        base.Awake();
    }

    /// <summary>
    /// 启用时订阅按钮并生成替补列表。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        _selectedReserveUnitId = string.Empty;
        RebuildReserveSlots();
        UpdateConfirmInteractable();
    }

    /// <summary>
    /// 禁用时取消订阅并清理动态槽位。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        ClearSpawned();
        base.OnDisable();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 订阅确认按钮（关闭叠层仅使用基类可选 closeBtn）。
    /// </summary>
    private void SubscribeButtons()
    {
        if (confirmBtn != null)
        {
            confirmBtn.onClick.AddListener(OnClickConfirm);
        }
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveListener(OnClickConfirm);
        }
    }

    /// <summary>
    /// 确认换牌：替换焦点场上舰娘并刷新主界面。
    /// </summary>
    private void OnClickConfirm()
    {
        var focusUnitId = BattleUiSession.FocusUnitId;
        if (string.IsNullOrWhiteSpace(focusUnitId))
        {
            Debug.LogWarning("BattleCardSwitchPanel: 未指定要替换的场上舰娘。");
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedReserveUnitId))
        {
            return;
        }

        var field = BattleContext.Current.Field;

        var slotInfo = field.FindSlot(focusUnitId);
        if (slotInfo == null)
        {
            Debug.LogWarning("BattleCardSwitchPanel: 焦点舰娘不在场上。");
            return;
        }

        var slotIndex = slotInfo.Value.slotIndex;

        // 换牌（传入 UnitId）
        if (!field.TrySwitchActive(focusUnitId, _selectedReserveUnitId, out var reason))
        {
            Debug.LogWarning($"BattleCardSwitchPanel: {reason}");
            return;
        }

        var actionOwner = BattleUiSession.ActionOwnerUnitId;
        if (string.IsNullOrWhiteSpace(actionOwner))
        {
            actionOwner = focusUnitId;
        }

        // 焦点设为新上场舰娘
        BattleUiSession.SetFocusUnit(_selectedReserveUnitId);

        var main = BattleMainPanel.EnsureInstance();
        if (main != null)
        {
            main.RefreshPlayerBattlefield();
        }

        // 使用槽位索引标记行动，换牌后新卡继承槽位的"已行动"状态
        if (slotIndex >= 0)
        {
            BattleContext.Current.Turns.TryCompleteCardAction(slotIndex, BattleSide.P1);
        }

        UIPanelRegistry.PopWhileTopIsAny(BattleUiFlow.BattleOverlayPanelNames);
        main?.RefreshTurnPresentation();
    }

    /// <summary>
    /// 生成未上场替补槽位列表。
    /// </summary>
    private void RebuildReserveSlots()
    {
        ClearSpawned();
        LoadConfigMap();
        var field = BattleContext.Current.Field;
        field.EnsureInitialized();
        field.CopyReserveUnitIds(BattleSide.P1, _reserveUnitIds);

        if (reservesRoot == null || reserveSlotPrefab == null)
        {
            Debug.LogWarning("BattleCardSwitchPanel: 未绑定 reservesRoot 或 reserveSlotPrefab。");
            return;
        }

        if (_reserveUnitIds.Count == 0)
        {
            Debug.LogWarning("BattleCardSwitchPanel: 没有可替换的替补舰娘。");
            return;
        }

        for (var i = 0; i < _reserveUnitIds.Count; i++)
        {
            var unitId = _reserveUnitIds[i];
            var unit = field.GetUnit(unitId);
            if (unit == null) continue;

            var cardId = unit.CardId;
            var go = Instantiate(reserveSlotPrefab, reservesRoot, false);
            go.name = $"Reserve_{i + 1}_{cardId}";
            var view = go.GetComponent<BattleCardSwitchReserveSlotView>()
                       ?? go.GetComponentInChildren<BattleCardSwitchReserveSlotView>(true);
            if (view == null)
            {
                Destroy(go);
                Debug.LogError("BattleCardSwitchPanel: reserveSlotPrefab 缺少 BattleCardSwitchReserveSlotView。");
                continue;
            }

            _configMap.TryGetValue(cardId, out var cfg);
            view.Bind(cardId, cfg, unit);
            view.SetSelected(false);
            _slotViews.Add(view);
            _spawnedSlots.Add(go);

            var btn = go.GetComponent<Button>() ?? go.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var graphic = go.GetComponent<Image>() ?? go.GetComponentInChildren<Image>(true);
            btn.targetGraphic = graphic;
            var deployable = !unit.IsDead;
            btn.interactable = deployable && graphic != null;
            var capturedUnitId = unitId;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnClickReserveSlot(capturedUnitId));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(reservesRoot);
    }

    /// <summary>
    /// 点击替补槽：已阵亡不可选；否则切换选中。
    /// </summary>
    private void OnClickReserveSlot(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return;
        }

        unitId = unitId.Trim();
        var unit = BattleContext.Current.Field.GetUnit(unitId);
        if (unit == null || unit.IsDead)
        {
            return;
        }

        _selectedReserveUnitId = _selectedReserveUnitId == unitId ? string.Empty : unitId;
        RefreshSelectionMarks();
        UpdateConfirmInteractable();
    }

    /// <summary>
    /// 刷新各槽选中高亮。
    /// </summary>
    private void RefreshSelectionMarks()
    {
        for (var i = 0; i < _slotViews.Count; i++)
        {
            var view = _slotViews[i];
            if (view == null)
            {
                continue;
            }

            view.SetSelected(!string.IsNullOrEmpty(_selectedReserveUnitId) &&
                             view.BoundUnitId == _selectedReserveUnitId);
        }
    }

    /// <summary>
    /// 确认按钮：已选替补且焦点卡牌存在时可点。
    /// </summary>
    private void UpdateConfirmInteractable()
    {
        if (confirmBtn == null)
        {
            return;
        }

        confirmBtn.interactable = !string.IsNullOrWhiteSpace(_selectedReserveUnitId) &&
                                  !string.IsNullOrWhiteSpace(BattleUiSession.FocusUnitId);
    }

    /// <summary>
    /// 加载卡牌配置表映射：将 <see cref="_configMap"/> 替换为 <see cref="GameResourceLoader.GetCardConfigMap"/>
    /// 返回的全局共享字典引用，避免每面板独立查询配置表产生的额外开销。
    /// </summary>
    private void LoadConfigMap()
    {
        _configMap = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
    }

    /// <summary>
    /// 销毁已生成槽位。
    /// </summary>
    private void ClearSpawned()
    {
        for (var i = 0; i < _spawnedSlots.Count; i++)
        {
            var go = _spawnedSlots[i];
            if (go != null)
            {
                Destroy(go);
            }
        }

        _spawnedSlots.Clear();
        _slotViews.Clear();
    }

    #endregion
}
