using System;

/// <summary>
/// 单条账号记录，包含用户名与密码哈希。
/// </summary>
[Serializable]
public class AccountEntry
{
    public string Username;
    public string PasswordHash;
}