using UnityEngine;

/// <summary>
/// 技能悬停提示宿主：引用「提示框预制体」，运行时实例化一次并复用；槽位脚本只调本组件，不直接持有提示框物体。
/// 提示框预制体根节点须挂 <see cref="SkillHoverTooltipView"/>（内部挂标题、正文等）。
/// </summary>
public sealed class SkillHoverTooltipPresenter : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 提示框预制体（根物体带 <see cref="SkillHoverTooltipView"/>）。
    /// </summary>
    [SerializeField] private GameObject tooltipPrefab;

    /// <summary>
    /// 实例化父节点；为空时使用 <see cref="rootCanvasOverride"/> 或向上查找的 <see cref="Canvas"/> 根变换。
    /// </summary>
    [SerializeField] private Transform tooltipSpawnParent;

    /// <summary>
    /// 坐标换算用画布：一般留空，由生成父节点推断；仅当父节点不在期望 Canvas 下时再指定（例如多 Canvas）。
    /// </summary>
    [SerializeField] private Canvas rootCanvasOverride;

    /// <summary>
    /// 运行时生成的提示视图。
    /// </summary>
    private SkillHoverTooltipView _instance;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 隐藏提示，避免叠在其它面板上。
    /// </summary>
    private void OnDisable()
    {
        Hide();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 显示技能说明（跟随鼠标）。
    /// </summary>
    /// <param name="skill">技能配置。</param>
    public void Show(SkillConfigSO skill)
    {
        EnsureInstance();
        _instance?.Show(skill);
    }

    /// <summary>
    /// 隐藏提示框。
    /// </summary>
    public void Hide()
    {
        _instance?.Hide();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 懒加载实例：Instantiate 预制体挂到父节点下，并把Presenter侧画布覆盖传给视图。
    /// </summary>
    private void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        if (tooltipPrefab == null)
        {
            Debug.LogWarning(
                "SkillHoverTooltipPresenter: 未绑定 tooltipPrefab，悬停提示无法显示。");
            return;
        }

        var parent = tooltipSpawnParent;
        if (parent == null)
        {
            var canvas = rootCanvasOverride != null ? rootCanvasOverride : GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                parent = canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;
            }
        }

        if (parent == null)
        {
            Debug.LogWarning(
                "SkillHoverTooltipPresenter: 无法确定 tooltip 父节点，请指定 tooltipSpawnParent 或置于 Canvas 下。");
            return;
        }

        var go = Instantiate(tooltipPrefab, parent, false);
        _instance = go.GetComponent<SkillHoverTooltipView>();
        if (_instance == null)
        {
            Destroy(go);
            Debug.LogError(
                "SkillHoverTooltipPresenter: tooltipPrefab 根节点缺少 SkillHoverTooltipView 组件。");
            return;
        }

        _instance.ApplyPresenterCanvas(rootCanvasOverride);
        _instance.Hide();
    }

    #endregion
}
