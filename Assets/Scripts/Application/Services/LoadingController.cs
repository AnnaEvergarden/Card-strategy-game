using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 加载控制器：桥接启动阶段/场景加载阶段的事件与 LoadingPanel 的 UI 显示。
///
/// 职责：
/// - GameBootstrap 阶段：通过 StartBootstrap() 打开 LoadingPanel，订阅 BootstrapProgressEvent
/// - 场景加载阶段：订阅 ISceneService 的 OnLoadingStarted / OnLoadingProgress / OnLoadingCompleted
/// - 生命周期管理：自动打开/关闭/更新 LoadingPanel
///
/// 进度分配：
///   0.00~0.85  Bootstrap 阶段（GameBootstrap 驱动）
///   0.85~1.00  场景加载阶段（SceneService 驱动）
/// </summary>
public class LoadingController : IDisposable
{
    private readonly ISceneService _sceneService;
    private readonly IUIService _uiService;
    private LoadingPanel _loadingPanel;
    private bool _bootstrapStarted;
    private bool _disposed;

    public LoadingController(ISceneService sceneService, IUIService uiService)
    {
        _sceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
        _uiService = uiService;

        _sceneService.OnLoadingStarted += OnLoadingStarted;
        _sceneService.OnLoadingProgress += OnLoadingProgress;
        _sceneService.OnLoadingCompleted += OnLoadingCompleted;
    }

    // ════════════════════════════════════════════════════════════════
    // Bootstrap 阶段
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 启动加载面板：在 GameBootstrap 阶段打开 LoadingPanel，
    /// 并接收 GameBootstrap 的进度更新。
    /// 应在所有全局服务创建完成后调用（Phase 1 末尾）。
    /// </summary>
    public async void StartBootstrap()
    {
        if (_disposed || _bootstrapStarted) return;
        _bootstrapStarted = true;

        try
        {
            _loadingPanel = await _uiService.OpenAsync<LoadingPanel>("Loading");
            if (_loadingPanel != null)
                _loadingPanel.UpdateProgress(0f, "正在准备...");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LoadingController] 打开 LoadingPanel 失败: {ex.Message}");
            _loadingPanel = null;
        }


    }

    /// <summary>
    /// GameBootstrap 各阶段完成后调用，推送进度和状态文字。
    /// </summary>
    public void ReportBootstrapProgress(float progress, string message)
    {
        if (_disposed || _loadingPanel == null) return;
        _loadingPanel.UpdateProgress(progress, message);
    }

    // ════════════════════════════════════════════════════════════════
    // 场景加载阶段
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 加载开始时打开 LoadingPanel（兜底——如果 bootstrap 已打开则跳过）。
    /// </summary>
    private async void OnLoadingStarted(SceneType sceneType, string taskDescription)
    {
        if (_disposed) return;

        try
        {
            if (_loadingPanel == null)
            {
                _loadingPanel = await _uiService.OpenAsync<LoadingPanel>("Loading");
            }
            if (_loadingPanel != null)
                _loadingPanel.SetTaskDescription(taskDescription);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LoadingController] 打开 LoadingPanel 失败: {ex.Message}");
            _loadingPanel = null;
        }
    }

    /// <summary>
    /// 加载进度更新时刷新 LoadingPanel。
    /// </summary>
    private void OnLoadingProgress(SceneType sceneType, float progress, string taskDescription)
    {
        if (_disposed || _loadingPanel == null) return;
        _loadingPanel.UpdateProgress(progress, taskDescription);
    }

    /// <summary>
    /// 加载完成时关闭 LoadingPanel。
    /// </summary>
    private async void OnLoadingCompleted(SceneType sceneType)
    {
        if (_disposed) return;

        try
        {
            _loadingPanel?.UpdateProgress(1f, "加载完成，进入游戏...");
            await UniTask.Delay(TimeSpan.FromSeconds(0.3f));

            await _uiService.CloseAsync("Loading");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LoadingController] 关闭 LoadingPanel 失败: {ex.Message}");
        }
        finally
        {
            _loadingPanel = null;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sceneService.OnLoadingStarted -= OnLoadingStarted;
        _sceneService.OnLoadingProgress -= OnLoadingProgress;
        _sceneService.OnLoadingCompleted -= OnLoadingCompleted;



        _loadingPanel = null;
    }
}

