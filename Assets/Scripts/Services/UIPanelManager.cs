using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理所有面板，按栈的方式打开/关闭面板,负责注册和切换面板
/// 挂载在(UIManager上)
/// </summary>
public class UIPanelManager : Singleton<UIPanelManager>
{
    private Dictionary<string, BaseUIPanel> panels = new Dictionary<string, BaseUIPanel>();

    /// <summary>
    /// 面板名称栈
    /// </summary>
    private Stack<string> panelStack = new Stack<string>();

    public bool useStack;

    public void RegisterPanel(string _panelName, BaseUIPanel _panel)
    {
        if(string.IsNullOrEmpty(_panelName) || _panel == null)
        {
            Debug.LogWarning($"_panelName 为空或 _panel为空！");
            return;
        }
        panels[_panelName] = _panel;
        _panel.HidePanel();
    }

    public void UnRegisterPanel(string _panelName, BaseUIPanel _panel)
    {
        if (string.IsNullOrEmpty(_panelName) || _panel == null)
        {
            Debug.LogWarning($"_panelName 为空或 _panel为空！");
            return;
        }
        if(panels.TryGetValue(_panelName, out BaseUIPanel _out))
        {
            _out.HidePanel();
            panels.Remove(_panelName);
        }
        else
        {
            Debug.LogWarning($"没有 {_panel} 面板");
        }
    }

    /// <summary>
    /// 显示指定面板，同时隐藏其他面板
    /// </summary>
    public void ShowPanel(string _panelName)
    {
        HideAllPanels();

        panels.TryGetValue(_panelName, out var _panel);
        if (_panel == null)
        {
            Debug.LogWarning($"{_panelName} 不存在！");
        }
        else _panel.ShowPanel();
    }

    private void HideAllPanels()
    {
        foreach (var _key in panels)
        {
            {
                _key.Value.HidePanel();
            }
        }
    }

    /// <summary> 
    /// 显示面板并压栈,返回时回到上一个面板
    /// </summary>
    public void PushPanel(string _panelName)
    {
        if (string.IsNullOrEmpty(_panelName))
        {
            Debug.LogWarning("_panelName为空！");
            return;
        }

        if (!panels.ContainsKey(_panelName))
        {
            Debug.LogWarning("[UIPanelManager] 不存在该面板！");
            return;
        }

        if(panels.Count > 0 && useStack)
        {
            string currrent = panelStack.Peek();
            if(currrent == _panelName)
            {
                //已经是该面板了
                return;
            }
        }

        if(useStack)
        {
            panelStack.Push(_panelName);
        }

        //HideAllPanels();
        panels[_panelName].ShowPanel();
    }

    /// <summary>
    /// 返回上一层面板（弹栈并显示栈顶）
    /// </summary>
    public void Back()
    {
        if (!useStack || panelStack.Count == 0)
        {
            return;
        }

        panelStack.Pop(); //弹出当前栈

        if(panelStack.Count == 0)
        {
            HideAllPanels();
            return;
        }

        // 显示之前的面板
        string previous = panelStack.Peek();
        //HideAllPanels();
        panels[previous].ShowPanel();
    }

    /// <summary>
    /// 用新面板替换当前栈顶面板
    /// </summary>
    public void ReplacePanel(string _panelName)
    {
        if (string.IsNullOrEmpty(_panelName))
        {
            Debug.LogWarning("_paneName 为空！");
            return;
        }

        if(panelStack.Count > 0 && useStack)
        {
            panelStack.Pop();
        }

        if (useStack)
        {
            panelStack.Push(_panelName);
        }

        //HideAllPanels();
        panels[_panelName].ShowPanel();
    }

    /// <summary>
    /// 清空栈并显示指定面板
    /// </summary>
    public void ClearStackAndShow(string _panelName)
    {
        panelStack.Clear();
        panelStack.Push(_panelName);
        panels[_panelName].ShowPanel();
    }

    /// <summary>
    /// 获取已注册的面板
    /// </summary>
    public BaseUIPanel GetPanel(string _panelName)
    {
        return panels.TryGetValue(_panelName, out var _value) ? _value : null; 
    }

    public int StackCount => useStack ? panelStack.Count : 0;
}
