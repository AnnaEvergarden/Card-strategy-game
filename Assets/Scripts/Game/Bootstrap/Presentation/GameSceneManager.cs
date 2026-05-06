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
        ShipyardPanel.EnsureInstance();
        FleetPanel.EnsureInstance();
        BuildPanel.EnsureInstance();
        CardRevealPanel.EnsureInstance();
        LevelSelectPanel.EnsureInstance();
        LevelAreaSelectPanel.EnsureInstance();
        LevelStageSelectPanel.EnsureInstance();
        UIPanelRegistry.Hide(PanelNames.InventoryPanel);
        UIPanelRegistry.Hide(PanelNames.ShipyardPanel);
        UIPanelRegistry.Hide(PanelNames.FleetPanel);
        UIPanelRegistry.Hide(PanelNames.BuildPanel);
        UIPanelRegistry.Hide(PanelNames.CardRevealPanel);
        UIPanelRegistry.Hide(PanelNames.ActivityPanel);
        UIPanelRegistry.Hide(PanelNames.LevelSelectPanel);
        UIPanelRegistry.Hide(PanelNames.LevelAreaSelectPanel);
        UIPanelRegistry.Hide(PanelNames.LevelStageSelectPanel);
    }

    #endregion
}

