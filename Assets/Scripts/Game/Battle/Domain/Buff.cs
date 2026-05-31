/// <summary>
/// 运行时 Buff（Domain）：持久化挂载在 <see cref="BattleUnit"/> 上的单一 Buff 实例。
/// </summary>
public sealed class Buff
{
    /// <summary>
    /// Buff 种类。
    /// </summary>
    public SkillBuffKind Kind { get; }

    /// <summary>
    /// 数值（如防御加成）。
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// 剩余回合数。
    /// </summary>
    public int RemainingTurns { get; set; }

    public Buff(SkillBuffKind kind, int value, int remainingTurns)
    {
        Kind = kind;
        Value = value;
        RemainingTurns = remainingTurns;
    }
}
