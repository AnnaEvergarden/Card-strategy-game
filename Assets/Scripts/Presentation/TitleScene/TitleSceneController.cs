using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 登录/标题场景控制器：管理 Title/Login 场景的进入初始化。
/// 挂载在 TitleScene 根物体上。
///
/// 进入时打开默认面板 Title。
/// </summary>
public class TitleSceneController : MonoBehaviour, ISceneController
{
    private const SceneType ThisSceneType = SceneType.Title;

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
        Debug.Log("[TitleSceneController] 初始化标题场景...");

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
        Debug.Log("[TitleSceneController] 清理标题场景...");
        await UniTask.CompletedTask;
    }
}
