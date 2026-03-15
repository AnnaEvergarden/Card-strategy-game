using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AccountEntry
{
    public string userName;
    public string accountId; // ??????
    public string password; // ??

    public AccountEntry() { }

    public AccountEntry(string _userName, string _password)
    {
        userName = _userName;
        accountId = Guid.NewGuid().ToString();
        password = _password;
    }
}

[Serializable]
public class AccountListData
{
    public List<AccountEntry> accounts = new List<AccountEntry>();
}
