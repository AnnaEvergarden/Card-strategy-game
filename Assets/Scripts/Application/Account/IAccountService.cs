using Cysharp.Threading.Tasks;

/// <summary>
/// 账号应用服务接口。密码哈希逻辑封装在此层，Domain 不感知哈希算法。
/// </summary>
public interface IAccountService
{
    /// <summary>登录：校验密码哈希，成功时设置当前用户。</summary>
    UniTask<bool> LoginAsync(string username, string password);

    /// <summary>注册：添加新账号。</summary>
    UniTask<bool> RegisterAsync(string username, string password);

    /// <summary>获取记住的账号密码。</summary>
    UniTask<(string user, string pass)> LoadRememberedAsync();

    /// <summary>保存记住的账号密码。</summary>
    UniTask SaveRememberedAsync(string username, string password, bool enabled);
}