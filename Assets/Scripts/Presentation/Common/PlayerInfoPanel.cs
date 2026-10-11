using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家信息面板：显示名称（可修改）、生日（可修改，年/月/日分输入框并校验）、等级。
/// 每行右侧"修改"按钮点击后进入该字段的编辑态；校验由 IPlayerProfileService（领域对象）完成。
/// 面板预制体由编辑器工具生成并注册到 PanelConfig（PanelId = PlayerInfoPanel），
/// 由 UIService 经 Addressables 加载；未注册时调用方可用 <see cref="Create"/> 动态创建兜底。
/// </summary>
public sealed class PlayerInfoPanel : PanelBase
{
    // ── 布局常量（预制体排版与运行时保底共用同一套数值，改这里即可） ──

    /// <summary>顶栏高度（返回按钮所在条带）。</summary>
    private const float TopBarHeight = 90f;

    /// <summary>区域距屏幕边缘的外边距。</summary>
    private const float AreaMargin = 24f;

    /// <summary>左右分区间距。</summary>
    private const float AreaGap = 16f;

    /// <summary>信息行标签 / 数值 / "修改"按钮的 X 坐标。</summary>
    private const float RowLabelX = -210f;
    private const float RowValueX = 0f;
    private const float RowEditX = 250f;

    /// <summary>信息行 Y 坐标（名称 / 生日 / 等级）。</summary>
    private const float RowNameY = 90f;
    private const float RowBirthdayY = 20f;
    private const float RowLevelY = -50f;

    /// <summary>面板标题位置与尺寸。</summary>
    private static readonly Vector2 TitlePosition = new Vector2(0f, 186f);
    private static readonly Vector2 TitleSize = new Vector2(460f, 44f);

    /// <summary>信息行各控件尺寸。</summary>
    private static readonly Vector2 RowLabelSize = new Vector2(120f, 32f);
    private static readonly Vector2 RowValueSize = new Vector2(330f, 32f);
    private static readonly Vector2 RowEditButtonSize = new Vector2(110f, 40f);

    /// <summary>编辑区位置与尺寸。</summary>
    private static readonly Vector2 EditAreaPosition = new Vector2(0f, -196f);
    private static readonly Vector2 EditAreaSize = new Vector2(720f, 230f);

    /// <summary>标签 / 标题 / 错误提示颜色。</summary>
    private static readonly Color LabelColor = new Color(0.8f, 0.85f, 0.95f);
    private static readonly Color TitleColor = new Color(1f, 0.95f, 0.7f);
    private static readonly Color ErrorColor = new Color(1f, 0.45f, 0.45f);

    private Action _onProfileChanged;

    [Header("控件引用（预制体已分配；留空时按子物体名解析兜底）")]
    [Tooltip("右半侧信息分区")]
    [SerializeField] private RectTransform _rightArea;
    [Tooltip("左上角返回按钮")]
    [SerializeField] private Button _backButton;
    [Tooltip("名称数值文本")]
    [SerializeField] private TMP_Text _nameValueText;
    [Tooltip("名称行「修改」按钮")]
    [SerializeField] private Button _nameEditButton;
    [Tooltip("生日数值文本")]
    [SerializeField] private TMP_Text _birthdayValueText;
    [Tooltip("生日行「修改」按钮")]
    [SerializeField] private Button _birthdayEditButton;
    [Tooltip("等级数值文本（只读）")]
    [SerializeField] private TMP_Text _levelValueText;
    [Tooltip("编辑区根节点（默认隐藏）")]
    [SerializeField] private RectTransform _editRoot;
    [Tooltip("编辑区标题文本")]
    [SerializeField] private TMP_Text _editTitleText;
    [Tooltip("名称输入框")]
    [SerializeField] private TMP_InputField _nameInput;
    [Tooltip("生日-年输入框")]
    [SerializeField] private TMP_InputField _yearInput;
    [Tooltip("生日-月输入框")]
    [SerializeField] private TMP_InputField _monthInput;
    [Tooltip("生日-日输入框")]
    [SerializeField] private TMP_InputField _dayInput;
    [Tooltip("编辑区确定按钮")]
    [SerializeField] private Button _confirmButton;
    [Tooltip("编辑区取消按钮")]
    [SerializeField] private Button _cancelButton;
    [Tooltip("编辑区错误提示文本")]
    [SerializeField] private TMP_Text _errorText;

    private bool _editingName;

    private bool _built;

    /// <summary>
    /// 创建面板实例（挂在给定父节点下；由调用方负责设置 UIService/ResourceService/ServiceContainer）。
    /// 仅搭建全屏容器，内部排版由 <see cref="BuildStructure"/> 在首次启用时补齐。
    /// </summary>
    /// <param name="parent">父节点（通常是场景 Canvas）</param>
    /// <param name="onProfileChanged">资料修改成功后的回调（刷新大厅挂件）</param>
    public static PlayerInfoPanel Create(RectTransform parent, Action onProfileChanged)
    {
        var panelObject = new GameObject("PlayerInfoPanel",
            typeof(RectTransform), typeof(Image), typeof(PlayerInfoPanel));
        var rect = (RectTransform)panelObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        var background = panelObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.09f, 0.78f);
        background.raycastTarget = true;

        var panel = panelObject.GetComponent<PlayerInfoPanel>();
        panel._onProfileChanged = onProfileChanged;
        return panel;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureStructure();
        RefreshAsync().Forget();
    }

    /// <summary>读取玩家资料并刷新显示。</summary>
    private async UniTaskVoid RefreshAsync()
    {
        var service = ServiceContainer?.TryResolve<IPlayerProfileService>();
        if (service == null)
        {
            SetError("玩家资料服务不可用");
            return;
        }

        var profile = await service.GetProfileAsync();
        if (profile == null || this == null)
            return;

        if (_nameValueText != null) _nameValueText.text = profile.PlayerName;
        if (_birthdayValueText != null) _birthdayValueText.text = profile.BirthdayText;
        if (_levelValueText != null) _levelValueText.text = $"Lv.{profile.Level}（经验 {profile.Experience}）";

        ExitEditMode();
        SetError(string.Empty);
    }

    /// <summary>点击"修改"：进入名称编辑态。</summary>
    private void OnClickEditName() { EnterEditMode(true); }

    /// <summary>点击"修改"：进入生日编辑态。</summary>
    private void OnClickEditBirthday() { EnterEditMode(false); }

    /// <summary>取消编辑。</summary>
    private void OnClickCancel() { ExitEditMode(); SetError(string.Empty); }

    /// <summary>确认修改：按当前编辑态调用对应服务方法。</summary>
    private async void OnClickConfirm()
    {
        var service = ServiceContainer?.TryResolve<IPlayerProfileService>();
        if (service == null)
        {
            SetError("玩家资料服务不可用");
            return;
        }

        ProfileUpdateResult result;

        if (_editingName)
        {
            result = await service.SetNameAsync(_nameInput != null ? _nameInput.text : string.Empty);
        }
        else
        {
            if (!TryParseBirthdayInputs(out var year, out var month, out var day, out var parseError))
            {
                SetError(parseError);
                return;
            }

            result = await service.SetBirthdayAsync(year, month, day);
        }

        if (this == null)
            return;

        if (!result.Success)
        {
            SetError(result.Error);
            return;
        }

        SetError(string.Empty);
        ExitEditMode();
        _onProfileChanged?.Invoke();
        RefreshAsync().Forget();
    }

    /// <summary>
    /// 点击左上角"返回"：已注册到 PanelConfig（经 UIService 打开）时走 UIService 关闭并出栈；
    /// 动态创建兜底路径下无 PanelId，仅隐藏自身。
    /// </summary>
    private void OnClickBack()
    {
        ExitEditMode();

        if (UIService != null && !string.IsNullOrEmpty(PanelId))
        {
            UIService.CloseAsync(PanelId).Forget();
            return;
        }

        gameObject.SetActive(false);
    }

    /// <summary>解析生日输入：三个框都必须为整数（ContentType=IntegerNumber 已限制字符，此处再校验空值）。</summary>
    private bool TryParseBirthdayInputs(out int year, out int month, out int day, out string error)
    {
        year = month = day = 0;
        error = string.Empty;

        if (!int.TryParse(_yearInput != null ? _yearInput.text : string.Empty, out year))
        {
            error = "请输入年份（数字）";
            return false;
        }

        if (!int.TryParse(_monthInput != null ? _monthInput.text : string.Empty, out month))
        {
            error = "请输入月份（数字）";
            return false;
        }

        if (!int.TryParse(_dayInput != null ? _dayInput.text : string.Empty, out day))
        {
            error = "请输入日期（数字）";
            return false;
        }

        return true;
    }

    /// <summary>进入编辑态（name=true 编辑名称；false 编辑生日）。</summary>
    private void EnterEditMode(bool name)
    {
        _editingName = name;

        if (_editRoot != null)
            _editRoot.gameObject.SetActive(true);

        if (_editTitleText != null)
            _editTitleText.text = name ? "修改名称" : "修改生日（年 / 月 / 日）";

        // 显示/隐藏对应输入框
        if (_nameInput != null) _nameInput.gameObject.SetActive(name);
        if (_yearInput != null) _yearInput.gameObject.SetActive(!name);
        if (_monthInput != null) _monthInput.gameObject.SetActive(!name);
        if (_dayInput != null) _dayInput.gameObject.SetActive(!name);

        SetError(string.Empty);
    }

    /// <summary>退出编辑态。</summary>
    private void ExitEditMode()
    {
        if (_editRoot != null)
            _editRoot.gameObject.SetActive(false);
    }

    /// <summary>设置错误提示。</summary>
    private void SetError(string message)
    {
        if (_errorText != null)
            _errorText.text = message;
    }

    /// <summary>
    /// 确保面板结构完整并完成事件接线。
    /// 控件引用优先取序列化字段（预制体已分配，无运行时查找）；字段为空时按子物体名解析，
    /// 节点缺失时由 <see cref="BuildStructure"/> 动态创建——构成三级兜底。
    /// </summary>
    private void EnsureStructure()
    {
        if (_built)
            return;
        _built = true;

        var root = (RectTransform)transform;

        // ① 结构：预制体优先；缺失节点由 BuildStructure 补齐（保底）
        BuildStructure(root);

        // ② 控件引用：序列化字段优先 → 子物体名解析（此时节点已保证存在）
        if (_rightArea == null)
            _rightArea = PanelWidgetUtil.FindChild<RectTransform>(root, "RightArea");
        if (_backButton == null)
            _backButton = PanelWidgetUtil.FindChild<Button>(root, "Back_Button");

        if (_nameValueText == null)
            _nameValueText = PanelWidgetUtil.FindChild<TMP_Text>(_rightArea, "Name_Value");
        if (_birthdayValueText == null)
            _birthdayValueText = PanelWidgetUtil.FindChild<TMP_Text>(_rightArea, "Birthday_Value");
        if (_levelValueText == null)
            _levelValueText = PanelWidgetUtil.FindChild<TMP_Text>(_rightArea, "Level_Value");
        if (_nameEditButton == null)
            _nameEditButton = PanelWidgetUtil.FindChild<Button>(_rightArea, "Name_Edit_Button");
        if (_birthdayEditButton == null)
            _birthdayEditButton = PanelWidgetUtil.FindChild<Button>(_rightArea, "Birthday_Edit_Button");

        // 编辑区（默认隐藏，点击「修改」后展开）
        if (_editRoot == null)
            _editRoot = PanelWidgetUtil.FindChild<RectTransform>(_rightArea, "EditArea");
        if (_editRoot != null)
        {
            if (_editTitleText == null)
                _editTitleText = PanelWidgetUtil.FindChild<TMP_Text>(_editRoot, "EditTitle");
            if (_nameInput == null)
                _nameInput = PanelWidgetUtil.FindChild<TMP_InputField>(_editRoot, "NameInput");
            if (_yearInput == null)
                _yearInput = PanelWidgetUtil.FindChild<TMP_InputField>(_editRoot, "YearInput");
            if (_monthInput == null)
                _monthInput = PanelWidgetUtil.FindChild<TMP_InputField>(_editRoot, "MonthInput");
            if (_dayInput == null)
                _dayInput = PanelWidgetUtil.FindChild<TMP_InputField>(_editRoot, "DayInput");
            if (_errorText == null)
                _errorText = PanelWidgetUtil.FindChild<TMP_Text>(_editRoot, "Error_Text");
            if (_confirmButton == null)
                _confirmButton = PanelWidgetUtil.FindChild<Button>(_editRoot, "Confirm_Button");
            if (_cancelButton == null)
                _cancelButton = PanelWidgetUtil.FindChild<Button>(_editRoot, "Cancel_Button");
        }

        // ③ 事件接线（先移除再添加，避免重复订阅）
        WireButton(_backButton, OnClickBack);
        WireButton(_nameEditButton, OnClickEditName);
        WireButton(_birthdayEditButton, OnClickEditBirthday);
        WireButton(_confirmButton, OnClickConfirm);
        WireButton(_cancelButton, OnClickCancel);

        if (_editRoot != null)
            _editRoot.gameObject.SetActive(false);
    }

    /// <summary>按钮事件接线（先移除同名监听，避免重复订阅）。</summary>
    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    /// <summary>
    /// 构建面板结构（仅补齐缺失节点；已存在的预制体节点保持不变，不覆盖美术排版）。
    /// 运行时保底与编辑器工具 PlayerInfoPanelSetup 共用本方法，保证两者排版一致。
    /// 布局：顶栏（左上角"返回"按钮）+ 左半侧预留区 + 右半侧信息区（标题、名称/生日/等级行、编辑区）。
    /// </summary>
    /// <param name="root">面板根节点（应已铺满父级）</param>
    public static void BuildStructure(RectTransform root)
    {
        if (root == null)
            return;

        // 顶栏（横向铺满、贴顶）+ 左上角"返回"按钮
        var topBar = FindOrCreateStretched(root, "TopBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -TopBarHeight), Vector2.zero);

        var backButton = PanelWidgetUtil.FindChild<Button>(topBar, "Back_Button");
        if (backButton == null)
        {
            backButton = PanelWidgetUtil.CreateButton(topBar, "Back_Button", "返回",
                Vector2.zero, new Vector2(120f, 50f));
            SetTopLeftAnchor((RectTransform)backButton.transform, new Vector2(AreaMargin, -AreaMargin));
        }

        // 左半侧：预留扩展区（暂不放内容；淡底色仅用于标示区域，可替换为正式美术或删除）
        var leftArea = FindOrCreateStretched(root, "LeftArea",
            new Vector2(0f, 0f), new Vector2(0.5f, 1f),
            new Vector2(AreaMargin, AreaMargin),
            new Vector2(-AreaGap * 0.5f, -(TopBarHeight + AreaMargin)));
        EnsureRegionTint(leftArea, new Color(1f, 1f, 1f, 0.04f));

        // 右半侧：玩家信息区
        var rightArea = FindOrCreateStretched(root, "RightArea",
            new Vector2(0.5f, 0f), new Vector2(1f, 1f),
            new Vector2(AreaGap * 0.5f, AreaMargin),
            new Vector2(-AreaMargin, -(TopBarHeight + AreaMargin)));
        EnsureRegionTint(rightArea, new Color(0f, 0f, 0f, 0.25f));

        var title = PanelWidgetUtil.FindChild<TMP_Text>(rightArea, "Title");
        if (title == null)
        {
            title = PanelWidgetUtil.CreateLabel(rightArea, "Title", "玩家信息", 28f, TitlePosition, TitleSize);
            title.color = TitleColor;
        }

        // 三行信息：名称 / 生日 / 等级
        EnsureInfoRow(rightArea, "Name", "名称", RowNameY, true);
        EnsureInfoRow(rightArea, "Birthday", "生日", RowBirthdayY, true);
        EnsureInfoRow(rightArea, "Level", "等级", RowLevelY, false);

        // 编辑区（默认隐藏）
        var editArea = PanelWidgetUtil.FindChild<RectTransform>(rightArea, "EditArea");
        if (editArea == null)
            editArea = CreateEditArea(rightArea);

        editArea.gameObject.SetActive(false);
    }

    /// <summary>创建编辑区（含标题、输入框、确定/取消与错误提示）。</summary>
    private static RectTransform CreateEditArea(RectTransform rightArea)
    {
        var editObject = new GameObject("EditArea", typeof(RectTransform), typeof(Image));
        var editArea = (RectTransform)editObject.transform;
        editArea.SetParent(rightArea, false);
        editArea.anchorMin = new Vector2(0.5f, 0.5f);
        editArea.anchorMax = new Vector2(0.5f, 0.5f);
        editArea.pivot = new Vector2(0.5f, 0.5f);
        editArea.anchoredPosition = EditAreaPosition;
        editArea.sizeDelta = EditAreaSize;
        editObject.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.1f, 0.9f);

        var editTitle = PanelWidgetUtil.CreateLabel(editArea, "EditTitle", "修改", 20f,
            new Vector2(0f, 84f), new Vector2(560f, 30f));
        editTitle.color = Color.white;

        // 名称输入（编辑名称时显示）
        CreateInputField(editArea, "NameInput", new Vector2(0f, 34f),
            new Vector2(480f, 50f), TMP_InputField.ContentType.Standard, PlayerProfile.MaxNameLength);

        // 生日三输入（编辑生日时显示）
        CreateInputField(editArea, "YearInput", new Vector2(-190f, 34f),
            new Vector2(150f, 50f), TMP_InputField.ContentType.IntegerNumber, 4);
        CreateInputField(editArea, "MonthInput", new Vector2(0f, 34f),
            new Vector2(100f, 50f), TMP_InputField.ContentType.IntegerNumber, 2);
        CreateInputField(editArea, "DayInput", new Vector2(190f, 34f),
            new Vector2(100f, 50f), TMP_InputField.ContentType.IntegerNumber, 2);

        // 确定 / 取消
        PanelWidgetUtil.CreateButton(editArea, "Confirm_Button", "确定",
            new Vector2(-110f, -58f), new Vector2(170f, 48f));
        PanelWidgetUtil.CreateButton(editArea, "Cancel_Button", "取消",
            new Vector2(110f, -58f), new Vector2(170f, 48f));

        // 错误提示
        var errorText = PanelWidgetUtil.CreateLabel(editArea, "Error_Text", string.Empty, 16f,
            new Vector2(0f, -102f), new Vector2(640f, 26f));
        errorText.color = ErrorColor;

        return editArea;
    }

    /// <summary>补齐一行信息（标签 / 数值 / 可选"修改"按钮）；事件接线由 EnsureStructure 统一完成。</summary>
    private static void EnsureInfoRow(RectTransform area, string name, string label, float y, bool editable)
    {
        if (area == null)
            return;

        var labelText = PanelWidgetUtil.FindChild<TMP_Text>(area, $"{name}_Label");
        if (labelText == null)
        {
            labelText = PanelWidgetUtil.CreateLabel(area, $"{name}_Label", label, 20f,
                new Vector2(RowLabelX, y), RowLabelSize);
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.color = LabelColor;
        }

        var valueText = PanelWidgetUtil.FindChild<TMP_Text>(area, $"{name}_Value");
        if (valueText == null)
        {
            valueText = PanelWidgetUtil.CreateLabel(area, $"{name}_Value", "-", 20f,
                new Vector2(RowValueX, y), RowValueSize);
            valueText.alignment = TextAlignmentOptions.Left;
        }

        if (editable && PanelWidgetUtil.FindChild<Button>(area, $"{name}_Edit_Button") == null)
            PanelWidgetUtil.CreateButton(area, $"{name}_Edit_Button", "修改",
                new Vector2(RowEditX, y), RowEditButtonSize);
    }

    /// <summary>
    /// 给分区补一层淡底色以标示区域边界（不拦截点击）。
    /// 仅在缺少 Image 时添加，因此不会覆盖预制体中已替换的正式美术。
    /// </summary>
    private static void EnsureRegionTint(RectTransform area, Color color)
    {
        if (area == null || area.GetComponent<Image>() != null)
            return;

        var image = area.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    /// <summary>解析或创建"按锚点拉伸"的子节点（预制体优先）。</summary>
    private static RectTransform FindOrCreateStretched(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var existing = parent.Find(name);
        if (existing != null)
            return (RectTransform)existing;

        var nodeObject = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)nodeObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    /// <summary>把节点锚定为父节点的左上角（用于左上角"返回"按钮）。</summary>
    private static void SetTopLeftAnchor(RectTransform rect, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
    }

    /// <summary>创建输入框（含背景与文本组件）。</summary>
    private static TMP_InputField CreateInputField(RectTransform parent, string name,
        Vector2 anchoredPosition, Vector2 size, TMP_InputField.ContentType contentType, int characterLimit)
    {
        var inputObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)inputObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        inputObject.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.22f, 1f);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);

        var inputText = textObject.GetComponent<TextMeshProUGUI>();
        inputText.fontSize = 18;
        inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.Left;

        var input = inputObject.AddComponent<TMP_InputField>();
        input.textComponent = inputText;
        input.textViewport = rect;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = contentType;
        if (characterLimit > 0)
            input.characterLimit = characterLimit;

        return input;
    }
}
