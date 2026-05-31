/// <summary>
/// 当前回合内哪一方可以行动。
/// </summary>
public enum BattleTurnPhase
{
    /// <summary>
    /// P1（玩家方）行动阶段（默认先手；PvP 先手待抛硬币，见 <see cref="BattleTurnSystem"/> TODO）。
    /// </summary>
    P1Action = 0,

    /// <summary>
    /// P2（对手方）行动阶段（此阶段 P1 方槽位不可操作）。
    /// </summary>
    P2Action = 1
}
