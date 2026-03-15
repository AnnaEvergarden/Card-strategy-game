using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AccountManager : Singleton<AccountManager>
{
    private const string AccountFileName = "accounts.json";
    private AccountListData accountList;
    private string currentAccountId; // 当前登录账号 Id

    private string AccountPath => Path.Combine(Application.persistentDataPath, AccountFileName);

    private void Awake()
    {
        LoadAccounts();
    }

    private void LoadAccounts()
    {
        accountList = new AccountListData();

        if (File.Exists(AccountPath))
        {
            try
            {
                string json = File.ReadAllText(AccountPath);
                accountList = JsonUtility.FromJson<AccountListData>(json);
                if (accountList?.accounts == null) accountList = new AccountListData();
            }
            catch
            {
                accountList = new AccountListData();
            }
        }
        else
        {
            Debug.LogWarning($"[AccountManager] 账号文件不存在，将创建新文件: {AccountPath}");
            accountList = new AccountListData();
        }
    }

    private void SaveAccounts()
    {
        string json = JsonUtility.ToJson(accountList);
        File.WriteAllText(AccountPath, json);
    }

    ///<summary>
    /// 注册账号，用户名或账号Id已存在则返回 false
    /// </summary>
    public bool Register(string _userName, string _password)
    {
        if(string.IsNullOrEmpty(_userName) || string.IsNullOrEmpty(_password))
        {
            Debug.LogWarning("[AccountManager] 用户名或密码不能为空");
            return false;
        }
        foreach(var a in accountList.accounts)
        {
            if(a.userName == _userName)
            {
                Debug.LogWarning("[AccountManager] 该账号或用户名已经存在!");
                return false;
            }
        }
        accountList.accounts.Add(new AccountEntry(_userName, _password));
        SaveAccounts();
        return true;
    }

    /// <summary>
    /// 登录验证，成功返回 True 并设置当前账号
    /// </summary>
    public bool Login(string _userName, string _password)
    {
        if (accountList?.accounts == null) return false;

        foreach(var a in accountList.accounts)
        {
            if(a.userName == _userName && a.password == _password)
            {
                currentAccountId = a.accountId;
                return true; 
            }
        }
        Debug.Log("账号不存在或输入信息有问题!");
        return false;
    }

    /// <summary>
    /// 登出当前账号
    /// </summary>
    public void Logout()
    {
        currentAccountId = null;
    }

    public string GetCurrentAccountId() => currentAccountId;
    public int? GetAccountNum() => accountList?.accounts.Count;
    public bool IsLoggedIn() => !string.IsNullOrEmpty(currentAccountId);
}
