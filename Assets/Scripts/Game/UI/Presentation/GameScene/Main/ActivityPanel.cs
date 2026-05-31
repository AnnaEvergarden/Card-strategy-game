using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 活动面板：需在场景中摆放并配置 <see cref="PanelName"/>、按钮等；同场景内通过 <see cref="UIPanelRegistry"/> 栈式前进/返回/主界面。
/// </summary>
public sealed class ActivityPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 返回主界面按钮。
    /// </summary>
    [SerializeField] private Button BackBtn;

    /// <summary>
    /// 主界面按钮：清空导航栈并打开 <see cref="PanelNames.MainPanel"/>。
    /// </summary>
    [SerializeField] private Button HomeBtn;

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
    /// 禁用时取消订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 点击返回：弹出当前面板，恢复上一层。
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

    #endregion

    #region Private Methods

    private void SubscribeButtons()
    {
        if (BackBtn != null)
        {
            BackBtn.onClick.AddListener(OnClickBack);
        }

        if (HomeBtn != null)
        {
            HomeBtn.onClick.AddListener(OnClickHome);
        }
    }

    private void UnsubscribeButtons()
    {
        if (BackBtn != null)
        {
            BackBtn.onClick.RemoveListener(OnClickBack);
        }

        if (HomeBtn != null)
        {
            HomeBtn.onClick.RemoveListener(OnClickHome);
        }
    }

    #endregion
}
