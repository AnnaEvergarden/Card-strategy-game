using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录面板：提供账号密码输入、记住密码、注册与登录功能。
/// 属于 Presentation 层，仅负责 UI 表现与用户输入收集，登录逻辑委托给 LoginService。
/// </summary>
public sealed class LoginPanel : PanelBase
{
    [Header("输入组件")]

    /// <summary>
    /// 用户名输入框。
    /// </summary>
    [SerializeField]
    [Tooltip("用户名输入框")]
    private TMP_InputField _usernameInput;

    /// <summary>
    /// 密码输入框。
    /// </summary>
    [SerializeField]
    [Tooltip("密码输入框")]
    private TMP_InputField _passwordInput;

    /// <summary>
    /// 记住密码勾选框。
    /// </summary>
    [SerializeField]
    [Tooltip("记住密码勾选框")]
    private Toggle _rememberToggle;

    [Header("按钮")]

    /// <summary>
    /// 登录按钮。
    /// </summary>
    [SerializeField]
    [Tooltip("登录按钮")]
    private Button _loginButton;

    /// <summary>
    /// 注册按钮。
    /// </summary>
    [SerializeField]
    [Tooltip("注册按钮")]
    private Button _registerButton;

    /// <summary>
    /// 返回按钮（回到标题面板）。
    /// </summary>
    [SerializeField]
    [Tooltip("返回按钮")]
    private Button _backButton;

    /// <summary>
    /// 面板启用时：订阅按钮、恢复记住的账号信息。
    /// </summary>
    protected override async void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        await LoadRememberedCredentials();
    }

    /// <summary>
    /// 面板禁用时取消按钮订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    /// <summary>
    /// 点击登录按钮：收集输入，调用 LoginService 校验并跳转。
    /// </summary>
    public async void OnClickLogin()
    {
        string user = _usernameInput != null ? _usernameInput.text : string.Empty;
        string pass = _passwordInput != null ? _passwordInput.text : string.Empty;
        bool remember = _rememberToggle != null && _rememberToggle.isOn;

        // 保存记住密码状态
        var loginService = ResolveLoginService();
        if (loginService == null) return;
        await loginService.SaveRememberedAsync(user, pass, remember);

        // 执行登录
        var loginResult = await loginService.TryLoginAsync(user, pass); if (!loginResult.Success)
        {
            Debug.LogWarning($"[LoginPanel] 登录失败: {loginResult.Error}");
            // 登录失败时在实际 UI 上显示错误
        }
    }

    /// <summary>
    /// 点击注册按钮：收集输入，调用 LoginService 注册新账号。
    /// </summary>
    public async void OnClickRegister()
    {
        string user = _usernameInput != null ? _usernameInput.text : string.Empty;
        string pass = _passwordInput != null ? _passwordInput.text : string.Empty;

        var loginService = ResolveLoginService();
        if (loginService == null) return;

        var regResult = await loginService.TryRegisterAsync(user, pass); if (!regResult.Success)
        {
            Debug.LogWarning($"[LoginPanel] 注册失败: {regResult.Error}");
        }
        else
        {
            Debug.Log("[LoginPanel] 注册成功");
        }
    }

    /// <summary>
    /// 点击返回按钮：弹出登录面板，回到标题面板。
    /// </summary>
    public void OnClickBack()
    {
        UIService?.GoBackAsync().Forget();
    }

    /// <summary>
    /// 从 ServiceContainer 解析登录服务。
    /// </summary>
    private LoginService ResolveLoginService()
    {
        if (ServiceContainer != null && ServiceContainer.IsRegistered<LoginService>())
            return ServiceContainer.Resolve<LoginService>();

        Debug.LogWarning("[LoginPanel] ServiceContainer 或 LoginService 未注册");
        return null;
    }

    /// <summary>
    /// 加载并恢复记住的账号密码。
    /// </summary>
    private async UniTask LoadRememberedCredentials()
    {
        var loginService = ResolveLoginService();
        if (loginService == null) return;

        var (user, pass) = await loginService.LoadRememberedAsync();

        if (_usernameInput != null && string.IsNullOrEmpty(_usernameInput.text))
            _usernameInput.text = user;

        if (_passwordInput != null && string.IsNullOrEmpty(_passwordInput.text))
            _passwordInput.text = pass;

        if (_rememberToggle != null)
            _rememberToggle.isOn = !string.IsNullOrEmpty(user);
    }

    /// <summary>
    /// 订阅按钮事件。
    /// </summary>
    private void SubscribeButtons()
    {
        if (_loginButton != null) _loginButton.onClick.AddListener(OnClickLogin);
        if (_registerButton != null) _registerButton.onClick.AddListener(OnClickRegister);
        if (_backButton != null) _backButton.onClick.AddListener(OnClickBack);
    }

    /// <summary>
    /// 取消按钮事件。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (_loginButton != null) _loginButton.onClick.RemoveListener(OnClickLogin);
        if (_registerButton != null) _registerButton.onClick.RemoveListener(OnClickRegister);
        if (_backButton != null) _backButton.onClick.RemoveListener(OnClickBack);
    }
}
