/// <summary>
/// 技能效果：可复用行为单元（伤害、治疗、Buff 等），由 <see cref="SkillPipeline"/> 顺序执行。
/// </summary>
public interface ISkillEffect
{
    #region Methods

    /// <summary>
    /// 执行效果。
    /// </summary>
    /// <param name="context">技能执行上下文。</param>
    /// <returns>是否成功；为 false 时流水线中止并回滚已生效 Effect。</returns>
    bool TryExecute(SkillExecutionContext context, out ISkillEffectRollback rollback);

    #endregion
}
