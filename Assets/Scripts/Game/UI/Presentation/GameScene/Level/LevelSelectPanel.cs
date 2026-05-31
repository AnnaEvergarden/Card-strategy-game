using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 选择关卡面板：提供常驻/活动入口，以及返回与主界面按钮（同场景栈式导航）。
/// </summary>
public sealed class LevelSelectPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 返回游戏主界面（MainPanel）。
    /// </summary>
    [SerializeField] private Button backBtn;

    /// <summary>
    /// 主界面按钮：清空栈后回 MainPanel。
    /// </summary>
    [SerializeField] private Button homeBtn;

    /// <summary>
    /// 进入常驻关卡列表界面。
    /// </summary>
    [SerializeField] private Button permanentLevelsBtn;

    /// <summary>
    /// 进入活动关卡列表界面。
    /// </summary>
    [SerializeField] private Button activityLevelsBtn;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中已放置的选关面板；若不存在则打调试警告并返回 null（不运行时创建默认物体）。
    /// </summary>
    public static LevelSelectPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<LevelSelectPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.LevelSelectPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "LevelSelectPanel: 场景中未找到 LevelSelectPanel，请在场景或 UI 预制体中挂载该组件；已取消运行时自动搭建界面。");
        return null;
    }

    /// <summary>
    /// 点击返回：弹出当前面板，恢复栈中上一层。
    /// </summary>
    public void OnClickBack()
    {
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 点击主界面：清空栈后以游戏主界面为唯一栈顶。
    /// </summary>
    public void OnClickHome()
    {
        UIPanelRegistry.ClearAndPush(PanelNames.MainPanel);
    }

    /// <summary>
    /// 点击常驻关卡：打开共用区域选择面板，并按常驻配置展示区域信息。
    /// </summary>
    public void OnClickPermanentLevels()
    {
        var panel = LevelAreaSelectPanel.EnsureInstance();
        if (panel == null)
        {
            return;
        }

        panel.OpenWithMode(LevelMode.Permanent);
        UIPanelRegistry.Push(PanelNames.LevelAreaSelectPanel);
    }

    /// <summary>
    /// 点击活动关卡：打开共用区域选择面板，并按活动配置展示区域信息。
    /// </summary>
    public void OnClickActivityLevels()
    {
        var panel = LevelAreaSelectPanel.EnsureInstance();
        if (panel == null)
        {
            return;
        }

        panel.OpenWithMode(LevelMode.Activity);
        UIPanelRegistry.Push(PanelNames.LevelAreaSelectPanel);
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时订阅按钮点击。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
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

    private void SubscribeButtons()
    {
        if (backBtn != null)
        {
            backBtn.onClick.AddListener(OnClickBack);
        }

        if (homeBtn != null)
        {
            homeBtn.onClick.AddListener(OnClickHome);
        }

        if (permanentLevelsBtn != null)
        {
            permanentLevelsBtn.onClick.AddListener(OnClickPermanentLevels);
        }

        if (activityLevelsBtn != null)
        {
            activityLevelsBtn.onClick.AddListener(OnClickActivityLevels);
        }
    }

    private void UnsubscribeButtons()
    {
        if (backBtn != null)
        {
            backBtn.onClick.RemoveListener(OnClickBack);
        }

        if (homeBtn != null)
        {
            homeBtn.onClick.RemoveListener(OnClickHome);
        }

        if (permanentLevelsBtn != null)
        {
            permanentLevelsBtn.onClick.RemoveListener(OnClickPermanentLevels);
        }

        if (activityLevelsBtn != null)
        {
            activityLevelsBtn.onClick.RemoveListener(OnClickActivityLevels);
        }
    }

    #endregion
}
