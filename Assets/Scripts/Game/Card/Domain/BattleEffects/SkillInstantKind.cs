/// <summary>
/// 即时效果子类型（<see cref="InstantEffectEntry"/>）。
/// </summary>
public enum SkillInstantKind
{
    /// <summary>
    /// 未指定。
    /// </summary>
    None = 0,

    /// <summary>
    /// 固定伤害（<see cref="InstantEffectEntry.Value"/>）。
    /// </summary>
    Damage = 1,

    /// <summary>
    /// 按概率刷新技能冷却（<see cref="InstantEffectEntry.ChancePercent"/>）。
    /// </summary>
    RefreshCooldown = 2,

    /// <summary>
    /// 治疗（<see cref="InstantEffectEntry.Value"/>）。
    /// </summary>
    Heal = 3,

    /// <summary>
    /// 扣除施法者生命（<see cref="InstantEffectEntry.Value"/>）。
    /// </summary>
    SelfHpDrain = 4
}
