using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 首发舰娘选择面板（按历史逻辑恢复）：向场景的 SlotsRoot 生成每张编队卡牌的槽位，
/// 显示选择先后序号（①②）；Confirm_Button 仅在选满 2 名时可用；Back_Button 返回编队选择；
/// Tip_Text 显示提示。出击顺序 = 点击顺序。
/// </summary>
public sealed class BattleActivePickPanel : PanelBase
{
    /// <summary>本场上阵首发数量。</summary>
    private const int ActivePickCount = 2;

    [SerializeField] private RectTransform _slotsRoot;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private TMP_Text _tipText;

    [Tooltip("槽位预制体（BattleActivePickSlotView.prefab）；留空则回退为动态按钮")]
    [SerializeField] private GameObject _slotPrefab;

    [Header("面板 ID")]
    [SerializeField]
    private string _battleMainPanelId = "BattleMain";

    private readonly List<int> _pickOrder = new List<int>(ActivePickCount);
    private readonly List<Button> _slotButtons = new List<Button>();
    private readonly List<string> _slotLabels = new List<string>();
    private readonly List<BattleActivePickSlotView> _slotViews = new List<BattleActivePickSlotView>();

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.SetAsLastSibling();
        ResolveWidgets();
        Subscribe();
        _pickOrder.Clear();
        SetTip($"按出击顺序选择 {ActivePickCount} 名首发");
        RebuildPickSlotsAsync().Forget();
    }

    protected override void OnDisable()
    {
        Unsubscribe();
        ClearSlots();
        base.OnDisable();
    }

    /// <summary>按子物体名解析场景控件（SlotsRoot / Confirm_Button / Back_Button / Tip_Text）。</summary>
    private void ResolveWidgets()
    {
        if (_slotsRoot == null)
            _slotsRoot = PanelWidgetUtil.FindChild<RectTransform>(transform, "SlotsRoot");
        if (_confirmButton == null)
            _confirmButton = PanelWidgetUtil.FindChild<Button>(transform, "Confirm_Button");
        if (_backButton == null)
            _backButton = PanelWidgetUtil.FindChild<Button>(transform, "Back_Button");
        if (_tipText == null)
            _tipText = PanelWidgetUtil.FindChild<TMP_Text>(transform, "Tip_Text");
    }

    private void Subscribe()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(OnClickConfirm);
        if (_backButton != null)
            _backButton.onClick.AddListener(OnClickBack);
    }

    private void Unsubscribe()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(OnClickConfirm);
        if (_backButton != null)
            _backButton.onClick.RemoveListener(OnClickBack);
    }

    /// <summary>返回上一层（编队选择）。</summary>
    private void OnClickBack()
    {
        UIService?.GoBackAsync().Forget();
    }

    /// <summary>确认首发：写入上下文（按出击顺序）并进入战斗主界面。</summary>
    private async void OnClickConfirm()
    {
        if (_pickOrder.Count != ActivePickCount)
        {
            SetTip($"请选择 {ActivePickCount} 名首发");
            return;
        }

        var entry = ServiceContainer?.TryResolve<BattleEntryService>();
        if (entry?.CurrentContext == null)
        {
            SetTip("战斗上下文缺失");
            return;
        }

        entry.CurrentContext.ActiveShipIndices = _pickOrder.ToArray();

        if (UIService != null && !string.IsNullOrEmpty(_battleMainPanelId))
            await UIService.OpenAsync<PanelBase>(_battleMainPanelId);

        // 关闭自身：本面板带全屏背景（raycast），不关闭会遮挡战斗主界面的所有点击。
        if (UIService != null && !string.IsNullOrEmpty(PanelId))
            UIService.CloseAsync(PanelId).Forget();
    }

    /// <summary>按当前编队成员重建槽位（名字经应用层预览查询）。</summary>
    private async UniTaskVoid RebuildPickSlotsAsync()
    {
        ClearSlots();

        var root = _slotsRoot != null ? _slotsRoot : PanelWidgetUtil.FindOrCreateRoot(transform, "SlotsRoot");
        if (root == null)
            return;

        var entry = ServiceContainer?.TryResolve<BattleEntryService>();
        var battleService = ServiceContainer?.TryResolve<IBattleService>();
        if (battleService == null)
        {
            SetTip("战斗服务不可用");
            return;
        }

        var fleetIndex = entry?.CurrentContext?.FleetIndex ?? 0;
        if (fleetIndex < 0)
            fleetIndex = 0;

        var members = await battleService.GetFleetPreviewAsync(fleetIndex);
        if (members == null || members.Count == 0)
        {
            SetTip("该编队没有可出击的舰娘，请返回重新选择编队");
            UpdateConfirmInteractable();
            return;
        }

        for (var i = 0; i < members.Count; i++)
        {
            var slotIndex = members[i].SlotIndex;
            var label = members[i].DisplayName;
            _slotLabels.Add(label);

            // 优先使用原预制体槽位（BattleActivePickSlotView.prefab）；缺失时回退动态按钮。
            if (_slotPrefab != null)
            {
                var viewObject = Instantiate(_slotPrefab, root, false);
                viewObject.name = $"Slot_{slotIndex}";
                var view = viewObject.GetComponent<BattleActivePickSlotView>()
                           ?? viewObject.GetComponentInChildren<BattleActivePickSlotView>(true);
                if (view != null)
                {
                    view.Bind(members[i].CardId, label, members[i].MaxHp);
                    view.Clicked += OnSlotViewClicked;
                    _slotViews.Add(view);
                    continue;
                }

                Destroy(viewObject);
            }

            var button = PanelWidgetUtil.CreateButton(root, $"Slot_{slotIndex}", label,
                new Vector2(0f, 140f - i * 52f), new Vector2(460f, 46f));
            button.onClick.AddListener(() => OnClickSlot(slotIndex));
            _slotButtons.Add(button);
        }

        RefreshPickMarks();
        UpdateConfirmInteractable();
    }

    /// <summary>点击槽位：切换选择状态（保持点击顺序，超过上限时提示）。</summary>
    private void OnClickSlot(int slotIndex)
    {
        var existing = _pickOrder.IndexOf(slotIndex);
        if (existing >= 0)
        {
            _pickOrder.RemoveAt(existing);
        }
        else
        {
            if (_pickOrder.Count >= ActivePickCount)
            {
                SetTip($"最多选择 {ActivePickCount} 名，请先取消已选舰娘");
                return;
            }
            _pickOrder.Add(slotIndex);
        }

        RefreshPickMarks();
        UpdateConfirmInteractable();
    }

    /// <summary>预制体槽位点击：按视图在列表中的位置得到编队序号，走统一选择逻辑。</summary>
    private void OnSlotViewClicked(BattleActivePickSlotView view)
    {
        if (view == null) return;

        var index = _slotViews.IndexOf(view);
        if (index >= 0)
            OnClickSlot(index);
    }

    /// <summary>刷新槽位的序号标记（①②＝出击顺序）与选中高亮。</summary>
    private void RefreshPickMarks()
    {
        // 预制体槽位：由视图呈现选择序号（Order / Order_Text）
        for (var i = 0; i < _slotViews.Count; i++)
        {
            var view = _slotViews[i];
            if (view != null)
                view.SetPickSelectionOrder(GetPickOrderOneBased(i));
        }

        // 动态按钮兜底路径
        for (var i = 0; i < _slotButtons.Count; i++)
        {
            var button = _slotButtons[i];
            if (button == null)
                continue;

            var pickIndex = GetPickOrderOneBased(i);
            var baseLabel = i < _slotLabels.Count ? _slotLabels[i] : $"槽位 {i + 1}";
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = pickIndex > 0 ? $"（{pickIndex}）{baseLabel}" : baseLabel;

            PanelWidgetUtil.SetButtonSelected(button, pickIndex > 0);
        }
    }

    /// <summary>查询槽位的出击序号（1 起；未选中返回 0）。</summary>
    private int GetPickOrderOneBased(int slotIndex)
    {
        for (var i = 0; i < _pickOrder.Count; i++)
        {
            if (_pickOrder[i] == slotIndex)
                return i + 1;
        }
        return 0;
    }

    /// <summary>选满 2 名才启用确认按钮（对应历史 UpdateConfirmInteractable）。</summary>
    private void UpdateConfirmInteractable()
    {
        if (_confirmButton != null)
            _confirmButton.interactable = _pickOrder.Count == ActivePickCount;
    }

    /// <summary>清理解释：销毁预制体槽位与动态按钮。</summary>
    private void ClearSlots()
    {
        for (var i = 0; i < _slotViews.Count; i++)
        {
            var view = _slotViews[i];
            if (view == null) continue;
            view.Clicked -= OnSlotViewClicked;
            Destroy(view.gameObject);
        }
        _slotViews.Clear();

        for (var i = 0; i < _slotButtons.Count; i++)
        {
            if (_slotButtons[i] != null)
                Destroy(_slotButtons[i].gameObject);
        }

        _slotButtons.Clear();
        _slotLabels.Clear();
    }

    private void SetTip(string message)
    {
        if (_tipText != null)
            _tipText.text = message;
    }
}
