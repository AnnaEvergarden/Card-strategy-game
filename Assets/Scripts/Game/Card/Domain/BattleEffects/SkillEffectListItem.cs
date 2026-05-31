using System;
using UnityEngine;

/// <summary>
/// 技能效果列表项：Inspector 先选 Instant/Buff，再选子类型并填参（由 <see cref="SkillEffectListItemDrawer"/> 绘制）。
/// </summary>
[Serializable]
public sealed class SkillEffectListItem
{
    #region Fields

    /// <summary>
    /// 效果大类（即时 / Buff）。
    /// </summary>
    [SerializeField] private SkillEffectCategory category = SkillEffectCategory.Instant;

    /// <summary>
    /// 即时效果配置（category 为 Instant 时使用）。
    /// </summary>
    [SerializeField] private InstantEffectEntry instant = new();

    /// <summary>
    /// Buff 配置（category 为 Buff 时使用）。
    /// </summary>
    [SerializeField] private BuffEffectEntry buff = new();

    #endregion

    #region Properties

    /// <summary>
    /// 效果大类。
    /// </summary>
    public SkillEffectCategory Category => category;

    /// <summary>
    /// 即时配置。
    /// </summary>
    public InstantEffectEntry Instant => instant;

    /// <summary>
    /// Buff 配置。
    /// </summary>
    public BuffEffectEntry Buff => buff;

    /// <summary>
    /// 当前项是否有效。
    /// </summary>
    public bool IsValid
    {
        get
        {
            return category switch
            {
                SkillEffectCategory.Instant => instant != null && instant.IsValid,
                SkillEffectCategory.Buff => buff != null && buff.IsValid,
                _ => false
            };
        }
    }

    /// <summary>
    /// 解析为战斗用配置条目（供 Factory 使用）。
    /// </summary>
    public BattleEffectEntry ResolveEntry()
    {
        return category switch
        {
            SkillEffectCategory.Instant => instant,
            SkillEffectCategory.Buff => buff,
            _ => null
        };
    }

    #endregion
}
