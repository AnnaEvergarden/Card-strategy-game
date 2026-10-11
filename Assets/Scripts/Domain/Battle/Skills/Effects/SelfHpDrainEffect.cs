/// <summary>
/// 自我损耗效果：施法者直接失去 Value 点生命（无视防御的技能代价，可致死）。
/// </summary>
public sealed class SelfHpDrainEffect : ISkillEffect
{
    /// <summary>
    /// 执行自我损耗并登记回滚记录。
    /// </summary>
    public bool Execute(SkillEffectConfig config, SkillExecutionContext context, out string failureReason)
    {
        failureReason = string.Empty;

        if (config.Value <= 0)
        {
            failureReason = "损耗值必须大于 0。";
            return false;
        }

        if (context.Caster.IsDead)
        {
            failureReason = "施法者已阵亡，无法支付代价。";
            return false;
        }

        context.Caster.DrainHp(config.Value, out var drained);
        context.RecordRollback(new DrainRollback(context.Caster, drained));

        // 代价致死：技能释放失败（管线回滚恢复施法者 HP，避免"次数已扣但人没了"的结算矛盾）。
        if (context.Caster.IsDead)
        {
            failureReason = "施法者因代价阵亡，技能未生效。";
            return false;
        }

        return true;
    }

    /// <summary>损耗回滚：恢复施法者失去的 HP。</summary>
    private sealed class DrainRollback : ISkillEffectRollback
    {
        private readonly BattleUnit _caster;
        private readonly int _drained;

        public DrainRollback(BattleUnit caster, int drained)
        {
            _caster = caster;
            _drained = drained;
        }

        public void Rollback()
        {
            _caster.RestoreHpForRollback(_drained);
        }
    }
}
