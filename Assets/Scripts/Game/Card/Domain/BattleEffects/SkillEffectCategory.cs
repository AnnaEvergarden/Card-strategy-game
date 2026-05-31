/// <summary>
/// 技能效果大类：即时（释放时执行）或持续 Buff。
/// </summary>
public enum SkillEffectCategory
{
    /// <summary>
    /// 即时效果 <see cref="InstantEffectEntry"/>。
    /// </summary>
    Instant = 0,

    /// <summary>
    /// 持续 Buff <see cref="BuffEffectEntry"/>。
    /// </summary>
    Buff = 1
}
