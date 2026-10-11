/// <summary>
/// 技能目标类型枚举。
/// </summary>
public enum SkillTargetKind
{
    None = 0,
    SingleEnemy,
    AllEnemies,
    SingleAlly,
    AllAllies,
    Self,
    AllTargets
}

/// <summary>
/// 技能效果种类枚举。
/// 技能 = 多个效果配置的组合（Skill = Effect Pipeline + Context），
/// 新增效果种类时：在此添加枚举值 + 实现 ISkillEffect + 注册到 SkillEffectRegistry。
/// </summary>
public enum SkillEffectKind
{
    /// <summary>未指定。</summary>
    None = 0,

    /// <summary>伤害：对目标造成 Value 点伤害（受目标防御减免）。</summary>
    Damage = 1,

    /// <summary>治疗：为目标回复 Value 点生命。</summary>
    Heal = 2,

    /// <summary>防御强化 Buff：为目标添加 Value 点防御、持续 Duration 回合。</summary>
    DefenseBuff = 3,

    /// <summary>自我损耗：施法者直接失去 Value 点生命（无视防御，技能代价）。</summary>
    SelfHpDrain = 4,

    /// <summary>冷却刷新：施法者每个冷却中的技能有 Chance 概率立即重置冷却。</summary>
    RefreshCooldownChance = 5,
}
