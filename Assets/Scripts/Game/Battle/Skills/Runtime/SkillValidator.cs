/// <summary>
/// 技能合法性检查：死亡、冷却、次数等（不执行 Effect、不扣血）。
/// </summary>
public static class SkillValidator
{
    #region Public API

    /// <summary>
    /// 校验施法者与技能配置是否允许释放。
    /// </summary>
    public static bool Validate(BattleUnit caster, SkillConfigSO skillConfig, out string failureReason)
    {
        failureReason = null;
        if (caster == null)
        {
            failureReason = "施法者无效";
            return false;
        }

        if (caster.IsDead)
        {
            failureReason = "施法者已无法战斗";
            return false;
        }

        if (skillConfig == null || string.IsNullOrWhiteSpace(skillConfig.SkillId))
        {
            failureReason = "技能配置无效";
            return false;
        }

        if (!caster.CanUseSkill(skillConfig))
        {
            if (caster.GetSkillCooldownRemaining(skillConfig.SkillId) > 0)
            {
                failureReason = "技能冷却中";
            }
            else
            {
                failureReason = "技能次数不足";
            }

            return false;
        }

        return true;
    }

    #endregion
}
