using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 集中管理 PanelName 的类
/// </summary>
public static class UIPanelNames
{
    public const string Title = "TitlePanel";
    public const string Login = "LoginPanel";
    public const string SaveSelcet = "SaveSelectPanel";
    public const string Battle = "BattlePanel";
}


/// <summary>
/// 所有面板的父类，负责创建方法用于重写
/// </summary>
public abstract class BaseUIPanel : MonoBehaviour 
{
    [SerializeField] public string PanelName;

    protected virtual void Awake()
    {
        RegisterPanel();

        HidePanel();
    }

    private void OnDestroy()
    {
        UnRegisterPanel();
    }

    /// <summary>
    /// 注册面板
    /// </summary>
    public virtual void RegisterPanel()
    {
        var uiManager = UIPanelManager.Instance;
        if(uiManager != null && !string.IsNullOrEmpty(PanelName))
        {
            uiManager.RegisterPanel(PanelName, this);
        }
        else
        {
            Debug.LogWarning($"UIManager为空或面板没有命名!");
        }
    }

    /// <summary>
    /// 注销面板
    /// </summary>
    public void UnRegisterPanel()
    {
        var uiManager = UIPanelManager.Instance;
        if (uiManager != null && !string.IsNullOrEmpty(PanelName))
        {
            uiManager.UnRegisterPanel(PanelName, this);
        }
        else
        {
            Debug.LogWarning($"UIManager为空或面板没有命名!");
        }
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    public virtual void ShowPanel()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    public virtual void HidePanel()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示面板时调用
    /// </summary>
    public virtual void OnShow() { }

    /// <summary>
    /// 隐藏面板时调用
    /// </summary>
    public virtual void OnHide() { }
}
