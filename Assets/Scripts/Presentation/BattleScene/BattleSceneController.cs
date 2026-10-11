using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 战斗场景控制器：管理战斗场景的进入初始化与退出清理。
/// 挂载在 BattleScene 根物体上，通过 SceneService 的注册机制管理生命周期。
///
/// 进入时：
/// - 从 BattleEntryService 获取战斗上下文
/// - 打开默认面板 BattleDeckSelect
/// - 创建战斗环境
///
/// 退出时：
/// - 清理战斗对象、保存数据
/// </summary>
public class BattleSceneController : MonoBehaviour, ISceneController
{
    private const SceneType ThisSceneType = SceneType.Battle;

    /// <summary>战斗场景默认面板（配置缺失/为空时的兜底，保证"选卡组 → 选出战 → 战斗"流程不中断）。</summary>
    private const string DefaultPanelIdFallback = "BattleDeckSelect";

    private void Awake()
    {
        GameBootstrap.Container?.Resolve<ISceneService>()?.RegisterController(this);
    }

    private void OnDestroy()
    {
        GameBootstrap.Container?.Resolve<ISceneService>()?.UnregisterController(this);
    }

    /// <summary>
    /// 场景初始化：打开默认面板（BattleDeckSelect）。
    /// 战斗会话由 BattleMainPanel 打开时经 IBattleService.StartBattleAsync 启动
    /// （需先完成编队选择与首发选择，上下文就绪后才开局）。
    /// </summary>
    public async UniTask InitializeAsync()
    {
        Debug.Log("[BattleSceneController] 初始化战斗场景...");

        // ── 1. 打开默认面板 ──
        await OpenDefaultPanelAsync();

        // ── 2. 获取战斗上下文（BattleEntryService 在关卡选择时写入）──
        var context = GameBootstrap.Container?.Resolve<BattleEntryService>()?.CurrentContext;
        if (context != null)
        {
            Debug.Log($"[BattleSceneController] 战斗上下文: StageId={context.StageId}, Mode={context.Mode}");
        }
    }

    /// <summary>
    /// 场景清理：释放战斗会话（事件订阅与领域对象）。
    /// </summary>
    public async UniTask DisposeAsync()
    {
        Debug.Log("[BattleSceneController] 清理战斗场景...");
        GameBootstrap.Container?.TryResolve<IBattleService>()?.ClearSession();
        await UniTask.CompletedTask;
    }

    private async UniTask OpenDefaultPanelAsync()
    {
        var container = GameBootstrap.Container;
        if (container == null) return;

        var config = container.Resolve<IConfigService>()
            ?.Get<SceneConfig>(((int)ThisSceneType).ToString());
        var panelId = config?.DefaultPanelId;

        // 配置表未提供默认面板时兜底为"选卡组"面板，避免进入战斗场景后无任何界面。
        if (string.IsNullOrEmpty(panelId))
            panelId = DefaultPanelIdFallback;
        Debug.Log($"[BattleSceneController] 默认面板: {panelId}");

        if (!string.IsNullOrEmpty(panelId))
        {
            var uiService = container.Resolve<IUIService>();
            if (uiService != null) await uiService.OpenAsync<PanelBase>(panelId);
        }
    }
}
