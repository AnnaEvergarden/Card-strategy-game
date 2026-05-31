using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个技能的静态配置：展示文案、战斗规则与效果列表（Instant / Buff）。
/// </summary>
[CreateAssetMenu(menuName = "Game/Card/Skill Config", fileName = "SkillConfig_")]
public sealed class SkillConfigSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 技能唯一 ID（与舰娘 <see cref="CardConfigSO"/> 中引用的字符串一致）。
    /// </summary>
    [SerializeField] private string skillId;

    /// <summary>
    /// 技能展示名称。
    /// </summary>
    [SerializeField] private string displayName;

    /// <summary>
    /// 技能详情说明（船坞详情、战斗 Tooltip 等）。
    /// </summary>
    [TextArea(2, 8)]
    [SerializeField] private string description;

    /// <summary>
    /// 目标选择规则。
    /// </summary>
    [SerializeField] private SkillTargetKind targetKind = SkillTargetKind.None;

    /// <summary>
    /// 战斗效果列表：每项先选 Instant/Buff，再选具体效果与数值。
    /// </summary>
    [Header("效果列表")]
    [SerializeField] private List<SkillEffectListItem> effectList = new();

    /// <summary>
    /// 冷却回合数（战斗规则启用时使用；0 表示无冷却或即时，具体含义由战斗系统定义）。
    /// </summary>
    [SerializeField] private int cooldownTurns;

    /// <summary>
    /// 技能所属阵营（筛选、Tooltip 展示；与舰娘 <see cref="CardConfigSO.Faction"/> 语义一致）。
    /// </summary>
    [SerializeField] private ShipFaction shipFaction = ShipFaction.Other;

    /// <summary>
    /// 技能图标 Resources 相对路径（不含扩展名）；留空则使用 <see cref="GameResourcePaths.BuildSkillIconPath"/>（展示名）。
    /// </summary>
    [SerializeField] private string skillIconResourcePath;

    /// <summary>
    /// 可使用次数类型：无限制或有限次数。
    /// </summary>
    [SerializeField] private SkillUseLimitKind useLimitKind = SkillUseLimitKind.Unlimited;

    /// <summary>
    /// 有限次数时的总可用次数（仅 <see cref="useLimitKind"/> 为 <see cref="SkillUseLimitKind.Limited"/> 时生效）。
    /// </summary>
    [SerializeField] private int limitedUseCount = 1;

    #endregion

    #region Public API

    /// <summary>
    /// 技能 ID。
    /// </summary>
    public string SkillId => skillId;

    /// <summary>
    /// 展示名称。
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// 详情描述。
    /// </summary>
    public string Description => description;

    /// <summary>
    /// 目标规则。
    /// </summary>
    public SkillTargetKind TargetKind => targetKind;

    /// <summary>
    /// 效果列表（只读）。
    /// </summary>
    public IReadOnlyList<SkillEffectListItem> EffectList => effectList;

    /// <summary>
    /// 是否配置了可执行效果。
    /// </summary>
    public bool HasExecutableEffects
    {
        get
        {
            if (effectList == null || effectList.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < effectList.Count; i++)
            {
                if (effectList[i] != null && effectList[i].IsValid)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 冷却回合数。
    /// </summary>
    public int CooldownTurns => cooldownTurns;

    /// <summary>
    /// 技能阵营。
    /// </summary>
    public ShipFaction ShipFaction => shipFaction;

    /// <summary>
    /// 技能图标 Resources 路径（Inspector 显式覆盖；未填时见 <see cref="ResolveSkillIconResourcePath"/>）。
    /// </summary>
    public string SkillIconResourcePath => skillIconResourcePath;

    /// <summary>
    /// 解析技能图标 Resources 路径：优先 Inspector 覆盖，否则 <c>Art/Icon/Skills/{DisplayName}</c>。
    /// </summary>
    public string ResolveSkillIconResourcePath()
    {
        if (!string.IsNullOrWhiteSpace(skillIconResourcePath))
        {
            return skillIconResourcePath.Trim().Replace('\\', '/').Trim('/');
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return GameResourcePaths.BuildSkillIconPath(displayName);
        }

        return string.Empty;
    }

    /// <summary>
    /// 可使用次数限制类型。
    /// </summary>
    public SkillUseLimitKind UseLimitKind => useLimitKind;

    /// <summary>
    /// 有限次数配置值（策划在 Inspector 填写；无限制时忽略）。
    /// </summary>
    public int LimitedUseCount => limitedUseCount;

    /// <summary>
    /// 是否为无限制使用次数。
    /// </summary>
    public bool IsUnlimitedUses => useLimitKind == SkillUseLimitKind.Unlimited;

    /// <summary>
    /// 配置上的最大可用次数；无限制时返回 <see cref="int.MaxValue"/>。
    /// </summary>
    public int ConfiguredMaxUses => IsUnlimitedUses ? int.MaxValue : Mathf.Max(1, limitedUseCount);

    /// <summary>
    /// 生成 Tooltip / 说明用的一行「可使用次数」文案。
    /// </summary>
    public string FormatUseLimitLine()
    {
        return IsUnlimitedUses ? "可使用次数：无限制" : $"可使用次数：{ConfiguredMaxUses} 次";
    }

    /// <summary>
    /// 从效果列表读取刷新冷却概率（0～100）。
    /// </summary>
    public int RefreshCooldownChancePercent
    {
        get
        {
            if (effectList == null)
            {
                return 0;
            }

            for (var i = 0; i < effectList.Count; i++)
            {
                var item = effectList[i];
                if (item?.Category != SkillEffectCategory.Instant)
                {
                    continue;
                }

                var instant = item.Instant;
                if (instant != null && instant.InstantKind == SkillInstantKind.RefreshCooldown)
                {
                    return instant.ChancePercent;
                }
            }

            return 0;
        }
    }

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：校验并同步资产文件名。
    /// </summary>
    private void OnValidate()
    {
        if (useLimitKind == SkillUseLimitKind.Limited)
        {
            limitedUseCount = Mathf.Max(1, limitedUseCount);
        }

        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
    }

    /// <summary>
    /// 期望文件名前缀 Skill + 显示名 + id。
    /// </summary>
    private string BuildPreferredAssetBaseName()
    {
        return ScriptableObjectAssetRenameUtility.BuildPreferredBaseNamePrefixDisplayId(
            "Skill",
            skillId,
            displayName,
            "Skill_");
    }
#endif
}
