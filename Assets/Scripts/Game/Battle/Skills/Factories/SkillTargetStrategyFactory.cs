/// <summary>
/// 按 <see cref="SkillTargetKind"/> 创建目标选择策略（无状态策略可缓存单例）。
/// </summary>
public static class SkillTargetStrategyFactory
{
    #region Fields

    /// <summary>
    /// 无目标策略单例。
    /// </summary>
    private static readonly NoTargetStrategy NoTarget = new();

    /// <summary>
    /// 单体敌方策略单例。
    /// </summary>
    private static readonly SingleEnemyTargetStrategy SingleEnemy = new();

    /// <summary>
    /// 单体己方策略单例。
    /// </summary>
    private static readonly SingleAllyTargetStrategy SingleAlly = new();

    /// <summary>
    /// 全体策略单例。
    /// </summary>
    private static readonly AllTargetsStrategy All = new();

    /// <summary>
    /// 全体己方策略单例。
    /// </summary>
    private static readonly AllAlliesTargetStrategy AllAllies = new();

    /// <summary>
    /// 全体敌方策略单例。
    /// </summary>
    private static readonly AllEnemiesTargetStrategy AllEnemies = new();

    #endregion

    #region Public API

    /// <summary>
    /// 获取目标策略实例。
    /// </summary>
    public static ITargetSelectionStrategy Get(SkillTargetKind kind)
    {
        switch (kind)
        {
            case SkillTargetKind.SingleEnemy:
                return SingleEnemy;
            case SkillTargetKind.SingleAlly:
                return SingleAlly;
            case SkillTargetKind.All:
                return All;
            case SkillTargetKind.AllAllies:
                return AllAllies;
            case SkillTargetKind.AllEnemies:
                return AllEnemies;
            case SkillTargetKind.None:
            default:
                return NoTarget;
        }
    }

    #endregion
}
