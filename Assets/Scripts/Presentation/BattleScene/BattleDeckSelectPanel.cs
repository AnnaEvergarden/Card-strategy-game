using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗编队选择面板（按历史逻辑恢复）：向场景的 DeckRoot 生成全部编队条目（含成员摘要），
/// 点击条目即选定编队并进入首发选择；Back_Button 返回大厅；Tip_Text 显示提示。
/// </summary>
public sealed class BattleDeckSelectPanel : PanelBase
{
    [SerializeField] private RectTransform _deckRoot;
    [SerializeField] private Button _backButton;
    [SerializeField] private TMP_Text _tipText;

    [Tooltip("编队条目预制体（BattleDeckEntryView.prefab）；留空则回退为动态按钮")]
    [SerializeField] private GameObject _entryPrefab;

    [Header("面板 ID")]
    [SerializeField]
    private string _battleActivePickPanelId = "BattleActivePick";

    private readonly List<Button> _deckEntries = new List<Button>();
    private readonly List<BattleDeckEntryView> _deckEntryViews = new List<BattleDeckEntryView>();

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.SetAsLastSibling();
        ResolveWidgets();
        Subscribe();
        SetTip("请选择出战编队");
        ReloadDecksAsync().Forget();
    }

    protected override void OnDisable()
    {
        Unsubscribe();
        ClearEntries();
        base.OnDisable();
    }

    /// <summary>按子物体名解析场景控件（DeckRoot / Back_Button / Tip_Text）。</summary>
    private void ResolveWidgets()
    {
        if (_deckRoot == null)
            _deckRoot = PanelWidgetUtil.FindChild<RectTransform>(transform, "DeckRoot");
        if (_backButton == null)
            _backButton = PanelWidgetUtil.FindChild<Button>(transform, "Back_Button");
        if (_tipText == null)
            _tipText = PanelWidgetUtil.FindChild<TMP_Text>(transform, "Tip_Text");
    }

    private void Subscribe()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(OnClickBack);
    }

    private void Unsubscribe()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(OnClickBack);
    }

    /// <summary>返回大厅（对应历史面板的"返回游戏"按钮）。</summary>
    private async void OnClickBack()
    {
        var entry = ServiceContainer?.TryResolve<BattleEntryService>();
        if (entry != null)
        {
            await entry.ReturnToGameScene();
            return;
        }

        UIService?.GoBackAsync().Forget();
    }

    /// <summary>重建编队条目：每个编队一个按钮，显示编队名与成员列表。</summary>
    private async UniTaskVoid ReloadDecksAsync()
    {
        ClearEntries();

        var root = _deckRoot != null ? _deckRoot : PanelWidgetUtil.FindOrCreateRoot(transform, "DeckRoot");
        if (root == null)
            return;

        var fleetService = ServiceContainer?.TryResolve<IFleetService>();
        var battleService = ServiceContainer?.TryResolve<IBattleService>();
        if (fleetService == null)
        {
            SetTip("编队服务不可用");
            return;
        }

        var groups = await fleetService.LoadAllGroupsAsync();
        if (groups == null || groups.Count == 0)
        {
            SetTip("暂无编队，请先在大厅编辑编队");
            return;
        }

        for (var i = 0; i < groups.Count; i++)
        {
            var groupIndex = groups[i].GroupIndex;

            // 成员信息（经应用层预览查询，表现层不直接访问配置服务）
            var memberNames = new List<string>();
            var memberIconIds = new List<int>();
            if (battleService != null)
            {
                var preview = await battleService.GetFleetPreviewAsync(groupIndex);
                if (preview != null)
                {
                    for (var m = 0; m < preview.Count; m++)
                    {
                        memberNames.Add(preview[m].DisplayName);
                        memberIconIds.Add(preview[m].IconResourceId);
                    }
                }
            }

            // 优先使用原预制体条目（BattleDeckEntryView.prefab），缺失时回退动态按钮。
            if (_entryPrefab != null)
            {
                var entryObject = Instantiate(_entryPrefab, root, false);
                entryObject.name = $"Deck_{groupIndex}";
                var view = entryObject.GetComponent<BattleDeckEntryView>()
                           ?? entryObject.GetComponentInChildren<BattleDeckEntryView>(true);
                if (view != null)
                {
                    // Name_Text 仅显示编队名称；成员卡图由 ShipIcon 槽位异步加载。
                    view.BindName(groupIndex, groups[i].GroupName);
                    view.BindIconsAsync(memberIconIds, ResourceService).Forget();
                    view.Clicked += OnDeckEntryViewClicked;
                    _deckEntryViews.Add(view);
                    continue;
                }

                Destroy(entryObject);
            }

            var label = BuildDeckLabel(groups[i].GroupName, groupIndex, memberNames);
            var entry = PanelWidgetUtil.CreateButton(root, $"Deck_{groupIndex}", label,
                new Vector2(0f, 120f - i * 64f), new Vector2(560f, 56f));
            entry.onClick.AddListener(() => OnDeckEntryClicked(groupIndex));
            _deckEntries.Add(entry);
        }
    }

    /// <summary>编队条目文案：编队名 + 成员列表。</summary>
    private static string BuildDeckLabel(string groupName, int groupIndex, List<string> memberNames)
    {
        var name = string.IsNullOrEmpty(groupName) ? $"编队 {groupIndex + 1}" : groupName;
        return memberNames != null && memberNames.Count > 0
            ? $"{name}：{string.Join("、", memberNames)}"
            : $"{name}（空编队）";
    }

    /// <summary>预制体条目点击（携带编队序号），走统一的进入首发选择逻辑。</summary>
    private void OnDeckEntryViewClicked(int groupIndex) { OnDeckEntryClicked(groupIndex); }

    /// <summary>点击编队条目：写入上下文编队索引并进入首发选择。</summary>
    private async void OnDeckEntryClicked(int groupIndex)
    {
        var entry = ServiceContainer?.TryResolve<BattleEntryService>();
        if (entry?.CurrentContext == null)
        {
            SetTip("战斗上下文缺失");
            return;
        }

        entry.CurrentContext.FleetIndex = groupIndex;

        if (UIService != null && !string.IsNullOrEmpty(_battleActivePickPanelId))
            await UIService.OpenAsync<PanelBase>(_battleActivePickPanelId);

        // 关闭自身：避免本面板的全屏背景遮挡新面板的点击。
        if (UIService != null && !string.IsNullOrEmpty(PanelId))
            UIService.CloseAsync(PanelId).Forget();
    }

    /// <summary>清理编队条目（预制体视图 + 动态按钮）。</summary>
    private void ClearEntries()
    {
        for (var i = 0; i < _deckEntryViews.Count; i++)
        {
            var view = _deckEntryViews[i];
            if (view == null) continue;
            view.Clicked -= OnDeckEntryViewClicked;
            Destroy(view.gameObject);
        }
        _deckEntryViews.Clear();

        for (var i = 0; i < _deckEntries.Count; i++)
        {
            if (_deckEntries[i] != null)
                Destroy(_deckEntries[i].gameObject);
        }
        _deckEntries.Clear();
    }

    private void SetTip(string message)
    {
        if (_tipText != null)
            _tipText.text = message;
    }
}
