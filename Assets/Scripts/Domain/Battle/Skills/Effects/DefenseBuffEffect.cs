/// <summary>
/// 防御强化效果：为每个目标添加 DefenseBuff（Value 点防御、持续 Duration 回合）。
/// </summary>
public sealed class DefenseBuffEffect : ISkillEffect
{
    /// <summary>
    /// 执行 Buff 添加并登记回滚记录。
    /// </summary>
    public bool Execute(SkillEffectConfig config, SkillExecutionContext context, out string failureReason)
    {
        failureReason = string.Empty;

        if (config.Value <= 0 || config.Duration <= 0)
        {
            failureReason = "防御 Buff 需要正值与持续时间。";
            return false;
        }

        for (var i = 0; i < context.Targets.Count; i++)
        {
            var target = context.Targets[i];
            if (target.IsDead)
                continue;

            target.AddBuff(new Buff(BuffKind.DefenseBuff, config.Value, config.Duration));
            context.RecordRollback(new BuffRollback(target, BuffKind.DefenseBuff));
        }

        return true;
    }

    /// <summary>Buff 回滚：移除刚添加的 Buff。</summary>
    private sealed class BuffRollback : ISkillEffectRollback
    {
        private readonly BattleUnit _target;
        private readonly BuffKind _kind;

        public BuffRollback(BattleUnit target, BuffKind kind)
        {
            _target = target;
            _kind = kind;
        }

        public void Rollback()
        {
            _target.RemoveBuffForRollback(_kind);
        }
    }
}
