using System;

/// <summary>
/// 战斗事件总线接口：解耦战斗逻辑层与表现层。
/// 定义在 Domain 层（依赖倒置）：BattleUnit / BattleField / 回合系统 / 技能系统只依赖此抽象，
/// 实现由 Infrastructure 提供（BattleEventBus），经构造函数注入。
/// 表现层（UI）只订阅事件、绝不发布战斗数据事件，也不直接修改战斗状态。
/// </summary>
public interface IBattleEventBus
{
    /// <summary>
    /// 发布战斗事件。
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="eventData">事件数据</param>
    void Publish<T>(T eventData) where T : IBattleEvent;

    /// <summary>
    /// 订阅战斗事件。返回 IDisposable，Dispose 时自动取消订阅。
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="handler">回调</param>
    IDisposable Subscribe<T>(Action<T> handler) where T : IBattleEvent;
}
