/// <summary>
/// 技能目标规则（配置在 <see cref="SkillConfigSO"/>，运行时映射为 Targeting 策略）。
/// </summary>
public enum SkillTargetKind
{
    /// <summary>
    /// 无目标（仅施法者或全局 Effect）。
    /// </summary>
    None = 0,

    /// <summary>
    /// 单体敌方（玩家点选）。
    /// </summary>
    SingleEnemy = 1,

    /// <summary>
    /// 单体己方（玩家点选，须为当前上场友方）。
    /// </summary>
    SingleAlly = 2,

    /// <summary>
    /// 全体：场上所有存活单位（敌我双方）。
    /// </summary>
    All = 3,

    /// <summary>
    /// 全体己方：当前上场友方单位。
    /// </summary>
    AllAllies = 4,

    /// <summary>
    /// 全体敌方：本关 NPC 等非上场友方单位。
    /// </summary>
    AllEnemies = 5
}
