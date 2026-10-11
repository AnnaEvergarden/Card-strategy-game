using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 场景控制器接口：定义场景的进入初始化与退出清理生命周期。
///
/// 实现类应为挂载在场景根物体上的 MonoBehaviour，通过注册机制与 SceneService 交互：
/// 1. Awake 中调用 SceneService.RegisterController(this) 注册
/// 2. SceneService 在场景就绪后自动调用 InitializeAsync
/// 3. SceneService 在场景切换前自动调用 DisposeAsync
/// 4. OnDestroy 中调用 SceneService.UnregisterController(this) 反注册（安全兜底）
///
/// 替代旧的 ISceneBootstrap 接口（OnSceneEnterAsync），提供更完整的进入+退出生命周期。
/// </summary>
public interface ISceneController
{
    /// <summary>
    /// 场景加载并激活后调用，用于初始化场景状态、创建环境、启动子系统等。
    /// </summary>
    UniTask InitializeAsync();

    /// <summary>
    /// 场景即将卸载时调用，用于清理对象、保存数据、释放资源等。
    /// </summary>
    UniTask DisposeAsync();
}
