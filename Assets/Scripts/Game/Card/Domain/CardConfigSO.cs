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
/// 单张舰娘（卡牌）静态配置：中文展示名、英文名与战斗向数值。
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
    /// 攻击力。
    /// </summary>
    [SerializeField] private int attack;

    /// <summary>
    /// 防御力。
    /// </summary>
    [SerializeField] private int defence;

    /// <summary>
    /// 舰娘稀有度：普通（Normal）、稀有（Rare）、精锐（Elite）、超稀有（Super Rare）、海上传奇（Sea Legend）、活动（Activity）。
    /// </summary>
    [SerializeField] private CardRarity rarity = CardRarity.Normal;

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
    /// 攻击力。
    /// </summary>
    public int Attack => attack;

    /// <summary>
    /// 防御力。
    /// </summary>
    public int Defence => defence;

    /// <summary>
    /// 舰娘稀有度（见 <see cref="CardRarity"/> 各档中英文注释）。
    /// </summary>
    public CardRarity Rarity => rarity;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：仅同步资产文件名。
    /// </summary>
    private void OnValidate()
    {
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
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
