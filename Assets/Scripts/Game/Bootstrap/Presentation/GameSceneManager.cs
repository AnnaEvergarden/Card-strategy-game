using UnityEngine;

/// <summary>
/// 游戏场景管理器：进入场景时初始化默认显示主界面。
/// </summary>
public sealed class GameSceneManager : MonoBehaviour
{
    #region Unity Lifecycle

    /// <summary>
    /// 初始化场景默认面板状态。
    /// </summary>
    private void Start()
    {
        UIPanelRegistry.EnsureDefaultStackForActiveScene();
        TryApplyBattleReturnNav();
        ShipyardPanel.EnsureInstance();
        FleetPanel.EnsureInstance();
        FleetPickPanel.EnsureInstance();
        BuildPanel.EnsureInstance();
        CardRevealPanel.EnsureInstance();
        LevelSelectPanel.EnsureInstance();
        LevelAreaSelectPanel.EnsureInstance();
        LevelStageSelectPanel.EnsureInstance();
        UIPanelRegistry.Hide(PanelNames.InventoryPanel);
        UIPanelRegistry.Hide(PanelNames.ShipyardPanel);
        UIPanelRegistry.Hide(PanelNames.FleetPanel);
        UIPanelRegistry.Hide(PanelNames.FleetPickPanel);
        UIPanelRegistry.Hide(PanelNames.BuildPanel);
        UIPanelRegistry.Hide(PanelNames.CardRevealPanel);
        UIPanelRegistry.Hide(PanelNames.ActivityPanel);
        UIPanelRegistry.Hide(PanelNames.LevelSelectPanel);
        UIPanelRegistry.Hide(PanelNames.LevelAreaSelectPanel);
        UIPanelRegistry.Hide(PanelNames.LevelStageSelectPanel);
    }

    /// <summary>
    /// 从战斗结算返回时：打开选关 → 区域 → 关卡列表（与进入战斗前一致）。
    /// </summary>
    private static void TryApplyBattleReturnNav()
    {
        if (!BattleStartContext.TryConsumeBattleReturnNav(out var mode, out var area))
        {
            return;
        }

        var levelSelect = LevelSelectPanel.EnsureInstance();
        if (levelSelect == null)
        {
            return;
        }

        UIPanelRegistry.Push(PanelNames.LevelSelectPanel);
        var areaPanel = LevelAreaSelectPanel.EnsureInstance();
        if (areaPanel != null)
        {
            areaPanel.OpenWithMode(mode);
            UIPanelRegistry.Push(PanelNames.LevelAreaSelectPanel);
        }

        var stagePanel = LevelStageSelectPanel.EnsureInstance();
        if (stagePanel != null && area != null)
        {
            stagePanel.SetContext(mode, area);
            UIPanelRegistry.Push(PanelNames.LevelStageSelectPanel);
        }
    }

    #endregion
}

