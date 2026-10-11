/// <summary>
/// Account 领域对象 ↔ SaveData DTO 映射器。纯函数，无状态。
/// </summary>
public static class AccountMapper
{
    /// <summary>SaveData → Domain</summary>
    public static Account ToDomain(AccountSaveData dto)
    {
        var account = new Account();
        if (dto == null) return account;

        if (dto.Accounts != null)
        {
            for (int i = 0; i < dto.Accounts.Count; i++)
            {
                var entry = dto.Accounts[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Username))
                    account.AddUser(entry.Username, entry.PasswordHash ?? string.Empty);
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.CurrentUser))
            account.SetCurrentUser(dto.CurrentUser);

        account.SetRemembered(dto.RememberUser, dto.RememberPass, dto.RememberEnabled);

        return account;
    }

    /// <summary>Domain → SaveData</summary>
    public static AccountSaveData ToSaveData(Account domain)
    {
        var dto = new AccountSaveData
        {
            CurrentUser = domain.CurrentUser ?? string.Empty,
            RememberEnabled = domain.RememberEnabled,
            RememberUser = domain.RememberUser ?? string.Empty,
            RememberPass = domain.RememberPass ?? string.Empty
        };

        var entries = domain.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            dto.Accounts.Add(new AccountEntry
            {
                Username = entry.Username,
                PasswordHash = entry.PasswordHash
            });
        }

        return dto;
    }
}