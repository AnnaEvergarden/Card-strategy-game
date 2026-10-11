using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景服务实现：通过 Addressables 加载场景，使用 SceneConfig 配置表驱动。
///
/// 职责：
/// - 通过 SceneType 查询 SceneConfig 获取 Addressables 地址并加载
/// - 管理场景加载生命周期（事件驱动，不引用具体 UI）
/// - 管理 SceneController 的注册、初始化与释放
/// </summary>
public class SceneService : ISceneService, IDisposable
{
    private IConfigService _configService;
    private IResourceService _resourceService;

    private SceneType _currentSceneType = SceneType.None;
    private SceneInstance _currentSceneInstance;
    private bool _isLoading;
    private float _loadingProgress;
    private bool _disposed;

    // SceneController 列表（通过 Register/Unregister 管理）
    private readonly List<ISceneController> _controllers = new List<ISceneController>();
    private readonly object _controllerLock = new object();

    public SceneType CurrentSceneType => _currentSceneType;
    public bool IsLoading => _isLoading;
    public float LoadingProgress => _loadingProgress;

    // ── 场景生命周期事件 ──
    public event Action<SceneType> OnSceneLoaded;
    public event Action<SceneType> OnSceneUnloading;

    // ── 加载进度事件 ──
    public event Action<SceneType, string> OnLoadingStarted;
    public event Action<SceneType, float, string> OnLoadingProgress;
    public event Action<SceneType> OnLoadingCompleted;

    public SceneService(IConfigService configService = null, IResourceService resourceService = null)
    {
        _configService = configService;
        _resourceService = resourceService;
        _currentSceneType = ResolveSceneTypeFromActiveScene();
    }

    public void SetConfigService(IConfigService configService)
    {
        if (configService == null) return;
        _configService = configService;
        if (_currentSceneType == SceneType.None)
        {
            _currentSceneType = ResolveSceneTypeFromActiveScene();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // SceneController 注册机制
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 注册场景控制器（由 SceneController.Awake 调用）。
    /// </summary>
    public void RegisterController(ISceneController controller)
    {
        if (_disposed || controller == null) return;

        lock (_controllerLock)
        {
            if (!_controllers.Contains(controller))
            {
                _controllers.Add(controller);
                Debug.Log($"[SceneService] SceneController 注册: {controller.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// 注销场景控制器（由 SceneController.OnDestroy 调用）。幂等操作。
    /// </summary>
    public void UnregisterController(ISceneController controller)
    {
        if (_disposed || controller == null) return;

        lock (_controllerLock)
        {
            if (_controllers.Remove(controller))
            {
                Debug.Log($"[SceneService] SceneController 注销: {controller.GetType().Name}");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 公开接口
    // ════════════════════════════════════════════════════════════════

    public async UniTask LoadSceneAsync(SceneType sceneType, object context = null, IProgress<float> progress = null)
    {
        if (_disposed) return;
        if (sceneType == SceneType.None)
        {
            Debug.LogError("[SceneService] 无法加载 SceneType.None");
            return;
        }
        if (_isLoading)
        {
            Debug.LogWarning($"[SceneService] 正在加载中，忽略重复请求（当前加载: {sceneType})");
            return;
        }

        _isLoading = true;
        _loadingProgress = 0f;

        try
        {
            // ── 步骤 1：通知加载开始 ──
            string taskDesc = "正在准备...";
            OnLoadingStarted?.Invoke(sceneType, taskDesc);
            ReportProgress(0f, sceneType, taskDesc, progress);

            // ── 步骤 2：释放旧场景（控制器 + 资源） ──
            taskDesc = "正在释放旧场景资源...";
            if (_currentSceneType != SceneType.None)
            {
                var prevConfig = QuerySceneConfig(_currentSceneType);
                if (prevConfig is { NeedReleaseOldScene: true })
                {
                    Debug.Log($"[SceneService] 释放旧场景: {_currentSceneType}");
                    OnSceneUnloading?.Invoke(_currentSceneType);
                    await DisposeControllersAsync();
                    await ReleaseSceneResourcesAsync();
                }
            }
            ReportProgress(0.15f, sceneType, taskDesc, progress);

            // ── 步骤 3：查询场景配置 ──
            taskDesc = "正在检查场景配置...";
            var sceneConfig = QuerySceneConfig(sceneType);
            if (sceneConfig == null)
            {
                Debug.LogError($"[SceneService] 未找到 SceneType={sceneType} 的配置");
                return;
            }
            var sceneAddress = sceneConfig.SceneAddress;
            if (string.IsNullOrWhiteSpace(sceneAddress))
            {
                Debug.LogError($"[SceneService] SceneType={sceneType} 的 SceneAddress 为空");
                return;
            }
            Debug.Log($"[SceneService] 开始加载场景: {sceneType} -> {sceneAddress}");

            // ── 步骤 4：检查依赖 ──
            taskDesc = "正在检查资源依赖...";
            ReportProgress(0.2f, sceneType, taskDesc, progress);
            await CheckAndDownloadDependenciesAsync(sceneAddress);

            // ── 步骤 5：加载场景（场景 Awake 中会注册 SceneController） ──
            taskDesc = $"正在加载场景: {sceneType}...";
            ReportProgress(0.35f, sceneType, taskDesc, progress);
            
            // [P2 FIX] 清除旧场景实例引用，避免异常路径下持有已卸载场景指针
            _currentSceneInstance = default;
            
            var loadHandle = Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Single, activateOnLoad: true);

            while (!loadHandle.IsDone)
            {
                _loadingProgress = 0.35f + loadHandle.PercentComplete * 0.55f;
                ReportProgress(_loadingProgress, sceneType, taskDesc, progress);
                await UniTask.Yield();
            }

            _currentSceneInstance = await loadHandle.ToUniTask();
            // ── 步骤 6：等待场景初始化（调用 SceneController.InitializeAsync） ──
            taskDesc = "正在初始化场景...";
            ReportProgress(0.9f, sceneType, taskDesc, progress);
            await WaitForSceneReadyAsync(context);

            // 更新当前场景
            _currentSceneType = sceneType;
            ReportProgress(1f, sceneType, "加载完成", progress);

            OnSceneLoaded?.Invoke(sceneType);
            OnLoadingCompleted?.Invoke(sceneType);
            Debug.Log($"[SceneService] 场景加载完成: {sceneType}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneService] 场景加载失败 {sceneType}: {ex.Message}\n{ex.StackTrace}");
            OnLoadingCompleted?.Invoke(sceneType);
        }
        finally
        {
            _isLoading = false;
            _loadingProgress = 0f;
        }
    }

    public async UniTask UnloadCurrentSceneAsync()
    {
        if (_disposed) return;
        if (_currentSceneType == SceneType.None)
        {
            Debug.LogWarning("[SceneService] 当前无场景可卸载");
            return;
        }

        var unloadingType = _currentSceneType;
        OnSceneUnloading?.Invoke(unloadingType);

        // 先释放所有 SceneController
        await DisposeControllersAsync();

        if (_currentSceneInstance.Scene.isLoaded)
        {
            Debug.Log($"[SceneService] 卸载场景: {unloadingType}");
            await Addressables.UnloadSceneAsync(_currentSceneInstance).ToUniTask();
        }

        _currentSceneType = SceneType.None;
        Debug.Log($"[SceneService] 场景已卸载: {unloadingType}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_controllerLock)
        {
            _controllers.Clear();
        }

        OnSceneLoaded = null;
        OnSceneUnloading = null;
        OnLoadingStarted = null;
        OnLoadingProgress = null;
        OnLoadingCompleted = null;
    }

    // ════════════════════════════════════════════════════════════════
    // SceneController 生命周期
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 调用所有已注册 SceneController 的 InitializeAsync。
    /// 在场景就绪后由 WaitForSceneReadyAsync 调用。
    /// </summary>
    private async UniTask InitializeControllersAsync()
    {
        ISceneController[] snapshot;
        lock (_controllerLock)
        {
            if (_controllers.Count == 0) return;
            snapshot = _controllers.ToArray();
        }

        foreach (var controller in snapshot)
        {
            try
            {
                await controller.InitializeAsync();
                Debug.Log($"[SceneService] SceneController 初始化完成: {controller.GetType().Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneService] SceneController 初始化失败 {controller.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 调用所有已注册 SceneController 的 DisposeAsync 并清空列表。
    /// 在场景切换或卸载前调用。
    /// </summary>
    private async UniTask DisposeControllersAsync()
    {
        ISceneController[] snapshot;
        lock (_controllerLock)
        {
            if (_controllers.Count == 0) return;
            snapshot = _controllers.ToArray();
            _controllers.Clear();
        }

        foreach (var controller in snapshot)
        {
            try
            {
                await controller.DisposeAsync();
                Debug.Log($"[SceneService] SceneController 释放完成: {controller.GetType().Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneService] SceneController 释放失败 {controller.GetType().Name}: {ex.Message}");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 私有辅助方法
    // ════════════════════════════════════════════════════════════════

    private SceneConfig QuerySceneConfig(SceneType sceneType)
    {
        if (_configService == null) return null;
        return _configService.Get<SceneConfig>(((int)sceneType).ToString());
    }

    private SceneType ResolveSceneTypeFromActiveScene()
    {
        try
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.path))
                return SceneType.None;

            var allScenes = _configService?.GetAll<SceneConfig>();
            if (allScenes == null) return SceneType.None;

            foreach (var config in allScenes)
            {
                if (config == null || string.IsNullOrEmpty(config.SceneAddress)) continue;
                if (activeScene.path.EndsWith(config.SceneAddress, StringComparison.OrdinalIgnoreCase))
                    return config.SceneType;
                var sceneName = ExtractSceneName(config.SceneAddress);
                if (!string.IsNullOrEmpty(sceneName) &&
                    string.Equals(activeScene.name, sceneName, StringComparison.OrdinalIgnoreCase))
                    return config.SceneType;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SceneService] 推断当前 SceneType 时发生异常: {ex.Message}");
        }
        return SceneType.None;
    }

    private static string ExtractSceneName(string address)
    {
        if (string.IsNullOrEmpty(address)) return string.Empty;
        int lastSlash = address.LastIndexOf('/');
        int start = lastSlash + 1;
        int dot = address.LastIndexOf('.');
        if (dot < 0) dot = address.Length;
        return address.Substring(start, dot - start);
    }

    private static async UniTask CheckAndDownloadDependenciesAsync(string address)
    {
        if (string.IsNullOrEmpty(address)) return;
        try
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(address);
            long downloadSize = await sizeHandle.ToUniTask();
            Addressables.Release(sizeHandle);
            if (downloadSize > 0)
            {
                Debug.Log($"[SceneService] 需要下载依赖资源: {downloadSize} bytes");
                var downloadHandle = Addressables.DownloadDependenciesAsync(address);
                await downloadHandle.ToUniTask();
                Addressables.Release(downloadHandle);
                Debug.Log("[SceneService] 依赖资源下载完成");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SceneService] 检查/下载依赖时发生异常（不影响加载）: {ex.Message}");
        }
    }

    private static async UniTask ReleaseSceneResourcesAsync()
    {
        await Resources.UnloadUnusedAssets();
        await UniTask.Yield();
    }

    /// <summary>
    /// 等待场景就绪并执行初始化。
    /// 1. 先调用已注册 SceneController 的 InitializeAsync
    /// 2. 向后兼容：查找 ISceneBootstrap 并调用 OnSceneEnterAsync
    /// </summary>
    private async UniTask WaitForSceneReadyAsync(object context)
    {
        // 等待一帧确保场景完全激活
        await UniTask.Yield();

        // 调用所有已注册 SceneController 的 InitializeAsync
        await InitializeControllersAsync();

        // [向后兼容] 查找并调用旧的 ISceneBootstrap
        try
        {
            var sceneRoots = _currentSceneInstance.Scene.GetRootGameObjects();
            if (sceneRoots == null || sceneRoots.Length == 0) return;

            foreach (var root in sceneRoots)
            {
                if (root == null) continue;
                var bootstrap = root.GetComponent<ISceneBootstrap>();
                if (bootstrap != null)
                {
                    await bootstrap.OnSceneEnterAsync(context);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SceneService] 场景 ISceneBootstrap 初始化异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 统一报告加载进度。
    /// </summary>
    private void ReportProgress(float value, SceneType sceneType, string taskDescription, IProgress<float> progress)
    {
        _loadingProgress = Mathf.Clamp01(value);
        progress?.Report(_loadingProgress);
        OnLoadingProgress?.Invoke(sceneType, _loadingProgress, taskDescription);
    }
}
