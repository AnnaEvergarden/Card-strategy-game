/// <summary>
/// 技能释放结果种类。
/// </summary>
public enum SkillCastResult
{
    /// <summary>释放成功（效果全部生效、次数与冷却已结算）。</summary>
    Success = 0,

    /// <summary>前置校验失败（阵亡/冷却中/次数耗尽/无效果配置）。</summary>
    ValidationFailed = 1,

    /// <summary>目标选取失败（需要手动目标但未提供/目标非法/无合法目标）。</summary>
    TargetSelectionFailed = 2,

    /// <summary>效果管线失败（已生效效果已逆序回滚，次数与冷却未消耗）。</summary>
    PipelineFailed = 3,
}

/// <summary>
/// 技能释放结果：种类与失败原因。
/// </summary>
public sealed class SkillCastOutcome
{
    /// <summary>结果种类。</summary>
    public SkillCastResult Result { get; }

    /// <summary>失败原因（成功时为空）。</summary>
    public string FailureReason { get; }

    /// <summary>是否成功。</summary>
    public bool IsSuccess => Result == SkillCastResult.Success;

    /// <summary>
    /// 构造释放结果。
    /// </summary>
    public SkillCastOutcome(SkillCastResult result, string failureReason)
    {
        Result = result;
        FailureReason = failureReason ?? string.Empty;
    }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    public static SkillCastOutcome Ok()
        => new SkillCastOutcome(SkillCastResult.Success, string.Empty);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    public static SkillCastOutcome Fail(SkillCastResult result, string reason)
        => new SkillCastOutcome(result, reason);
}
