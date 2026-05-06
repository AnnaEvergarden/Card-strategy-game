using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仓库面板：负责分页显示仓库道具、返回主界面和回到标题场景。
/// </summary>
public sealed class InventoryPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 返回按钮（回 MainPanel）。
    /// </summary>
    [SerializeField] private Button BackBtn;

    /// <summary>
    /// 主界面按钮：清空导航栈并打开游戏主界面（<see cref="PanelNames.MainPanel"/>）。
    /// </summary>
    [SerializeField] private Button HomeBtn;

    /// <summary>
    /// 下一页按钮。
    /// </summary>
    [SerializeField] private Button NextPageBtn;

    /// <summary>
    /// 上一页按钮。
    /// </summary>
    [SerializeField] private Button PreviousPageBtn;

    /// <summary>
    /// 页码文本。
    /// </summary>
    [SerializeField] private TMP_Text pageText;

    /// <summary>
    /// 格子预制体（InventorySlot）。
    /// </summary>
    [SerializeField] private GameObject slotPrefab;

    /// <summary>
    /// 格子容器。
    /// </summary>
    [SerializeField] private RectTransform container;

    /// <summary>
    /// 每页最大格子数量。
    /// </summary>
    [SerializeField] private int maxSlotsPerPage = 45;

    /// <summary>
    /// 当前页（从 1 开始）。
    /// </summary>
    private int _currentPage = 1;

    /// <summary>
    /// 仓库数据缓存。
    /// </summary>
    private List<InventoryStore.InventoryItemData> _items = new();

    /// <summary>
    /// 当前页实例化出来的格子缓存，便于刷新前销毁。
    /// </summary>
    private readonly List<GameObject> _spawnedSlots = new();

    /// <summary>
    /// 道具配置映射（key 为 itemId）。
    /// </summary>
    private readonly Dictionary<string, ItemConfigSO> _itemConfigMap = new();

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 面板启用时注册并刷新仓库页面。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        ReloadAndRender();
    }

    /// <summary>
    /// 面板禁用时取消订阅并清理格子。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        ClearSlots();
        base.OnDisable();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 点击返回按钮：弹出当前面板，恢复上一层。
    /// </summary>
    public void OnClickBack()
    {
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 点击主界面按钮：清空栈后以游戏主界面为唯一栈顶。
    /// </summary>
    public void OnClickHome()
    {
        UIPanelRegistry.ClearAndPush(PanelNames.MainPanel);
    }

    /// <summary>
    /// 点击下一页按钮。
    /// </summary>
    public void OnClickNextPage()
    {
        var totalPage = GetTotalPage();
        if (_currentPage >= totalPage) return;
        _currentPage++;
        RenderCurrentPage();
    }

    /// <summary>
    /// 点击上一页按钮。
    /// </summary>
    public void OnClickPreviousPage()
    {
        if (_currentPage <= 1) return;
        _currentPage--;
        RenderCurrentPage();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 重新读取仓库数据并渲染当前页。
    /// </summary>
    private void ReloadAndRender()
    {
        LoadItemConfigs();
        var data = InventoryStore.Load();
        _items = data?.items ?? new List<InventoryStore.InventoryItemData>();
        _currentPage = Mathf.Clamp(_currentPage, 1, GetTotalPage());
        RenderCurrentPage();
    }

    /// <summary>
    /// 渲染当前页格子。
    /// </summary>
    private void RenderCurrentPage()
    {
        ClearSlots();

        if (slotPrefab == null || container == null)
        {
            UpdatePageText();
            return;
        }

        var startIndex = (_currentPage - 1) * maxSlotsPerPage;
        for (var i = 0; i < maxSlotsPerPage; i++)
        {
            var go = Object.Instantiate(slotPrefab, container);
            _spawnedSlots.Add(go);

            var itemIndex = startIndex + i;
            var itemData = itemIndex < _items.Count ? _items[itemIndex] : null;
            var itemConfig = TryGetItemConfig(itemData);

            var slotView = go.GetComponent<InventorySlotView>();
            if (slotView != null)
            {
                slotView.Bind(itemData, itemConfig);
            }
        }

        UpdatePageText();
    }

    /// <summary>
    /// 销毁当前页已经创建的所有格子。
    /// </summary>
    private void ClearSlots()
    {
        for (var i = 0; i < _spawnedSlots.Count; i++)
        {
            var go = _spawnedSlots[i];
            if (go != null) Object.Destroy(go);
        }
        _spawnedSlots.Clear();
    }

    /// <summary>
    /// 计算总页数，至少为 1。
    /// </summary>
    private int GetTotalPage()
    {
        if (maxSlotsPerPage <= 0) return 1;
        return Mathf.Max(1, Mathf.CeilToInt(_items.Count / (float)maxSlotsPerPage));
    }

    /// <summary>
    /// 更新页码文本与分页按钮状态。
    /// </summary>
    private void UpdatePageText()
    {
        var totalPage = GetTotalPage();
        if (pageText != null)
        {
            pageText.text = $"{_currentPage}/{totalPage}";
        }

        if (PreviousPageBtn != null) PreviousPageBtn.interactable = _currentPage > 1;
        if (NextPageBtn != null) NextPageBtn.interactable = _currentPage < totalPage;
    }

    /// <summary>
    /// 订阅按钮事件。
    /// </summary>
    private void SubscribeButtons()
    {
        if (BackBtn != null) BackBtn.onClick.AddListener(OnClickBack);
        if (HomeBtn != null) HomeBtn.onClick.AddListener(OnClickHome);
        if (NextPageBtn != null) NextPageBtn.onClick.AddListener(OnClickNextPage);
        if (PreviousPageBtn != null) PreviousPageBtn.onClick.AddListener(OnClickPreviousPage);
    }

    /// <summary>
    /// 取消按钮订阅。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (BackBtn != null) BackBtn.onClick.RemoveListener(OnClickBack);
        if (HomeBtn != null) HomeBtn.onClick.RemoveListener(OnClickHome);
        if (NextPageBtn != null) NextPageBtn.onClick.RemoveListener(OnClickNextPage);
        if (PreviousPageBtn != null) PreviousPageBtn.onClick.RemoveListener(OnClickPreviousPage);
    }

    /// <summary>
    /// 从 Resources 读取道具配置库并建立 itemId 索引。
    /// </summary>
    private void LoadItemConfigs()
    {
        _itemConfigMap.Clear();

        var database = GameResourceLoader.LoadItemConfigDatabase(logOnMissing: false);
        if (database == null || database.Items == null)
        {
            return;
        }

        for (var i = 0; i < database.Items.Count; i++)
        {
            var config = database.Items[i];
            if (config == null) continue;
            if (string.IsNullOrWhiteSpace(config.ItemId)) continue;
            _itemConfigMap[config.ItemId] = config;
        }
    }

    /// <summary>
    /// 根据仓库数据查找对应道具配置。
    /// </summary>
    private ItemConfigSO TryGetItemConfig(InventoryStore.InventoryItemData itemData)
    {
        if (itemData == null || string.IsNullOrWhiteSpace(itemData.itemId)) return null;
        return _itemConfigMap.TryGetValue(itemData.itemId, out var config) ? config : null;
    }

    #endregion
}

