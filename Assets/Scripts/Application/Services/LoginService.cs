using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 登录操作结果。
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public string Error { get; set; }
}

/// <summary>
/// 登录服务：提供 UI 层登录流程编排。
/// 输入校验、场景导航在此层处理，账号数据操作委托给 IAccountService。
/// </summary>
public class LoginService
{
    private readonly IAccountService _accountService;
    private readonly ISceneService _sceneService;

    public LoginService(IAccountService accountService, ISceneService sceneService)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _sceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
    }

    /// <summary>尝试登录：校验账号密码，成功时保存状态并跳转。</summary>
    public async UniTask<LoginResult> TryLoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new LoginResult { Error = "账号不能为空" };

        if (string.IsNullOrWhiteSpace(password))
            return new LoginResult { Error = "密码不能为空" };

        username = username.Trim();

        bool success = await _accountService.LoginAsync(username, password);
        if (!success)
            return new LoginResult { Error = "账号或密码错误" };

        await _sceneService.LoadSceneAsync(SceneType.Lobby);
        return new LoginResult { Success = true };
    }

    /// <summary>尝试注册新账号。</summary>
    public async UniTask<LoginResult> TryRegisterAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new LoginResult { Error = "账号不能为空" };

        if (string.IsNullOrWhiteSpace(password))
            return new LoginResult { Error = "密码不能为空" };

        username = username.Trim();

        bool success = await _accountService.RegisterAsync(username, password);
        if (!success)
            return new LoginResult { Error = "账号已存在" };

        return new LoginResult { Success = true };
    }

    /// <summary>加载记住的账号密码。</summary>
    public async UniTask<(string user, string pass)> LoadRememberedAsync()
    {
        return await _accountService.LoadRememberedAsync();
    }

    /// <summary>保存记住的账号密码。</summary>
    public async UniTask SaveRememberedAsync(string username, string password, bool enabled)
    {
        await _accountService.SaveRememberedAsync(username, password, enabled);
    }
}