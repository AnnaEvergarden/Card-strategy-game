using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 舰娘/卡牌稀有度（配置表与 UI 展示共用；0～5 为固定档位勿改序号，避免已存资产与存档语义漂移）。
/// 注释格式：中文（English）。
/// </summary>
public enum CardRarity
{
    /// <summary>
    /// 普通（Normal）。
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 稀有（Rare）。
    /// </summary>
    Rare = 1,

    /// <summary>
    /// 精锐（Elite）。
    /// </summary>
    Elite = 2,

    /// <summary>
    /// 超稀有（Super Rare）。
    /// </summary>
    SuperRare = 3,

    /// <summary>
    /// 海上传奇（Sea Legend）。
    /// </summary>
    SeaLegend = 4,

    /// <summary>
    /// 活动（Activity）。
    /// </summary>
    Activity = 5
}

/// <summary>
/// 单张舰娘（卡牌）静态配置：中文展示名、英文名与生命值等静态数值。
/// 图标资源路径在运行时由「固定根目录 + 英文名」拼接，不再在 OnValidate 自动写入。
/// </summary>
[CreateAssetMenu(menuName = "Game/Card/Card Config", fileName = "CardConfig_")]
public sealed class CardConfigSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 舰娘配置 ID，需与仓库中的 cardId 一致。
    /// </summary>
    [SerializeField] private string cardId;

    /// <summary>
    /// 舰娘中文展示名称（用于 UI 显示）。
    /// </summary>
    [SerializeField] private string displayName;

    /// <summary>
    /// 舰娘英文名（用于定位图标资源名，建议仅使用英文/数字/下划线）。
    /// </summary>
    [SerializeField] private string englishName;

    /// <summary>
    /// 舰娘所属阵营（与卡池 <see cref="BuildPoolConfigSO.PoolFaction"/> 对应；非混池卡池仅允许同阵营掉落）。
    /// </summary>
    [SerializeField] private ShipFaction shipFaction = ShipFaction.Other;

    /// <summary>
    /// 生命值。
    /// </summary>
    [SerializeField] private int hp;

    /// <summary>
    /// 舰娘稀有度：普通（Normal）、稀有（Rare）、精锐（Elite）、超稀有（Super Rare）、海上传奇（Sea Legend）、活动（Activity）。
    /// </summary>
    [SerializeField] private CardRarity rarity = CardRarity.Normal;

    /// <summary>
    /// 舰娘携带的技能 ID 列表（上限 <see cref="MaxSkillsPerCard"/>；在编辑器中会裁剪重复与空项）。
    /// 与 <see cref="skillRefs"/> 成对维护，运行时可由 <see cref="GetSkillRefsOrdered"/> 直接读取 SO。
    /// </summary>
    [SerializeField] private string[] skillIds = System.Array.Empty<string>();

    /// <summary>
    /// 舰娘携带的技能配置引用（与 <see cref="skillIds"/> 一一对应；由编辑器同步工具维护）。
    /// </summary>
    [SerializeField] private SkillConfigSO[] skillRefs = System.Array.Empty<SkillConfigSO>();

    /// <summary>
    /// 单舰娘可装备技能数量上限。
    /// </summary>
    public const int MaxSkillsPerCard = 3;

    /// <summary>
    /// 运行时缓存：<see cref="GetSkillIdsOrdered"/> 的结果缓存（<see cref="OnValidate"/> 时清空）。
    /// </summary>
    private IReadOnlyList<string> _cachedSkillIds;

    /// <summary>
    /// 运行时缓存：<see cref="GetSkillRefsOrdered"/> 的结果缓存（<see cref="OnValidate"/> 时清空）。
    /// </summary>
    private IReadOnlyList<SkillConfigSO> _cachedSkillRefs;

    #endregion

    #region Public API

    /// <summary>
    /// 舰娘配置 ID。
    /// </summary>
    public string CardId => cardId;

    /// <summary>
    /// 舰娘展示名称（图标文件名与之相同，除非另行约定）。
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// 舰娘英文名（用于图标查找）。
    /// </summary>
    public string EnglishName => englishName;

    /// <summary>
    /// 舰娘阵营。
    /// </summary>
    public ShipFaction Faction => shipFaction;

    /// <summary>
    /// 生命值。
    /// </summary>
    public int HP => hp;

    /// <summary>
    /// 舰娘稀有度（见 <see cref="CardRarity"/> 各档中英文注释）。
    /// </summary>
    public CardRarity Rarity => rarity;

    /// <summary>
    /// 按配置顺序返回至多 <see cref="MaxSkillsPerCard"/> 个非空、去重后的技能 ID（运行时供详情 UI、战斗栏查询）。
    /// </summary>
    /// <returns>技能 ID 列表（长度 0～3）。</returns>
    public IReadOnlyList<string> GetSkillIdsOrdered()
    {
        if (_cachedSkillIds != null)
        {
            return _cachedSkillIds;
        }

        if (skillIds == null || skillIds.Length == 0)
        {
            _cachedSkillIds = System.Array.Empty<string>();
            return _cachedSkillIds;
        }

        var result = new List<string>(MaxSkillsPerCard);
        for (var i = 0; i < skillIds.Length; i++)
        {
            var raw = skillIds[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var id = raw.Trim();
            if (result.Contains(id))
            {
                continue;
            }

            result.Add(id);
            if (result.Count >= MaxSkillsPerCard)
            {
                break;
            }
        }

        _cachedSkillIds = result;
        return result;
    }

    /// <summary>
    /// 按槽位顺序返回非空的技能 SO（至多 <see cref="MaxSkillsPerCard"/> 个）。
    /// </summary>
    public IReadOnlyList<SkillConfigSO> GetSkillRefsOrdered()
    {
        if (_cachedSkillRefs != null)
        {
            return _cachedSkillRefs;
        }

        if (skillRefs == null || skillRefs.Length == 0)
        {
            _cachedSkillRefs = System.Array.Empty<SkillConfigSO>();
            return _cachedSkillRefs;
        }

        var result = new List<SkillConfigSO>(MaxSkillsPerCard);
        for (var i = 0; i < skillRefs.Length && result.Count < MaxSkillsPerCard; i++)
        {
            var sk = skillRefs[i];
            if (sk == null)
            {
                continue;
            }

            var duplicate = false;
            for (var j = 0; j < result.Count; j++)
            {
                if (result[j] == sk)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
            {
                result.Add(sk);
            }
        }

        _cachedSkillRefs = result;
        return result;
    }

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：仅同步资产文件名。
    /// </summary>
    private void OnValidate()
    {
        _cachedSkillIds = null;
        _cachedSkillRefs = null;
        NormalizeSkillIdsInEditor();
        EnsureSkillRefsArraySizeInEditor();
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
    }

    /// <summary>
    /// 与 skillIds 对齐：固定 <see cref="MaxSkillsPerCard"/> 个 SO 槽位。
    /// </summary>
    private void EnsureSkillRefsArraySizeInEditor()
    {
        if (skillRefs == null || skillRefs.Length != MaxSkillsPerCard)
        {
            System.Array.Resize(ref skillRefs, MaxSkillsPerCard);
        }
    }

    /// <summary>
    /// 裁剪技能 ID：去重、最多 <see cref="MaxSkillsPerCard"/> 条；保留 Inspector 中未填写的空槽，避免 OnValidate 把「刚点 +」的数组立刻清空。
    /// </summary>
    private void NormalizeSkillIdsInEditor()
    {
        if (skillIds == null)
        {
            skillIds = System.Array.Empty<string>();
            return;
        }

        if (skillIds.Length > MaxSkillsPerCard)
        {
            System.Array.Resize(ref skillIds, MaxSkillsPerCard);
        }

        var hasNonEmpty = false;
        for (var i = 0; i < skillIds.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(skillIds[i]))
            {
                hasNonEmpty = true;
                break;
            }
        }

        if (!hasNonEmpty)
        {
            return;
        }

        var trimmed = new List<string>(skillIds.Length);
        for (var i = 0; i < skillIds.Length; i++)
        {
            var raw = skillIds[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                trimmed.Add(string.Empty);
                continue;
            }

            var id = raw.Trim();
            var duplicate = false;
            for (var j = 0; j < trimmed.Count; j++)
            {
                if (trimmed[j] == id)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
            {
                trimmed.Add(id);
            }
        }

        while (trimmed.Count > MaxSkillsPerCard)
        {
            trimmed.RemoveAt(trimmed.Count - 1);
        }

        skillIds = trimmed.ToArray();
    }

    /// <summary>
    /// 期望文件名：<c>Card</c> + 显示名 + id；规则见 <see cref="ScriptableObjectAssetRenameUtility.BuildPreferredBaseNamePrefixDisplayId"/>。
    /// </summary>
    private string BuildPreferredAssetBaseName()
    {
        return ScriptableObjectAssetRenameUtility.BuildPreferredBaseNamePrefixDisplayId(
            "Card",
            cardId,
            displayName,
            "Card_");
    }
#endif
}
