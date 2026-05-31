/// <summary>
/// 单次技能释放请求（Presentation → <see cref="BattleFacade"/> 用，非流水线内部上下文）。
/// </summary>
public sealed class SkillCastRequest
{
    #region Properties

    /// <summary>
    /// 施法者 UnitId。
    /// </summary>
    public string CasterUnitId { get; set; } = string.Empty;

    /// <summary>
    /// 玩家点选的目标 UnitId（需手动选目标的技能在点选后填入）。
    /// </summary>
    public string TargetUnitId { get; set; } = string.Empty;

    /// <summary>
    /// 技能 id。
    /// </summary>
    public string SkillId { get; set; } = string.Empty;

    /// <summary>
    /// 技能静态配置。
    /// </summary>
    public SkillConfigSO SkillConfig { get; set; }

    #endregion
}
