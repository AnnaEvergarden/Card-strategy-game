using System;
using System.Collections.Generic;

/// <summary>
/// 技能执行上下文：一次释放过程中的共享运行时数据
/// （Skill = Effect Pipeline + Context 中的 Context）。
/// 效果只通过上下文读写战斗状态，避免效果方法参数膨胀。
/// </summary>
public sealed class SkillExecutionContext
{
    private readonly List<ISkillEffectRollback> _rollbackRecords = new List<ISkillEffectRollback>();

    /// <summary>施法单位。</summary>
    public BattleUnit Caster { get; }

    /// <summary>本次选取的目标列表。</summary>
    public IReadOnlyList<BattleUnit> Targets { get; }

    /// <summary>技能配置。</summary>
    public SkillConfig Skill { get; }

    /// <summary>战场（效果需要时的全局视图）。</summary>
    public BattleField Field { get; }

    /// <summary>随机源（会话注入可播种 Random，保证单元测试确定性；效果不得自行 new Random）。</summary>
    public Random Random { get; }

    /// <summary>
    /// 构造技能执行上下文。
    /// </summary>
    public SkillExecutionContext(BattleUnit caster, IReadOnlyList<BattleUnit> targets,
        SkillConfig skill, BattleField field, Random random)
    {
        Caster = caster;
        Targets = targets ?? new List<BattleUnit>(0);
        Skill = skill;
        Field = field;
        Random = random ?? new Random();
    }

    /// <summary>
    /// 登记回滚记录：效果成功生效后必须调用，管线失败时逆序执行。
    /// </summary>
    public void RecordRollback(ISkillEffectRollback rollback)
    {
        if (rollback != null)
            _rollbackRecords.Add(rollback);
    }

    /// <summary>
    /// 逆序执行全部回滚记录并清空（静默，不发事件）。
    /// </summary>
    public void RollbackAll()
    {
        for (var i = _rollbackRecords.Count - 1; i >= 0; i--)
            _rollbackRecords[i].Rollback();
        _rollbackRecords.Clear();
    }
}
