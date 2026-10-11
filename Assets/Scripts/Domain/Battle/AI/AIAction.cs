/// <summary>
/// AI 行动候选：单位 × 技能 × 目标的组合与评分。
/// </summary>
public sealed class AIAction
{
    /// <summary>施法单位 UnitId。</summary>
    public string CasterUnitId { get; }

    /// <summary>技能配置。</summary>
    public SkillConfig Skill { get; }

    /// <summary>目标 UnitId（无需手动目标的技能为 null）。</summary>
    public string TargetUnitId { get; }

    /// <summary>综合评分（越高越优先）。</summary>
    public float Score { get; }

    /// <summary>
    /// 构造 AI 行动候选。
    /// </summary>
    public AIAction(string casterUnitId, SkillConfig skill, string targetUnitId, float score)
    {
        CasterUnitId = casterUnitId;
        Skill = skill;
        TargetUnitId = targetUnitId;
        Score = score;
    }
}
