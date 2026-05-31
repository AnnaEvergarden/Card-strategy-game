using System.Collections.Generic;

/// <summary>
/// 技能 Effect 常用回滚记录（HP、Buff、冷却）。
/// </summary>
internal static class SkillEffectRollbackRecords
{
    #region Nested Types

    /// <summary>
    /// 单条 HP 变更回滚：正数为治疗，负数为伤害。
    /// 使用 <see cref="BattleUnit.UnitId"/> 而非 cardId+Side，避免同名卡牌双方在场时定位错误。
    /// </summary>
    public sealed class HpChangeRollback : ISkillEffectRollback
    {
        private readonly List<(string unitId, int hpDelta)> _changes = new(4);

        /// <param name="unitId">目标 <see cref="BattleUnit.UnitId"/>。</param>
        /// <param name="hpDelta">记录值（伤害为负数，治疗为正数）。</param>
        public void Record(string unitId, int hpDelta)
        {
            if (hpDelta == 0 || string.IsNullOrEmpty(unitId))
            {
                return;
            }
            _changes.Add((unitId, hpDelta));
        }

        public void Rollback()
        {
            var field = BattleContext.Current?.Field;
            for (var i = _changes.Count - 1; i >= 0; i--)
            {
                var (unitId, hpDelta) = _changes[i];
                field?.GetUnit(unitId)?.ApplyHpRollback(hpDelta);
            }
        }
    }

    /// <summary>
    /// 防御 Buff 回滚：移除 Buff 条目并扣回运行时防御。
    /// </summary>
    public sealed class DefenseBuffRollback : ISkillEffectRollback
    {
        private readonly List<(string cardId, BattleBuffState.RuntimeBuff buff)> _applied = new(4);

        public void Record(string cardId, BattleBuffState.RuntimeBuff buff)
        {
            if (buff == null || string.IsNullOrEmpty(cardId))
            {
                return;
            }
            _applied.Add((cardId, buff));
        }

        public void Rollback()
        {
            for (var i = _applied.Count - 1; i >= 0; i--)
            {
                var (cardId, buff) = _applied[i];
                BattleBuffState.TryRemoveBuff(cardId, buff);
            }
        }
    }

    /// <summary>
    /// 技能冷却回滚：恢复清空前的剩余回合。
    /// </summary>
    private sealed class CooldownRollback : ISkillEffectRollback
    {
        private readonly string _unitId;
        private readonly string _skillId;
        private readonly int _previousRemaining;
        private readonly bool _hadEntry;

        public CooldownRollback(string unitId, string skillId, int previousRemaining, bool hadEntry)
        {
            _unitId = unitId;
            _skillId = skillId;
            _previousRemaining = previousRemaining;
            _hadEntry = hadEntry;
        }

        public void Rollback()
        {
            BattleContext.Current?.Field?.GetUnit(_unitId)
                ?.RestoreCooldown(_skillId, _previousRemaining, _hadEntry);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 创建 HP 变更回滚记录器。
    /// </summary>
    public static HpChangeRollback CreateHpRollback() => new();

    /// <summary>
    /// 创建防御 Buff 回滚记录器。
    /// </summary>
    public static DefenseBuffRollback CreateDefenseBuffRollback() => new();

    /// <summary>
    /// 根据清空前的冷却快照构建回滚对象；未实际清空时返回 null。
    /// </summary>
    public static ISkillEffectRollback TryCreateCooldownRollback(
        string unitId,
        string skillId,
        int previousRemaining,
        bool hadEntry,
        bool cleared)
    {
        if (!cleared)
        {
            return null;
        }
        return new CooldownRollback(unitId, skillId, previousRemaining, hadEntry);
    }

    #endregion
}
