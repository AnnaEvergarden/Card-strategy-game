using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仓库面板：道具列表展示。
/// 通过 IInventoryService 获取数据，不再直接访问 ISaveService。
/// 监听 InventoryChangedEvent 自动刷新。
/// </summary>
public sealed class InventoryPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;
    [SerializeField] private Button _prevPageBtn;
    [SerializeField] private Button _nextPageBtn;

    [Header("Container")]
    [SerializeField] private Transform _container;
    [SerializeField] private GameObject _slotPrefab;

    [Inject] private IInventoryService _inventoryService;

    private IDisposable _eventToken;

    protected override void OnEnable()
    {
        base.OnEnable();
        ServiceContainer?.TryInject(this);
        Subscribe();

        var eventBus = ServiceContainer?.TryResolve<IEventBus>();
        _eventToken = eventBus?.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
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
        await PopulateSlotsAsync();
    }

    private async void OnInventoryChanged(InventoryChangedEvent evt)
    {
        await PopulateSlotsAsync();
    }

    private async UniTask PopulateSlotsAsync()
    {
        if (_container == null || _slotPrefab == null)
        {
            Debug.LogWarning("[InventoryPanel] _container or _slotPrefab not assigned");
            return;
        }

        // 清除旧槽位
        for (int i = _container.childCount - 1; i >= 0; i--)
        {
            var child = _container.GetChild(i);
            Destroy(child.gameObject);
        }

        // 通过 Application Service 获取道具数据
        IReadOnlyList<InventoryItem> items;
        try
        {
            items = await _inventoryService.GetItemsAsync();
        }
        catch
        {
            Debug.Log("[InventoryPanel] no items");
            return;
        }

        if (items == null || items.Count == 0)
        {
            Debug.Log("[InventoryPanel] no items");
            return;
        }

        // 获取配置
        var configService = ServiceContainer?.TryResolve<IConfigService>();
        var allConfigs = configService?.GetAll<ItemConfig>();
        if (allConfigs == null || allConfigs.Count == 0)
        {
            Debug.LogWarning("[InventoryPanel] item config table empty");
            return;
        }

        var configMap = new Dictionary<string, ItemConfig>(allConfigs.Count);
        for (int i = 0; i < allConfigs.Count; i++)
        {
            var cfg = allConfigs[i];
            if (cfg != null && !string.IsNullOrWhiteSpace(cfg.ItemId) && !configMap.ContainsKey(cfg.ItemId))
                configMap[cfg.ItemId] = cfg;
        }

        // 生成槽位
        var loadTasks = new List<UniTask>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null || item.Quantity <= 0)
                continue;

            if (!configMap.TryGetValue(item.ItemId, out var config))
            {
                Debug.LogWarning($"[InventoryPanel] item config not found: {item.ItemId}");
                continue;
            }

            var slotGo = Instantiate(_slotPrefab, _container, false);
            if (!slotGo.TryGetComponent<InventorySlotView>(out var slotView))
            {
                Debug.LogError("[InventoryPanel] Slot Prefab missing InventorySlotView component");
                continue;
            }

            loadTasks.Add(slotView.SetData(config, item.Quantity, ResourceService));
        }

        if (loadTasks.Count > 0)
            await UniTask.WhenAll(loadTasks);
    }

    public void OnClickBack() { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome() { UIService?.GoHomeAsync().Forget(); }
    public void OnClickPrevPage() { Debug.Log("[Inventory] 上一页"); }
    public void OnClickNextPage() { Debug.Log("[Inventory] 下一页"); }

    private void Subscribe()
    {
        if (_backBtn) _backBtn.onClick.AddListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.AddListener(OnClickHome);
        if (_prevPageBtn) _prevPageBtn.onClick.AddListener(OnClickPrevPage);
        if (_nextPageBtn) _nextPageBtn.onClick.AddListener(OnClickNextPage);
    }
    private void Unsubscribe()
    {
        if (_backBtn) _backBtn.onClick.RemoveListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.RemoveListener(OnClickHome);
        if (_prevPageBtn) _prevPageBtn.onClick.RemoveListener(OnClickPrevPage);
        if (_nextPageBtn) _nextPageBtn.onClick.RemoveListener(OnClickNextPage);
    }
}