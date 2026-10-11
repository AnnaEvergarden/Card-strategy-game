using System;

/// <summary>
/// 技能效果配置：一条效果的种类与参数。
/// 由 SkillConfigTable JSON（ConfigService 加载）反序列化，效果参数完全数据驱动——
/// 策划改数值不需要改代码，新增效果种类才需要写 ISkillEffect 实现。
/// </summary>
[Serializable]
public class SkillEffectConfig
{
    /// <summary>效果种类。</summary>
    public SkillEffectKind Kind;

    /// <summary>数值参数（伤害/治疗/防御值等，含义由种类决定）。</summary>
    public int Value;

    /// <summary>持续回合（Buff 类效果使用，即时效果为 0）。</summary>
    public int Duration;

    /// <summary>触发概率（0~1，概率类效果使用，其余忽略）。</summary>
    public float Chance = 1f;
}
