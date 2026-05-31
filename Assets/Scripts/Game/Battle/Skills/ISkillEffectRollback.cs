/// <summary>
/// 技能效果回滚：流水线失败时按相反顺序撤销已生效的状态变更。
/// </summary>
public interface ISkillEffectRollback
{
    #region Methods

    /// <summary>
    /// 撤销本 Effect 已写入的战斗状态（不撤销已发布的表现层事件）。
    /// </summary>
    void Rollback();

    #endregion
}
