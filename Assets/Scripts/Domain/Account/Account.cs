using System;
using System.Collections.Generic;

/// <summary>
/// 账号聚合根。封装所有账号操作，对外只暴露只读视图。
/// 密码校验逻辑放在 Application Service 层（密码哈希属于基础设施），
/// Domain 只负责查找和状态管理。
/// </summary>
public sealed class Account
{
    private readonly List<AccountEntry> _entries = new List<AccountEntry>();

    /// <summary>已注册账号列表（只读）。</summary>
    public IReadOnlyList<AccountEntry> Entries => _entries;

    /// <summary>当前登录的用户名（空表示未登录）。</summary>
    public string CurrentUser { get; private set; } = string.Empty;

    /// <summary>是否启用记住密码。</summary>
    public bool RememberEnabled { get; private set; } = true;

    /// <summary>记住的用户名。</summary>
    public string RememberUser { get; private set; } = string.Empty;

    /// <summary>记住的密码（明文，仅限本地使用）。</summary>
    public string RememberPass { get; private set; } = string.Empty;

    /// <summary>是否已登录。</summary>
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(CurrentUser);

    /// <summary>检查用户名是否已存在。</summary>
    public bool HasUser(string username)
    {
        return FindUser(username) != null;
    }

    /// <summary>按用户名查找账号。</summary>
    public AccountEntry FindUser(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (string.Equals(_entries[i].Username, username, StringComparison.Ordinal))
                return _entries[i];
        }
        return null;
    }

    /// <summary>添加新账号。</summary>
    /// <returns>添加成功返回 true；用户名已存在返回 false。</returns>
    public bool AddUser(string username, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        if (string.IsNullOrWhiteSpace(passwordHash)) return false;
        if (HasUser(username)) return false;

        _entries.Add(new AccountEntry
        {
            Username = username,
            PasswordHash = passwordHash
        });
        return true;
    }

    /// <summary>设置当前登录用户。</summary>
    public void SetCurrentUser(string username)
    {
        CurrentUser = username ?? string.Empty;
    }

    /// <summary>设置记住密码信息。</summary>
    public void SetRemembered(string username, string password, bool enabled)
    {
        RememberEnabled = enabled;
        RememberUser = enabled ? (username ?? string.Empty) : string.Empty;
        RememberPass = enabled ? (password ?? string.Empty) : string.Empty;
    }

    /// <summary>清除所有账号状态（登出时使用）。</summary>
    public void Clear()
    {
        _entries.Clear();
        CurrentUser = string.Empty;
        RememberEnabled = true;
        RememberUser = string.Empty;
        RememberPass = string.Empty;
    }
}