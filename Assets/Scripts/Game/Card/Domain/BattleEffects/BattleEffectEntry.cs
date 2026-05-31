using System;
using UnityEngine;

/// <summary>
/// 技能效果配置基类：分为 <see cref="InstantEffectEntry"/> 与 <see cref="BuffEffectEntry"/>。
/// </summary>
[Serializable]
public abstract class BattleEffectEntry
{
    #region Properties

    /// <summary>
    /// 效果大类。
    /// </summary>
    public abstract SkillEffectCategory Category { get; }

    /// <summary>
    /// 是否可在战斗中实例化为 <see cref="ISkillEffect"/>。
    /// </summary>
    public abstract bool IsValid { get; }

    #endregion
}
