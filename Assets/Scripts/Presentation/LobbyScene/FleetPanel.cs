using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编队面板：多套队伍查看、切换与编辑入口。
/// 根据存储的编队信息生成卡片展示（Icon、数值），上下按钮切换编队。
/// </summary>
public sealed class FleetPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;
    [SerializeField] private Button _editBtn;
    [SerializeField] private Button _prevGroupBtn;
    [SerializeField] private Button _nextGroupBtn;

    [Header("面板 ID")]
    [SerializeField] private string _fleetPickPanelId = "FleetPick";

    [Header("编队信息")]
    [SerializeField] private TMP_Text _groupNameText;

    [Header("Card Display")]
    [SerializeField] private Transform _cardsRoot;
    [SerializeField] private GameObject _cardItemPrefab;

    [Header("新增控件（留空则按子物体名解析/动态创建）")]
    [Tooltip("GroupName 旁的改名按钮")]
    [SerializeField] private Button _renameBtn;
    [Tooltip("左下角的编队列表侧边栏开关")]
    [SerializeField] private Button _sidebarBtn;

    [Header("侧边栏预制体（留空则代码动态构建）")]
    [Tooltip("侧边栏容器预制体（含 Viewport / Rows / AddFleet_Button）")]
    [SerializeField] private GameObject _sidebarPrefab;
    [Tooltip("编队条目预制体（含 Name_Text）")]
    [SerializeField] private GameObject _fleetRowPrefab;

    /// <summary>编队序号清单（升序），用于循环翻页与侧边栏。</summary>
    private readonly List<int> _groupIndices = new List<int>();

    private RectTransform _sidebarRoot;
    private RectTransform _sidebarRowsRoot;
    private Button _sidebarAddBtn;
    private RectTransform _renameDialog;
    private TMP_InputField _renameInput;
    private string _currentGroupName = string.Empty;
    private bool _sidebarOpen;

    private int _currentGroupIndex;

    /// <summary>
    /// 刷新序号：同一帧内可能被 OnEnable 与 OnOpenAsync 各触发一次刷新（首次打开时两次都会真正挂起等待），
    /// 仅允许最新一次填充卡片，避免两次刷新交错导致卡片重复显示。
    /// </summary>
    private int _refreshToken;

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureExtraControls();
        Subscribe();
        RefreshFleetDisplay();
    }
    protected override void OnDisable()
    {
        // 面板关闭时作废进行中的刷新，避免异步回调写入已关闭的面板
        _refreshToken++;
        Unsubscribe();
        base.OnDisable();
    }

    /// <summary>
    /// 打开/返回本面板时刷新：从 FleetPickPanel 经 GoBackAsync 返回时不会再触发 OnEnable，
    /// 因此必须在此刷新编队内容（修复"编辑编队后返回内容不更新"）。
    /// </summary>
    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);
        EnsureExtraControls();
        RefreshFleetDisplay();
    }

    public void OnClickBack()  { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome()  { UIService?.GoHomeAsync().Forget(); }
    public async void OnClickEdit()
    {
        if (UIService != null)
            await UIService.OpenAsync<PanelBase>(_fleetPickPanelId, _currentGroupIndex);
    }
    /// <summary>上一编队（已是第一个时循环到最后一个）。</summary>
    public void OnClickPrevGroup()
    {
        if (_groupIndices.Count == 0) return;

        var pos = _groupIndices.IndexOf(_currentGroupIndex);
        if (pos < 0) pos = 0;
        pos = (pos - 1 + _groupIndices.Count) % _groupIndices.Count;
        _currentGroupIndex = _groupIndices[pos];

        RefreshFleetDisplay();
        RefreshSidebar();
    }

    /// <summary>下一编队（已是最后一个时循环回第一个；翻页不再新增编队）。</summary>
    public void OnClickNextGroup()
    {
        if (_groupIndices.Count == 0) return;

        var pos = _groupIndices.IndexOf(_currentGroupIndex);
        if (pos < 0) pos = 0;
        pos = (pos + 1) % _groupIndices.Count;
        _currentGroupIndex = _groupIndices[pos];

        RefreshFleetDisplay();
        RefreshSidebar();
    }

    /// <summary>点击"修改"按钮：打开编队改名对话框。</summary>
    public void OnClickRename() { OpenRenameDialog(); }

    /// <summary>点击侧边栏按钮：展开/收起编队列表。</summary>
    public void OnClickToggleSidebar() { SetSidebarOpen(!_sidebarOpen); }

    private async void RefreshFleetDisplay()
    {
        // 令牌：本方法可能被同帧触发多次；只有最后一次允许写入卡片列表
        var token = ++_refreshToken;

        var fleetService = ServiceContainer?.TryResolve<IFleetService>();
        if (fleetService == null) return;

        // 编队清单（序号升序）：用于循环翻页、侧边栏与名称显示
        var groups = await fleetService.LoadAllGroupsAsync();
        if (token != _refreshToken) return;

        _groupIndices.Clear();
        if (groups != null)
        {
            for (var i = 0; i < groups.Count; i++)
                _groupIndices.Add(groups[i].GroupIndex);
            _groupIndices.Sort();
        }

        if (_groupIndices.Count > 0 && !_groupIndices.Contains(_currentGroupIndex))
            _currentGroupIndex = _groupIndices[0];

        // 编队名称（优先取存档中的自定义名称）
        var groupName = $"编队 {_currentGroupIndex + 1}";
        if (groups != null)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].GroupIndex != _currentGroupIndex) continue;
                if (!string.IsNullOrWhiteSpace(groups[i].GroupName)) groupName = groups[i].GroupName;
                break;
            }
        }

        _currentGroupName = groupName;
        if (_groupNameText != null)
            _groupNameText.text = groupName;

        RefreshSidebar();

        // 清空旧卡片：先脱离父节点再 Destroy（Destroy 帧末才生效，先脱离可避免同帧残留被重复计数）
        for (int i = _cardsRoot.childCount - 1; i >= 0; i--)
        {
            var oldCard = _cardsRoot.GetChild(i);
            oldCard.SetParent(null, false);
            Destroy(oldCard.gameObject);
        }

        if (_cardItemPrefab == null) return;

        // 读取已收集卡牌
        var collectionService = ServiceContainer?.TryResolve<ICardCollectionService>();
        var configService = ServiceContainer?.TryResolve<IConfigService>();

        var instanceIds = await fleetService.LoadGroupAsync(_currentGroupIndex);
        if (token != _refreshToken) return;
        if (instanceIds == null || instanceIds.Count == 0) return;

        // 读取已收集卡牌
        IReadOnlyList<CardInstance> allCards = null;
        if (collectionService != null)
        {
            try { allCards = await collectionService.GetCardsAsync(); } catch { }
        }

        if (token != _refreshToken) return;
        if (allCards == null || allCards.Count == 0) return;

        // 为编队中的每张卡生成展示
        for (int i = 0; i < instanceIds.Count; i++)
        {
            // 匹配 InstanceId
            var cardInstance = allCards.FirstOrDefault(c => c.InstanceId == instanceIds[i]);
            if (cardInstance == null) continue;

            var config = configService?.Get<CardConfig>(cardInstance.CardId);
            if (config == null) continue;

            var go = Instantiate(_cardItemPrefab, _cardsRoot, false);
            var view = go.GetComponent<FleetCardItemView>();
            if (view != null)
                view.SetData(cardInstance, config, ResourceService);
        }
    }

    /// <summary>侧边栏宽度（收起时向左位移同值）。</summary>
    private const float SidebarWidth = 360f;

    /// <summary>侧边栏底部为"添加编队"按钮预留的高度。</summary>
    private const float SidebarBottomReserve = 76f;

    /// <summary>侧边栏行高。</summary>
    private const float SidebarRowHeight = 52f;

    /// <summary>
    /// 确保新增控件存在：GroupName 旁"修改"按钮、左下角编队列表按钮、侧边栏容器。
    /// 场景中已有同名子物体时复用（按子物体名解析），否则动态创建。
    /// </summary>
    private void EnsureExtraControls()
    {
        var panelRect = (RectTransform)transform;

        // GroupName 旁的"修改"按钮
        if (_renameBtn == null)
            _renameBtn = PanelWidgetUtil.FindChild<Button>(transform, "Rename_Button");
        if (_renameBtn == null)
            _renameBtn = PanelWidgetUtil.CreateButton(panelRect, "Rename_Button", "修改",
                new Vector2(300f, 330f), new Vector2(96f, 42f));
        _renameBtn.onClick.RemoveListener(OnClickRename);
        _renameBtn.onClick.AddListener(OnClickRename);

        // 左下角侧边栏开关
        if (_sidebarBtn == null)
            _sidebarBtn = PanelWidgetUtil.FindChild<Button>(transform, "Sidebar_Button");
        if (_sidebarBtn == null)
            _sidebarBtn = PanelWidgetUtil.CreateButton(panelRect, "Sidebar_Button", "编队列表",
                new Vector2(-720f, -330f), new Vector2(160f, 46f));
        _sidebarBtn.onClick.RemoveListener(OnClickToggleSidebar);
        _sidebarBtn.onClick.AddListener(OnClickToggleSidebar);

        if (_sidebarRoot == null)
            BuildSidebar(panelRect);
    }

    /// <summary>构建侧边栏：优先实例化预制体（_sidebarPrefab），未配置时按代码动态构建同构布局。</summary>
    private void BuildSidebar(RectTransform panelRect)
    {
        if (_sidebarPrefab != null)
        {
            var prefabInstance = Instantiate(_sidebarPrefab, panelRect, false);
            prefabInstance.name = "FleetSidebar";
            _sidebarRoot = (RectTransform)prefabInstance.transform;
        }
        else
        {
            BuildSidebarByCode(panelRect);
        }

        // 位置与锚点统一：左侧竖直铺满，收起时整体左移
        _sidebarRoot.anchorMin = new Vector2(0f, 0f);
        _sidebarRoot.anchorMax = new Vector2(0f, 1f);
        _sidebarRoot.pivot = new Vector2(0f, 0.5f);
        _sidebarRoot.sizeDelta = new Vector2(SidebarWidth, 0f);
        _sidebarRoot.anchoredPosition = new Vector2(-SidebarWidth, 0f);

        // 行容器与"添加编队"按钮：预制体按名解析，缺失时创建（两条路径都能工作）
        _sidebarRowsRoot = PanelWidgetUtil.FindChild<RectTransform>(_sidebarRoot, "Rows");
        if (_sidebarRowsRoot == null)
            _sidebarRowsRoot = CreateRowsContainer(_sidebarRoot);

        _sidebarAddBtn = PanelWidgetUtil.FindChild<Button>(_sidebarRoot, "AddFleet_Button");
        if (_sidebarAddBtn == null)
            _sidebarAddBtn = CreateAddFleetButton(_sidebarRoot);
        _sidebarAddBtn.onClick.RemoveListener(OnClickAddFleet);
        _sidebarAddBtn.onClick.AddListener(OnClickAddFleet);

        _sidebarRoot.gameObject.SetActive(false);
        _sidebarOpen = false;
    }

    /// <summary>代码构建侧边栏结构（无预制体时的兜底）：根 + Viewport + Rows + ScrollRect + 添加按钮。</summary>
    private void BuildSidebarByCode(RectTransform panelRect)
    {
        var sidebarObject = new GameObject("FleetSidebar", typeof(RectTransform), typeof(Image));
        _sidebarRoot = (RectTransform)sidebarObject.transform;
        _sidebarRoot.SetParent(panelRect, false);
        sidebarObject.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.11f, 0.96f);

        // 可视区（裁剪）
        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        var viewportRect = (RectTransform)viewportObject.transform;
        viewportRect.SetParent(_sidebarRoot, false);
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(0f, SidebarBottomReserve);
        viewportRect.offsetMax = Vector2.zero;

        // 行容器（ScrollRect content）
        _sidebarRowsRoot = CreateRowsContainer(_sidebarRoot);
        _sidebarRowsRoot.SetParent(viewportRect, false);

        var scroll = sidebarObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = _sidebarRowsRoot;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    /// <summary>创建行容器（纵向布局 + 内容自适应高度）。</summary>
    private static RectTransform CreateRowsContainer(RectTransform parent)
    {
        var rowsObject = new GameObject("Rows", typeof(RectTransform));
        var rows = (RectTransform)rowsObject.transform;
        rows.SetParent(parent, false);
        rows.anchorMin = new Vector2(0f, 1f);
        rows.anchorMax = new Vector2(1f, 1f);
        rows.pivot = new Vector2(0.5f, 1f);
        rows.anchoredPosition = Vector2.zero;
        rows.sizeDelta = new Vector2(0f, 0f);

        var rowsLayout = rowsObject.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 6f;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = false;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;

        var rowsFitter = rowsObject.AddComponent<ContentSizeFitter>();
        rowsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rows;
    }

    /// <summary>创建底部"添加编队"按钮。</summary>
    private static Button CreateAddFleetButton(RectTransform sidebarRoot)
    {
        var button = PanelWidgetUtil.CreateButton(sidebarRoot, "AddFleet_Button", "+ 添加编队",
            Vector2.zero, new Vector2(300f, 46f));
        var rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 18f);
        return button;
    }


    /// <summary>重建侧边栏编队列表（序号 + 名称，可滑动）。</summary>
    private void RefreshSidebar()
    {
        if (_sidebarRowsRoot == null) return;

        for (var i = _sidebarRowsRoot.childCount - 1; i >= 0; i--)
            Destroy(_sidebarRowsRoot.GetChild(i).gameObject);

        if (_groupIndices.Count == 0) return;

        for (var i = 0; i < _groupIndices.Count; i++)
        {
            var groupIndex = _groupIndices[i];
            var captured = groupIndex;

            // 优先使用行预制体（_fleetRowPrefab，含 Name_Text），缺失时动态创建按钮。
            if (_fleetRowPrefab != null)
            {
                var rowObject = Instantiate(_fleetRowPrefab, _sidebarRowsRoot, false);
                rowObject.name = $"Fleet_{groupIndex}";

                var label = rowObject.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"{i + 1}. 编队 {groupIndex + 1}";

                var rowButton = rowObject.GetComponent<Button>()
                                ?? rowObject.GetComponentInChildren<Button>(true);
                if (rowButton != null)
                {
                    rowButton.onClick.AddListener(() => OnClickSidebarEntry(captured));
                    continue;
                }

                Destroy(rowObject);
            }

            var button = PanelWidgetUtil.CreateButton(_sidebarRowsRoot, $"Fleet_{groupIndex}",
                $"{i + 1}. 编队 {groupIndex + 1}", Vector2.zero, new Vector2(0f, SidebarRowHeight - 6f));
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -i * SidebarRowHeight);
            rect.sizeDelta = new Vector2(0f, SidebarRowHeight - 6f);
            button.onClick.AddListener(() => OnClickSidebarEntry(captured));
        }

        // 立即重建布局，保证刚展开侧边栏时内容尺寸已正确（ContentSizeFitter 负责高度）
        LayoutRebuilder.ForceRebuildLayoutImmediate(_sidebarRowsRoot);
        RefreshSidebarNamesAsync().Forget();
    }

    /// <summary>异步填充侧边栏各行名称（存档中的自定义编队名）。</summary>
    private async UniTaskVoid RefreshSidebarNamesAsync()
    {
        var fleetService = ServiceContainer?.TryResolve<IFleetService>();
        if (fleetService == null || _sidebarRowsRoot == null) return;

        var groups = await fleetService.LoadAllGroupsAsync();
        if (groups == null || _sidebarRowsRoot == null) return;

        for (var i = 0; i < _groupIndices.Count && i < _sidebarRowsRoot.childCount; i++)
        {
            var child = _sidebarRowsRoot.GetChild(i);
            if (child == null) continue;

            var label = child.GetComponentInChildren<TMP_Text>();
            if (label == null) continue;

            var groupIndex = _groupIndices[i];
            var name = $"编队 {groupIndex + 1}";
            for (var g = 0; g < groups.Count; g++)
            {
                if (groups[g].GroupIndex != groupIndex) continue;
                if (!string.IsNullOrWhiteSpace(groups[g].GroupName)) name = groups[g].GroupName;
                break;
            }

            label.text = $"{i + 1}. {name}";
        }
    }

    /// <summary>点击侧边栏条目：切换到该编队并收起侧边栏。</summary>
    private void OnClickSidebarEntry(int groupIndex)
    {
        _currentGroupIndex = groupIndex;
        RefreshFleetDisplay();
        SetSidebarOpen(false);
    }

    /// <summary>点击"添加编队"：按当前最大序号 +1 新增并切换到新编队。</summary>
    private async void OnClickAddFleet()
    {
        var fleetService = ServiceContainer?.TryResolve<IFleetService>();
        if (fleetService == null) return;

        var newIndex = await fleetService.CreateGroupAsync();
        _currentGroupIndex = newIndex;
        RefreshFleetDisplay();
        RefreshSidebar();
    }

    /// <summary>展开/收起侧边栏（DOTween 从左侧滑出）。</summary>
    private void SetSidebarOpen(bool open)
    {
        if (_sidebarRoot == null) return;

        _sidebarOpen = open;
        if (open)
        {
            _sidebarRoot.gameObject.SetActive(true);
            _sidebarRoot.SetAsLastSibling();
            RefreshSidebar();
            _sidebarRoot.DOAnchorPosX(0f, 0.2f).SetEase(Ease.OutCubic);
        }
        else
        {
            _sidebarRoot.DOAnchorPosX(-SidebarWidth, 0.18f).SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    if (_sidebarRoot != null) _sidebarRoot.gameObject.SetActive(false);
                });
        }
    }

    /// <summary>打开改名对话框（首次使用时动态构建，含输入框与确定/取消）。</summary>
    private void OpenRenameDialog()
    {
        var panelRect = (RectTransform)transform;
        if (_renameDialog == null)
        {
            var dialogObject = new GameObject("RenameDialog", typeof(RectTransform), typeof(Image));
            _renameDialog = (RectTransform)dialogObject.transform;
            _renameDialog.SetParent(panelRect, false);
            _renameDialog.anchorMin = new Vector2(0.5f, 0.5f);
            _renameDialog.anchorMax = new Vector2(0.5f, 0.5f);
            _renameDialog.pivot = new Vector2(0.5f, 0.5f);
            _renameDialog.sizeDelta = new Vector2(460f, 190f);
            dialogObject.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.97f);

            PanelWidgetUtil.CreateLabel(_renameDialog, "Title", "修改编队名称", 18f,
                new Vector2(0f, 70f), new Vector2(400f, 30f));

            var inputObject = new GameObject("Input", typeof(RectTransform), typeof(Image));
            var inputRect = (RectTransform)inputObject.transform;
            inputRect.SetParent(_renameDialog, false);
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = new Vector2(0f, 20f);
            inputRect.sizeDelta = new Vector2(400f, 52f);
            inputObject.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.22f, 1f);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(inputRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);
            var inputText = textObject.GetComponent<TextMeshProUGUI>();
            inputText.fontSize = 18;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.Left;

            _renameInput = inputObject.AddComponent<TMP_InputField>();
            _renameInput.textComponent = inputText;
            _renameInput.textViewport = inputRect;
            _renameInput.lineType = TMP_InputField.LineType.SingleLine;

            var confirm = PanelWidgetUtil.CreateButton(_renameDialog, "RenameConfirm", "确定",
                new Vector2(-90f, -62f), new Vector2(150f, 44f));
            confirm.onClick.AddListener(OnRenameConfirmed);
            var cancel = PanelWidgetUtil.CreateButton(_renameDialog, "RenameCancel", "取消",
                new Vector2(90f, -62f), new Vector2(150f, 44f));
            cancel.onClick.AddListener(CloseRenameDialog);

            _renameDialog.gameObject.SetActive(false);
        }

        if (_renameInput != null)
            _renameInput.text = _currentGroupName;

        _renameDialog.gameObject.SetActive(true);
        _renameDialog.SetAsLastSibling();
    }

    /// <summary>确认改名：写入服务并刷新显示与侧边栏。</summary>
    private void OnRenameConfirmed()
    {
        var newName = _renameInput != null ? _renameInput.text : string.Empty;
        SaveGroupNameAsync(newName).Forget();
        CloseRenameDialog();
    }

    private async UniTaskVoid SaveGroupNameAsync(string groupName)
    {
        var fleetService = ServiceContainer?.TryResolve<IFleetService>();
        if (fleetService == null) return;

        await fleetService.SaveGroupNameAsync(_currentGroupIndex, groupName);
        RefreshFleetDisplay();
        RefreshSidebar();
    }

    private void CloseRenameDialog()
    {
        if (_renameDialog != null)
            _renameDialog.gameObject.SetActive(false);
    }

    private void Subscribe()
    {
        SafeSubscribe(_backBtn, OnClickBack);
        SafeSubscribe(_homeBtn, OnClickHome);
        SafeSubscribe(_editBtn, OnClickEdit);
        SafeSubscribe(_prevGroupBtn, OnClickPrevGroup);
        SafeSubscribe(_nextGroupBtn, OnClickNextGroup);
        SafeSubscribe(_renameBtn, OnClickRename);
        SafeSubscribe(_sidebarBtn, OnClickToggleSidebar);
    }

    private void Unsubscribe()
    {
        SafeUnsubscribe(_backBtn, OnClickBack);
        SafeUnsubscribe(_homeBtn, OnClickHome);
        SafeUnsubscribe(_editBtn, OnClickEdit);
        SafeUnsubscribe(_prevGroupBtn, OnClickPrevGroup);
        SafeUnsubscribe(_nextGroupBtn, OnClickNextGroup);
        SafeUnsubscribe(_renameBtn, OnClickRename);
        SafeUnsubscribe(_sidebarBtn, OnClickToggleSidebar);
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