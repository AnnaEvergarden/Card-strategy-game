/// <summary>
/// AI 单次行动候选：记录施法者 UnitId、技能配置、目标 UnitId 以及 Utility 评分。
/// 由 <see cref="BattleAIService.EvaluateBestActions"/> 创建，由 <see cref="BattleAIService.ExecuteSingleAction"/> 消费。
/// </summary>
public sealed class AIAction
{
    #region Properties

    /// <summary>
    /// 施法者 UnitId（全局唯一，UI 查找/字典索引用）。
    /// </summary>
    public string CasterUnitId { get; }

    /// <summary>
    /// 要释放的技能运行时配置（含效果列表、冷却、目标规则等）。
    /// </summary>
    public SkillConfigSO SkillConfig { get; }

    /// <summary>
    /// 技能目标 UnitId。对于无需选目标的技能为空字符串。
    /// </summary>
    public string TargetUnitId { get; }

    /// <summary>
    /// Utility 评分值，越高表示该行动越优。
    /// </summary>
    public float Score { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// 初始化行动候选。
    /// </summary>
    /// <param name="casterUnitId">施法者 UnitId。</param>
    /// <param name="skillConfig">技能配置。</param>
    /// <param name="targetUnitId">目标 UnitId（无目标技能传空）。</param>
    /// <param name="score">初始评分。</param>
    public AIAction(string casterUnitId, SkillConfigSO skillConfig, string targetUnitId, float score = 0f)
    {
        CasterUnitId = casterUnitId ?? string.Empty;
        SkillConfig = skillConfig;
        TargetUnitId = targetUnitId ?? string.Empty;
        Score = score;
    }

    #endregion
}
