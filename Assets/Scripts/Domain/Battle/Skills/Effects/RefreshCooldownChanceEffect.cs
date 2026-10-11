using System.Collections.Generic;

/// <summary>
/// 冷却刷新效果：施法者每个冷却中的技能有 Chance 概率立即重置冷却。
/// </summary>
public sealed class RefreshCooldownChanceEffect : ISkillEffect
{
    /// <summary>
    /// 执行冷却刷新并登记回滚记录（记录被刷新技能的原冷却值）。
    /// </summary>
    public bool Execute(SkillEffectConfig config, SkillExecutionContext context, out string failureReason)
    {
        failureReason = string.Empty;

        if (config.Chance <= 0f)
        {
            failureReason = "刷新概率必须大于 0。";
            return false;
        }

        if (config.Chance > 1f)
        {
            failureReason = "刷新概率不能大于 1。";
            return false;
        }

        // 快照当前冷却表（遍历期间不可修改字典，回滚记录也基于快照）。
        var snapshot = new List<KeyValuePair<string, int>>(context.Caster.SkillCooldowns);
        for (var i = 0; i < snapshot.Count; i++)
        {
            var pair = snapshot[i];
            if (pair.Value <= 0)
                continue;

            if (context.Random.NextDouble() >= config.Chance)
                continue;

            context.Caster.SetSkillCooldown(pair.Key, 0);
            context.RecordRollback(new CooldownRollback(context.Caster, pair.Key, pair.Value));
        }

        return true;
    }

    /// <summary>冷却回滚：恢复被刷新技能的原冷却值。</summary>
    private sealed class CooldownRollback : ISkillEffectRollback
    {
        private readonly BattleUnit _caster;
        private readonly string _skillId;
        private readonly int _originalTurns;

        public CooldownRollback(BattleUnit caster, string skillId, int originalTurns)
        {
            _caster = caster;
            _skillId = skillId;
            _originalTurns = originalTurns;
        }

        public void Rollback()
        {
            _caster.SetSkillCooldown(_skillId, _originalTurns);
        }
    }
}
