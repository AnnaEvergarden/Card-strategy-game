/// <summary>
/// 战斗结果枚举。
/// </summary>
public enum BattleResult
{
    /// <summary>未分胜负。</summary>
    Undecided = 0,

    /// <summary>P1（玩家侧）获胜。</summary>
    P1Victory = 1,

    /// <summary>P2（敌方侧）获胜。</summary>
    P2Victory = 2,

    /// <summary>平局（双方同时全灭等极端场景）。</summary>
    Draw = 3,

    /// <summary>P1（玩家侧）撤退认负。</summary>
    P1Retreat = 4,
}

/// <summary>
/// 战斗结果数据：不可变值对象，由 BattleSession 产出、应用层与表现层只读消费。
/// </summary>
public sealed class BattleOutcome
{
    /// <summary>结果种类。</summary>
    public BattleResult Result { get; }

    /// <summary>获胜阵营。</summary>
    public BattleSide WinnerSide { get; }

    /// <summary>结束时回合数。</summary>
    public int RoundNumber { get; }

    /// <summary>结束原因描述。</summary>
    public string Reason { get; }

    /// <summary>
    /// 构造战斗结果。
    /// </summary>
    private BattleOutcome(BattleResult result, BattleSide winnerSide, int roundNumber, string reason)
    {
        Result = result;
        WinnerSide = winnerSide;
        RoundNumber = roundNumber;
        Reason = reason ?? string.Empty;
    }

    /// <summary>
    /// 创建 P1 胜利结果。
    /// </summary>
    public static BattleOutcome P1Won(int roundNumber, string reason)
        => new BattleOutcome(BattleResult.P1Victory, BattleSide.P1, roundNumber, reason);

    /// <summary>
    /// 创建 P2 胜利结果。
    /// </summary>
    public static BattleOutcome P2Won(int roundNumber, string reason)
        => new BattleOutcome(BattleResult.P2Victory, BattleSide.P2, roundNumber, reason);

    /// <summary>
    /// 创建平局结果。
    /// </summary>
    public static BattleOutcome Drawn(int roundNumber, string reason)
        => new BattleOutcome(BattleResult.Draw, BattleSide.P1, roundNumber, reason);

    /// <summary>
    /// 创建 P1 撤退结果。
    /// </summary>
    public static BattleOutcome P1Retreated(int roundNumber, string reason)
        => new BattleOutcome(BattleResult.P1Retreat, BattleSide.P2, roundNumber, reason);
}
