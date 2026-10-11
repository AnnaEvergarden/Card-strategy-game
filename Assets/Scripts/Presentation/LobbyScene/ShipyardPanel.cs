using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 船坞面板：玩家已收集舰娘展示（Scroll View 懒加载列表）。
/// 通过 ICardCollectionService 获取数据，不再直接访问 ISaveService。
/// 监听 CardCollectionChangedEvent 自动刷新。
/// </summary>
public sealed class ShipyardPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;

    [Header("Scroll View")]
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _slotPrefab;

    [Header("Detail Panel")]
    [SerializeField] private string _shipgirlDetailPanelId = "ShipgirlDetail";

    [Inject] private ICardCollectionService _cardCollectionService;

    private IDisposable _eventToken;

    protected override void OnEnable()
    {
        base.OnEnable();
        ServiceContainer?.TryInject(this);
        Subscribe();

        var eventBus = ServiceContainer?.TryResolve<IEventBus>();
        _eventToken = eventBus?.Subscribe<CardCollectionChangedEvent>(OnCollectionChanged);
    }

    protected override void OnDisable()
    {
        _eventToken?.Dispose();
        _eventToken = null;
        Unsubscribe();
        base.OnDisable();
    }

    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);
        await PopulateCardSlotsAsync();
    }

    private async void OnCollectionChanged(CardCollectionChangedEvent evt)
    {
        await PopulateCardSlotsAsync();
    }

    private async UniTask PopulateCardSlotsAsync()
    {
        if (_content == null || _slotPrefab == null)
        {
            Debug.LogWarning("[ShipyardPanel] _content or _slotPrefab not assigned, skip slot generation");
            return;
        }

        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            var child = _content.GetChild(i);
            Destroy(child.gameObject);
        }

        // 通过 Application Service 获取卡牌数据
        IReadOnlyList<CardInstance> cards;
        try
        {
            cards = await _cardCollectionService.GetCardsAsync();
        }
        catch
        {
            Debug.Log("[ShipyardPanel] no collected cards");
            return;
        }

        if (cards == null || cards.Count == 0)
        {
            Debug.Log("[ShipyardPanel] no collected cards");
            return;
        }

        // 获取卡牌配置（用于显示时仍需配置数据）
        var configService = ServiceContainer?.TryResolve<IConfigService>();
        var allConfigs = configService?.GetAll<CardConfig>();
        if (allConfigs == null || allConfigs.Count == 0)
        {
            Debug.LogWarning("[ShipyardPanel] card config table empty");
            return;
        }

        var configMap = new Dictionary<string, CardConfig>(allConfigs.Count);
        for (int i = 0; i < allConfigs.Count; i++)
        {
            var cfg = allConfigs[i];
            if (cfg != null && !string.IsNullOrWhiteSpace(cfg.CardId) && !configMap.ContainsKey(cfg.CardId))
                configMap[cfg.CardId] = cfg;
        }

        // 生成槽位
        for (int i = 0; i < cards.Count; i++)
        {
            var cardInstance = cards[i];
            if (cardInstance == null || string.IsNullOrWhiteSpace(cardInstance.CardId))
                continue;

            if (!configMap.TryGetValue(cardInstance.CardId, out var config))
            {
                Debug.LogWarning($"[ShipyardPanel] card config not found: CardId={cardInstance.CardId}");
                continue;
            }

            var slotGo = Instantiate(_slotPrefab, _content, false);
            if (!slotGo.TryGetComponent<ShipyardCardSlotView>(out var slotView))
            {
                Debug.LogError("[ShipyardPanel] Slot Prefab missing ShipyardCardSlotView component");
                continue;
            }

            slotView.OnSlotClicked += OnSlotClicked;
            slotView.SetData(config, ResourceService).Forget();
        }
    }

    private void OnSlotClicked(CardConfig config)
    {
        if (string.IsNullOrWhiteSpace(_shipgirlDetailPanelId))
        {
            Debug.LogWarning("[ShipyardPanel] _shipgirlDetailPanelId not configured");
            return;
        }
        UIService?.OpenAsync<ShipgirlDetailPanel>(_shipgirlDetailPanelId, config).Forget();
    }

    public void OnClickBack() { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome() { UIService?.GoHomeAsync().Forget(); }

    private void Subscribe()
    {
        if (_backBtn) _backBtn.onClick.AddListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.AddListener(OnClickHome);
    }

    private void Unsubscribe()
    {
        if (_backBtn) _backBtn.onClick.RemoveListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.RemoveListener(OnClickHome);
    }
}
