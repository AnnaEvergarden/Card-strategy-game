using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoginPanel : BaseUIPanel
{
    [Header("引用面板")]
    [Tooltip("需要引用的面板名称")]
    [SerializeField] public string _SaveSelectPanelName;
    [SerializeField] public string _TitlePanelName;

    [Header("输入框")]
    [SerializeField] private TMP_InputField userNameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [SerializeField] private AccountManager accountManager;

    private void Start()
    {
        //实现检查逻辑
        if (accountManager == null)
        {
            Debug.LogWarning("AccountManager为空!");
            return;
        }
    }

    public void OnClickLogin()
    {
        string userName = userNameInput.text;
        string password = passwordInput.text;

        if(!accountManager.Login(userName, password))
        {
            Debug.Log("登录失败!");
            return;
        }

        UIPanelManager.Instance.ShowPanel(_SaveSelectPanelName);
    }

    public void OnClickRegister()
    {
        string userName = userNameInput.text;
        string password = passwordInput.text;

        if(accountManager.Login(userName, password))
        {
            Debug.Log($"{userName} 该用户已存在并成功登录!");
        }
        else
        {
            //注册逻辑
            accountManager.Register(userName, password);
            Debug.Log($"{userName} 注册成功！");
        }
    }

    public void OnClickCancel()
    {
        UIPanelManager.Instance.ShowPanel(_TitlePanelName);
    }
}
