using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗场景叠层面板基类：提供统一关闭（弹栈）按钮。
/// </summary>
public abstract class BattleOverlayPanelBase : BasePanel
{
    #region Fields

    /// <summary>
    /// 关闭并弹栈按钮（可选）。
    /// </summary>
    [SerializeField] protected Button closeBtn;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 订阅关闭按钮。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(OnClickClose);
        }
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    protected override void OnDisable()
    {
        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveListener(OnClickClose);
        }

        base.OnDisable();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// 关闭当前叠层（弹栈）。
    /// </summary>
    protected virtual void OnClickClose()
    {
        UIPanelRegistry.TryPop();
    }

    #endregion
}
