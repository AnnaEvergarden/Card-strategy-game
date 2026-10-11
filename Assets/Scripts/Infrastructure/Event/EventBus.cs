using System;
using System.Collections.Generic;

/// <summary>
/// 事件总线实现：线程安全的发布/订阅机制。
/// Subscribe 返回 IDisposable，Dispose 时自动取消订阅。
/// 发布使用 Delegate[] 快照缓存：订阅列表变化时标记脏，发布时惰性重建数组，
/// 高频发布直接复用缓存数组，避免每次发布都分配新的 List。
/// </summary>
public class EventBus : IEventBus
{
    /// <summary>订阅者源数据：事件类型 → 订阅者列表。</summary>
    private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

    /// <summary>快照缓存：事件类型 → 发布时遍历的委托数组。</summary>
    private readonly Dictionary<Type, Delegate[]> _snapshotCache = new Dictionary<Type, Delegate[]>();

    /// <summary>脏标记：订阅列表已变化但快照尚未重建的事件类型。</summary>
    private readonly HashSet<Type> _dirtyTypes = new HashSet<Type>();

    private readonly object _lock = new object();

    /// <summary>
    /// 发布事件：遍历当前事件的委托数组快照。
    /// 快照数组不可变，因此发布过程中订阅/取消订阅不会影响本次遍历。
    /// </summary>
    public void Publish<T>(T eventData) where T : IDomainEvent
    {
        var type = typeof(T);

        var snapshot = GetSnapshot(type);
        if (snapshot == null)
            return;

        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] is Action<T> handler)
            {
                try
                {
                    handler(eventData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EventBus] Handler error for {typeof(T).Name}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 订阅事件：写入订阅列表并标记快照脏。
    /// </summary>
    public IDisposable Subscribe<T>(Action<T> handler) where T : IDomainEvent
    {
        var type = typeof(T);
        lock (_lock)
        {
            if (!_handlers.TryGetValue(type, out var delegates))
            {
                delegates = new List<Delegate>();
                _handlers[type] = delegates;
            }
            delegates.Add(handler);
            MarkDirty(type);
        }

        return new SubscriptionToken(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(type, out var delegates))
                {
                    delegates.Remove(handler);
                    MarkDirty(type);
                }
            }
        });
    }

    /// <summary>
    /// 获取发布快照：优先返回缓存数组，仅在订阅列表变化后惰性重建。
    /// 数组内容永不修改，重建时整体替换引用，保证发布遍历的线程安全。
    /// </summary>
    private Delegate[] GetSnapshot(Type type)
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(type, out var delegates))
                return null;

            if (_dirtyTypes.Contains(type) || !_snapshotCache.TryGetValue(type, out var snapshot))
            {
                snapshot = delegates.ToArray();
                _snapshotCache[type] = snapshot;
                _dirtyTypes.Remove(type);
            }

            return snapshot;
        }
    }

    /// <summary>标记事件类型的快照已过期（调用方需已持有锁）。</summary>
    private void MarkDirty(Type type)
    {
        _dirtyTypes.Add(type);
    }

    private sealed class SubscriptionToken : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;
        public SubscriptionToken(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _unsubscribe?.Invoke();
        }
    }
}
