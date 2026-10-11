/// <summary>
/// 账号变更事件。
/// </summary>
public sealed class AccountChangedEvent : IDomainEvent
{
    /// <summary>变更后的账号视图。</summary>
    public AccountSnapshot Snapshot { get; }

    /// <summary>变更类型。</summary>
    public ChangeType Type { get; }

    public enum ChangeType { LoggedIn, Registered, Remembered, LoggedOut, Unknown }

    public AccountChangedEvent(Account account, ChangeType type = ChangeType.Unknown)
    {
        Snapshot = new AccountSnapshot(account);
        Type = type;
    }
}

/// <summary>
/// 账号数据的不可变快照，供 UI 层读取，不暴露可修改的引用。
/// </summary>
public sealed class AccountSnapshot
{
    public string CurrentUser { get; }
    public bool IsLoggedIn { get; }
    public int AccountCount { get; }

    public AccountSnapshot(Account account)
    {
        CurrentUser = account.CurrentUser;
        IsLoggedIn = account.IsLoggedIn;
        AccountCount = account.Entries.Count;
    }
}