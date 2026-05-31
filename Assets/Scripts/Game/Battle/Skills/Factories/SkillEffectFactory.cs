using System.Collections.Generic;

/// <summary>
/// 将 <see cref="BattleEffectEntry"/> 配置实例化为战斗 <see cref="ISkillEffect"/>。
/// </summary>
public static class SkillEffectFactory
{
    #region Public API

    /// <summary>
    /// 从技能效果列表按顺序构建流水线。
    /// </summary>
    public static ISkillEffect[] BuildPipelineFromEffectList(IReadOnlyList<SkillEffectListItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return System.Array.Empty<ISkillEffect>();
        }

        var list = new List<ISkillEffect>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var effect = CreateFromListItem(items[i]);
            if (effect != null)
            {
                list.Add(effect);
            }
        }

        return list.Count == 0 ? System.Array.Empty<ISkillEffect>() : list.ToArray();
    }

    /// <summary>
    /// 单条列表项 → Effect。
    /// </summary>
    public static ISkillEffect CreateFromListItem(SkillEffectListItem item)
    {
        if (item == null || !item.IsValid)
        {
            return null;
        }

        return CreateFromEntry(item.ResolveEntry());
    }

    /// <summary>
    /// 配置条目 → Effect。
    /// </summary>
    public static ISkillEffect CreateFromEntry(BattleEffectEntry entry)
    {
        if (entry == null || !entry.IsValid)
        {
            return null;
        }

        switch (entry)
        {
            case InstantEffectEntry instant:
                return CreateInstant(instant);
            case BuffEffectEntry buff:
                return CreateBuff(buff);
            default:
                return null;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 实例化即时效果。
    /// </summary>
    private static ISkillEffect CreateInstant(InstantEffectEntry instant)
    {
        if (instant == null)
        {
            return null;
        }

        switch (instant.InstantKind)
        {
            case SkillInstantKind.Damage:
                return new DamageEffect(instant.Value);
            case SkillInstantKind.RefreshCooldown:
                return new RefreshCooldownChanceEffect(instant.ChancePercent);
            case SkillInstantKind.Heal:
                return new HealEffect(instant.Value);
            case SkillInstantKind.SelfHpDrain:
                return new SelfHpDrainEffect(instant.Value);
            default:
                return null;
        }
    }

    /// <summary>
    /// 实例化 Buff 效果（释放时挂载；持续回合由 Buff 系统扩展）。
    /// </summary>
    private static ISkillEffect CreateBuff(BuffEffectEntry buff)
    {
        if (buff == null)
        {
            return null;
        }

        switch (buff.BuffKind)
        {
            case SkillBuffKind.DefenseBuff:
                return new DefenseBuffEffect(buff.Value, buff.DurationTurns);
            default:
                return null;
        }
    }

    #endregion
}
