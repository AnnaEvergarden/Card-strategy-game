using System;

/// <summary>
/// 事件总线接口：解耦事件发布者与订阅者。
/// Subscribe 返回 IDisposable，Dispose 时自动取消订阅，避免手动管理生命周期。
/// </summary>
public interface IEventBus
{
    /// <summary>发布事件。</summary>
    void Publish<T>(T eventData) where T : IDomainEvent;

    /// <summary>订阅事件。返回的 IDisposable 用于取消订阅。</summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : IDomainEvent;
}