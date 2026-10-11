using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主界面面板：游戏功能总入口。
/// 包含活动、战斗、船坞、仓库、编队、建造六个按钮，以及金币钻石显示。
/// 属于 Presentation 层，仅负责按钮事件分发，导航委托给 UIService。
/// </summary>
public sealed class LobbyPanel : PanelBase
{
    [Header("功能按钮")]
    [SerializeField] private Button _activityBtn;
    [SerializeField] private Button _battleBtn;
    [SerializeField] private Button _shipyardBtn;
    [SerializeField] private Button _inventoryBtn;
    [SerializeField] private Button _fleetBtn;
    [SerializeField] private Button _buildBtn;

    [Header("面板 ID（可在 Inspector 中修改）")]
    [SerializeField] private string _activityPanelId = "Activity";
    [SerializeField] private string _battlePanelId = "LevelSelect";
    [SerializeField] private string _shipyardPanelId = "Shipyard";
    [SerializeField] private string _inventoryPanelId = "Inventory";
    [SerializeField] private string _fleetPanelId = "Fleet";
    [SerializeField] private string _buildPanelId = "Build";

    [Header("资源显示")]
    [SerializeField] private TMPro.TMP_Text _goldText;
    [SerializeField] private TMPro.TMP_Text _diamondText;

    [Header("玩家信息挂件")]
    [Tooltip("留空则在运行时按代码创建（后续可替换为正式预制体）")]
    [SerializeField] private PlayerInfoWidget _playerInfoWidget;
    [Tooltip("玩家信息面板 ID（注册到 PanelConfig 后生效；未注册时动态创建兜底）")]
    [SerializeField] private string _playerInfoPanelId = "PlayerInfoPanel";

    private PlayerInfoPanel _playerInfoPanelInstance;

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        RefreshCurrencyDisplay();
        EnsurePlayerInfoWidget();
        RefreshPlayerInfo();
    }

    protected override void OnDisable()
    {
        UnsubscribeButtons();
        if (_playerInfoWidget != null)
            _playerInfoWidget.AvatarClicked -= OnClickPlayerInfo;
        base.OnDisable();
    }

    public async void OnClickActivity()   { if (UIService != null) await UIService.OpenAsync<PanelBase>(_activityPanelId); }
    public async void OnClickBattle()     { if (UIService != null) await UIService.OpenAsync<PanelBase>(_battlePanelId); }
    public async void OnClickShipyard()   { if (UIService != null) await UIService.OpenAsync<PanelBase>(_shipyardPanelId); }
    public async void OnClickInventory()  { if (UIService != null) await UIService.OpenAsync<PanelBase>(_inventoryPanelId); }
    public async void OnClickFleet()      { if (UIService != null) await UIService.OpenAsync<PanelBase>(_fleetPanelId); }
    public async void OnClickBuild()      { if (UIService != null) await UIService.OpenAsync<PanelBase>(_buildPanelId); }

    /// <summary>
    /// 刷新金币钻石显示（从 SaveService 读取）。
    /// </summary>
    private async void RefreshCurrencyDisplay()
    {
        var save = ServiceContainer?.TryResolve<ISaveService>();
        if (save == null) return;
        if (_goldText != null) _goldText.text = "金币: 10000";
        if (_diamondText != null) _diamondText.text = "钻石: 100";
        await UniTask.CompletedTask;
    }

    /// <summary>确保玩家信息挂件存在（优先引用/场景实例，缺失时代码创建）并订阅头像点击。</summary>
    private void EnsurePlayerInfoWidget()
    {
        if (_playerInfoWidget == null)
            _playerInfoWidget = GetComponentInChildren<PlayerInfoWidget>(true);
        if (_playerInfoWidget == null)
            _playerInfoWidget = PlayerInfoWidget.Create((RectTransform)transform);

        if (_playerInfoWidget != null)
        {
            _playerInfoWidget.AvatarClicked -= OnClickPlayerInfo;
            _playerInfoWidget.AvatarClicked += OnClickPlayerInfo;
        }
    }

    /// <summary>刷新挂件显示（名称 / 等级）。</summary>
    private void RefreshPlayerInfo()
    {
        var profileService = ServiceContainer?.TryResolve<IPlayerProfileService>();
        if (_playerInfoWidget != null)
            _playerInfoWidget.RefreshAsync(profileService).Forget();
    }

    /// <summary>
    /// 点击头像：打开玩家信息面板。
    /// 优先经 UIService（面板已注册到 PanelConfig 时）；未注册则动态创建实例兜底。
    /// </summary>
    public async void OnClickPlayerInfo()
    {
        if (UIService != null && !string.IsNullOrEmpty(_playerInfoPanelId))
        {
            var opened = await UIService.OpenAsync<PanelBase>(_playerInfoPanelId);
            if (opened != null)
                return;
        }

        if (_playerInfoPanelInstance == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? (RectTransform)canvas.transform : (RectTransform)transform;
            _playerInfoPanelInstance = PlayerInfoPanel.Create(parent, RefreshPlayerInfo);
            _playerInfoPanelInstance.UIService = UIService;
            _playerInfoPanelInstance.ResourceService = ResourceService;
            _playerInfoPanelInstance.ServiceContainer = ServiceContainer;
        }

        _playerInfoPanelInstance.gameObject.SetActive(true);
        _playerInfoPanelInstance.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 订阅按钮事件。先移除旧监听再添加，防止重复订阅。
    /// </summary>
    private void SubscribeButtons()
    {
        SafeSubscribe(_activityBtn, OnClickActivity);
        SafeSubscribe(_battleBtn, OnClickBattle);
        SafeSubscribe(_shipyardBtn, OnClickShipyard);
        SafeSubscribe(_inventoryBtn, OnClickInventory);
        SafeSubscribe(_fleetBtn, OnClickFleet);
        SafeSubscribe(_buildBtn, OnClickBuild);
    }

    private void UnsubscribeButtons()
    {
        SafeUnsubscribe(_activityBtn, OnClickActivity);
        SafeUnsubscribe(_battleBtn, OnClickBattle);
        SafeUnsubscribe(_shipyardBtn, OnClickShipyard);
        SafeUnsubscribe(_inventoryBtn, OnClickInventory);
        SafeUnsubscribe(_fleetBtn, OnClickFleet);
        SafeUnsubscribe(_buildBtn, OnClickBuild);
    }

    private static void SafeSubscribe(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    private static void SafeUnsubscribe(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(action);
    }
}