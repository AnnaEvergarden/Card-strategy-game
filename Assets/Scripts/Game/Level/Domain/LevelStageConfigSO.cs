using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡难度（战斗数值、敌方行为或掉落等可由本字段分支）。
/// 注释格式：中文（English）。
/// </summary>
public enum LevelStageDifficulty
{
    /// <summary>
    /// 简单（Easy）。
    /// </summary>
    Easy = 0,

    /// <summary>
    /// 普通（Normal）。
    /// </summary>
    Normal = 1,

    /// <summary>
    /// 困难（Hard）。
    /// </summary>
    Hard = 2
}

/// <summary>
/// 单个关卡（关卡按钮）的 ScriptableObject 配置：可被多个区域引用，便于复用与单独版本管理。
/// </summary>
[CreateAssetMenu(
    fileName = "LevelStageConfig",
    menuName = "CardGame/Level/Level Stage Config")]
public sealed class LevelStageConfigSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 本关卡敌方（NPC）编队可配置的舰娘数量上限；与玩家编队 <see cref="FleetStore.MaxCardsPerFleet"/> 一致，便于战斗对称读取。
    /// </summary>
    public const int MaxNpcShipCardsPerStage = FleetStore.MaxCardsPerFleet;

    /// <summary>
    /// 关卡所属区域 id（建议三位字符串：001、002...）。
    /// </summary>
    [SerializeField] private string areaId;

    /// <summary>
    /// 区域内关卡序号（从 1 开始）；展示或拼接时将格式化为三位数。
    /// </summary>
    [SerializeField] private int stageIndexInArea = 1;

    /// <summary>
    /// 关卡显示名称（按钮文本）。
    /// </summary>
    [SerializeField] private string displayName;

    /// <summary>
    /// 是否可挑战（false 时按钮显示为禁用）。
    /// </summary>
    [SerializeField] private bool isUnlocked = true;

    /// <summary>
    /// 关卡难度：简单（Easy）、普通（Normal）、困难（Hard）。
    /// </summary>
    [SerializeField] private LevelStageDifficulty difficulty = LevelStageDifficulty.Normal;

    /// <summary>
    /// 敌方 NPC 舰娘配置列表（按上阵顺序；战斗开始前读取并生成对应卡牌，至多 <see cref="MaxNpcShipCardsPerStage"/> 条）。
    /// </summary>
    [SerializeField] private List<CardConfigSO> npcShipCardConfigs = new();

    /// <summary>
    /// 本关卡敌方 AI 行为配置（可选；未分配时使用 <see cref="EnemyAIService"/> 的默认权重）。
    /// </summary>
    [SerializeField] private AIProfileSO enemyAIProfile;

    #endregion

    #region Public API

    /// <summary>
    /// 关卡所属区域 id。
    /// </summary>
    public string AreaId => areaId;

    /// <summary>
    /// 区域内关卡序号（从 1 开始）。
    /// </summary>
    public int StageIndexInArea => Mathf.Max(1, stageIndexInArea);

    /// <summary>
    /// 区域内三位序号文本（001、002...）。
    /// </summary>
    public string StageIndexInAreaCode => StageIndexInArea.ToString("D3");

    /// <summary>
    /// 复合关卡 id：{区域id}-{区域内序号}。当区域 id 为空时退化为仅序号。
    /// </summary>
    public string StageId => string.IsNullOrWhiteSpace(areaId)
        ? StageIndexInAreaCode
        : $"{areaId}-{StageIndexInAreaCode}";

    /// <summary>
    /// 关卡显示名称。
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// 是否可挑战。
    /// </summary>
    public bool IsUnlocked => isUnlocked;

    /// <summary>
    /// 关卡难度。
    /// </summary>
    public LevelStageDifficulty Difficulty => difficulty;

    /// <summary>
    /// 敌方 AI 行为配置；null 表示使用全局默认权重。
    /// </summary>
    public AIProfileSO EnemyAIProfile => enemyAIProfile;

    /// <summary>
    /// 按列表顺序复制非 null 的 NPC 舰娘配置（长度不超过 <see cref="MaxNpcShipCardsPerStage"/>）。
    /// </summary>
    /// <param name="destination">输出列表（会先 Clear）。</param>
    public void CopyNpcShipCardConfigsNonNull(List<CardConfigSO> destination)
    {
        destination?.Clear();
        if (destination == null || npcShipCardConfigs == null || npcShipCardConfigs.Count == 0)
        {
            return;
        }

        for (var i = 0; i < npcShipCardConfigs.Count && destination.Count < MaxNpcShipCardsPerStage; i++)
        {
            var cfg = npcShipCardConfigs[i];
            if (cfg != null)
            {
                destination.Add(cfg);
            }
        }
    }

    /// <summary>
    /// 按列表顺序复制非 null 配置对应的 <see cref="CardConfigSO.CardId"/>（用于存档外仅 id 的战斗生成路径）。
    /// </summary>
    /// <param name="destination">输出列表（会先 Clear）。</param>
    public void CopyNpcCardIdsNonNull(List<string> destination)
    {
        destination?.Clear();
        if (destination == null || npcShipCardConfigs == null || npcShipCardConfigs.Count == 0)
        {
            return;
        }

        for (var i = 0; i < npcShipCardConfigs.Count && destination.Count < MaxNpcShipCardsPerStage; i++)
        {
            var cfg = npcShipCardConfigs[i];
            if (cfg == null || string.IsNullOrWhiteSpace(cfg.CardId))
            {
                continue;
            }

            destination.Add(cfg.CardId.Trim());
        }
    }

    #endregion

#if UNITY_EDITOR
    #region Private Methods

    /// <summary>
    /// 编辑器内：按 <c>LevelStage_{displayName}_{StageId}</c> 规则同步资产文件名（缺项则省略对应段）。
    /// </summary>
    private void OnValidate()
    {
        if (stageIndexInArea < 1)
        {
            stageIndexInArea = 1;
        }

        NormalizeNpcShipListInEditor();

        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
    }

    /// <summary>
    /// 限制 NPC 舰娘列表长度不超过 <see cref="MaxNpcShipCardsPerStage"/>。
    /// </summary>
    private void NormalizeNpcShipListInEditor()
    {
        if (npcShipCardConfigs == null)
        {
            npcShipCardConfigs = new List<CardConfigSO>();
            return;
        }

        while (npcShipCardConfigs.Count > MaxNpcShipCardsPerStage)
        {
            npcShipCardConfigs.RemoveAt(npcShipCardConfigs.Count - 1);
        }
    }

    /// <summary>
    /// 期望文件名：<c>LevelStage</c> + 显示名 + <see cref="StageId"/>。
    /// </summary>
    private string BuildPreferredAssetBaseName()
    {
        return ScriptableObjectAssetRenameUtility.BuildPreferredBaseNamePrefixDisplayId(
            "LevelStage",
            displayName,
            StageId,
            "LevelStage_");
    }

    #endregion

#endif
}
