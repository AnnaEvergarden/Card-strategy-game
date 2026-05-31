using UnityEngine;

/// <summary>
/// 战斗场景启动补偿：在部分加载顺序下确保 UI 栈落到卡组选择面板。
/// </summary>
public sealed class BattleSceneBootstrap : MonoBehaviour
{
    #region Unity Lifecycle

    /// <summary>
    /// 首帧前尝试应用当前场景的默认面板栈。
    /// </summary>
    private void Start()
    {
        SkillDefinitionRegistry.EnsureDefaultDefinitionsRegistered();
        EnsureSkillEventPresenter();
        UIPanelRegistry.EnsureDefaultStackForActiveScene();
        BattleSlotActionMenuPanel.EnsureInstance();
        BattleSkillSelectPanel.EnsureInstance();
        BattleCardSwitchPanel.EnsureInstance();
        BattleEmojiPanel.EnsureInstance();
        BattleSettlementPanel.EnsureInstance();
        UIPanelRegistry.Hide(PanelNames.BattleSlotActionMenuPanel);
        UIPanelRegistry.Hide(PanelNames.BattleSkillSelectPanel);
        UIPanelRegistry.Hide(PanelNames.BattleCardSwitchPanel);
        UIPanelRegistry.Hide(PanelNames.BattleEmojiPanel);
        UIPanelRegistry.Hide(PanelNames.BattleSettlementPanel);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 确保战斗场景存在技能事件表现桥接（无则挂到本物体）。
    /// </summary>
    private void EnsureSkillEventPresenter()
    {
        if (GetComponent<BattleSkillEventPresenter>() != null)
        {
            return;
        }

        gameObject.AddComponent<BattleSkillEventPresenter>();
    }

    #endregion
}
