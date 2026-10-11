using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 标题面板：显示游戏标题、登录、设置与退出游戏入口按钮。
/// 属于 Presentation 层，仅负责按钮点击事件分发，不处理业务逻辑。
/// </summary>
public sealed class TitlePanel : PanelBase
{
    [Header("按钮引用")]

    /// <summary>
    /// 登录按钮。
    /// </summary>
    [SerializeField]
    [Tooltip("登录按钮")]
    private Button _loginButton;

    /// <summary>
    /// 设置按钮。
    /// </summary>
    [SerializeField]
    [Tooltip("设置按钮")]
    private Button _settingButton;

    /// <summary>
    /// 退出游戏按钮。
    /// </summary>
    [SerializeField]
    [Tooltip("退出游戏按钮")]
    private Button _exitButton;

    [Header("面板 ID")]
    [SerializeField]
    [Tooltip("登录面板 ID")]
    private string _loginPanelId = "Login";

    /// <summary>
    /// 面板启用时订阅按钮事件。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
    }

    /// <summary>
    /// 面板禁用时取消按钮事件订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    /// <summary>
    /// 点击登录按钮：打开登录面板。
    /// </summary>
    public async void OnClickLogin()
    {
        if (UIService == null) return;
            await UIService.OpenAsync<PanelBase>(_loginPanelId);
    }

    /// <summary>
    /// 点击设置按钮：打开设置面板（暂未实现）。
    /// </summary>
    public void OnClickSetting()
    {
        Debug.Log("[Setting Panel] 设置功能待实现");
    }

    /// <summary>
    /// 点击退出按钮：退出游戏。
    /// </summary>
    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        UnityEngine.Application.Quit();
#endif
    }

    /// <summary>
    /// 订阅按钮点击事件。
    /// </summary>
    private void SubscribeButtons()
    {
        if (_loginButton != null) _loginButton.onClick.AddListener(OnClickLogin);
        if (_settingButton != null) _settingButton.onClick.AddListener(OnClickSetting);
        if (_exitButton != null) _exitButton.onClick.AddListener(OnClickExit);
    }

    /// <summary>
    /// 取消按钮点击事件。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (_loginButton != null) _loginButton.onClick.RemoveListener(OnClickLogin);
        if (_settingButton != null) _settingButton.onClick.RemoveListener(OnClickSetting);
        if (_exitButton != null) _exitButton.onClick.RemoveListener(OnClickExit);
    }
}
