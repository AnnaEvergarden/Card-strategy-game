/// <summary>
/// 战斗事件标记接口：所有战斗事件 DTO 实现此接口。
/// 战斗事件总线以此约束事件类型，与全局 IEventBus（跨模块事件）隔离，
/// 使战斗内部事件不污染全局事件流。
/// </summary>
public interface IBattleEvent
{
}
