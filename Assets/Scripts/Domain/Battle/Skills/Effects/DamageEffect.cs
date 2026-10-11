using System.Collections.Generic;

/// <summary>
/// 伤害效果：对每个目标造成 Value 点伤害（经目标防御结算）。
/// </summary>
public sealed class DamageEffect : ISkillEffect
{
    /// <summary>
    /// 执行伤害：逐目标结算并登记回滚记录。
    /// </summary>
    public bool Execute(SkillEffectConfig config, SkillExecutionContext context, out string failureReason)
    {
        failureReason = string.Empty;

        if (config.Value <= 0)
        {
            failureReason = "伤害值必须大于 0。";
            return false;
        }

        for (var i = 0; i < context.Targets.Count; i++)
        {
            var target = context.Targets[i];
            if (target.IsDead)
                continue;

            target.TakeDamage(config.Value, out var applied);
            context.RecordRollback(new DamageRollback(target, applied));
        }

        return true;
    }

    /// <summary>伤害回滚：恢复被扣减的 HP。</summary>
    private sealed class DamageRollback : ISkillEffectRollback
    {
        private readonly BattleUnit _target;
        private readonly int _applied;

        public DamageRollback(BattleUnit target, int applied)
        {
            _target = target;
            _applied = applied;
        }

        public void Rollback()
        {
            _target.RestoreHpForRollback(_applied);
        }
    }
}
