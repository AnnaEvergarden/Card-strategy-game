using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 船坞面板（卡牌仓库）：纵向懒加载列表、返回主界面与回标题。
/// </summary>
public sealed class ShipyardPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 返回主界面按钮。
    /// </summary>
    [SerializeField] private Button BackBtn;

    /// <summary>
    /// 主界面按钮：清空导航栈并打开 <see cref="PanelNames.MainPanel"/>。
    /// </summary>
    [SerializeField] private Button HomeBtn;

    /// <summary>
    /// 懒加载纵向列表。
    /// </summary>
    [SerializeField] private ShipyardLazyVerticalList lazyList;

    /// <summary>
    /// cardId 到 <see cref="CardConfigSO"/> 的映射缓存（通过 <see cref="GameResourceLoader.GetCardConfigMap"/> 加载）。
    /// 非 readonly 以允许 <see cref="LoadConfigMap"/> 替换为全局共享字典引用。
    /// </summary>
    private Dictionary<string, CardConfigSO> _configMap = new();

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中已放置的船坞面板；若不存在则打调试警告并返回 null（不运行时创建默认物体）。
    /// </summary>
    public static ShipyardPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<ShipyardPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.ShipyardPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "ShipyardPanel: 场景中未找到 ShipyardPanel，请在场景或 UI 预制体中挂载该组件；已取消运行时自动搭建界面。");
        return null;
    }

    /// <summary>
    /// 点击返回：弹出当前面板，恢复上一层。
    /// </summary>
    public void OnClickBack()
    {
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 点击主界面：清空栈后以游戏主界面为唯一栈顶。
    /// </summary>
    public void OnClickHome()
    {
        UIPanelRegistry.ClearAndPush(PanelNames.MainPanel);
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时订阅按钮并刷新列表数据。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        ReloadList();
    }

    /// <summary>
    /// 禁用时取消订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        if (lazyList != null)
        {
            lazyList.SetShipyardDetailClickHandler(null);
        }

        base.OnDisable();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 订阅按钮。
    /// </summary>
    private void SubscribeButtons()
    {
        if (BackBtn != null)
        {
            BackBtn.onClick.AddListener(OnClickBack);
        }

        if (HomeBtn != null)
        {
            HomeBtn.onClick.AddListener(OnClickHome);
        }
    }

    /// <summary>
    /// 取消订阅按钮。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (BackBtn != null)
        {
            BackBtn.onClick.RemoveListener(OnClickBack);
        }

        if (HomeBtn != null)
        {
            HomeBtn.onClick.RemoveListener(OnClickHome);
        }
    }

    /// <summary>
    /// 加载卡牌仓库与配置并刷新懒加载列表。
    /// </summary>
    private void ReloadList()
    {
        var data = CardCollectionStore.Load(forceReload: true);
        LoadConfigMap();
        if (lazyList != null)
        {
            lazyList.SetShipyardDetailClickHandler(OnShipyardCardClicked);
            lazyList.SetData(data.cards, _configMap);
        }
    }

    /// <summary>
    /// 船坞槽位点击：打开舰娘详情（技能等）。
    /// </summary>
    /// <param name="cardId">收藏 cardId。</param>
    private void OnShipyardCardClicked(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return;
        }

        var trimmed = cardId.Trim();

        // 如果详情面板已在栈顶，直接强制刷新，避免 OnEnable 不触发导致数据不更新
        var top = UIPanelRegistry.PeekStackTop();
        if (string.Equals(top, PanelNames.ShipgirlDetailPanel, System.StringComparison.Ordinal))
        {
            var detail = ShipgirlDetailPanel.EnsureInstance();
            if (detail != null)
            {
                detail.ForceRefresh(trimmed);
                return;
            }
        }

        ShipgirlDetailOpenRequest.SetPending(trimmed);
        UIPanelRegistry.Push(PanelNames.ShipgirlDetailPanel);
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
