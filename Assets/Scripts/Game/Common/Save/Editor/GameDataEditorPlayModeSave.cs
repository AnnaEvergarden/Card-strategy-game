#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 编辑器 Play 模式结束时自动保存本地数据，避免仅改内存未走 Quit 导致丢档。
/// </summary>
[InitializeOnLoad]
public static class GameDataEditorPlayModeSave
{
    #region Constructors

    /// <summary>
    /// 注册 Play 模式状态监听。
    /// </summary>
    static GameDataEditorPlayModeSave()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 退出 Play 前落盘全部本地数据。
    /// </summary>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            GameDataSaveService.SaveAll();
        }
    }

    #endregion
}
#endif
