using System.Collections.Generic;

/// <summary>
/// 账号存档 DTO。只用于序列化，与 Domain/Save/AccountData.cs 结构一致。
/// </summary>
[System.Serializable]
public class AccountSaveData
{
    public List<AccountEntry> Accounts = new List<AccountEntry>();
    public string CurrentUser = string.Empty;
    public bool RememberEnabled = true;
    public string RememberUser = string.Empty;
    public string RememberPass = string.Empty;
}