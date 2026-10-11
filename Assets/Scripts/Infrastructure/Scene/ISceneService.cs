using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 场景服务接口：统一管理所有场景的异步加载、卸载与生命周期。
/// 所有场景切换必须通过此接口，禁止直接调用 SceneManager.LoadScene 或硬编码场景名称。
/// 通过 SceneType 标识场景，内部查询 SceneConfig 配置表获取 Addressables 地址进行加载。
///
/// 加载进度通过以下机制通知外部：
/// - IProgress&lt;float&gt; 参数：供直接调用方获取简单进度回调
/// - OnLoadingStarted / OnLoadingProgress / OnLoadingCompleted 事件：供 UI 层订阅以驱动 LoadingPanel
/// SceneService 不直接引用任何具体 UI，通过事件与 UI 层解耦。
/// </summary>
public interface ISceneService
{
    /// <summary>
    /// 当前激活的场景类型。
    /// </summary>
    SceneType CurrentSceneType { get; }

    /// <summary>
    /// 当前是否正在执行场景加载。
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// 当前加载进度（0～1），仅当 IsLoading 为 true 时有效。
    /// </summary>
    float LoadingProgress { get; }

    /// <summary>
    /// 异步加载指定场景。
    /// 所有场景切换均通过 Addressables 完成，禁止绕行此接口直接操作 SceneManager。
    /// </summary>
    UniTask LoadSceneAsync(SceneType sceneType, object context = null, IProgress<float> progress = null);

    /// <summary>
    /// 卸载当前场景。
    /// </summary>
    UniTask UnloadCurrentSceneAsync();

    // ── SceneController 注册机制 ──

    /// <summary>
    /// 注册场景控制器（由 SceneController.Awake 调用）。
    /// SceneService 在场景就绪后自动调用其 InitializeAsync。
    /// </summary>
    void RegisterController(ISceneController controller);

    /// <summary>
    /// 注销场景控制器（由 SceneController.OnDestroy 调用）。
    /// 已通过 DisposeAsync 清理的控制器可安全调用此方法（幂等）。
    /// </summary>
    void UnregisterController(ISceneController controller);

    // ── 场景生命周期事件 ──

    /// <summary>场景加载完成后触发。</summary>
    event Action<SceneType> OnSceneLoaded;

    /// <summary>场景开始卸载时触发。</summary>
    event Action<SceneType> OnSceneUnloading;

    // ── 加载进度事件 ──

    /// <summary>场景开始加载时触发。</summary>
    event Action<SceneType, string> OnLoadingStarted;

    /// <summary>加载进度更新时触发。</summary>
    event Action<SceneType, float, string> OnLoadingProgress;

    /// <summary>场景加载完成时触发。</summary>
    event Action<SceneType> OnLoadingCompleted;
}
