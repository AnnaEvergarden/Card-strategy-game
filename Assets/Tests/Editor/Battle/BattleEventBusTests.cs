using NUnit.Framework;

/// <summary>
/// BattleEventBus 单元测试：发布/订阅/退订与快照遍历安全。
/// </summary>
public class BattleEventBusTests
{
    /// <summary>订阅后能收到发布的事件。</summary>
    [Test]
    public void Subscribe_ReceivesPublishedEvent()
    {
        var bus = new BattleEventBus();
        RoundStartedEvent received = null;
        using var _ = bus.Subscribe<RoundStartedEvent>(e => received = e);

        bus.Publish(new RoundStartedEvent(3));

        Assert.IsNotNull(received);
        Assert.AreEqual(3, received.RoundNumber);
    }

    /// <summary>Dispose 订阅令牌后不再收到事件。</summary>
    [Test]
    public void Subscribe_DisposeToken_StopsReceiving()
    {
        var bus = new BattleEventBus();
        var count = 0;
        var token = bus.Subscribe<RoundStartedEvent>(_ => count++);

        bus.Publish(new RoundStartedEvent(1));
        token.Dispose();
        bus.Publish(new RoundStartedEvent(2));

        Assert.AreEqual(1, count);
    }

    /// <summary>无订阅者时发布不抛异常。</summary>
    [Test]
    public void Publish_WithNoSubscriber_DoesNotThrow()
    {
        var bus = new BattleEventBus();

        Assert.DoesNotThrow(() => bus.Publish(new RoundStartedEvent(1)));
    }

    /// <summary>多个订阅者均收到同一事件。</summary>
    [Test]
    public void Publish_MultipleSubscribers_AllNotified()
    {
        var bus = new BattleEventBus();
        var first = 0;
        var second = 0;
        using var _1 = bus.Subscribe<RoundStartedEvent>(_ => first++);
        using var _2 = bus.Subscribe<RoundStartedEvent>(_ => second++);

        bus.Publish(new RoundStartedEvent(1));

        Assert.AreEqual(1, first);
        Assert.AreEqual(1, second);
    }

    /// <summary>发布期间退订不影响本次遍历（快照安全）。</summary>
    [Test]
    public void Publish_UnsubscribeDuringPublish_DoesNotBreakIteration()
    {
        var bus = new BattleEventBus();
        var count = 0;
        System.IDisposable token = null;
        token = bus.Subscribe<RoundStartedEvent>(_ =>
        {
            count++;
            token?.Dispose(); // 首个回调中退订自身
        });

        Assert.DoesNotThrow(() => bus.Publish(new RoundStartedEvent(1)));
        Assert.AreEqual(1, count);
    }
}
