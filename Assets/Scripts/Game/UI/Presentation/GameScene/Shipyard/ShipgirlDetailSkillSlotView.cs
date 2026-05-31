using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 舰娘详情技能槽：仅展示技能图标；名称与详情在悬停时由 <see cref="SkillHoverTooltipPresenter"/> 弹出提示框。
/// Instantiate 预制体会生成完整物体（含本脚本）；Inspector 引用的是「带本组件的预制体资源」，不是单独引用脚本。
/// </summary>
public sealed class ShipgirlDetailSkillSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Fields

    /// <summary>
    /// 技能图标（无技能时可隐藏）。
    /// </summary>
    [SerializeField] private Image skillIcon;

    /// <summary>
    /// 接收指针事件的 Graphic：一般为槽位底图 <see cref="Image"/>（须勾选 Raycast Target）；留空则尝试使用 <see cref="skillIcon"/>。
    /// </summary>
    [SerializeField] private Graphic raycastTargetGraphic;

    /// <summary>
    /// 悬停提示宿主（详情面板上挂的 Presenter，负责 Instantiate 提示框预制体）。
    /// </summary>
    private SkillHoverTooltipPresenter _tooltipPresenter;

    /// <summary>
    /// 当前绑定的技能；空槽为 null。
    /// </summary>
    private SkillConfigSO _skill;

    #endregion

    #region Public API

    /// <summary>
    /// 绑定技能与提示宿主。
    /// </summary>
    /// <param name="skill">技能配置；null 表示空槽。</param>
    /// <param name="tooltipPresenter">提示框 Presenter；可为 null（则无悬停）。</param>
    public void Bind(SkillConfigSO skill, SkillHoverTooltipPresenter tooltipPresenter)
    {
        _skill = skill;
        _tooltipPresenter = tooltipPresenter;

        if (skillIcon != null)
        {
            if (skill != null)
            {
                var sprite = GameResourceLoader.LoadSkillIcon(skill, logOnMissing: false);
                skillIcon.sprite = sprite;
                skillIcon.enabled = sprite != null;
            }
            else
            {
                skillIcon.sprite = null;
                skillIcon.enabled = false;
            }
        }

        var rayGraphic = raycastTargetGraphic != null ? raycastTargetGraphic : skillIcon as Graphic;
        if (rayGraphic != null)
        {
            rayGraphic.raycastTarget = skill != null;
        }
    }

    #endregion

    #region EventSystems

    /// <summary>
    /// 指针进入：显示 Tooltip。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_skill == null || _tooltipPresenter == null)
        {
            return;
        }

        _tooltipPresenter.Show(_skill);
    }

    /// <summary>
    /// 指针离开：隐藏 Tooltip。
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltipPresenter?.Hide();
    }

    #endregion
}
