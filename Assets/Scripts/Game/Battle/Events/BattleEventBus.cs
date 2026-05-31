using System;
using System.Collections.Generic;

/// <summary>
/// 战斗事件总线：解耦逻辑层与表现层。逻辑只 Publish，View 只 Subscribe。
/// </summary>
public sealed class BattleEventBus
{
    #region Fields

    /// <summary>
    /// 事件类型到订阅者列表。
    /// </summary>
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    /// <summary>
    /// 事件类型到快照数组缓存（写时复制，避免每次发布分配新数组）。
    /// </summary>
    private readonly Dictionary<Type, Delegate[]> _snapshotCache = new();

    #endregion

    #region Public API

    /// <summary>
    /// 订阅指定类型事件。
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IBattleEvent
    {
        if (handler == null)
        {
            return;
        }

        var type = typeof(TEvent);
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _subscribers[type] = list;
        }

        list.Add(handler);
        InvalidateSnapshot(type);
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IBattleEvent
    {
        if (handler == null)
        {
            return;
        }

        var type = typeof(TEvent);
        if (!_subscribers.TryGetValue(type, out var list))
        {
            return;
        }

        list.Remove(handler);
        if (list.Count == 0)
        {
            _subscribers.Remove(type);
        }

        InvalidateSnapshot(type);
    }

    /// <summary>
    /// 按运行时类型发布事件（供 <see cref="SkillExecutionContext.FlushPendingEvents"/> 等批量刷新使用）。
    /// </summary>
    public void Publish(IBattleEvent evt)
    {
        if (evt == null)
        {
            return;
        }

        switch (evt)
        {
            case DamageEvent damage:
                Publish(damage);
                break;
            case DeadEvent dead:
                Publish(dead);
                break;
            case SkillCastStartEvent castStart:
                Publish(castStart);
                break;
            case SkillCastFinishEvent castFinish:
                Publish(castFinish);
                break;
        }
    }

    /// <summary>
    /// 发布事件给所有订阅者。
    /// </summary>
    public void Publish<TEvent>(TEvent evt) where TEvent : IBattleEvent
    {
        if (evt == null)
        {
            return;
        }

        var type = typeof(TEvent);
        if (!_subscribers.TryGetValue(type, out var list) || list.Count == 0)
        {
            return;
        }

        if (!_snapshotCache.TryGetValue(type, out var snapshot) || snapshot == null)
        {
            snapshot = list.ToArray();
            _snapshotCache[type] = snapshot;
        }

        for (var i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] is Action<TEvent> action)
            {
                action.Invoke(evt);
            }
        }
    }

    /// <summary>
    /// 使指定事件类型的快照失效，下次发布时重建。
    /// </summary>
    private void InvalidateSnapshot(Type type)
    {
        _snapshotCache.Remove(type);
    }

    /// <summary>
    /// 清空全部订阅与快照缓存（如离开战斗场景时调用）。
    /// </summary>
    public void ClearAll()
    {
        _subscribers.Clear();
        _snapshotCache.Clear();
    }

    #endregion
}
