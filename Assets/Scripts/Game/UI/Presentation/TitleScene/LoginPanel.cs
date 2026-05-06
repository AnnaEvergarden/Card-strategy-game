using Game.Common.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录面板：处理账号输入、注册、登录与返回标题面板。
/// </summary>
public sealed class LoginPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 用户名输入框引用。
    /// </summary>
    [SerializeField] private TMP_InputField userNameInput;

    /// <summary>
    /// 密码输入框引用。
    /// </summary>
    [SerializeField] private TMP_InputField passwordInput;

    /// <summary>
    /// 记住密码勾选框引用。
    /// </summary>
    [SerializeField] private Toggle _rememberToggle;

    /// <summary>
    /// 登录按钮引用。
    /// </summary>
    [SerializeField] private Button _loginButton;

    /// <summary>
    /// 注册按钮引用。
    /// </summary>
    [SerializeField] private Button _registerButton;

    /// <summary>
    /// 取消按钮引用。
    /// </summary>
    [SerializeField] private Button _cancelButton;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 面板启用时注册面板、恢复记住的账号密码并订阅按钮。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();

        var (user, pass) = AccountStore.LoadRememberedCredentials();
        if (userNameInput != null && string.IsNullOrEmpty(userNameInput.text)) userNameInput.text = user;
        if (passwordInput != null && string.IsNullOrEmpty(passwordInput.text)) passwordInput.text = pass;

        if (_rememberToggle != null) _rememberToggle.isOn = AccountStore.GetRememberEnabled();
    }

    /// <summary>
    /// 面板禁用时取消按钮订阅，防止重复添加监听。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 点击取消按钮：弹出登录面板，恢复标题面板。
    /// </summary>
    public void OnClickCancel()
    {
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 点击注册按钮：用当前输入的账号密码执行注册。
    /// </summary>
    public void OnClickRegister()
    {
        var user = userNameInput != null ? userNameInput.text : string.Empty;
        var pass = passwordInput != null ? passwordInput.text : string.Empty;

        if (!AccountStore.TryRegister(user, pass, out var error))
        {
            Debug.LogWarning($"Register failed: {error}");
            return;
        }

        Debug.Log("Register success");
    }

    /// <summary>
    /// 点击登录按钮：校验成功后保存记住密码并进入游戏场景。
    /// </summary>
    public void OnClickLogin()
    {
        var user = userNameInput != null ? userNameInput.text : string.Empty;
        var pass = passwordInput != null ? passwordInput.text : string.Empty;

        if (_rememberToggle != null)
        {
            AccountStore.SetRememberEnabled(_rememberToggle.isOn);
        }

        if (!AccountStore.TryLogin(user, pass, out var error))
        {
            Debug.LogWarning($"Login failed: {error}");
            return;
        }

        AccountStore.SaveRememberedCredentials(user, pass);
        UIPanelRegistry.LoadScene(SceneNames.GameScene);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 统一订阅按钮事件，避免漏绑 Inspector 的 OnClick。
    /// </summary>
    private void SubscribeButtons()
    {
        if (_loginButton != null) _loginButton.onClick.AddListener(OnClickLogin);
        if (_registerButton != null) _registerButton.onClick.AddListener(OnClickRegister);
        if (_cancelButton != null) _cancelButton.onClick.AddListener(OnClickCancel);
    }

    /// <summary>
    /// 统一取消按钮事件，避免重复订阅导致多次调用。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (_loginButton != null) _loginButton.onClick.RemoveListener(OnClickLogin);
        if (_registerButton != null) _registerButton.onClick.RemoveListener(OnClickRegister);
        if (_cancelButton != null) _cancelButton.onClick.RemoveListener(OnClickCancel);
    }

    #endregion
}

