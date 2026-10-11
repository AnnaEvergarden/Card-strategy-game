using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 资源服务接口：统一管理所有动态资源的加载与释放。
/// 业务层通过 ResourceId 请求资源，内部通过 ResourceConfig 映射为 Addressables 地址进行加载。
/// 不允许业务层直接调用 Addressables API 或知道具体资源路径。
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// 异步加载指定类型的资源。
    /// </summary>
    /// <typeparam name="T">资源类型（Sprite、GameObject、AudioClip、TextAsset 等）</typeparam>
    /// <param name="resourceId">资源标识符</param>
    /// <param name="progress">加载进度回调（可选）</param>
    /// <param name="estimatedSizeBytes">本次资源预估大小（字节），用于加载前内存压力判断，未知可传 0</param>
    /// <returns>资源实例</returns>
    UniTask<T> LoadAsync<T>(ResourceId resourceId, IProgress<float> progress = null, long estimatedSizeBytes = 0) where T : UnityEngine.Object;

    /// <summary>
    /// 同步尝试获取已加载的资源（仅限已加载并缓存的情况）。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="resourceId">资源标识符</param>
    /// <param name="result">输出资源实例</param>
    /// <returns>是否成功获取</returns>
    bool TryGetLoaded<T>(ResourceId resourceId, out T result) where T : UnityEngine.Object;

    /// <summary>
    /// 释放指定资源，减少引用计数。
    /// </summary>
    /// <param name="resourceId">资源标识符</param>
    void Release(ResourceId resourceId);

    /// <summary>
    /// 释放所有已加载资源，清空缓存。
    /// </summary>
    void ReleaseAll();

    /// <summary>
    /// 预加载指定资源列表（用于 Loading 阶段批量缓存）。
    /// </summary>
    /// <param name="resourceIds">需要预加载的资源 ID 列表</param>
    /// <param name="progress">整体进度回调</param>
    UniTask PreloadAsync(System.Collections.Generic.IReadOnlyList<ResourceId> resourceIds, IProgress<float> progress = null);
}
