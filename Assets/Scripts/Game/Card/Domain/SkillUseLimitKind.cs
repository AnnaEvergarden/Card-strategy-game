/// <summary>
/// 技能可使用次数限制类型（配置于 <see cref="SkillConfigSO"/>）。
/// </summary>
public enum SkillUseLimitKind
{
    /// <summary>
    /// 无限制，本局可重复使用。
    /// </summary>
    Unlimited = 0,

    /// <summary>
    /// 有限次数，次数由 <see cref="SkillConfigSO.LimitedUseCount"/> 指定。
    /// </summary>
    Limited = 1
}
