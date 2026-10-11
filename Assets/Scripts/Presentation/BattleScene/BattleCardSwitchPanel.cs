using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 换牌面板（复用场景 UI：Close_Button / Confirm_Button / ReservesRoot）。
/// 打开参数：要换下的场上单位 UnitId。
/// 替补列表动态生成到 ReservesRoot；选中一名替补后点确认完成换牌（消耗该槽位行动权）。
/// 失败时不提示（与历史一致）。
/// </summary>
public sealed class BattleCardSwitchPanel : BattleOverlayPanelBase
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private RectTransform _reservesRoot;

    [Tooltip("替补槽位预制体（BattleCardSwitchReserveSlotView.prefab）；留空则回退为动态按钮")]
    [SerializeField] private GameObject _reserveSlotPrefab;

    private readonly List<Button> _reserveButtons = new List<Button>();
    private readonly List<BattleCardSwitchReserveSlotView> _reserveSlotViews = new List<BattleCardSwitchReserveSlotView>();
    private string _outgoingUnitId;
    private string _selectedReserveUnitId;

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.SetAsLastSibling();
        if (_closeButton == null) _closeButton = FindButton(transform, "Close_Button");
        if (_confirmButton == null) _confirmButton = FindButton(transform, "Confirm_Button");
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    protected override void OnDisable()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        if (_confirmButton != null) _confirmButton.onClick.RemoveListener(OnConfirmClicked);
        ClearReserveButtons();
        base.OnDisable();
    }

    /// <summary>
    /// 打开面板：记录要换下的单位并重建替补列表。
    /// </summary>
    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);
        transform.SetAsLastSibling();

        if (param is string unitId)
            _outgoingUnitId = unitId;

        _selectedReserveUnitId = null;
        RebuildReserveList();
        UpdateConfirmState();
    }

    /// <summary>重建替补按钮列表（数据来自战斗只读视图的 P1Reserve）。</summary>
    private void RebuildReserveList()
    {
        ClearReserveButtons();

        var view = ServiceContainer?.TryResolve<IBattleService>()?.GetBattleView();
        var reservers = view?.P1Reserve;
        var root = EnsureReservesRoot();

        if (reservers == null || reservers.Count == 0)
            return;

        for (var i = 0; i < reservers.Count; i++)
        {
            var reserve = reservers[i];

            // 优先使用原预制体槽位（BattleCardSwitchReserveSlotView.prefab），缺失时回退动态按钮。
            if (_reserveSlotPrefab != null)
            {
                var slotObject = Instantiate(_reserveSlotPrefab, root, false);
                slotObject.name = $"Reserve_{reserve.UnitId}";
                var slotView = slotObject.GetComponent<BattleCardSwitchReserveSlotView>()
                               ?? slotObject.GetComponentInChildren<BattleCardSwitchReserveSlotView>(true);
                if (slotView != null)
                {
                    slotView.Bind(reserve.UnitId, reserve.DisplayName, reserve.Hp, reserve.MaxHp);
                    slotView.Clicked += OnReserveClicked;
                    _reserveSlotViews.Add(slotView);
                    continue;
                }

                Destroy(slotObject);
            }

            var label = $"{reserve.DisplayName}（HP {reserve.Hp}/{reserve.MaxHp}）";
            var button = CreateButton(root, $"Reserve_{reserve.UnitId}", label,
                new Vector2(0f, 70f - i * 52f), new Vector2(400f, 46f));
            var capturedId = reserve.UnitId;
            button.onClick.AddListener(() => OnReserveClicked(capturedId));
            _reserveButtons.Add(button);
        }
    }

    /// <summary>点击替补：记录选择并高亮。</summary>
    private void OnReserveClicked(string reserveUnitId)
    {
        _selectedReserveUnitId = reserveUnitId;

        // 预制体槽位：使用视图自带的高亮标记（HighLight）
        for (var i = 0; i < _reserveSlotViews.Count; i++)
        {
            var slotView = _reserveSlotViews[i];
            if (slotView != null)
                slotView.SetSelected(slotView.BoundUnitId == reserveUnitId);
        }

        for (var i = 0; i < _reserveButtons.Count; i++)
        {
            var button = _reserveButtons[i];
            if (button == null)
                continue;

            var image = button.GetComponent<Image>();
            if (image == null)
                continue;

            var selected = button.name == $"Reserve_{reserveUnitId}";
            image.color = selected
                ? new Color(0.3f, 0.7f, 0.4f, 0.95f)
                : new Color(0.25f, 0.35f, 0.55f, 0.95f);
        }

        UpdateConfirmState();
    }

    /// <summary>确认换牌：经服务执行（消耗该槽位行动权）。</summary>
    private void OnConfirmClicked()
    {
        var battleService = ServiceContainer?.TryResolve<IBattleService>();
        if (battleService == null || string.IsNullOrEmpty(_selectedReserveUnitId))
            return;

        // 换牌失败时不提示（与历史行为一致）；成功则关闭面板。
        if (battleService.TrySwitchUnit(_outgoingUnitId, _selectedReserveUnitId, out _))
            CloseSelf();
    }

    private void OnCloseClicked() { CloseSelf(); }

    /// <summary>未选择替补时确认按钮不可用。</summary>
    private void UpdateConfirmState()
    {
        if (_confirmButton != null)
            _confirmButton.interactable = !string.IsNullOrEmpty(_selectedReserveUnitId);
    }

    private void ClearReserveButtons()
    {
        for (var i = 0; i < _reserveSlotViews.Count; i++)
        {
            var slotView = _reserveSlotViews[i];
            if (slotView == null) continue;
            slotView.Clicked -= OnReserveClicked;
            Destroy(slotView.gameObject);
        }
        _reserveSlotViews.Clear();

        for (var i = 0; i < _reserveButtons.Count; i++)
        {
            if (_reserveButtons[i] != null)
                Destroy(_reserveButtons[i].gameObject);
        }
        _reserveButtons.Clear();
    }

    /// <summary>替补列表容器（优先复用场景中的 ReservesRoot）。</summary>
    private RectTransform EnsureReservesRoot()
    {
        if (_reservesRoot == null)
            _reservesRoot = FindOrCreateRoot(transform, "ReservesRoot", true);
        return _reservesRoot;
    }
}
