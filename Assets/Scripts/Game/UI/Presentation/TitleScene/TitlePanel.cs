using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 标题面板：处理登录、退出账号、设置、退出游戏等入口按钮逻辑。
/// </summary>
public sealed class TitlePanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 登录面板名，用于从标题面板切换到登录面板。
    /// </summary>
    [SerializeField] private string _LoginPanelName = PanelNames.LoginPanel;

    /// <summary>
    /// 登录游戏按钮引用。
    /// </summary>
    [SerializeField] private Button _loginButton;

    /// <summary>
    /// 退出账号按钮引用。
    /// </summary>
    [SerializeField] private Button _logoutButton;

    /// <summary>
    /// 游戏设置按钮引用。
    /// </summary>
    [SerializeField] private Button _settingButton;

    /// <summary>
    /// 退出游戏按钮引用。
    /// </summary>
    [SerializeField] private Button _exitButton;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 首帧若栈仍为空则压入标题面板（补偿 sceneLoaded 订阅时机）。
    /// </summary>
    private void Start()
    {
        UIPanelRegistry.EnsureDefaultStackForActiveScene();
    }

    /// <summary>
    /// 面板启用时注册面板并订阅按钮点击事件。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
    }

    /// <summary>
    /// 面板禁用时取消按钮点击事件，防止重复订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 点击登录按钮：压栈打开登录面板。
    /// </summary>
    public void OnClickLogin()
    {
        UIPanelRegistry.Push(_LoginPanelName);
    }

    /// <summary>
    /// 点击退出账号按钮：清除当前登录账号。
    /// </summary>
    public void OnClickLogout()
    {
        Game.Common.Auth.AccountStore.Logout();
    }

    /// <summary>
    /// 点击设置按钮：当前先打印日志，后续接设置面板。
    /// </summary>
    public void OnClickSetting()
    {
        // 先占位：未来接入 SettingPanel/SettingScene
        Debug.Log("Open Setting Panel (TODO)");
    }

    /// <summary>
    /// 点击退出按钮：退出游戏或停止编辑器运行。
    /// </summary>
    public void OnClickExit()
    {
        UIPanelRegistry.QuitGame();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 统一订阅按钮事件，避免忘记在 Inspector 绑定。
    /// </summary>
    private void SubscribeButtons()
    {
        if (_loginButton != null) _loginButton.onClick.AddListener(OnClickLogin);
        if (_logoutButton != null) _logoutButton.onClick.AddListener(OnClickLogout);
        if (_settingButton != null) _settingButton.onClick.AddListener(OnClickSetting);
        if (_exitButton != null) _exitButton.onClick.AddListener(OnClickExit);
    }

    /// <summary>
    /// 统一取消按钮事件，避免重复订阅导致多次触发。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (_loginButton != null) _loginButton.onClick.RemoveListener(OnClickLogin);
        if (_logoutButton != null) _logoutButton.onClick.RemoveListener(OnClickLogout);
        if (_settingButton != null) _settingButton.onClick.RemoveListener(OnClickSetting);
        if (_exitButton != null) _exitButton.onClick.RemoveListener(OnClickExit);
    }

    #endregion
}

