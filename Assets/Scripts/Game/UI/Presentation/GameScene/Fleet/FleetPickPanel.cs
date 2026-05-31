using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编队选舰面板：布局与船坞相同的纵向滚动网格；点击舰娘切换上阵（最多 6、不重复、有序号遮罩），确认写入当前卡组并返回编队界面。
/// </summary>
public sealed class FleetPickPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 与船坞共用的网格列表组件。
    /// </summary>
    [SerializeField] private ShipyardLazyVerticalList lazyList;

    /// <summary>
    /// 取消并关闭。
    /// </summary>
    [SerializeField] private Button backBtn;

    /// <summary>
    /// 确认保存。
    /// </summary>
    [SerializeField] private Button confirmBtn;

    /// <summary>
    /// 校验提示（可选；未绑定则在控制台输出）。
    /// </summary>
    [SerializeField] private TMP_Text tipText;

    /// <summary>
    /// 当前编辑的卡组下标。
    /// </summary>
    private int _editingGroupIndex;

    /// <summary>
    /// 上阵顺序（唯一 cardId，长度不超过 <see cref="FleetStore.MaxCardsPerFleet"/>）。
    /// </summary>
    private readonly List<string> _pickedOrder = new();

    /// <summary>
    /// 卡牌配置映射。
    /// </summary>
    private readonly Dictionary<string, CardConfigSO> _configMap = new();

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中的编队选舰面板。
    /// </summary>
    public static FleetPickPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<FleetPickPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.FleetPickPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "FleetPickPanel: 场景中未找到 FleetPickPanel，请在 GameScene UI 下挂载（可参考 ShipyardPanel 布局绑定 lazyList）。");
        return null;
    }

    /// <summary>
    /// 打开并加载指定卡组的当前上阵列表（须在面板已被 Push 显示后调用，以便按钮订阅已完成）。
    /// </summary>
    /// <param name="groupIndex">卡组下标，与 <see cref="FleetPanel"/> 当前组一致。</param>
    public void OpenForEditing(int groupIndex)
    {
        _editingGroupIndex = groupIndex;
        _pickedOrder.Clear();

        var fleet = FleetStore.Load();
        if (fleet.groups != null && groupIndex >= 0 && groupIndex < fleet.groups.Count)
        {
            var g = fleet.groups[groupIndex];
            if (g?.cardIds != null)
            {
                for (var i = 0; i < g.cardIds.Count; i++)
                {
                    var raw = g.cardIds[i];
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    var id = raw.Trim();
                    if (!_pickedOrder.Contains(id))
                    {
                        _pickedOrder.Add(id);
                    }
                }
            }
        }

        ClearTip();
        ReloadGridAndMarks();
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时订阅按钮。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
    }

    /// <summary>
    /// 禁用时取消订阅并解除列表点击回调，避免船坞误用编队逻辑。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        if (lazyList != null)
        {
            lazyList.SetFleetPickClickHandler(null);
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
        if (backBtn != null)
        {
            backBtn.onClick.AddListener(OnClickBack);
        }

        if (confirmBtn != null)
        {
            confirmBtn.onClick.AddListener(OnClickConfirm);
        }
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (backBtn != null)
        {
            backBtn.onClick.RemoveListener(OnClickBack);
        }

        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveListener(OnClickConfirm);
        }
    }

    /// <summary>
    /// 返回上一层，不保存。
    /// </summary>
    private void OnClickBack()
    {
        ClearTip();
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 校验至少一艘、写入编队并存档后关闭。
    /// </summary>
    private void OnClickConfirm()
    {
        if (_pickedOrder.Count < 1)
        {
            ShowTip("至少选择一艘舰娘");
            return;
        }

        ClearTip();

        var data = FleetStore.Load();
        data.groups ??= new List<FleetStore.FleetGroupData>();
        while (data.groups.Count <= _editingGroupIndex)
        {
            data.groups.Add(new FleetStore.FleetGroupData { groupName = $"卡组 {data.groups.Count + 1}" });
        }

        var g = data.groups[_editingGroupIndex];
        g.cardIds = new List<string>(_pickedOrder);
        FleetStore.Save(data);

        UIPanelRegistry.TryPop();
        FleetPanel.EnsureInstance()?.ReloadAfterFleetPickEdit();
    }

    /// <summary>
    /// 加载收藏与配置表，刷新网格与上阵遮罩。
    /// </summary>
    private void ReloadGridAndMarks()
    {
        _configMap.Clear();
        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        foreach (var pair in map)
        {
            _configMap[pair.Key] = pair.Value;
        }

        var coll = CardCollectionStore.Load();
        var entries = coll != null ? coll.cards : new List<CardCollectionStore.CardEntry>();

        if (lazyList != null)
        {
            lazyList.SetFleetPickClickHandler(OnFleetSlotClicked);
            lazyList.SetData(entries, _configMap);
        }

        RefreshAllSelectionMarks();
    }

    /// <summary>
    /// 点击槽位：已选中则移除，否则未满则追加（同一 cardId 不会重复）。
    /// </summary>
    private void OnFleetSlotClicked(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return;
        }

        cardId = cardId.Trim();
        var idx = _pickedOrder.IndexOf(cardId);
        if (idx >= 0)
        {
            _pickedOrder.RemoveAt(idx);
        }
        else if (_pickedOrder.Count >= FleetStore.MaxCardsPerFleet)
        {
            ShowTip($"最多选择 {FleetStore.MaxCardsPerFleet} 艘舰娘");
            return;
        }
        else
        {
            _pickedOrder.Add(cardId);
        }

        ClearTip();
        RefreshAllSelectionMarks();
    }

    /// <summary>
    /// 根据 <see cref="_pickedOrder"/> 更新每个可见槽位的序号遮罩。
    /// </summary>
    private void RefreshAllSelectionMarks()
    {
        var root = lazyList != null ? lazyList.GetContentRoot() : null;
        if (root == null)
        {
            return;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var view = child.GetComponent<ShipyardCardSlotView>();
            if (view == null)
            {
                continue;
            }

            var ord = GetPickOrderOneBased(view.BoundCardId);
            view.SetFleetPickSelectionOrder(ord);
        }
    }

    /// <summary>
    /// 查询 cardId 在当前上阵列表中的序号（1-based），未选中返回 0。
    /// </summary>
    private int GetPickOrderOneBased(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return 0;
        }

        var idx = _pickedOrder.IndexOf(cardId.Trim());
        return idx >= 0 ? idx + 1 : 0;
    }

    /// <summary>
    /// 显示提示文案。
    /// </summary>
    private void ShowTip(string message)
    {
        if (tipText != null)
        {
            tipText.text = message;
            tipText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[FleetPickPanel] {message}");
        }
    }

    /// <summary>
    /// 隐藏提示。
    /// </summary>
    private void ClearTip()
    {
        if (tipText != null)
        {
            tipText.text = string.Empty;
            tipText.gameObject.SetActive(false);
        }
    }

    #endregion
}
