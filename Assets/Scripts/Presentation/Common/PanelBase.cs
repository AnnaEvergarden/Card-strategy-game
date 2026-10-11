using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 面板基类：所有 UI 面板的基类。
/// </summary>
public abstract class PanelBase : MonoBehaviour
{
    [SerializeField]
    [Tooltip("面板标识符，与 PanelConfig 表中的 PanelId 对应")]
    private string _panelId = string.Empty;

    public string PanelId => _panelId;
    public IUIService UIService { get; set; }
    public IResourceService ResourceService { get; set; }
    public ServiceContainer ServiceContainer { get; set; }

    protected bool IsOpening { get; private set; }
    protected bool IsClosing { get; private set; }

    /// <summary>面板启用时调用（子类重写以订阅事件）。</summary>
    protected virtual void OnEnable() { }

    /// <summary>面板禁用时调用（子类重写以取消订阅）。</summary>
    protected virtual void OnDisable() { }

    public virtual UniTask OnOpenAsync(object param = null)
    {
        IsOpening = true;
        gameObject.SetActive(true);
        IsOpening = false;
        return UniTask.CompletedTask;
    }

    public virtual UniTask OnCloseAsync()
    {
        IsClosing = true;
        gameObject.SetActive(false);
        IsClosing = false;
        return UniTask.CompletedTask;
    }

    public virtual void OnBack() { UIService?.GoBackAsync().Forget(); }
    public virtual void OnHome() { UIService?.GoHomeAsync().Forget(); }
}
