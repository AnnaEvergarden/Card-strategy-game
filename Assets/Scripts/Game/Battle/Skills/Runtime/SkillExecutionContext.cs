using System.Collections.Generic;

/// <summary>
/// 技能执行上下文：流水线与 Effect 共享的运行时数据（含施法者、已解析目标、Outcome）。
/// 与 <see cref="SkillCastRequest"/> 区分：后者是 UI→门面的一次释放请求 DTO，不含 <see cref="BattleUnit"/>。
/// </summary>
public sealed class SkillExecutionContext
{
    #region Fields

    /// <summary>
    /// 技能目标列表。
    /// </summary>
    private readonly List<BattleUnit> _targets = new(4);

    /// <summary>
    /// 流水线执行期间暂存的伤害/死亡事件（成功后再统一发布，失败则丢弃）。
    /// </summary>
    private readonly List<IBattleEvent> _pendingEvents = new(8);

    #endregion

    #region Properties

    /// <summary>
    /// 施法者。
    /// </summary>
    public BattleUnit Caster { get; set; }

    /// <summary>
    /// 目标列表（只读视图）。
    /// </summary>
    public IReadOnlyList<BattleUnit> Targets => _targets;

    /// <summary>
    /// 技能静态配置。
    /// </summary>
    public SkillConfigSO SkillConfig { get; set; }

    /// <summary>
    /// 技能 id。
    /// </summary>
    public string SkillId { get; set; } = string.Empty;

    /// <summary>
    /// 本局战斗上下文。
    /// </summary>
    public BattleContext Battle { get; set; }

    /// <summary>
    /// 本次释放累积结果。
    /// </summary>
    public SkillCastOutcome Outcome { get; } = new();

    #endregion

    #region Public API

    /// <summary>
    /// 清空目标列表（供 <see cref="TargetSelector"/> 使用）。
    /// </summary>
    public void ClearTargets()
    {
        _targets.Clear();
    }

    /// <summary>
    /// 添加目标（供 <see cref="TargetSelector"/> 使用）。
    /// </summary>
    public void AddTarget(BattleUnit unit)
    {
        if (unit != null)
        {
            _targets.Add(unit);
        }
    }

    /// <summary>
    /// 暂存一次伤害及其可能触发的死亡事件（供 <see cref="BattleUnit.TakeDamage"/> 在流水线内调用）。
    /// </summary>
    public void StageDamageOutcome(string targetCardId, int appliedDamage, int remainingHp)
    {
        if (appliedDamage <= 0 || string.IsNullOrEmpty(targetCardId))
        {
            return;
        }

        _pendingEvents.Add(new DamageEvent(targetCardId, appliedDamage, remainingHp));
        if (remainingHp <= 0)
        {
            _pendingEvents.Add(new DeadEvent(targetCardId));
        }
    }

    /// <summary>
    /// 流水线全部 Effect 成功后，按发生顺序发布暂存事件。
    /// </summary>
    public void FlushPendingEvents()
    {
        if (Battle?.Events == null || _pendingEvents.Count == 0)
        {
            _pendingEvents.Clear();
            return;
        }

        for (var i = 0; i < _pendingEvents.Count; i++)
        {
            Battle.Events.Publish(_pendingEvents[i]);
        }

        _pendingEvents.Clear();
    }

    /// <summary>
    /// 流水线失败或回滚时丢弃暂存事件，避免 UI 收到已撤销的伤害/死亡。
    /// </summary>
    public void DiscardPendingEvents()
    {
        _pendingEvents.Clear();
    }

    #endregion
}
