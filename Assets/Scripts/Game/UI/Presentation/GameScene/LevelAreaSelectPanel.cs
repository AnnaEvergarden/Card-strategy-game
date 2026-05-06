using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 区域选择面板：常驻/活动共用；横向 Scroll 内每项为区域信息预制体（<see cref="LevelAreaInfoCardView"/>），由本面板负责实例化并 Bind。
/// </summary>
public sealed class LevelAreaSelectPanel : BasePanel, IBeginDragHandler, IEndDragHandler
{
    #region Fields

    /// <summary>
    /// 返回上一面板按钮（弹栈）。
    /// </summary>
    [SerializeField] private Button backBtn;

    /// <summary>
    /// 主界面按钮（清栈回 MainPanel）。
    /// </summary>
    [SerializeField] private Button homeBtn;

    /// <summary>
    /// 上一个区域按钮。
    /// </summary>
    [SerializeField] private Button previousAreaBtn;

    /// <summary>
    /// 下一个区域按钮。
    /// </summary>
    [SerializeField] private Button nextAreaBtn;

    /// <summary>
    /// 进入当前区域按钮。
    /// </summary>
    [SerializeField] private Button enterAreaBtn;

    /// <summary>
    /// 横向滚动切换控件。
    /// </summary>
    [SerializeField] private ScrollRect areaScrollRect;

    /// <summary>
    /// 横向页面容器（ScrollRect 的 Content；每项为区域卡片预制体）。
    /// </summary>
    [SerializeField] private RectTransform areaPagesRoot;

    /// <summary>
    /// Scroll Content 下的单项预制体（根或子节点需有 <see cref="LevelAreaInfoCardView"/>；可选 <see cref="Button"/> 用于点击选中）。
    /// 未配置时使用代码生成的占位页。
    /// </summary>
    [FormerlySerializedAs("areaInfoPrefab")]
    [SerializeField] private LevelAreaInfoCardView areaPageItemPrefab;

    /// <summary>
    /// 运行时加载的区域数据库。
    /// </summary>
    private LevelAreaDatabaseSO _database;

    /// <summary>
    /// 当前模式（常驻/活动）。
    /// </summary>
    private LevelMode _currentMode = LevelMode.Permanent;

    /// <summary>
    /// 当前模式下可展示区域配置（SO 引用）列表。
    /// </summary>
    private readonly List<LevelAreaConfigSO> _areas = new();

    /// <summary>
    /// 当前选中区域下标。
    /// </summary>
    private int _currentIndex;

    /// <summary>
    /// 是否正在手势拖动（拖动结束时吸附到最近页）。
    /// </summary>
    private bool _isDragging;

    /// <summary>
    /// 拖拽结束后等待惯性衰减再触发一次吸附。
    /// </summary>
    private bool _pendingSnap;

    /// <summary>
    /// 吸附触发的横向速度阈值（像素/秒）：低于该值时认为惯性基本停止。
    /// </summary>
    [SerializeField] private float snapVelocityThreshold = 120f;

    /// <summary>
    /// 吸附过渡速度（越大越快贴近目标页）。
    /// </summary>
    [SerializeField] private float snapLerpSpeed = 12f;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中已放置的区域选择面板；若不存在则打调试警告并返回 null（不运行时创建默认物体）。
    /// </summary>
    public static LevelAreaSelectPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<LevelAreaSelectPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.LevelAreaSelectPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "LevelAreaSelectPanel: 场景中未找到 LevelAreaSelectPanel，请在场景或 UI 预制体中挂载该组件；已取消运行时自动搭建界面。");
        return null;
    }

    /// <summary>
    /// 以指定模式打开区域选择（常驻与活动共用一个面板，仅读取配置不同）。
    /// </summary>
    /// <param name="mode">要展示的模式。</param>
    public void OpenWithMode(LevelMode mode)
    {
        _currentMode = mode;
        ReloadAreas();
    }

    /// <summary>
    /// 点击返回：弹出当前面板回到上层。
    /// </summary>
    public void OnClickBack()
    {
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 点击主界面：清空栈并回 MainPanel。
    /// </summary>
    public void OnClickHome()
    {
        UIPanelRegistry.ClearAndPush(PanelNames.MainPanel);
    }

    /// <summary>
    /// 点击上一个区域。
    /// </summary>
    public void OnClickPreviousArea()
    {
        if (_areas.Count == 0)
        {
            return;
        }

        SelectArea(Mathf.Max(0, _currentIndex - 1), true);
    }

    /// <summary>
    /// 点击下一个区域。
    /// </summary>
    public void OnClickNextArea()
    {
        if (_areas.Count == 0)
        {
            return;
        }

        SelectArea(Mathf.Min(_areas.Count - 1, _currentIndex + 1), true);
    }

    /// <summary>
    /// 点击进入：进入当前区域的关卡按钮面板。
    /// </summary>
    public void OnClickEnterArea()
    {
        if (_areas.Count == 0 || _currentIndex < 0 || _currentIndex >= _areas.Count)
        {
            return;
        }

        var area = _areas[_currentIndex];
        if (area == null || !area.IsUnlocked)
        {
            return;
        }

        var stagePanel = LevelStageSelectPanel.EnsureInstance();
        if (stagePanel == null)
        {
            return;
        }

        stagePanel.SetContext(_currentMode, area);
        UIPanelRegistry.Push(PanelNames.LevelStageSelectPanel);
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时订阅按钮并刷新展示。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        ReloadAreas();
    }

    /// <summary>
    /// 禁用时取消订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    /// <summary>
    /// 非拖动状态下：先等待惯性衰减，再吸附到最近页并平滑贴合目标位置。
    /// </summary>
    private void LateUpdate()
    {
        if (areaScrollRect == null || _areas.Count <= 1 || _isDragging)
        {
            return;
        }

        if (_pendingSnap)
        {
            if (Mathf.Abs(areaScrollRect.velocity.x) > snapVelocityThreshold)
            {
                return;
            }

            _pendingSnap = false;
            areaScrollRect.velocity = Vector2.zero;
            var nearest = FindNearestAreaIndexByCenter();
            SelectArea(nearest, false);
        }

        var target = _currentIndex / (float)(_areas.Count - 1);
        areaScrollRect.horizontalNormalizedPosition = Mathf.Lerp(
            areaScrollRect.horizontalNormalizedPosition,
            target,
            snapLerpSpeed * Time.unscaledDeltaTime);

        if (Mathf.Abs(areaScrollRect.horizontalNormalizedPosition - target) < 0.0005f)
        {
            areaScrollRect.horizontalNormalizedPosition = target;
        }
    }

    #endregion

    #region Drag Handlers

    /// <summary>
    /// 开始拖动时记录状态。
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _pendingSnap = false;
    }

    /// <summary>
    /// 结束拖动后根据滚动位置吸附到最近页。
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        if (_areas.Count <= 1 || areaScrollRect == null)
        {
            return;
        }

        // 保留惯性，等速度降到阈值后再吸附最近页。
        _pendingSnap = true;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 加载数据库并重建当前模式的区域页面。
    /// </summary>
    private void ReloadAreas()
    {
        LoadDatabase();
        BuildAreaListFromDatabase();
        RebuildAreaPages();
        SelectArea(Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, _areas.Count - 1)), false);
    }

    /// <summary>
    /// 从 Resources 读取区域数据库（只读一次后缓存）。
    /// </summary>
    private void LoadDatabase()
    {
        if (_database != null)
        {
            return;
        }

        _database = GameResourceLoader.LoadLevelAreaDatabase(logOnMissing: false);
        if (_database == null)
        {
            Debug.LogWarning($"LevelAreaSelectPanel: 未找到区域数据库 Resources/{GameResourcePaths.LevelAreaDatabase}。");
        }
    }

    /// <summary>
    /// 将当前模式对应的区域配置拷贝到运行时列表。
    /// </summary>
    private void BuildAreaListFromDatabase()
    {
        _areas.Clear();
        if (_database == null)
        {
            return;
        }

        var list = _database.GetAreas(_currentMode);
        if (list == null)
        {
            return;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var cfg = list[i];
            if (cfg != null)
            {
                _areas.Add(cfg);
            }
        }
    }

    /// <summary>
    /// 清空并重建区域横向页面。
    /// </summary>
    private void RebuildAreaPages()
    {
        if (areaPagesRoot == null)
        {
            return;
        }

        for (var i = areaPagesRoot.childCount - 1; i >= 0; i--)
        {
            var child = areaPagesRoot.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        for (var i = 0; i < _areas.Count; i++)
        {
            CreateAreaPage(areaPagesRoot, _areas[i], i);
        }

        EnsureEdgeItemsCanCenter();
        LayoutRebuilder.ForceRebuildLayoutImmediate(areaPagesRoot);
        if (areaScrollRect != null)
        {
            areaScrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 创建一个区域页：优先实例化 <see cref="areaPageItemPrefab"/> 并 Bind；否则生成占位按钮页。
    /// </summary>
    private void CreateAreaPage(Transform parent, LevelAreaConfigSO area, int index)
    {
        if (areaPageItemPrefab != null)
        {
            var instance = Instantiate(areaPageItemPrefab.gameObject, parent, false);
            instance.name = $"AreaPage_{index}";
            var view = instance.GetComponent<LevelAreaInfoCardView>()
                       ?? instance.GetComponentInChildren<LevelAreaInfoCardView>(true);
            view?.Bind(_currentMode, area, index, _areas.Count);
            var button = instance.GetComponent<Button>()
                      ?? instance.GetComponentInChildren<Button>(true);
            if (button != null)
            {
                var captured = index;
                button.onClick.AddListener(() => SelectArea(captured, true));
            }

            return;
        }

        var go = new GameObject($"AreaPage_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(760f, 300f);

        var img = go.GetComponent<Image>();
        img.color = area != null && area.IsUnlocked
            ? new Color(0.2f, 0.32f, 0.52f, 1f)
            : new Color(0.25f, 0.25f, 0.25f, 1f);

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => SelectArea(index, true));

        var label = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(go.transform, false);
        var lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(640f, 96f);
        var tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = area == null || string.IsNullOrWhiteSpace(area.DisplayName)
            ? $"区域 {index + 1}"
            : area.DisplayName;
        tmp.fontSize = 40f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }
    }

    /// <summary>
    /// 切换当前区域并刷新交互状态（各页数据已在创建时 Bind）。
    /// </summary>
    private void SelectArea(int index, bool snapScroll)
    {
        if (_areas.Count == 0)
        {
            _currentIndex = 0;
            if (enterAreaBtn != null) enterAreaBtn.interactable = false;
            if (previousAreaBtn != null) previousAreaBtn.interactable = false;
            if (nextAreaBtn != null) nextAreaBtn.interactable = false;
            return;
        }

        _currentIndex = Mathf.Clamp(index, 0, _areas.Count - 1);
        var area = _areas[_currentIndex];

        if (enterAreaBtn != null)
        {
            enterAreaBtn.interactable = area != null && area.IsUnlocked;
        }

        if (previousAreaBtn != null)
        {
            previousAreaBtn.interactable = _currentIndex > 0;
        }

        if (nextAreaBtn != null)
        {
            nextAreaBtn.interactable = _currentIndex < _areas.Count - 1;
        }

        if (snapScroll && areaScrollRect != null && _areas.Count > 1)
        {
            _pendingSnap = false;
            areaScrollRect.velocity = Vector2.zero;
            areaScrollRect.horizontalNormalizedPosition = _currentIndex / (float)(_areas.Count - 1);
        }
    }

    /// <summary>
    /// 计算“当前离 viewport 中心最近”的区域索引，用于拖拽结束后的吸附目标。
    /// </summary>
    private int FindNearestAreaIndexByCenter()
    {
        if (areaPagesRoot == null || areaPagesRoot.childCount == 0 || areaScrollRect == null)
        {
            return _currentIndex;
        }

        var viewport = areaScrollRect.viewport != null
            ? areaScrollRect.viewport
            : areaScrollRect.GetComponent<RectTransform>();
        if (viewport == null)
        {
            return _currentIndex;
        }

        var nearestIndex = _currentIndex;
        var minDistance = float.MaxValue;
        var viewportCenterX = viewport.rect.center.x;
        for (var i = 0; i < areaPagesRoot.childCount; i++)
        {
            var child = areaPagesRoot.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            var worldCenter = child.TransformPoint(child.rect.center);
            var localCenter = viewport.InverseTransformPoint(worldCenter);
            var dist = Mathf.Abs(localCenter.x - viewportCenterX);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestIndex = i;
            }
        }

        return Mathf.Clamp(nearestIndex, 0, _areas.Count - 1);
    }

    /// <summary>
    /// 自动为横向布局两端补齐 padding，使首尾区域也能吸附到屏幕中心。
    /// </summary>
    private void EnsureEdgeItemsCanCenter()
    {
        if (areaScrollRect == null || areaPagesRoot == null || areaPagesRoot.childCount == 0)
        {
            return;
        }

        var viewport = areaScrollRect.viewport != null
            ? areaScrollRect.viewport
            : areaScrollRect.GetComponent<RectTransform>();
        var hlg = areaPagesRoot.GetComponent<HorizontalLayoutGroup>();
        var first = areaPagesRoot.GetChild(0) as RectTransform;
        if (viewport == null || hlg == null || first == null)
        {
            return;
        }

        var itemWidth = LayoutUtility.GetPreferredWidth(first);
        if (itemWidth <= 0f)
        {
            itemWidth = first.rect.width;
        }

        var targetPadding = Mathf.Max(0, Mathf.RoundToInt((viewport.rect.width - itemWidth) * 0.5f));
        if (hlg.padding.left == targetPadding && hlg.padding.right == targetPadding)
        {
            return;
        }

        hlg.padding.left = targetPadding;
        hlg.padding.right = targetPadding;
    }

    private void SubscribeButtons()
    {
        if (backBtn != null) backBtn.onClick.AddListener(OnClickBack);
        if (homeBtn != null) homeBtn.onClick.AddListener(OnClickHome);
        if (previousAreaBtn != null) previousAreaBtn.onClick.AddListener(OnClickPreviousArea);
        if (nextAreaBtn != null) nextAreaBtn.onClick.AddListener(OnClickNextArea);
        if (enterAreaBtn != null) enterAreaBtn.onClick.AddListener(OnClickEnterArea);
    }

    private void UnsubscribeButtons()
    {
        if (backBtn != null) backBtn.onClick.RemoveListener(OnClickBack);
        if (homeBtn != null) homeBtn.onClick.RemoveListener(OnClickHome);
        if (previousAreaBtn != null) previousAreaBtn.onClick.RemoveListener(OnClickPreviousArea);
        if (nextAreaBtn != null) nextAreaBtn.onClick.RemoveListener(OnClickNextArea);
        if (enterAreaBtn != null) enterAreaBtn.onClick.RemoveListener(OnClickEnterArea);
    }

    #endregion
}
