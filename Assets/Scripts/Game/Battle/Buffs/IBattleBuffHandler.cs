/// <summary>
/// 单种 Buff 的运行时行为：施加、到期移除（新增 Buff 种类时实现本接口并注册，避免在 Tick 里堆 if）。
/// </summary>
public interface IBattleBuffHandler
{
    #region Public API

    /// <summary>
    /// Buff 刚挂上时（可改 <see cref="BattleFieldState.CardRuntime"/> 等）。
    /// </summary>
    void OnApplied(string cardId, BattleBuffState.RuntimeBuff buff);

    /// <summary>
    /// 剩余回合归零、Buff 被移除前（回收属性加成等）。
    /// </summary>
    void OnExpired(string cardId, BattleBuffState.RuntimeBuff buff);

    #endregion
}
