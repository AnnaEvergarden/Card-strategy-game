/// <summary>
/// 技能效果接口：效果的真正逻辑核心。
/// 一个效果类服务全部技能（参数来自 SkillEffectConfig，数据驱动），
/// 执行成功必须通过 context.RecordRollback 登记撤销记录，供管线失败时逆序回滚。
/// </summary>
public interface ISkillEffect
{
    /// <summary>
    /// 执行效果。
    /// </summary>
    /// <param name="config">效果参数</param>
    /// <param name="context">技能执行上下文</param>
    /// <param name="failureReason">输出：失败原因</param>
    /// <returns>是否成功</returns>
    bool Execute(SkillEffectConfig config, SkillExecutionContext context, out string failureReason);
}

/// <summary>
/// 效果回滚记录：撤销单个已生效效果的原子操作。
/// 由各效果在 Execute 成功时登记，管线失败时逆序执行 Rollback。
/// </summary>
public interface ISkillEffectRollback
{
    /// <summary>
    /// 撤销本记录对应的已生效改动（静默，不发事件）。
    /// </summary>
    void Rollback();
}
