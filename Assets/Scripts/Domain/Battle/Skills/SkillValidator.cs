/// <summary>
/// 技能合法性校验器：释放前的前置条件检查。
/// 校验失败直接终止释放（不进入目标选取与效果管线）。
/// </summary>
public sealed class SkillValidator
{
    /// <summary>
    /// 校验施法者是否满足释放条件。
    /// </summary>
    /// <param name="caster">施法单位</param>
    /// <param name="skill">技能配置</param>
    /// <param name="failureReason">输出：失败原因</param>
    /// <returns>是否通过校验</returns>
    public bool Validate(BattleUnit caster, SkillConfig skill, out string failureReason)
    {
        failureReason = string.Empty;

        if (skill == null)
        {
            failureReason = "技能配置不存在。";
            return false;
        }

        if (caster == null)
        {
            failureReason = "施法者不存在。";
            return false;
        }

        if (caster.IsDead)
        {
            failureReason = $"施法者 {caster.DisplayName} 已阵亡。";
            return false;
        }

        if (skill.Effects == null || skill.Effects.Count == 0)
        {
            failureReason = $"技能 {skill.DisplayName} 未配置任何效果。";
            return false;
        }

        if (!caster.CanUseSkill(skill.SkillId))
        {
            caster.IsSkillOnCooldown(skill.SkillId, out var remainingTurns);
            if (remainingTurns > 0)
            {
                failureReason = $"技能 {skill.DisplayName} 冷却中（剩余 {remainingTurns} 回合）。";
                return false;
            }

            failureReason = $"技能 {skill.DisplayName} 本场使用次数已耗尽。";
            return false;
        }

        return true;
    }
}
