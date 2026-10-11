using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 主界面场景控制器：管理 Lobby 场景的进入初始化。
/// 挂载在 LobbyScene 根物体上。
///
/// 进入时打开默认面板 Lobby。
/// </summary>
public class LobbySceneController : MonoBehaviour, ISceneController
{
    private const SceneType ThisSceneType = SceneType.Lobby;

    private void Awake()
    {
        GameBootstrap.Container?.Resolve<ISceneService>()?.RegisterController(this);
    }

    private void OnDestroy()
    {
        GameBootstrap.Container?.Resolve<ISceneService>()?.UnregisterController(this);
    }

    public async UniTask InitializeAsync()
    {
        Debug.Log("[LobbySceneController] 初始化主界面场景...");

        var container = GameBootstrap.Container;
        if (container == null) return;

        var config = container.Resolve<IConfigService>()
            ?.Get<SceneConfig>(((int)ThisSceneType).ToString());
        var panelId = config?.DefaultPanelId;

        if (!string.IsNullOrEmpty(panelId))
        {
            var uiService = container.Resolve<IUIService>();
            if (uiService != null) await uiService.OpenAsync<PanelBase>(panelId);
        }
    }

    public async UniTask DisposeAsync()
    {
        Debug.Log("[LobbySceneController] 清理主界面场景...");
        await UniTask.CompletedTask;
    }
}
