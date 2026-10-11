using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗场上槽位视图：单个上场单位的头像、名称、HP 条、防御与状态表现。
/// 支持 [SerializeField] 预制体绑定；字段为空时经 Create 动态构建全部子控件
/// （无预制体也能完整运行，后续可在编辑器中美术化替换）。
/// 表现层只读：不修改任何战斗数据。
/// </summary>
public sealed class BattleShipFieldSlotView : MonoBehaviour
{
    [SerializeField] private RectTransform _scaleRoot;
    [SerializeField] private Image _portraitImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _defenseText;
    [SerializeField] private Button _clickButton;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _dimOverlay;
    [SerializeField] private Image _highlightFrame;

    /// <summary>绑定的单位 UnitId（空槽为 null）。</summary>
    public string UnitId { get; private set; }

    /// <summary>槽位阵营。</summary>
    public BattleSide Side { get; private set; }

    /// <summary>设置槽位阵营（预制体实例化路径使用；绑定数据时也会自动设置）。</summary>
    public void SetSide(BattleSide side)
    {
        Side = side;
    }

    /// <summary>点击回调（由 BattleMainPanel 订阅）。</summary>
    public event Action<BattleShipFieldSlotView> Clicked;

    /// <summary>
    /// 动态构建槽位视图（含全部子控件），返回挂载好的组件实例。
    /// </summary>
    /// <param name="parent">父容器</param>
    /// <param name="side">阵营</param>
    /// <param name="slotIndex">槽位下标</param>
    public static BattleShipFieldSlotView Create(RectTransform parent, BattleSide side, int slotIndex)
    {
        var rootObject = new GameObject($"SlotView_{side}_{slotIndex}",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(BattleShipFieldSlotView));
        var rootRect = (RectTransform)rootObject.transform;
        rootRect.SetParent(parent, false);
        rootRect.sizeDelta = new Vector2(170f, 240f);

        var view = rootObject.GetComponent<BattleShipFieldSlotView>();
        view._scaleRoot = rootRect;
        view._clickButton = rootObject.GetComponent<Button>();
        view._canvasGroup = rootObject.AddComponent<CanvasGroup>();
        view.Side = side;

        view.BuildChildControls();
        return view;
    }

    /// <summary>
    /// 绑定单位数据（头像异步加载）。
    /// </summary>
    /// <param name="data">单位只读视图</param>
    /// <param name="resourceService">资源服务（图标加载）</param>
    public async UniTask BindAsync(BattleUnitViewData data, IResourceService resourceService)
    {
        if (data == null)
        {
            MarkEmpty();
            return;
        }

        UnitId = data.UnitId;
        Side = data.Side;
        gameObject.name = $"SlotView_{data.UnitId}";

        if (_nameText != null)
            _nameText.text = data.DisplayName;
        if (_hpText != null)
            _hpText.text = $"{data.Hp}/{data.MaxHp}";
        if (_defenseText != null)
            _defenseText.text = $"防 {data.Defense}";

        if (_portraitImage != null && resourceService != null && data.IconResourceId > 0)
        {
            var sprite = await resourceService.LoadAsync<Sprite>((ResourceId)data.IconResourceId);
            if (sprite != null && _portraitImage != null)
                _portraitImage.sprite = sprite;
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        PlayEnterFx();
    }

    /// <summary>
    /// 标记空槽（阵亡移除且无替补）。
    /// </summary>
    public void MarkEmpty()
    {
        UnitId = null;
        if (_nameText != null)
            _nameText.text = "—";
        if (_hpText != null)
            _hpText.text = string.Empty;
        if (_defenseText != null)
            _defenseText.text = string.Empty;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0.25f;
        SetDimmed(false);
        SetHighlight(false);
    }

    /// <summary>
    /// 更新 HP 显示（事件驱动，不重建绑定）。
    /// </summary>
    public void UpdateHp(int hp, int maxHp)
    {
        if (_hpText != null)
            _hpText.text = $"{hp}/{maxHp}";
    }

    /// <summary>
    /// 更新防御显示（Buff 生效/到期时）。
    /// </summary>
    public void UpdateDefense(int defense)
    {
        if (_defenseText != null)
            _defenseText.text = $"防 {defense}";
    }

    /// <summary>
    /// 设置该槽位是否允许打开操作菜单（对应历史 SetActionMenuAvailable：仅我方槽位可用）。
    /// </summary>
    public void SetActionMenuAvailable(bool available)
    {
        CanOpenActionMenu = available;
    }

    /// <summary>是否允许打开操作菜单。</summary>
    public bool CanOpenActionMenu { get; private set; }

    /// <summary>
    /// 打开操作菜单时的放大强调（对应历史 PlayActionMenuOpenScale）。
    /// </summary>
    public void PlayActionMenuOpenScale()
    {
        if (_scaleRoot == null)
            return;

        _scaleRoot.DOKill();
        _scaleRoot.DOScale(Vector3.one * 1.12f, 0.18f).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// 还原操作菜单放大（未行动就关闭菜单时调用，对应历史 ResetActionMenuScale）。
    /// </summary>
    public void ResetActionMenuScale()
    {
        if (_scaleRoot == null)
            return;

        _scaleRoot.DOKill();
        _scaleRoot.DOScale(Vector3.one, 0.15f);
    }

    /// <summary>
    /// AI/敌方施法者强调动画（对应历史 PlayActionEmphasis）。
    /// </summary>
    public void PlayActionEmphasis()
    {
        if (_scaleRoot == null)
            return;

        _scaleRoot.DOKill();
        _scaleRoot.localScale = Vector3.one;
        _scaleRoot.DOPunchScale(Vector3.one * 0.18f, 0.35f, 8, 1f);
    }

    /// <summary>
    /// 行动/阵亡变暗遮罩。
    /// </summary>
    public void SetDimmed(bool dimmed)
    {
        if (_dimOverlay != null)
            _dimOverlay.gameObject.SetActive(dimmed);
    }

    /// <summary>
    /// 选目标模式高亮边框。
    /// </summary>
    public void SetHighlight(bool on)
    {
        if (_highlightFrame != null)
            _highlightFrame.gameObject.SetActive(on);
    }

    /// <summary>
    /// 受击表现：抖动 + 红闪（HP 由 MainPanel 经事件刷新）。
    /// </summary>
    public void PlayHitFx(int damage)
    {
        if (_scaleRoot == null)
            return;

        _scaleRoot.DOShakeAnchorPos(0.25f, 12f, 24);

        if (_portraitImage != null)
        {
            var original = _portraitImage.color;
            _portraitImage.color = new Color(1f, 0.35f, 0.35f, 1f);
            _portraitImage.DOColor(original, 0.35f);
        }
    }

    /// <summary>
    /// 治疗表现：绿闪。
    /// </summary>
    public void PlayHealFx()
    {
        if (_portraitImage == null)
            return;

        var original = _portraitImage.color;
        _portraitImage.color = new Color(0.5f, 1f, 0.5f, 1f);
        _portraitImage.DOColor(original, 0.35f);
    }

    /// <summary>
    /// 入场/换上表现：缩放弹跳强调。
    /// </summary>
    public void PlayEnterFx()
    {
        if (_scaleRoot == null)
            return;

        _scaleRoot.localScale = Vector3.one * 0.7f;
        _scaleRoot.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    private void Awake()
    {
        ResolvePrefabChildren();

        if (_clickButton == null)
            _clickButton = GetComponent<Button>();
        if (_clickButton != null)
            _clickButton.onClick.AddListener(OnClick);
    }

    /// <summary>
    /// 预制体子物体按名解析（[SerializeField] 已赋值时跳过）：使 BattleShipFieldSlotView.prefab
    /// 可直接复用（其子物体为 Name_Text / HP_Text / HP / Button）；缺失的遮罩/高亮/缩放根自动补齐。
    /// </summary>
    private void ResolvePrefabChildren()
    {
        var root = transform;

        if (_scaleRoot == null)
            _scaleRoot = root as RectTransform;
        if (_nameText == null)
            _nameText = FindChildText(root, "Name_Text");
        if (_hpText == null)
            _hpText = FindChildText(root, "HP_Text");
        if (_defenseText == null)
            _defenseText = FindChildText(root, "Defense_Text");

        if (_clickButton == null)
        {
            var buttonNode = root.Find("Button");
            if (buttonNode != null)
                _clickButton = buttonNode.GetComponent<Button>();
        }

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_dimOverlay == null)
            _dimOverlay = CreateRuntimeOverlay(root, "DimOverlay", new Color(0f, 0f, 0f, 0.45f), Vector2.zero);
        if (_highlightFrame == null)
            _highlightFrame = CreateRuntimeOverlay(root, "HighlightFrame", new Color(1f, 0.85f, 0.2f, 0.9f), new Vector2(-4f, 4f));
    }

    /// <summary>按名查找文本子物体（返回 TextMeshProUGUI，与字段类型一致）。</summary>
    private static TextMeshProUGUI FindChildText(Transform root, string childName)
    {
        var child = root.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    /// <summary>动态创建铺满槽位的遮罩/高亮图（预制体未提供时保持表现一致）。</summary>
    private static Image CreateRuntimeOverlay(Transform root, string name, Color color, Vector2 inflate)
    {
        var overlayObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)overlayObject.transform;
        rect.SetParent(root, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = -inflate;
        rect.offsetMax = inflate;

        var image = overlayObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        overlayObject.SetActive(false);
        return image;
    }

    private void OnDestroy()
    {
        if (_clickButton != null)
            _clickButton.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        if (!string.IsNullOrEmpty(UnitId))
            Clicked?.Invoke(this);
    }

    /// <summary>动态构建全部子控件（头像/名称/HP 条/防御/遮罩/高亮框）。</summary>
    private void BuildChildControls()
    {
        var root = (RectTransform)transform;

        // 槽位底板。
        var frame = root.GetComponent<Image>();
        frame.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

        // 头像（上方大图）。
        _portraitImage = CreateChildImage(root, "Portrait", new Vector2(0f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(0f, 0f), new Vector2(154f, 140f));
        _portraitImage.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        // 名称。
        _nameText = CreateChildText(root, "Name", new Vector2(0f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -152f), new Vector2(160f, 26f), 16, FontStyles.Bold, TextAlignmentOptions.Center);
        _nameText.text = "—";

        // HP 数值（本视图不需要 HP 条填充，仅显示数值文本）。
        _hpText = CreateChildText(root, "HpText", new Vector2(0f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 26f), new Vector2(160f, 22f), 13, FontStyles.Normal, TextAlignmentOptions.Center);

        // 防御。
        _defenseText = CreateChildText(root, "DefenseText", new Vector2(0f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 8f), new Vector2(160f, 20f), 12, FontStyles.Normal, TextAlignmentOptions.Center);

        // 行动变暗遮罩（覆盖整槽）。
        _dimOverlay = CreateChildImage(root, "DimOverlay", Vector2.zero, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Vector2.zero);
        _dimOverlay.rectTransform.anchorMin = Vector2.zero;
        _dimOverlay.rectTransform.anchorMax = Vector2.one;
        _dimOverlay.rectTransform.offsetMin = Vector2.zero;
        _dimOverlay.rectTransform.offsetMax = Vector2.zero;
        _dimOverlay.color = new Color(0f, 0f, 0f, 0.45f);
        _dimOverlay.raycastTarget = false;
        _dimOverlay.gameObject.SetActive(false);

        // 选目标高亮框。
        _highlightFrame = CreateChildImage(root, "HighlightFrame", Vector2.zero, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Vector2.zero);
        _highlightFrame.rectTransform.anchorMin = Vector2.zero;
        _highlightFrame.rectTransform.anchorMax = Vector2.one;
        _highlightFrame.rectTransform.offsetMin = new Vector2(-4f, -4f);
        _highlightFrame.rectTransform.offsetMax = new Vector2(4f, 4f);
        _highlightFrame.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        _highlightFrame.raycastTarget = false;
        _highlightFrame.gameObject.SetActive(false);
    }

    /// <summary>动态创建子 Image。</summary>
    private static Image CreateChildImage(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 pivot, Vector2 sizeDelta)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)imageObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return imageObject.GetComponent<Image>();
    }

    /// <summary>动态创建子文本。</summary>
    private static TextMeshProUGUI CreateChildText(RectTransform parent, string name, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize,
        FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)textObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
