using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编队面板：展示玩家多套卡组，支持上下切换、返回、主界面与修改编队入口。
/// </summary>
public sealed class FleetPanel : BasePanel
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
    /// 修改编队按钮（当前先留入口日志）。
    /// </summary>
    [SerializeField] private Button editFleetBtn;

    /// <summary>
    /// 上一套卡组按钮。
    /// </summary>
    [SerializeField] private Button previousGroupBtn;

    /// <summary>
    /// 下一套卡组按钮。
    /// </summary>
    [SerializeField] private Button nextGroupBtn;

    /// <summary>
    /// 当前卡组序号显示文本（例如：卡组 1/3）。
    /// </summary>
    [SerializeField] private TMP_Text groupIndexText;

    /// <summary>
    /// 当前卡组名称显示文本（例如：白鹰一队）。
    /// </summary>
    [SerializeField] private TMP_Text groupNameText;

    /// <summary>
    /// 卡牌条目容器（建议在 Inspector 挂 HorizontalLayoutGroup）。
    /// </summary>
    [SerializeField] private RectTransform cardsContentRoot;

    /// <summary>
    /// 卡牌条目预制体（会实例化到 cardsContentRoot 下并绑定 UI）。
    /// </summary>
    [SerializeField] private GameObject cardEntryPrefab;

    /// <summary>
    /// 已实例化条目缓存。
    /// </summary>
    private readonly List<GameObject> _spawnedEntries = new();

    /// <summary>
    /// 当前编队数据快照。
    /// </summary>
    private FleetStore.FleetData _fleetData;

    /// <summary>
    /// cardId 到配置映射。
    /// </summary>
    private readonly Dictionary<string, CardConfigSO> _configMap = new();

    /// <summary>
    /// 当前显示卡组下标。
    /// </summary>
    private int _currentGroupIndex;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中已放置的编队面板；若不存在则打调试警告并返回 null（不运行时创建默认物体）。
    /// </summary>
    public static FleetPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<FleetPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.FleetPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "FleetPanel: 场景中未找到 FleetPanel，请在场景或 UI 预制体中挂载该组件；已取消运行时自动搭建界面。");
        return null;
    }

    /// <summary>
    /// 进入编队面板时调用：默认显示第一套卡组。
    /// </summary>
    public void OpenDefault()
    {
        _currentGroupIndex = 0;
        ReloadAndRefresh();
    }

    /// <summary>
    /// 点击返回：弹栈回上一面板。
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
    /// 点击修改编队：当前仅输出入口日志（后续可接编辑流程）。
    /// </summary>
    public void OnClickEditFleet()
    {
        Debug.Log("[FleetPanel] 点击修改编队（TODO: 接入编辑流程）。");
    }

    /// <summary>
    /// 点击上一套卡组。
    /// </summary>
    public void OnClickPreviousGroup()
    {
        if (_fleetData == null || _fleetData.groups == null || _fleetData.groups.Count == 0)
        {
            return;
        }

        _currentGroupIndex = Mathf.Max(0, _currentGroupIndex - 1);
        RefreshGroupView();
    }

    /// <summary>
    /// 点击下一套卡组。
    /// </summary>
    public void OnClickNextGroup()
    {
        if (_fleetData == null || _fleetData.groups == null || _fleetData.groups.Count == 0)
        {
            return;
        }

        _currentGroupIndex = Mathf.Min(_fleetData.groups.Count - 1, _currentGroupIndex + 1);
        RefreshGroupView();
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时订阅按钮并刷新。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        ReloadAndRefresh();
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
    /// 重载配置与编队数据后刷新当前组。
    /// </summary>
    private void ReloadAndRefresh()
    {
        LoadConfigMap();
        _fleetData = FleetStore.Load();
        var count = _fleetData != null && _fleetData.groups != null ? _fleetData.groups.Count : 0;
        if (count <= 0)
        {
            _currentGroupIndex = 0;
        }
        else
        {
            _currentGroupIndex = Mathf.Clamp(_currentGroupIndex, 0, count - 1);
        }

        RefreshGroupView();
    }

    /// <summary>
    /// 刷新当前卡组显示（标题、按钮可用状态、卡牌条目）。
    /// </summary>
    private void RefreshGroupView()
    {
        ClearCardEntries();

        var groups = _fleetData != null ? _fleetData.groups : null;
        var total = groups != null ? groups.Count : 0;
        if (groupIndexText != null)
        {
            groupIndexText.text = total <= 0
                ? "卡组 0/0"
                : $"卡组 {_currentGroupIndex + 1}/{total}";
        }

        if (previousGroupBtn != null)
        {
            previousGroupBtn.interactable = _currentGroupIndex > 0;
        }

        if (nextGroupBtn != null)
        {
            nextGroupBtn.interactable = total > 0 && _currentGroupIndex < total - 1;
        }

        if (total <= 0 || _currentGroupIndex < 0 || _currentGroupIndex >= total)
        {
            if (groupNameText != null)
            {
                groupNameText.text = "暂无卡组";
            }

            CreateHintEntry("暂无可用卡组。");
            return;
        }

        var group = groups[_currentGroupIndex];
        if (groupNameText != null)
        {
            groupNameText.text = string.IsNullOrWhiteSpace(group.groupName)
                ? $"卡组 {_currentGroupIndex + 1}"
                : group.groupName;
        }

        if (group == null || group.cardIds == null || group.cardIds.Count == 0)
        {
            CreateHintEntry("该卡组暂无卡牌。");
            return;
        }

        if (cardEntryPrefab == null)
        {
            CreateHintEntry("未配置卡牌预制体。");
            Debug.LogWarning("FleetPanel: 未配置 cardEntryPrefab，无法生成卡牌条目。");
            return;
        }

        for (var i = 0; i < group.cardIds.Count; i++)
        {
            var id = group.cardIds[i];
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            CreateCardEntry(i, id.Trim());
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContentRoot);
    }

    /// <summary>
    /// 创建并绑定单个卡牌条目。
    /// </summary>
    private void CreateCardEntry(int slotIndex, string cardId)
    {
        if (cardsContentRoot == null || cardEntryPrefab == null)
        {
            return;
        }

        var entry = Object.Instantiate(cardEntryPrefab, cardsContentRoot, false);
        entry.name = $"FleetCard_{slotIndex + 1}";

        _configMap.TryGetValue(cardId, out var config);
        var view = entry.GetComponent<FleetCardSlotView>() ?? entry.GetComponentInChildren<FleetCardSlotView>(true);
        if (view != null)
        {
            view.Bind(slotIndex, cardId, config);
            _spawnedEntries.Add(entry);
            return;
        }

        // 兜底：预制体没挂 FleetCardSlotView 时，尝试直接找一个 TMP_Text 写标题。
        var tmp = entry.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            var display = config != null && !string.IsNullOrWhiteSpace(config.DisplayName)
                ? config.DisplayName
                : cardId;
            tmp.text = $"{slotIndex + 1:D2}. {display}";
        }

        _spawnedEntries.Add(entry);
    }

    /// <summary>
    /// 显示提示条目。
    /// </summary>
    private void CreateHintEntry(string text)
    {
        if (cardsContentRoot == null)
        {
            return;
        }

        var go = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(cardsContentRoot, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 80f;
        le.preferredHeight = 100f;
        _spawnedEntries.Add(go);
    }

    /// <summary>
    /// 清理已生成条目。
    /// </summary>
    private void ClearCardEntries()
    {
        for (var i = 0; i < _spawnedEntries.Count; i++)
        {
            var go = _spawnedEntries[i];
            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        _spawnedEntries.Clear();
    }

    /// <summary>
    /// 加载卡牌配置映射。
    /// </summary>
    private void LoadConfigMap()
    {
        _configMap.Clear();
        var db = GameResourceLoader.LoadCardConfigDatabase(logOnMissing: false);
        if (db == null)
        {
            Debug.LogWarning($"FleetPanel: 未找到卡牌配置库 Resources/{GameResourcePaths.CardConfigDatabase}。");
            return;
        }

        var cards = db.Cards;
        if (cards == null)
        {
            return;
        }

        for (var i = 0; i < cards.Count; i++)
        {
            var cfg = cards[i];
            if (cfg != null && !string.IsNullOrWhiteSpace(cfg.CardId))
            {
                _configMap[cfg.CardId] = cfg;
            }
        }
    }

    /// <summary>
    /// 订阅按钮。
    /// </summary>
    private void SubscribeButtons()
    {
        if (backBtn != null) backBtn.onClick.AddListener(OnClickBack);
        if (homeBtn != null) homeBtn.onClick.AddListener(OnClickHome);
        if (editFleetBtn != null) editFleetBtn.onClick.AddListener(OnClickEditFleet);
        if (previousGroupBtn != null) previousGroupBtn.onClick.AddListener(OnClickPreviousGroup);
        if (nextGroupBtn != null) nextGroupBtn.onClick.AddListener(OnClickNextGroup);
    }

    /// <summary>
    /// 取消订阅按钮。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (backBtn != null) backBtn.onClick.RemoveListener(OnClickBack);
        if (homeBtn != null) homeBtn.onClick.RemoveListener(OnClickHome);
        if (editFleetBtn != null) editFleetBtn.onClick.RemoveListener(OnClickEditFleet);
        if (previousGroupBtn != null) previousGroupBtn.onClick.RemoveListener(OnClickPreviousGroup);
        if (nextGroupBtn != null) nextGroupBtn.onClick.RemoveListener(OnClickNextGroup);
    }

    #endregion
}
