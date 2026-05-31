using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 建造面板：支持卡池切换、资源展示、单抽/十连抽，并在抽卡后进入逐张领卡展示。
/// 卡池详情 UI（图、名、按钮区等）由 <see cref="BuildPoolDetailView"/> 预制体提供，本面板负责实例化与数据绑定。
/// </summary>
public sealed class BuildPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 返回按钮（弹栈）。
    /// </summary>
    [SerializeField] private Button backBtn;

    /// <summary>
    /// 主界面按钮（清栈回 MainPanel）。
    /// </summary>
    [SerializeField] private Button homeBtn;

    /// <summary>
    /// 卡池详情预制体挂载点；为空则使用本面板根 <see cref="RectTransform"/>。
    /// </summary>
    [SerializeField] private RectTransform poolDetailHost;

    /// <summary>
    /// 卡池详情预制体（根物体上挂 <see cref="BuildPoolDetailView"/>）。
    /// </summary>
    [SerializeField] private GameObject poolDetailPrefab;

    /// <summary>
    /// 货币信息弹窗预制体（后续用于展示金币/钻石/船票等资源）。
    /// </summary>
    [SerializeField] private GameObject currencyPopupPrefab;

    /// <summary>
    /// 卡池列表项预制体（建议挂 <see cref="BuildPoolButtonView"/>）。
    /// </summary>
    [SerializeField] private GameObject poolButtonPrefab;

    /// <summary>
    /// 运行时实例化的卡池详情视图。
    /// </summary>
    private BuildPoolDetailView _poolDetailView;

    /// <summary>
    /// 运行时创建的卡池按钮缓存。
    /// </summary>
    private readonly List<GameObject> _spawnedPoolButtons = new();

    /// <summary>
    /// 当前可选卡池列表。
    /// </summary>
    private readonly List<BuildPoolConfigSO> _pools = new();

    /// <summary>
    /// 当前选中卡池下标。
    /// </summary>
    private int _currentPoolIndex;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中已放置的建造面板；若不存在则打调试警告并返回 null（不运行时创建默认物体）。
    /// </summary>
    public static BuildPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<BuildPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.BuildPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "BuildPanel: 场景中未找到 BuildPanel，请在场景或 UI 预制体中挂载该组件；已取消运行时自动搭建界面。");
        return null;
    }

    /// <summary>
    /// 以默认卡池（第一个）打开建造面板。
    /// </summary>
    public void OpenDefault()
    {
        _currentPoolIndex = 0;
        ReloadPoolsAndRefresh();
    }

    /// <summary>
    /// 点击返回：弹栈回上层。
    /// </summary>
    public void OnClickBack()
    {
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 点击主界面：清栈回 MainPanel。
    /// </summary>
    public void OnClickHome()
    {
        UIPanelRegistry.ClearAndPush(PanelNames.MainPanel);
    }

    /// <summary>
    /// 点击建造一次。
    /// </summary>
    public void OnClickBuildOne()
    {
        TryBuild(1);
    }

    /// <summary>
    /// 点击建造十次。
    /// </summary>
    public void OnClickBuildTen()
    {
        TryBuild(10);
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时确保详情视图存在、订阅按钮并刷新展示。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        EnsurePoolDetailView();
        SubscribeButtons();
        ReloadPoolsAndRefresh();
    }

    /// <summary>
    /// 禁用时取消订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 在宿主下实例化卡池详情预制体（仅首次）；缺少预制体或组件时打警告。
    /// </summary>
    private void EnsurePoolDetailView()
    {
        if (_poolDetailView != null)
        {
            return;
        }

        if (poolDetailPrefab == null)
        {
            Debug.LogWarning(
                "BuildPanel: 未配置 poolDetailPrefab，无法显示卡池详情；请在 Inspector 指定含 BuildPoolDetailView 的预制体。");
            return;
        }

        var host = poolDetailHost != null ? poolDetailHost : GetComponent<RectTransform>();
        if (host == null)
        {
            Debug.LogWarning("BuildPanel: 无法解析 poolDetailHost，跳过卡池详情实例化。");
            return;
        }

        var instance = Instantiate(poolDetailPrefab, host, false);
        instance.name = "BuildPoolDetail (Runtime)";
        _poolDetailView = instance.GetComponent<BuildPoolDetailView>();
        if (_poolDetailView == null)
        {
            _poolDetailView = instance.GetComponentInChildren<BuildPoolDetailView>(true);
        }

        if (_poolDetailView == null)
        {
            Debug.LogWarning(
                "BuildPanel: poolDetailPrefab 上未找到 BuildPoolDetailView，请在该预制体根或子级挂载该脚本。");
        }
    }

    /// <summary>
    /// 读取卡池并重建按钮列表。
    /// </summary>
    private void ReloadPoolsAndRefresh()
    {
        _pools.Clear();
        var pools = BuildService.GetPools();
        if (pools != null)
        {
            for (var i = 0; i < pools.Count; i++)
            {
                var p = pools[i];
                if (p != null)
                {
                    _pools.Add(p);
                }
            }
        }

        if (_pools.Count == 0)
        {
            _currentPoolIndex = 0;
        }
        else
        {
            _currentPoolIndex = Mathf.Clamp(_currentPoolIndex, 0, _pools.Count - 1);
        }

        RebuildPoolButtons();
        RefreshPoolView();
        RefreshCurrencyPopup();
    }

    /// <summary>
    /// 重建卡池选择按钮。
    /// </summary>
    private void RebuildPoolButtons()
    {
        for (var i = 0; i < _spawnedPoolButtons.Count; i++)
        {
            var go = _spawnedPoolButtons[i];
            if (go != null)
            {
                Destroy(go);
            }
        }

        _spawnedPoolButtons.Clear();
        var buttonRoot = _poolDetailView != null ? _poolDetailView.PoolButtonRoot : null;
        if (buttonRoot == null || poolButtonPrefab == null)
        {
            return;
        }

        for (var i = 0; i < _pools.Count; i++)
        {
            var pool = _pools[i];
            var go = Instantiate(poolButtonPrefab, buttonRoot, false);
            go.name = $"PoolBtn_{i + 1}";
            var captured = i;
            var view = go.GetComponent<BuildPoolButtonView>() ?? go.GetComponentInChildren<BuildPoolButtonView>(true);
            if (view != null)
            {
                view.Bind(pool.DisplayName, () =>
                {
                    _currentPoolIndex = captured;
                    RefreshPoolView();
                });
            }
            else
            {
                var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        _currentPoolIndex = captured;
                        RefreshPoolView();
                    });
                }

                var tmp = go.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                {
                    tmp.text = pool.DisplayName;
                }
            }

            _spawnedPoolButtons.Add(go);
        }
    }

    /// <summary>
    /// 刷新当前选中卡池 UI（图片、名称、消耗）。
    /// </summary>
    private void RefreshPoolView()
    {
        if (_poolDetailView == null)
        {
            return;
        }

        var banner = _poolDetailView.PoolBannerImage;
        var nameTmp = _poolDetailView.PoolNameText;
        var costTmp = _poolDetailView.PoolCostText;
        var buildOne = _poolDetailView.BuildOneButton;
        var buildTen = _poolDetailView.BuildTenButton;

        if (_pools.Count == 0 || _currentPoolIndex < 0 || _currentPoolIndex >= _pools.Count)
        {
            if (nameTmp != null) nameTmp.text = "暂无卡池";
            if (costTmp != null) costTmp.text = "-";
            if (banner != null)
            {
                banner.sprite = null;
                banner.enabled = false;
            }

            if (buildOne != null) buildOne.interactable = false;
            if (buildTen != null) buildTen.interactable = false;
            return;
        }

        var pool = _pools[_currentPoolIndex];
        if (nameTmp != null)
        {
            nameTmp.text = string.IsNullOrWhiteSpace(pool.DisplayName) ? "未命名卡池" : pool.DisplayName;
        }

        if (costTmp != null)
        {
            costTmp.text = BuildCostSummary(pool);
        }

        if (banner != null)
        {
            banner.sprite = pool.PoolBannerSprite;
            banner.enabled = pool.PoolBannerSprite != null;
            banner.preserveAspect = true;
        }

        if (buildOne != null) buildOne.interactable = true;
        if (buildTen != null) buildTen.interactable = true;
    }

    /// <summary>
    /// 刷新货币弹窗（当前仅保留预留入口，后续由弹窗组件负责实际渲染）。
    /// </summary>
    private void RefreshCurrencyPopup()
    {
        if (currencyPopupPrefab == null)
        {
            return;
        }

        // 预留：后续接入货币弹窗实例/组件后，在此同步 CurrencyStore 数据。
    }

    /// <summary>
    /// 执行建造并打开领卡面板。
    /// </summary>
    private void TryBuild(int count)
    {
        if (_pools.Count == 0 || _currentPoolIndex < 0 || _currentPoolIndex >= _pools.Count)
        {
            return;
        }

        var pool = _pools[_currentPoolIndex];
        if (!BuildService.TryBuild(pool.PoolId, count, out var result, out var error))
        {
            Debug.LogWarning($"BuildPanel: 建造失败 - {error}");
            RefreshCurrencyPopup();
            return;
        }

        RefreshCurrencyPopup();
        var reveal = CardRevealPanel.EnsureInstance();
        if (reveal == null)
        {
            Debug.LogWarning("BuildPanel: 未找到 CardRevealPanel，无法展示抽卡结果。");
            return;
        }

        reveal.OpenWithCards(result.cardIds);
        UIPanelRegistry.Push(PanelNames.CardRevealPanel);
    }

    /// <summary>
    /// 组装当前卡池消耗摘要文案。
    /// </summary>
    private static string BuildCostSummary(BuildPoolConfigSO pool)
    {
        var one = pool.BuildOneCost;
        var ten = pool.BuildTenCost;
        var oneTxt = one == null
            ? "单抽 -"
            : $"单抽 金币{Mathf.Max(0, one.gold)} 钻石{Mathf.Max(0, one.diamond)} 船票{Mathf.Max(0, one.shipTicket)}";
        var tenTxt = ten == null
            ? "十连 -"
            : $"十连 金币{Mathf.Max(0, ten.gold)} 钻石{Mathf.Max(0, ten.diamond)} 船票{Mathf.Max(0, ten.shipTicket)}";
        return $"{oneTxt}\n{tenTxt}";
    }

    /// <summary>
    /// 订阅按钮。
    /// </summary>
    private void SubscribeButtons()
    {
        if (backBtn != null) backBtn.onClick.AddListener(OnClickBack);
        if (homeBtn != null) homeBtn.onClick.AddListener(OnClickHome);
        if (_poolDetailView != null)
        {
            var one = _poolDetailView.BuildOneButton;
            var ten = _poolDetailView.BuildTenButton;
            if (one != null) one.onClick.AddListener(OnClickBuildOne);
            if (ten != null) ten.onClick.AddListener(OnClickBuildTen);
        }
    }

    /// <summary>
    /// 取消订阅按钮。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (backBtn != null) backBtn.onClick.RemoveListener(OnClickBack);
        if (homeBtn != null) homeBtn.onClick.RemoveListener(OnClickHome);
        if (_poolDetailView != null)
        {
            var one = _poolDetailView.BuildOneButton;
            var ten = _poolDetailView.BuildTenButton;
            if (one != null) one.onClick.RemoveListener(OnClickBuildOne);
            if (ten != null) ten.onClick.RemoveListener(OnClickBuildTen);
        }
    }

    #endregion
}
