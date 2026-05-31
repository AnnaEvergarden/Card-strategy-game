/// <summary>
/// 单次技能流水线执行结果（Effect 链写入，供 Application 扣次数与刷新 UI）。
/// </summary>
public sealed class SkillCastOutcome
{
    #region Properties

    /// <summary>
    /// 是否执行成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 提示或失败原因。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 是否触发了刷新冷却效果。
    /// </summary>
    public bool CooldownRefreshed { get; set; }

    /// <summary>
    /// 累计造成的伤害。
    /// </summary>
    public int TotalDamageDealt { get; set; }

    #endregion
}
