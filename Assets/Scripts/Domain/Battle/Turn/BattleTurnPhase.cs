/// <summary>
/// 回合阶段枚举：当前轮到哪个阵营行动。
/// 双方各自行动完毕后进入下一回合（P1Action 先手）。
/// </summary>
public enum BattleTurnPhase
{
    /// <summary>P1（玩家侧）行动阶段。</summary>
    P1Action = 0,

    /// <summary>P2（敌方侧）行动阶段。</summary>
    P2Action = 1,

    /// <summary>战斗已结束（结算阶段）。</summary>
    Ended = 2,
}
