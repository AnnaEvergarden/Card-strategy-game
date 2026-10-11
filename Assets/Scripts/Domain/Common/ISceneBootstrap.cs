using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 场景启动器接口：场景加载完成后由 SceneService 自动调用其生命周期方法。
/// 场景根物体上挂载实现此接口的 MonoBehaviour，在场景进入时执行初始化逻辑。
/// Application 层通过此接口在场景层级执行初始化，避免在 SceneService 中耦合业务逻辑。
/// </summary>
public interface ISceneBootstrap
{
    /// <summary>
    /// 场景加载并激活后调用，用于初始化场景状态、打开默认面板、恢复数据等。
    /// </summary>
    /// <param name="context">LoadSceneAsync 传入的自定义上下文</param>
    UniTask OnSceneEnterAsync(object context = null);
}
