using UnityEngine;

/// <summary>
/// 战斗场景 UI 导航：打开槽位菜单、子功能面板与结算面板。
/// </summary>
public static class BattleUiFlow
{
    #region Fields

    /// <summary>
    /// 战斗主界面之上的叠层面板（互斥显示；弹出时保留 <see cref="PanelNames.BattleMainPanel"/> 等栈底）。
    /// </summary>
    public static readonly string[] BattleOverlayPanelNames =
    {
        PanelNames.BattleSlotActionMenuPanel,
        PanelNames.BattleSkillSelectPanel,
        PanelNames.BattleCardSwitchPanel,
        PanelNames.BattleEmojiPanel
    };

    #endregion

    #region Public API

    /// <summary>
    /// 打开舰娘槽位操作小面板。
    /// </summary>
    public static void OpenSlotActionMenu(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            Debug.LogWarning("BattleUiFlow: unitId 为空，无法打开操作菜单。");
            return;
        }

        unitId = unitId.Trim();
        if (!BattleContext.Current.Turns.TrySelectCardForAction(unitId, out var reason))
        {
            Debug.LogWarning($"BattleUiFlow: {reason}");
            return;
        }

        BattleUiSession.SetFocusUnit(unitId);
        BattleUiSession.SetActionOwnerUnit(unitId);
        var main = BattleMainPanel.EnsureInstance();
        main?.HighlightSlotForAction(unitId);

        var panel = BattleSlotActionMenuPanel.EnsureInstance();
        if (panel == null)
        {
            return;
        }

        panel.SetTargetUnit(unitId);
        ShowBattleOverlay(PanelNames.BattleSlotActionMenuPanel);
    }

    /// <summary>
    /// 打开技能选择面板（需已设置 <see cref="BattleUiSession.FocusUnitId"/>）。
    /// </summary>
    public static void OpenSkillSelectPanel()
    {
        if (string.IsNullOrWhiteSpace(BattleUiSession.FocusUnitId))
        {
            Debug.LogWarning("BattleUiFlow: 未设置焦点舰娘，无法打开技能面板。");
            return;
        }

        if (BattleSkillSelectPanel.EnsureInstance() == null)
        {
            return;
        }

        ShowBattleOverlay(PanelNames.BattleSkillSelectPanel);
    }

    /// <summary>
    /// 打开卡牌切换面板（待完善战斗逻辑）。
    /// </summary>
    public static void OpenCardSwitchPanel()
    {
        if (BattleCardSwitchPanel.EnsureInstance() == null)
        {
            return;
        }

        ShowBattleOverlay(PanelNames.BattleCardSwitchPanel);
    }

    /// <summary>
    /// 打开发送表情面板（占位，待实现具体表情逻辑）。
    /// </summary>
    public static void OpenEmojiPanel()
    {
        if (BattleEmojiPanel.EnsureInstance() == null)
        {
            return;
        }

        ShowBattleOverlay(PanelNames.BattleEmojiPanel);
    }

    /// <summary>
    /// 打开结算面板并记录结算类型。
    /// </summary>
    /// <param name="kind">胜利 / 失败 / 放弃。</param>
    public static void OpenSettlement(BattleSettlementKind kind)
    {
        BattleStartContext.SetLastSettlement(kind);
        var panel = BattleSettlementPanel.EnsureInstance();
        if (panel == null)
        {
            return;
        }

        panel.ApplySettlement(kind);
        UIPanelRegistry.ClearAndPush(PanelNames.BattleSettlementPanel);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 显示战斗叠层：先关闭栈顶其它叠层（含操作菜单），再 Push 目标；栈底主界面保持显示。
    /// </summary>
    private static void ShowBattleOverlay(string panelName)
    {
        var key = (panelName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (UIPanelRegistry.PeekStackTop() == key)
        {
            UIPanelRegistry.Push(key);
            return;
        }

        UIPanelRegistry.PopWhileTopIsAny(BattleOverlayPanelNames);
        UIPanelRegistry.Push(key);
    }

    #endregion
}
