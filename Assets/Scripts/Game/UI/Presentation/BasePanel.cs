using UnityEngine;

/// <summary>
/// 面板基类：统一管理面板在启用和禁用时的注册与注销。
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 面板名称（用于字典索引和面板切换）。
    /// 字段名必须叫 PanelName：TitleScene 已序列化依赖它。
    /// </summary>
    public string PanelName;

    /// <summary>
    /// 是否为场景初始默认打开面板。
    /// 勾选则注册后保持显示；不勾选则注册后自动隐藏。
    /// </summary>
    [SerializeField] private bool defaultOpenOnSceneStart = false;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 实例化时注册到面板注册表（未激活的面板也能被 Show 找到）。
    /// </summary>
    protected virtual void Awake()
    {
        UIPanelRegistry.Register(this);
        if (!defaultOpenOnSceneStart && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 子类可重写以在启用时订阅事件（注册仍在 Awake 完成）。
    /// </summary>
    protected virtual void OnEnable()
    {
    }

    /// <summary>
    /// 子类可重写以在禁用时取消订阅。
    /// </summary>
    protected virtual void OnDisable()
    {
    }

    /// <summary>
    /// 销毁时从面板注册表移除。
    /// </summary>
    protected virtual void OnDestroy()
    {
        UIPanelRegistry.Unregister(this);
    }

    #endregion
}

