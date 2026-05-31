using TMPro;
using UnityEngine;

/// <summary>
/// 技能悬停说明视图：挂在「提示框预制体」根节点上；由 <see cref="SkillHoverTooltipPresenter"/> Instantiate 生成。
/// <list type="bullet">
/// <item><description><b>tooltipRoot</b>：提示框面板 Rect（控制显隐与位移）；未指定时用本物体 <see cref="RectTransform"/>。</description></item>
/// <item><description><b>rootCanvasOverride</b>：一般留空，由父链推断 Canvas；多 Canvas 或异常层级时由 Presenter 注入。</description></item>
/// </list>
/// </summary>
public sealed class SkillHoverTooltipView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 若为 null，则使用本物体所在 <see cref="Canvas.rootCanvas"/> 做屏幕坐标换算。
    /// </summary>
    [SerializeField] private Canvas rootCanvasOverride;

    /// <summary>
    /// 提示框根 Rect（默认可为空，将在 <see cref="Awake"/> 填为本物体）。
    /// </summary>
    [SerializeField] private RectTransform tooltipRoot;

    /// <summary>
    /// 标题（技能名）。
    /// </summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>
    /// 正文（描述、冷却等）。
    /// </summary>
    [SerializeField] private TMP_Text bodyText;

    /// <summary>
    /// 相对光标的屏幕像素偏移。
    /// </summary>
    [SerializeField] private Vector2 screenOffset = new(18f, -18f);

    /// <summary>
    /// 当前是否应跟随鼠标更新位置。
    /// </summary>
    private bool _followMouse;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 默认把提示根设为当前物体 Rect。
    /// </summary>
    private void Awake()
    {
        if (tooltipRoot == null)
        {
            tooltipRoot = transform as RectTransform;
        }
    }

    /// <summary>
    /// 每帧末尾把 Tooltip 贴着光标（悬停期间）。
    /// </summary>
    private void LateUpdate()
    {
        if (!_followMouse || tooltipRoot == null || !tooltipRoot.gameObject.activeSelf)
        {
            return;
        }

        PlaceAtMouse();
    }

    /// <summary>
    /// 禁用时隐藏，避免挡住其它界面。
    /// </summary>
    private void OnDisable()
    {
        Hide();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 由 Presenter 在 Instantiate 后注入坐标用画布（可选）。
    /// </summary>
    /// <param name="canvas">优先用于坐标换算的 Canvas；null 则不覆盖序列化字段。</param>
    public void ApplyPresenterCanvas(Canvas canvas)
    {
        if (canvas != null)
        {
            rootCanvasOverride = canvas;
        }
    }

    /// <summary>
    /// 显示技能说明并跟随鼠标。
    /// </summary>
    /// <param name="skill">技能配置；为 null 则等同 <see cref="Hide"/>。</param>
    public void Show(SkillConfigSO skill)
    {
        if (tooltipRoot == null)
        {
            tooltipRoot = transform as RectTransform;
        }

        if (tooltipRoot == null)
        {
            return;
        }

        if (skill == null)
        {
            Hide();
            return;
        }

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(skill.DisplayName) ? skill.SkillId : skill.DisplayName;
        }

        if (bodyText != null)
        {
            var cd = skill.CooldownTurns > 0 ? $"冷却：{skill.CooldownTurns} 回合\n" : string.Empty;
            var uses = $"{skill.FormatUseLimitLine()}\n";
            var refreshCd = skill.RefreshCooldownChancePercent > 0
                ? $"刷新冷却概率：{skill.RefreshCooldownChancePercent}%\n"
                : string.Empty;
            var fac = $"阵营：{FormatFaction(skill.ShipFaction)}\n";
            bodyText.text = $"{fac}{uses}{refreshCd}{cd}\n{skill.Description}";
        }

        tooltipRoot.gameObject.SetActive(true);
        tooltipRoot.SetAsLastSibling();
        _followMouse = true;
        PlaceAtMouse();
    }

    /// <summary>
    /// 隐藏 Tooltip。
    /// </summary>
    public void Hide()
    {
        _followMouse = false;
        if (tooltipRoot != null)
        {
            tooltipRoot.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 将 Tooltip 置于当前鼠标屏幕坐标附近（Overlay / Camera 模式均适用）。
    /// </summary>
    private void PlaceAtMouse()
    {
        var canvas = rootCanvasOverride != null ? rootCanvasOverride.rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null || tooltipRoot == null)
        {
            return;
        }

        var canvasRt = canvas.transform as RectTransform;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screenPoint = (Vector2)Input.mousePosition + screenOffset;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPoint, cam, out var localPoint))
        {
            tooltipRoot.localRotation = Quaternion.identity;
            tooltipRoot.localScale = Vector3.one;
            tooltipRoot.anchoredPosition = localPoint;
        }
    }

    /// <summary>
    /// 阵营枚举中文简写。
    /// </summary>
    private static string FormatFaction(ShipFaction faction)
    {
        switch (faction)
        {
            case ShipFaction.EagleUnion:
                return "白鹰";
            case ShipFaction.RoyalNavy:
                return "皇家";
            case ShipFaction.SakuraEmpire:
                return "重樱";
            case ShipFaction.IronBlood:
                return "铁血";
            case ShipFaction.DragonEmpery:
                return "东煌";
            default:
                return "其他";
        }
    }

    #endregion
}
