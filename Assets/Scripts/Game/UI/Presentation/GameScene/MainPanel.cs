using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏主界面面板：开始战斗（进入选关）、道具仓库、船坞与活动等入口；展示部分玩家资源（金币、钻石）。
/// </summary>
public sealed class MainPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 开始战斗按钮。
    /// </summary>
    [SerializeField] private Button battleBtn;

    /// <summary>
    /// 道具仓库按钮。
    /// </summary>
    [SerializeField] private Button inventoryBtn;

    /// <summary>
    /// 船坞（卡牌仓库）按钮。
    /// </summary>
    [SerializeField] private Button shipyardBtn;

    /// <summary>
    /// 编队按钮。
    /// </summary>
    [SerializeField] private Button fleetBtn;

    /// <summary>
    /// 建造按钮。
    /// </summary>
    [SerializeField] private Button buildBtn;

    /// <summary>
    /// 活动按钮。
    /// </summary>
    [SerializeField] private Button activityBtn;

    /// <summary>
    /// 金币数量展示（可选）。
    /// </summary>
    [SerializeField] private TMP_Text goldText;

    /// <summary>
    /// 钻石数量展示（可选）。
    /// </summary>
    [SerializeField] private TMP_Text diamondText;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 面板启用时注册并订阅按钮，并刷新资源展示。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        RefreshCurrencyDisplay();
    }

    /// <summary>
    /// 面板禁用时取消按钮订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 点击开始战斗：进入选择关卡面板（常驻 / 活动关卡等）。
    /// </summary>
    public void OnClickBattle()
    {
        if (LevelSelectPanel.EnsureInstance() == null)
        {
            return;
        }

        UIPanelRegistry.Push(PanelNames.LevelSelectPanel);
    }

    /// <summary>
    /// 点击仓库：压栈打开仓库面板。
    /// </summary>
    public void OnClickInventory()
    {
        UIPanelRegistry.Push(PanelNames.InventoryPanel);
    }

    /// <summary>
    /// 点击船坞：压栈打开卡牌仓库面板。
    /// </summary>
    public void OnClickShipyard()
    {
        if (ShipyardPanel.EnsureInstance() == null)
        {
            return;
        }

        UIPanelRegistry.Push(PanelNames.ShipyardPanel);
    }

    /// <summary>
    /// 点击编队：压栈打开编队面板，默认显示第一套卡组。
    /// </summary>
    public void OnClickFleet()
    {
        var panel = FleetPanel.EnsureInstance();
        if (panel == null)
        {
            return;
        }

        panel.OpenDefault();
        UIPanelRegistry.Push(PanelNames.FleetPanel);
    }

    /// <summary>
    /// 点击建造：压栈打开建造面板，默认显示第一个卡池。
    /// </summary>
    public void OnClickBuild()
    {
        var panel = BuildPanel.EnsureInstance();
        if (panel == null)
        {
            return;
        }

        panel.OpenDefault();
        UIPanelRegistry.Push(PanelNames.BuildPanel);
    }

    /// <summary>
    /// 点击活动：压栈打开活动面板。
    /// </summary>
    public void OnClickActivity()
    {
        UIPanelRegistry.Push(PanelNames.ActivityPanel);
    }

    /// <summary>
    /// 从 <see cref="CurrencyStore"/> 刷新主界面金币、钻石文案（供外部在消费后主动调用）。
    /// </summary>
    public void RefreshCurrencyDisplay()
    {
        var data = CurrencyStore.Load();
        if (goldText != null)
        {
            goldText.text = $"金币: {data.gold}";
        }

        if (diamondText != null)
        {
            diamondText.text = $"钻石: {data.diamond}";
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 订阅按钮事件。
    /// </summary>
    private void SubscribeButtons()
    {
        if (battleBtn != null) battleBtn.onClick.AddListener(OnClickBattle);
        if (inventoryBtn != null) inventoryBtn.onClick.AddListener(OnClickInventory);
        if (shipyardBtn != null) shipyardBtn.onClick.AddListener(OnClickShipyard);
        if (fleetBtn != null) fleetBtn.onClick.AddListener(OnClickFleet);
        if (buildBtn != null) buildBtn.onClick.AddListener(OnClickBuild);
        if (activityBtn != null) activityBtn.onClick.AddListener(OnClickActivity);
    }

    /// <summary>
    /// 取消按钮事件订阅。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (battleBtn != null) battleBtn.onClick.RemoveListener(OnClickBattle);
        if (inventoryBtn != null) inventoryBtn.onClick.RemoveListener(OnClickInventory);
        if (shipyardBtn != null) shipyardBtn.onClick.RemoveListener(OnClickShipyard);
        if (fleetBtn != null) fleetBtn.onClick.RemoveListener(OnClickFleet);
        if (buildBtn != null) buildBtn.onClick.RemoveListener(OnClickBuild);
        if (activityBtn != null) activityBtn.onClick.RemoveListener(OnClickActivity);
    }

    #endregion
}
