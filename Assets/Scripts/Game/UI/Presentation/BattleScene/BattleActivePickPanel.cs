using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗准备：从所选卡组中挑选 <see cref="FleetStore.MinActivesPerBattleFleet"/>～<see cref="FleetStore.MaxActivesPerBattleFleet"/> 名出战舰娘；确认后进入正式战斗布局面板。
/// 请将 <see cref="BasePanel.PanelName"/> 设为 <see cref="PanelNames.BattleActivePickPanel"/>（脚本会在 Awake 中补全为空的情况）；初始建议隐藏。
/// </summary>
public sealed class BattleActivePickPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 舰娘槽位父节点。
    /// </summary>
    [SerializeField] private RectTransform slotsRoot;

    /// <summary>
    /// 槽位预制体（根或子级含 <see cref="BattleActivePickSlotView"/>，用于图标、名称、攻防生命与出战序号遮罩）。
    /// </summary>
    [SerializeField] private GameObject slotPrefab;

    /// <summary>
    /// 确认出战阵容（至少选中 1 名）。
    /// </summary>
    [SerializeField] private Button confirmBtn;

    /// <summary>
    /// 返回卡组选择。
    /// </summary>
    [SerializeField] private Button backBtn;

    /// <summary>
    /// 提示文本（可选）。
    /// </summary>
    [SerializeField] private TMP_Text tipText;

    /// <summary>
    /// 当前界面展示的卡组 cardId（与槽位顺序一致，长度 1～6）。
    /// </summary>
    private readonly List<string> _deckCardIds = new(FleetStore.MaxCardsPerFleet);

    /// <summary>
    /// 已选出战顺序（至多 <see cref="FleetStore.MaxActivesPerBattleFleet"/> 名）。
    /// </summary>
    private readonly List<string> _pickOrder = new(FleetStore.MaxActivesPerBattleFleet);

    /// <summary>
    /// 槽位视图缓存（与 <see cref="_deckCardIds"/> 对齐）。
    /// </summary>
    private readonly List<BattleActivePickSlotView> _slotViews = new(FleetStore.MaxCardsPerFleet);

    /// <summary>
    /// cardId 到 <see cref="CardConfigSO"/> 的映射缓存（通过 <see cref="GameResourceLoader.GetCardConfigMap"/> 加载）。
    /// 非 readonly 以允许 <see cref="LoadConfigMap"/> 替换为全局共享字典引用。
    /// </summary>
    private Dictionary<string, CardConfigSO> _configMap = new();

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 注册前写入默认面板名。
    /// </summary>
    protected override void Awake()
    {
        if (string.IsNullOrWhiteSpace(PanelName))
        {
            PanelName = PanelNames.BattleActivePickPanel;
        }

        base.Awake();
    }

    /// <summary>
    /// 启用时订阅并重建槽位。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        RebuildPickSlots();
        UpdateConfirmInteractable();
        ClearTip();
    }

    /// <summary>
    /// 禁用时取消订阅并清理动态槽位。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        ClearSlots();
        base.OnDisable();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 订阅按钮。
    /// </summary>
    private void SubscribeButtons()
    {
        if (confirmBtn != null)
        {
            confirmBtn.onClick.AddListener(OnClickConfirm);
        }

        if (backBtn != null)
        {
            backBtn.onClick.AddListener(OnClickBack);
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

        if (backBtn != null)
        {
            backBtn.onClick.RemoveListener(OnClickBack);
        }
    }

    /// <summary>
    /// 返回上一层（卡组选择）。
    /// </summary>
    private void OnClickBack()
    {
        ClearTip();
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 确认出战阵容并进入战斗主界面。
    /// </summary>
    private void OnClickConfirm()
    {
        if (!FleetStore.TryValidateBattleActives(_pickOrder, _deckCardIds, out var reason))
        {
            ShowTip(reason);
            return;
        }

        ClearTip();
        BattleStartContext.SetPlayerActives(_pickOrder);
        UIPanelRegistry.ClearAndPush(PanelNames.BattleMainPanel);
    }

    /// <summary>
    /// 从上下文读取卡组舰娘并生成可点击槽位。
    /// </summary>
    private void RebuildPickSlots()
    {
        ClearSlots();
        _pickOrder.Clear();
        _deckCardIds.Clear();
        LoadConfigMap();

        BattleStartContext.CopySelectedDeckCardIds(_deckCardIds);
        if (_deckCardIds.Count < FleetStore.MinCardsPerBattleFleet)
        {
            ShowTip("卡组数据异常，请返回重新选择");
            Debug.LogWarning(
                $"BattleActivePickPanel: 期望至少 {FleetStore.MinCardsPerBattleFleet} 张卡组舰娘，实际 {_deckCardIds.Count}。");
            return;
        }

        if (slotsRoot == null || slotPrefab == null)
        {
            Debug.LogWarning("BattleActivePickPanel: 未绑定 slotsRoot 或 slotPrefab。");
            return;
        }

        for (var i = 0; i < _deckCardIds.Count; i++)
        {
            var cardId = _deckCardIds[i];
            var go = Instantiate(slotPrefab, slotsRoot, false);
            go.name = $"PickSlot_{i + 1}_{cardId}";
            var view = go.GetComponent<BattleActivePickSlotView>()
                       ?? go.GetComponentInChildren<BattleActivePickSlotView>(true);
            if (view == null)
            {
                Destroy(go);
                Debug.LogError("BattleActivePickPanel: slotPrefab 上未找到 BattleActivePickSlotView。");
                continue;
            }

            _configMap.TryGetValue(cardId, out var cfg);
            view.Bind(cardId, cfg);
            view.SetPickSelectionOrder(0);
            _slotViews.Add(view);

            var btn = go.GetComponent<Button>() ?? go.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var graphic = go.GetComponent<Image>() ?? go.GetComponentInChildren<Image>(true);
            btn.targetGraphic = graphic;
            btn.interactable = graphic != null;
            var capturedId = cardId;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnClickSlot(capturedId));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(slotsRoot);
        RefreshPickMarks();
        UpdateConfirmInteractable();
    }

    /// <summary>
    /// 点击槽位：切换选中（<see cref="FleetStore.MinActivesPerBattleFleet"/>～<see cref="FleetStore.MaxActivesPerBattleFleet"/> 名），并刷新序号遮罩。
    /// </summary>
    private void OnClickSlot(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return;
        }

        cardId = cardId.Trim();
        var idx = _pickOrder.IndexOf(cardId);
        if (idx >= 0)
        {
            _pickOrder.RemoveAt(idx);
        }
        else if (_pickOrder.Count < FleetStore.MaxActivesPerBattleFleet)
        {
            _pickOrder.Add(cardId);
        }
        else
        {
            ShowTip($"出战最多选择 {FleetStore.MaxActivesPerBattleFleet} 名舰娘");
            return;
        }

        ClearTip();
        RefreshPickMarks();
        UpdateConfirmInteractable();
    }

    /// <summary>
    /// 根据 <see cref="_pickOrder"/> 刷新各槽位遮罩序号。
    /// </summary>
    private void RefreshPickMarks()
    {
        for (var i = 0; i < _slotViews.Count; i++)
        {
            var view = _slotViews[i];
            if (view == null)
            {
                continue;
            }

            var ord = GetPickOrderOneBased(view.BoundCardId);
            view.SetPickSelectionOrder(ord);
        }
    }

    /// <summary>
    /// 查询 cardId 在首发列表中的 1-based 序号；未选中为 0。
    /// </summary>
    private int GetPickOrderOneBased(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return 0;
        }

        var idx = _pickOrder.IndexOf(cardId.Trim());
        return idx >= 0 ? idx + 1 : 0;
    }

    /// <summary>
    /// 确认按钮在已选人数处于允许范围内时可点。
    /// </summary>
    private void UpdateConfirmInteractable()
    {
        if (confirmBtn != null)
        {
            var count = _pickOrder.Count;
            confirmBtn.interactable = count >= FleetStore.MinActivesPerBattleFleet
                                      && count <= FleetStore.MaxActivesPerBattleFleet;
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

    /// <summary>
    /// 清理动态实例。
    /// </summary>
    private void ClearSlots()
    {
        for (var i = 0; i < _slotViews.Count; i++)
        {
            var v = _slotViews[i];
            if (v != null && v.gameObject != null)
            {
                Destroy(v.gameObject);
            }
        }

        _slotViews.Clear();
    }

    private void ShowTip(string message)
    {
        if (tipText != null)
        {
            tipText.text = message ?? string.Empty;
            tipText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
        else
        {
            Debug.LogWarning($"[BattleActivePickPanel] {message}");
        }
    }

    private void ClearTip()
    {
        if (tipText != null)
        {
            tipText.text = string.Empty;
            tipText.gameObject.SetActive(false);
        }
    }

    #endregion
}
