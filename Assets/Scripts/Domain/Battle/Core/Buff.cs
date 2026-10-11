/// <summary>
/// Buff 运行时状态：种类、数值与剩余持续回合。
/// 本局战斗内由 BattleUnit 持有与 Tick（数据所有权原则：Buff 属于单位）；
/// 非持久化数据，不写入存档。
/// </summary>
public sealed class Buff
{
    /// <summary>Buff 种类。</summary>
    public BuffKind Kind { get; }

    /// <summary>数值（防御加成量等，含义由种类决定）。</summary>
    public int Value { get; }

    /// <summary>剩余持续回合数。</summary>
    public int RemainingTurns { get; private set; }

    /// <summary>是否已到期。</summary>
    public bool IsExpired => RemainingTurns <= 0;

    /// <summary>
    /// 构造 Buff。
    /// </summary>
    /// <param name="kind">种类</param>
    /// <param name="value">数值</param>
    /// <param name="remainingTurns">剩余持续回合数（至少 1）</param>
    public Buff(BuffKind kind, int value, int remainingTurns)
    {
        Kind = kind;
        Value = value;
        RemainingTurns = remainingTurns > 0 ? remainingTurns : 1;
    }

    /// <summary>
    /// 回合结束时 Tick：持续回合 -1，返回是否因此到期。
    /// </summary>
    public bool TickTurnEnd()
    {
        if (RemainingTurns > 0)
            RemainingTurns--;
        return IsExpired;
    }
}
