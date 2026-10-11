/// <summary>
/// 治疗效果：为每个目标回复 Value 点生命。
/// </summary>
public sealed class HealEffect : ISkillEffect
{
    /// <summary>
    /// 执行治疗并登记回滚记录。
    /// </summary>
    public bool Execute(SkillEffectConfig config, SkillExecutionContext context, out string failureReason)
    {
        failureReason = string.Empty;

        if (config.Value <= 0)
        {
            failureReason = "治疗值必须大于 0。";
            return false;
        }

        for (var i = 0; i < context.Targets.Count; i++)
        {
            var target = context.Targets[i];
            if (target.IsDead)
                continue;

            target.Heal(config.Value, out var healed);
            context.RecordRollback(new HealRollback(target, healed));
        }

        return true;
    }

    /// <summary>治疗回滚：扣回被回复的 HP。</summary>
    private sealed class HealRollback : ISkillEffectRollback
    {
        private readonly BattleUnit _target;
        private readonly int _healed;

        public HealRollback(BattleUnit target, int healed)
        {
            _target = target;
            _healed = healed;
        }

        public void Rollback()
        {
            _target.RestoreHpForRollback(-_healed);
        }
    }
}
