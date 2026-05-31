/// <summary>
/// 技能流程事件：释放完成（由 <see cref="SkillSystem"/> 发布）。
/// </summary>
public sealed class SkillCastFinishEvent : IBattleEvent
{
    #region Fields

    /// <summary>
    /// 施法者 cardId。
    /// </summary>
    public readonly string CasterCardId;

    /// <summary>
    /// 技能 id。
    /// </summary>
    public readonly string SkillId;

    /// <summary>
    /// 是否执行成功。
    /// </summary>
    public readonly bool Success;

    /// <summary>
    /// 结果说明。
    /// </summary>
    public readonly string Message;

    #endregion

    #region Constructors

    /// <summary>
    /// 构造技能完成事件。
    /// </summary>
    public SkillCastFinishEvent(string casterCardId, string skillId, bool success, string message)
    {
        CasterCardId = casterCardId ?? string.Empty;
        SkillId = skillId ?? string.Empty;
        Success = success;
        Message = message ?? string.Empty;
    }

    #endregion
}
