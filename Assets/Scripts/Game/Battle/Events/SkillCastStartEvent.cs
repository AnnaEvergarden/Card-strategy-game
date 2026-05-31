/// <summary>
/// 技能流程事件：开始释放（由 <see cref="SkillSystem"/> 发布）。
/// </summary>
public sealed class SkillCastStartEvent : IBattleEvent
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

    #endregion

    #region Constructors

    /// <summary>
    /// 构造技能开始事件。
    /// </summary>
    public SkillCastStartEvent(string casterCardId, string skillId)
    {
        CasterCardId = casterCardId ?? string.Empty;
        SkillId = skillId ?? string.Empty;
    }

    #endregion
}
