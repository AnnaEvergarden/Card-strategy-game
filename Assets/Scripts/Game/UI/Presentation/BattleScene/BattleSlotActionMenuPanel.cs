using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 舰娘槽位操作小面板：释放技能、切换卡牌、发送表情。
/// </summary>
public sealed class BattleSlotActionMenuPanel : BattleOverlayPanelBase
{
    #region Fields

    /// <summary>
    /// 释放技能按钮。
    /// </summary>
    [SerializeField] private Button castSkillBtn;

    /// <summary>
    /// 切换卡牌按钮。
    /// </summary>
    [SerializeField] private Button switchCardBtn;

    /// <summary>
    /// 发送表情按钮。
    /// </summary>
    [SerializeField] private Button emojiBtn;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景实例。
    /// </summary>
    public static BattleSlotActionMenuPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<BattleSlotActionMenuPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.BattleSlotActionMenuPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "BattleSlotActionMenuPanel: 场景中未找到该面板，请在 BattleScene UI 下挂载并设置 PanelName。");
        return null;
    }

    /// <summary>
    /// 设置操作目标舰娘（写入 <see cref="BattleUiSession"/>）。
    /// </summary>
    public void SetTargetUnit(string unitId)
    {
        BattleUiSession.SetFocusUnit(unitId);
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
            PanelName = PanelNames.BattleSlotActionMenuPanel;
        }

        base.Awake();
    }

    /// <summary>
    /// 订阅功能按钮。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        var owner = BattleUiSession.ActionOwnerUnitId;
        if (!string.IsNullOrEmpty(owner) && !BattleContext.Current.Turns.HasActedThisRound(owner))
        {
            BattleMainPanel.EnsureInstance()?.ClearSlotHighlightIfOwner(owner);
        }

        base.OnDisable();
    }

    #endregion

    #region Private Methods

    private void SubscribeButtons()
    {
        if (castSkillBtn != null) castSkillBtn.onClick.AddListener(OnClickCastSkill);
        if (switchCardBtn != null) switchCardBtn.onClick.AddListener(OnClickSwitchCard);
        if (emojiBtn != null) emojiBtn.onClick.AddListener(OnClickEmoji);
    }

    private void UnsubscribeButtons()
    {
        if (castSkillBtn != null) castSkillBtn.onClick.RemoveListener(OnClickCastSkill);
        if (switchCardBtn != null) switchCardBtn.onClick.RemoveListener(OnClickSwitchCard);
        if (emojiBtn != null) emojiBtn.onClick.RemoveListener(OnClickEmoji);
    }

    private void OnClickCastSkill()
    {
        BattleUiFlow.OpenSkillSelectPanel();
    }

    private void OnClickSwitchCard()
    {
        BattleUiFlow.OpenCardSwitchPanel();
    }

    private void OnClickEmoji()
    {
        BattleUiFlow.OpenEmojiPanel();
    }

    #endregion
}
