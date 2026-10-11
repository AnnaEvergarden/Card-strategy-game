using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// UI 服务接口：管理面板的打开、关闭、栈导航与层级管理。
/// 面板通过 string 标识，内部通过 PanelConfig → ResourceId（预制体资源） 链式加载。
/// 保证面板之间不直接互相调用，统一通过此服务通信。
/// </summary>
public interface IUIService
{
    /// <summary>
    /// 打开指定面板。
    /// </summary>
    /// <typeparam name="TPanel">面板类型</typeparam>
    /// <param name="panelId">面板标识</param>
    /// <param name="param">打开参数（可选，传递给面板的 OnOpenAsync）</param>
    /// <returns>面板实例</returns>
    UniTask<TPanel> OpenAsync<TPanel>(string panelId, object param = null) where TPanel : PanelBase;

    /// <summary>
    /// 关闭指定面板（若面板在导航栈中，同时从栈中移除）。
    /// </summary>
    /// <param name="panelId">面板标识</param>
    UniTask CloseAsync(string panelId);

    /// <summary>
    /// 返回上一级面板：弹出导航栈顶，隐藏当前面板，恢复上一层面板。
    /// </summary>
    UniTask GoBackAsync();

    /// <summary>
    /// 返回主界面：清空导航栈，显示 LobbyPanel。
    /// </summary>
    UniTask GoHomeAsync();

    /// <summary>
    /// 打开面板时触发的事件（供 Application 层监听面板打开事件）。
    /// </summary>

    /// <summary>
    /// 关闭面板时触发的事件。
    /// </summary>
}



