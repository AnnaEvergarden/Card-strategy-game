using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 资源服务实现：通过 ResourceId 统一管理 Addressables 资源的加载、缓存、引用计数与释放。
/// 业务层不直接调用 Addressables API，全部通过此服务中转。
/// 非静态、非单例——由 ServiceContainer 注册，通过接口注入到各模块。
/// </summary>
public class ResourceService : IResourceService, IDisposable
{
    /// <summary>
    /// 资源标识到地址的查询表。
    /// </summary>
    private readonly ResourceTable _resourceTable;

    /// <summary>
    /// 已加载资源的强引用缓存：ResourceId → 资源对象。
    /// </summary>
    private readonly Dictionary<int, object> _assetCache = new Dictionary<int, object>();

    /// <summary>
    /// 弱引用缓存：引用计数归零但对象仍存活的资源。
    /// 允许 GC 回收托管包装，对应 Addressables 句柄保留到对象失效后由 SweepWeakEntries 释放。
    /// </summary>
    private readonly Dictionary<int, WeakReference<object>> _weakCache = new Dictionary<int, WeakReference<object>>();

    /// <summary>
    /// 加载中的操作句柄（用于释放）：ResourceId → AsyncOperationHandle。
    /// </summary>
    private readonly Dictionary<int, AsyncOperationHandle> _loadHandles = new Dictionary<int, AsyncOperationHandle>();

    /// <summary>
    /// 引用计数：ResourceId → 引用次数（LoadAsync +1，Release -1，归零时移入弱引用缓存）。
    /// </summary>
    private readonly Dictionary<int, int> _refCount = new Dictionary<int, int>();

    /// <summary>
    /// 正在加载中的异步任务锁：防止同一资源并发加载多次。
    /// </summary>
    private readonly Dictionary<int, UniTaskCompletionSource<object>> _pendingLoads = new Dictionary<int, UniTaskCompletionSource<object>>();

    /// <summary>
    /// 并发控制锁对象。
    /// </summary>
    private readonly object _lock = new object();

    /// <summary>
    /// 加载前内存压力检测器。
    /// </summary>
    private readonly MemoryGuard _memoryGuard = new MemoryGuard();

    /// <summary>
    /// 是否已释放。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 通过 ResourceTable 构造资源服务。
    /// </summary>
    /// <param name="resourceTable">已构建好的资源配置表</param>
    /// <exception cref="ArgumentNullException">resourceTable 为 null 时抛出</exception>
    public ResourceService(ResourceTable resourceTable)
    {
        _resourceTable = resourceTable ?? throw new ArgumentNullException(nameof(resourceTable));
    }

    /// <summary>
    /// 异步加载指定类型的资源。
    /// 内部流程：查表 → 查缓存 → 查并发锁 → Addressables.Load  → 缓存 → 返回。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="resourceId">资源标识符</param>
    /// <param name="progress">加载进度回调</param>
    /// <returns>资源实例</returns>
    public async UniTask<T> LoadAsync<T>(ResourceId resourceId, IProgress<float> progress = null, long estimatedSizeBytes = 0) where T : UnityEngine.Object
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ResourceService));

        int id = resourceId;

        // 1. 查缓存（快速路径）
        lock (_lock)
        {
            if (_assetCache.TryGetValue(id, out var cached) && cached is T typed)
            {
                IncrementRefCount(id);
                return typed;
            }
        }

        // 1.5 弱引用缓存命中：对象仍存活则直接转强引用复用
        lock (_lock)
        {
            if (TryPromoteWeakCache(id, out T weakHit))
            {
                IncrementRefCount(id);
                return weakHit;
            }
        }

        // 2. 查资源配置表
        if (!_resourceTable.TryGetAddress(resourceId, out var address))
        {
            Debug.LogError($"[ResourceService] 未找到 ResourceId={id} 的资源配置，请检查 ResourceTable 是否已正确导出。");
            return null;
        }

        // 2.2 清理已失效的弱引用条目，释放对应句柄
        lock (_lock)
        {
            SweepWeakEntriesLocked();
        }

        // 2.5 内存压力检查：危险等级时先清理再复检，仍超限则拒绝本次加载
        var pressure = await _memoryGuard.CheckAsync(estimatedSizeBytes);
        if (pressure == MemoryPressureLevel.Critical)
        {
            Debug.LogError($"[ResourceService] 内存压力过高，拒绝加载 ResourceId={id}, Address={address}");
            return null;
        }

        // 3. 并发控制：同一资源正在加载中则等待已有任务
        // 注：lock 内不能 await，因此分两步：先判断是否已有加载中，再在 lock 外 await
        UniTaskCompletionSource<object> pendingSource;
        bool isNewLoad;

        lock (_lock)
        {
            if (_pendingLoads.TryGetValue(id, out pendingSource))
            {
                isNewLoad = false; // 已有加载中的任务
            }
            else
            {
                pendingSource = new UniTaskCompletionSource<object>();
                _pendingLoads[id] = pendingSource;
                isNewLoad = true;
            }
        }

        if (!isNewLoad)
        {
            var result = await pendingSource.Task;
            if (result is T existing)
            {
                lock (_lock) IncrementRefCount(id);
                return existing;
            }
            return null;
        }
        try
        {
            // 4. 执行 Addressables 加载
            var handle = Addressables.LoadAssetAsync<T>(address);

            // 进度回调
            if (progress != null)
            {
                while (!handle.IsDone)
                {
                    progress.Report(handle.PercentComplete);
                    await UniTask.Yield();
                }
            }

            await handle.ToUniTask();

            // 5. 加载完成：缓存 + 更新句柄
            T asset = handle.Result;

            lock (_lock)
            {
                _loadHandles[id] = handle;
                _assetCache[id] = asset;
                IncrementRefCount(id);
                pendingSource.TrySetResult(asset);
            }

            progress?.Report(1f);
            return asset;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                pendingSource.TrySetException(ex);
            }
            Debug.LogError($"[ResourceService] 加载资源失败 ResourceId={id}, Address={address}: {ex.Message}");
            return null;
        }
        finally
        {
            lock (_lock)
            {
                _pendingLoads.Remove(id);
            }
        }
    }

    /// <summary>
    /// 同步尝试获取已缓存资源（仅限已加载完成的资源）。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="resourceId">资源标识符</param>
    /// <param name="result">输出资源实例</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetLoaded<T>(ResourceId resourceId, out T result) where T : UnityEngine.Object
    {
        lock (_lock)
        {
            if (_assetCache.TryGetValue(resourceId, out var cached) && cached is T typed)
            {
                result = typed;
                return true;
            }
        }
        result = null;
        return false;
    }

    /// <summary>
    /// 释放指定资源：减少引用计数，归零时移入弱引用缓存，对象失效后再释放句柄。
    /// </summary>
    /// <param name="resourceId">资源标识符</param>
    public void Release(ResourceId resourceId)
    {
        int id = resourceId;
        lock (_lock)
        {
            if (!_refCount.TryGetValue(id, out var count) || count <= 0)
                return;

            count--;
            _refCount[id] = count;

            if (count > 0)
                return;

            // 引用计数归零：移入弱引用缓存，对象失效后再释放句柄
            if (_assetCache.TryGetValue(id, out var released))
            {
                _weakCache[id] = new WeakReference<object>(released);
                _assetCache.Remove(id);
            }
            SweepWeakEntriesLocked();
        }
    }

    /// <summary>
    /// 释放所有已加载资源，清空缓存与引用计数。
    /// </summary>
    public void ReleaseAll()
    {
        lock (_lock)
        {
            foreach (var kvp in _loadHandles)
            {
                if (kvp.Value.IsValid())
                    Addressables.Release(kvp.Value);
            }
            _loadHandles.Clear();
            _assetCache.Clear();
            _weakCache.Clear();
            _refCount.Clear();
        }
    }

    /// <summary>
    /// 预加载指定资源列表：批量加载到缓存中但不返回给调用方。
    /// 用于 Loading 阶段的资源预热。
    /// </summary>
    /// <param name="resourceIds">需要预加载的资源 ID 列表</param>
    /// <param name="progress">整体进度回调</param>
    public async UniTask PreloadAsync(IReadOnlyList<ResourceId> resourceIds, IProgress<float> progress = null)
    {
        if (resourceIds == null || resourceIds.Count == 0)
            return;

        int total = resourceIds.Count;
        int completed = 0;

        for (int i = 0; i < total; i++)
        {
            var id = resourceIds[i];

            // 已经缓存或正在加载的跳过
            bool alreadyLoaded;
            lock (_lock)
            {
                alreadyLoaded = _assetCache.ContainsKey(id) || _pendingLoads.ContainsKey(id);
            }

            if (!alreadyLoaded)
            {
                // 预加载时不关心具体类型，用 Object 加载
                await LoadAsync<UnityEngine.Object>(id);
            }

            completed++;
            progress?.Report((float)completed / total);
        }
    }

    /// <summary>
    /// 获取资源配置表（供其他组件查询）。
    /// </summary>
    public ResourceTable Table => _resourceTable;

    /// <summary>
    /// 释放所有资源并清理内部状态。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReleaseAll();
    }

    /// <summary>
    /// 增加引用计数（内部方法，调用方需已持有锁）。
    /// </summary>
    private void IncrementRefCount(int id)
    {
        if (!_refCount.TryGetValue(id, out var count))
            count = 0;
        _refCount[id] = count + 1;
    }

    /// <summary>
    /// 尝试从弱引用缓存提升资源为强引用（调用方需已持有锁）。
    /// </summary>
    private bool TryPromoteWeakCache<T>(int id, out T asset) where T : UnityEngine.Object
    {
        asset = null;
        if (!_weakCache.TryGetValue(id, out var weakRef))
            return false;
        if (!weakRef.TryGetTarget(out var target) || !IsAlive(target) || !(target is T typed))
            return false;

        _weakCache.Remove(id);
        _assetCache[id] = typed;
        asset = typed;
        return true;
    }

    /// <summary>
    /// 清理已失效的弱引用条目并释放其 Addressables 句柄（调用方需已持有锁）。
    /// </summary>
    private void SweepWeakEntriesLocked()
    {
        if (_weakCache.Count == 0)
            return;

        List<int> deadIds = null;
        foreach (var kvp in _weakCache)
        {
            if (kvp.Value.TryGetTarget(out var target) && IsAlive(target))
                continue;

            if (deadIds == null)
                deadIds = new List<int>();
            deadIds.Add(kvp.Key);
        }

        if (deadIds == null)
            return;

        for (int i = 0; i < deadIds.Count; i++)
        {
            int id = deadIds[i];
            _weakCache.Remove(id);
            if (_loadHandles.TryGetValue(id, out var handle) && handle.IsValid())
            {
                Addressables.Release(handle);
                _loadHandles.Remove(id);
            }
        }
    }

    /// <summary>
    /// 判断弱引用目标是否仍然可用：Unity 对象需未被销毁。
    /// </summary>
    private static bool IsAlive(object target)
    {
        if (target == null)
            return false;
        if (target is UnityEngine.Object unityObject)
            return unityObject != null;
        return true;
    }
}
