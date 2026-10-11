/// <summary>
/// Buff 种类枚举。
/// 新增 Buff 种类时：在此添加枚举值 + 实现 IBuffHandler 处理器 + 注册到 BuffHandlerRegistry，
/// 不在回合 Tick 逻辑中堆 if/switch。
/// </summary>
public enum BuffKind
{
    /// <summary>未指定。</summary>
    None = 0,

    /// <summary>防御强化：持续 N 回合，增加防御值。</summary>
    DefenseBuff = 1,
}
