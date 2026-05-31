using UnityEngine;

/// <summary>
/// 战斗表情面板（表情发送逻辑待实现）。
/// </summary>
public sealed class BattleEmojiPanel : BattleOverlayPanelBase
{
    #region Public API

    /// <summary>
    /// 查找场景实例。
    /// </summary>
    public static BattleEmojiPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<BattleEmojiPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.BattleEmojiPanel;
            }

            return existing;
        }

        Debug.LogWarning("BattleEmojiPanel: 场景中未找到该面板，请在 BattleScene UI 下挂载。");
        return null;
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 补全面板名。
    /// </summary>
    protected override void Awake()
    {
        if (string.IsNullOrWhiteSpace(PanelName))
        {
            PanelName = PanelNames.BattleEmojiPanel;
        }

        base.Awake();
    }

    #endregion
}
