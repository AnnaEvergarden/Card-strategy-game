using System.Collections.Generic;

/// <summary>
/// 技能效果注册表：SkillEffectKind → ISkillEffect 实例。
/// 效果类无状态（参数来自配置），注册单例复用；
/// 新增效果种类 = 枚举值 + 效果类 + 此处注册，不修改管线与技能系统。
/// </summary>
public sealed class SkillEffectRegistry
{
    private readonly Dictionary<SkillEffectKind, ISkillEffect> _effects =
        new Dictionary<SkillEffectKind, ISkillEffect>();

    /// <summary>
    /// 构造注册表并登记内置效果。
    /// </summary>
    public SkillEffectRegistry()
    {
        Register(SkillEffectKind.Damage, new DamageEffect());
        Register(SkillEffectKind.Heal, new HealEffect());
        Register(SkillEffectKind.DefenseBuff, new DefenseBuffEffect());
        Register(SkillEffectKind.SelfHpDrain, new SelfHpDrainEffect());
        Register(SkillEffectKind.RefreshCooldownChance, new RefreshCooldownChanceEffect());
    }

    /// <summary>
    /// 注册/替换效果。
    /// </summary>
    public void Register(SkillEffectKind kind, ISkillEffect effect)
    {
        if (effect != null && kind != SkillEffectKind.None)
            _effects[kind] = effect;
    }

    /// <summary>
    /// 按种类获取效果。
    /// </summary>
    /// <returns>效果实例；未注册返回 null</returns>
    public ISkillEffect TryGet(SkillEffectKind kind)
    {
        return _effects.TryGetValue(kind, out var effect) ? effect : null;
    }
}
