using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 船坞卡牌网格列表：按玩家条目为每条实例化对应稀有度的 Resources 槽位预制体，Bind 后设置图标；
/// 根据预制体根 Rect 尺寸与 Content 可用宽度计算列数并自动换行；Content 宽度变化时重建网格。
/// </summary>
public sealed class ShipyardLazyVerticalList : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// Resources 路径格式（不含扩展名），必须含占位符 {0}，将替换为稀有度枚举名。
    /// </summary>
    [SerializeField] private string resourcesSlotPrefabPathFormat = "Prefabs/UI/Card/CardSlot_{0}";

    /// <summary>
    /// 列表所在滚动视图（用于复位纵向滚动位置等）。
    /// </summary>
    [SerializeField] private ScrollRect scrollRect;

    /// <summary>
    /// 槽位实例的父节点，通常为 ScrollRect 的 Content。
    /// </summary>
    [SerializeField] private RectTransform content;

    /// <summary>
    /// 当预制体根高度读不出有效值时，用作单元格高度的兜底（像素）。
    /// </summary>
    [SerializeField] private float rowHeightFallback = 148f;

    /// <summary>
    /// 网格列与列之间的水平间距（像素）。
    /// </summary>
    [SerializeField] private float horizontalSpacing = 10f;

    /// <summary>
    /// 网格行与行之间的垂直间距（像素）。
    /// </summary>
    [SerializeField] private float verticalSpacing = 10f;

    /// <summary>
    /// Content 区域左侧内边距（像素）。
    /// </summary>
    [SerializeField] private float paddingLeft = 16f;

    /// <summary>
    /// Content 区域右侧内边距（像素）。
    /// </summary>
    [SerializeField] private float paddingRight = 16f;

    /// <summary>
    /// Content 区域顶部内边距（像素）。
    /// </summary>
    [SerializeField] private float paddingTop = 16f;

    /// <summary>
    /// Content 区域底部内边距（像素）。
    /// </summary>
    [SerializeField] private float paddingBottom = 16f;

    /// <summary>
    /// 为 true 时在控制台输出网格列数、Resources 加载路径等调试信息。
    /// </summary>
    [SerializeField] private bool debugLog = false;

    /// <summary>
    /// 各稀有度槽位预制体模板（Resources.Load 结果缓存，避免重复加载）。
    /// </summary>
    private readonly Dictionary<CardRarity, GameObject> _prefabTemplateByRarity = new();

    /// <summary>
    /// 当前要展示的收藏条目列表（与船坞数据一致）。
    /// </summary>
    private List<CardCollectionStore.CardEntry> _entries = new();

    /// <summary>
    /// cardId 到卡牌配置的映射，用于稀有度与 Bind 展示。
    /// </summary>
    private readonly Dictionary<string, CardConfigSO> _configMap = new();

    /// <summary>
    /// 上次布局时记录的 Content 宽度；变化超过阈值时在 LateUpdate 中触发重建。
    /// </summary>
    private float _lastContentWidthForGrid = -1f;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 检测 Content 宽度变化（如窗口缩放），变化时重新计算列数并重建全部槽位。
    /// </summary>
    private void LateUpdate()
    {
        if (content == null || _entries == null || _entries.Count == 0)
        {
            return;
        }

        var w = content.rect.width;
        if (_lastContentWidthForGrid < 0f)
        {
            _lastContentWidthForGrid = w;
            return;
        }

        if (Mathf.Abs(w - _lastContentWidthForGrid) > 0.5f)
        {
            _lastContentWidthForGrid = w;
            RebuildGrid();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 运行时注入滚动视图与 Content 引用（代码创建 UI 时调用）。
    /// </summary>
    /// <param name="scroll">滚动矩形组件。</param>
    /// <param name="contentRoot">内容区 RectTransform。</param>
    public void SetRefs(ScrollRect scroll, RectTransform contentRoot)
    {
        scrollRect = scroll;
        content = contentRoot;
    }

    /// <summary>
    /// 运行时覆盖槽位预制体的 Resources 路径格式；会清空模板缓存与当前子节点，若有数据则立即重建网格。
    /// </summary>
    /// <param name="pathFormat">含 {0} 的格式串，{0} 替换为稀有度枚举名；或不含占位符时与枚举名直接拼接。</param>
    public void SetResourcesSlotPrefabPathFormat(string pathFormat)
    {
        if (!string.IsNullOrWhiteSpace(pathFormat))
        {
            resourcesSlotPrefabPathFormat = pathFormat.Trim();
        }

        _prefabTemplateByRarity.Clear();
        DestroyAllContentChildren();
        _lastContentWidthForGrid = content != null ? content.rect.width : -1f;
        if (_entries != null && _entries.Count > 0)
        {
            RebuildGrid();
        }
    }

    /// <summary>
    /// 设置列表数据与配置映射，清空旧实例后按网格规则重新实例化槽位并 Bind。
    /// </summary>
    /// <param name="entries">玩家拥有的卡牌条目列表。</param>
    /// <param name="configMap">cardId 到 <see cref="CardConfigSO"/> 的字典。</param>
    public void SetData(List<CardCollectionStore.CardEntry> entries, Dictionary<string, CardConfigSO> configMap)
    {
        _entries = entries ?? new List<CardCollectionStore.CardEntry>();
        _configMap.Clear();
        if (configMap != null)
        {
            foreach (var kv in configMap)
            {
                _configMap[kv.Key] = kv.Value;
            }
        }

        _lastContentWidthForGrid = content != null ? content.rect.width : -1f;
        RebuildGrid();

        if (debugLog)
        {
            Debug.Log($"[ShipyardList] SetData entries={_entries.Count} map={_configMap.Count}");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 销毁 Content 下所有子物体（重建网格前调用）。
    /// </summary>
    private void DestroyAllContentChildren()
    {
        if (content == null)
        {
            return;
        }

        for (var i = content.childCount - 1; i >= 0; i--)
        {
            var c = content.GetChild(i);
            if (c != null)
            {
                Destroy(c.gameObject);
            }
        }
    }

    /// <summary>
    /// 读取预制体根节点的设计宽高（优先 sizeDelta，无效时用 rect；宽/高过小则回退到默认值）。
    /// </summary>
    /// <param name="prefabRoot">预制体上的 RectTransform。</param>
    /// <param name="heightFallback">高度读不出时的兜底像素值。</param>
    /// <returns>用于排布的单元格宽高（像素）。</returns>
    private static Vector2 ReadPrefabRootSize(RectTransform prefabRoot, float heightFallback)
    {
        if (prefabRoot == null)
        {
            return new Vector2(120f, heightFallback);
        }

        var w = prefabRoot.sizeDelta.x;
        if (w < 0.5f)
        {
            w = prefabRoot.rect.width;
        }

        var h = prefabRoot.sizeDelta.y;
        if (h < 0.5f)
        {
            h = prefabRoot.rect.height;
        }

        if (w < 0.5f)
        {
            w = 120f;
        }

        if (h < 0.5f)
        {
            h = heightFallback;
        }

        return new Vector2(w, h);
    }

    /// <summary>
    /// 根据当前列表中出现的稀有度，取各对应预制体根尺寸的最大宽、最大高，作为统一网格步进基准。
    /// </summary>
    private Vector2 GetMaxCellSizeForCurrentEntries()
    {
        var maxW = 0f;
        var maxH = 0f;

        foreach (var entry in _entries)
        {
            if (entry == null)
            {
                continue;
            }

            var rarity = GetRarityForEntry(entry);
            var prefab = GetPrefabTemplateForRarity(rarity);
            if (prefab == null)
            {
                maxW = Mathf.Max(maxW, 120f);
                maxH = Mathf.Max(maxH, rowHeightFallback);
                continue;
            }

            var pr = prefab.GetComponent<RectTransform>();
            var sz = ReadPrefabRootSize(pr, rowHeightFallback);
            maxW = Mathf.Max(maxW, sz.x);
            maxH = Mathf.Max(maxH, sz.y);
        }

        if (maxW < 0.5f)
        {
            maxW = 120f;
        }

        if (maxH < 0.5f)
        {
            maxH = rowHeightFallback;
        }

        return new Vector2(maxW, maxH);
    }

    /// <summary>
    /// 清空子节点后按当前数据重新计算列数、Content 高度，逐条实例化槽位、布局并调用 <see cref="ShipyardCardSlotView.Bind"/>。
    /// </summary>
    private void RebuildGrid()
    {
        if (content == null)
        {
            return;
        }

        DestroyAllContentChildren();

        var count = _entries.Count;
        if (count == 0)
        {
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, paddingTop + paddingBottom);
            content.anchoredPosition = Vector2.zero;
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }

            return;
        }

        var cell = GetMaxCellSizeForCurrentEntries();
        var cellW = cell.x;
        var cellH = cell.y;
        var strideX = cellW + horizontalSpacing;
        var strideY = cellH + verticalSpacing;

        var innerW = Mathf.Max(50f, content.rect.width - paddingLeft - paddingRight);
        var columns = Mathf.Max(1, Mathf.FloorToInt((innerW + horizontalSpacing) / Mathf.Max(0.01f, strideX)));
        var rows = Mathf.CeilToInt(count / (float)columns);

        var contentH = paddingTop + paddingBottom;
        if (rows > 0)
        {
            contentH += rows * cellH + Mathf.Max(0, rows - 1) * verticalSpacing;
        }

        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentH);
        content.anchoredPosition = Vector2.zero;
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        for (var i = 0; i < count; i++)
        {
            var entry = _entries[i];
            if (entry == null)
            {
                continue;
            }

            var rarity = GetRarityForEntry(entry);
            var row = i / columns;
            var col = i % columns;

            var rt = InstantiateSlotForRarity(rarity);
            if (rt == null)
            {
                continue;
            }

            ApplyGridSlotAnchorsAndSize(rt, rarity);
            rt.anchoredPosition = new Vector2(
                paddingLeft + col * strideX,
                -paddingTop - row * strideY);

            _configMap.TryGetValue(entry.cardId, out var cfg);
            var view = rt.GetComponent<ShipyardCardSlotView>();
            if (view != null)
            {
                view.Bind(entry, cfg);
            }
        }

        if (debugLog)
        {
            Debug.Log($"[ShipyardList] Grid cell=({cellW:F0},{cellH:F0}) cols={columns} rows={rows} innerW={innerW:F0} count={count}");
        }
    }

    /// <summary>
    /// 将槽位设为左上角锚点，并按该稀有度预制体（或兜底尺寸）设置宽高。
    /// </summary>
    /// <param name="rt">实例上的 RectTransform。</param>
    /// <param name="rarity">用于选择尺寸模板的稀有度。</param>
    private void ApplyGridSlotAnchorsAndSize(RectTransform rt, CardRarity rarity)
    {
        if (rt == null)
        {
            return;
        }

        var prefab = GetPrefabTemplateForRarity(rarity);
        Vector2 ownSize;
        if (prefab != null)
        {
            var pr = prefab.GetComponent<RectTransform>();
            ownSize = ReadPrefabRootSize(pr, rowHeightFallback);
        }
        else
        {
            ownSize = new Vector2(120f, rowHeightFallback);
        }

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ownSize.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ownSize.y);
    }

    /// <summary>
    /// 按稀有度实例化 Resources 预制体到 Content 下；若无预制体则创建代码生成的兜底行。
    /// </summary>
    /// <param name="rarity">槽位稀有度。</param>
    /// <returns>实例根 RectTransform；content 为空时返回 null。</returns>
    private RectTransform InstantiateSlotForRarity(CardRarity rarity)
    {
        if (content == null)
        {
            return null;
        }

        var prefab = GetPrefabTemplateForRarity(rarity);
        if (prefab != null)
        {
            var go = Instantiate(prefab, content, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = go.AddComponent<RectTransform>();
            }

            if (go.GetComponent<ShipyardCardSlotView>() == null)
            {
                go.AddComponent<ShipyardCardSlotView>();
            }

            return rt;
        }

        return CreateRowProgrammatic();
    }

    /// <summary>
    /// 获取指定稀有度的槽位预制体模板（带缓存）；路径由 <see cref="resourcesSlotPrefabPathFormat"/> 解析。
    /// </summary>
    /// <param name="rarity">稀有度枚举值。</param>
    /// <returns>预制体资源；未配置路径或加载失败时返回 null。</returns>
    private GameObject GetPrefabTemplateForRarity(CardRarity rarity)
    {
        if (_prefabTemplateByRarity.TryGetValue(rarity, out var cached) && cached != null)
        {
            return cached;
        }

        if (string.IsNullOrWhiteSpace(resourcesSlotPrefabPathFormat))
        {
            return null;
        }

        var path = resourcesSlotPrefabPathFormat.Contains("{0}")
            ? string.Format(resourcesSlotPrefabPathFormat, rarity)
            : $"{resourcesSlotPrefabPathFormat}{rarity}";

        var go = GameResourceLoader.LoadPrefab(path, logOnMissing: false);
        if (go != null)
        {
            _prefabTemplateByRarity[rarity] = go;
            if (debugLog)
            {
                Debug.Log($"[ShipyardList] Resources.Load OK rarity={rarity} path={path}");
            }
        }
        else
        {
            Debug.LogWarning($"ShipyardLazyVerticalList: 未找到稀有度槽位预制体 Resources/{path}，将使用代码生成兜底行。");
        }

        return go;
    }

    /// <summary>
    /// 无对应 Resources 预制体时，创建带背景、图标与名称文本的简易槽位，并 <see cref="ShipyardCardSlotView.Wire"/>。
    /// </summary>
    private RectTransform CreateRowProgrammatic()
    {
        var go = new GameObject("CardRow", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(content, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(120f, rowHeightFallback);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.08f);

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.SetParent(rt, false);
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.sizeDelta = new Vector2(96f, 96f);
        iconRt.anchoredPosition = new Vector2(16f, 0f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;

        var textGo = new GameObject("Name", typeof(RectTransform));
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(120f, 8f);
        textRt.offsetMax = new Vector2(-16f, -8f);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        var rowView = go.AddComponent<ShipyardCardSlotView>();
        rowView.Wire(iconImg, tmp);
        return rt;
    }

    /// <summary>
    /// 从配置映射解析条目的稀有度；无配置或无效条目时视为 <see cref="CardRarity.Normal"/>。
    /// </summary>
    private CardRarity GetRarityForEntry(CardCollectionStore.CardEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.cardId))
        {
            return CardRarity.Normal;
        }

        return _configMap.TryGetValue(entry.cardId, out var cfg) && cfg != null
            ? cfg.Rarity
            : CardRarity.Normal;
    }

    #endregion
}
