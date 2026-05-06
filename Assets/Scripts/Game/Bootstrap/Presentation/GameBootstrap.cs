using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏启动器：确保运行时从标题场景进入。
/// </summary>
public sealed class GameBootstrap : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 强制进入的起始场景名。
    /// </summary>
    [SerializeField] private string _forceEntrySceneName = "TitleScene";

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 初始化启动流程并校正当前场景。
    /// </summary>
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 确保从任意场景运行都回到 Title。
        if (!string.IsNullOrWhiteSpace(_forceEntrySceneName) &&
            !string.Equals(SceneManager.GetActiveScene().name, _forceEntrySceneName))
        {
            SceneManager.LoadScene(_forceEntrySceneName);
        }
    }

    /// <summary>
    /// 应用退出时自动保存本地数据。
    /// </summary>
    private void OnApplicationQuit()
    {
        GameDataSaveService.SaveAll();
    }

    /// <summary>
    /// 应用切到后台时自动保存一次，降低异常退出丢档风险。
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            GameDataSaveService.SaveAll();
        }
    }

    #endregion
}

