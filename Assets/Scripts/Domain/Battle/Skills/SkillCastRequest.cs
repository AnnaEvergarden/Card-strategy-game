/// <summary>
/// 技能释放请求：UI/AI → 技能系统的输入参数。
/// </summary>
public sealed class SkillCastRequest
{
    /// <summary>施法单位 UnitId。</summary>
    public string CasterUnitId { get; }

    /// <summary>技能配置（由应用层从 ConfigService 解析后传入，Domain 不依赖配置服务）。</summary>
    public SkillConfig SkillConfig { get; }

    /// <summary>手动选择的目标 UnitId（需要手动选目标的技能必填；群体/自身类技能可为空）。</summary>
    public string TargetUnitId { get; }

    /// <summary>
    /// 构造技能释放请求。
    /// </summary>
    public SkillCastRequest(string casterUnitId, SkillConfig skillConfig, string targetUnitId)
    {
        CasterUnitId = casterUnitId;
        SkillConfig = skillConfig;
        TargetUnitId = targetUnitId;
    }
}
