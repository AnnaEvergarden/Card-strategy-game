/// <summary>
/// 持续 Buff 子类型（<see cref="BuffEffectEntry"/>）。
/// </summary>
public enum SkillBuffKind
{
    /// <summary>
    /// 未指定。
    /// </summary>
    None = 0,

    /// <summary>
    /// 防御增益（<see cref="BuffEffectEntry.Value"/> + <see cref="BuffEffectEntry.DurationTurns"/>）。
    /// </summary>
    DefenseBuff = 1
}
