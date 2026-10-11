using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 建造面板：卡池选择、资源消耗与建造执行入口。
/// 打开时从配置表读取开启的卡池，生成按钮切换展示大图。
/// 建造时调用 BuildService 执行抽卡，结果传给 CardRevealPanel 展示。
/// </summary>
public sealed class BuildPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;
    [SerializeField] private Button _buildOneBtn;
    [SerializeField] private Button _buildTenBtn;

    [Header("Pool Selection")]
    [SerializeField] private Transform _poolButtonsRoot;
    [SerializeField] private GameObject _poolButtonPrefab;
    [SerializeField] private Image _poolBannerImage;
    [SerializeField] private TMP_Text _poolNameText;

    [Header("面板 ID")]
    [SerializeField]
    private string _cardRevealPanelId = "CardReveal";

    private BuildPoolConfig _selectedPool;
    private List<BuildPoolConfig> _openPools = new List<BuildPoolConfig>();

    protected override void OnEnable()
    {
        base.OnEnable();
        Subscribe();
    }

    protected override void OnDisable() { Unsubscribe(); base.OnDisable(); }

    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);
        await PopulatePoolsAsync();
    }

    private async UniTask PopulatePoolsAsync()
    {
        for (int i = _poolButtonsRoot.childCount - 1; i >= 0; i--)
            Destroy(_poolButtonsRoot.GetChild(i).gameObject);

        _openPools.Clear();
        _selectedPool = null;

        var configService = ServiceContainer?.TryResolve<IConfigService>();
        var allPools = configService?.GetAll<BuildPoolConfig>();
        if (allPools == null) return;

        for (int i = 0; i < allPools.Count; i++)
        {
            if (allPools[i] != null && allPools[i].IsOpen)
                _openPools.Add(allPools[i]);
        }

        if (_openPools.Count == 0) return;
        if (_poolButtonPrefab == null || _poolButtonsRoot == null) return;

        var loadTasks = new List<UniTask>(_openPools.Count);

        for (int i = 0; i < _openPools.Count; i++)
        {
            var pool = _openPools[i];
            var btnGo = Instantiate(_poolButtonPrefab, _poolButtonsRoot, false);

            var btnView = btnGo.GetComponent<BuildPoolButtonView>();
            if (btnView != null)
            {
                btnView.SetData(pool, () => OnSelectPool(pool));

                if (pool.ButtonIconResourceId > 0)
                {
                    var resourceId = pool.ButtonIconResourceId;
                    loadTasks.Add(LoadButtonIconAsync(btnView, resourceId));
                }
            }
        }

        if (loadTasks.Count > 0)
            await UniTask.WhenAll(loadTasks);

        OnSelectPool(_openPools[0]);
    }

    private async UniTask LoadButtonIconAsync(BuildPoolButtonView btnView, int resourceId)
    {
        var sprite = await ResourceService.LoadAsync<Sprite>((ResourceId)resourceId);
        // await 后重新校验：按钮可能已随面板销毁。
        if (sprite != null && btnView != null)
            btnView.SetIcon(sprite);
    }

    private async void OnSelectPool(BuildPoolConfig pool)
    {
        _selectedPool = pool;

        if (_poolNameText != null)
            _poolNameText.text = pool.DisplayName;

        if (_poolBannerImage != null && pool.BannerResourceId > 0)
        {
            var sprite = await ResourceService.LoadAsync<Sprite>((ResourceId)pool.BannerResourceId);
            // await 后重新校验：面板可能已销毁，或玩家已切换到其他卡池（避免显示过期横幅）。
            if (sprite != null && _poolBannerImage != null && _selectedPool == pool)
                _poolBannerImage.sprite = sprite;
        }
    }

    public void OnClickBack() { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome() { UIService?.GoHomeAsync().Forget(); }

    private async void OnClickBuild(int count)
    {
        Debug.Log($"[BuildPanel] OnClickBuild({count}) start");
        Debug.Log($"[BuildPanel] _selectedPool={(UIService != null ? (_selectedPool?.PoolId ?? "null") : "UIService=null")}");

        if (_selectedPool == null || UIService == null) return;

        Debug.Log($"[BuildPanel] pool={_selectedPool.PoolId}, DropEntries={_selectedPool.DropEntries?.Count}");

        var result = BuildService.Draw(_selectedPool, count);
        Debug.Log($"[BuildPanel] Draw result CardIds={result?.CardIds?.Count}");

        if (result == null || result.CardIds == null || result.CardIds.Count == 0) return;

        var configService = ServiceContainer?.TryResolve<IConfigService>();
        Debug.Log($"[BuildPanel] configService={(configService != null ? "ok" : "null")}");

        var drawnCards = new List<CardConfig>(result.CardIds.Count);
        for (int i = 0; i < result.CardIds.Count; i++)
        {
            var cardConfig = configService?.Get<CardConfig>(result.CardIds[i]);
            Debug.Log($"[BuildPanel] CardId={result.CardIds[i]} config={(cardConfig != null ? cardConfig.DisplayName : "null")}");
            if (cardConfig != null) drawnCards.Add(cardConfig);
        }
        Debug.Log($"[BuildPanel] drawnCards={drawnCards.Count}");

        if (drawnCards.Count == 0) return;

        Debug.Log($"[BuildPanel] OpenAsync CardReveal panelId={_cardRevealPanelId}");
        await UIService.OpenAsync<PanelBase>(_cardRevealPanelId, drawnCards);
        Debug.Log($"[BuildPanel] OpenAsync done");
    }

    public void OnClickBuildOne() { OnClickBuild(1); }
    public void OnClickBuildTen() { OnClickBuild(10); }

    private void Subscribe()
    {
        SafeSubscribe(_backBtn, OnClickBack);
        SafeSubscribe(_homeBtn, OnClickHome);
        SafeSubscribe(_buildOneBtn, OnClickBuildOne);
        SafeSubscribe(_buildTenBtn, OnClickBuildTen);
    }

    private void Unsubscribe()
    {
        SafeUnsubscribe(_backBtn, OnClickBack);
        SafeUnsubscribe(_homeBtn, OnClickHome);
        SafeUnsubscribe(_buildOneBtn, OnClickBuildOne);
        SafeUnsubscribe(_buildTenBtn, OnClickBuildTen);
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