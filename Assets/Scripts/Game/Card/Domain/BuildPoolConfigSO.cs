using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个卡池配置：图片、消耗与掉落权重等（独立脚本文件，便于 Unity 正确生成 MonoScript 与资源引用）。
/// </summary>
[CreateAssetMenu(menuName = "Game/Card/Build Pool Config", fileName = "BuildPool_")]
public sealed class BuildPoolConfigSO : ScriptableObject
{
    #region Nested Models

    /// <summary>
    /// 单次/十次建造消耗配置。
    /// </summary>
    [Serializable]
    public sealed class BuildCost
    {
        /// <summary>
        /// 消耗金币。
        /// </summary>
        public int gold;

        /// <summary>
        /// 消耗钻石。
        /// </summary>
        public int diamond;

        /// <summary>
        /// 消耗船票。
        /// </summary>
        public int shipTicket;
    }

    /// <summary>
    /// 单个掉落项：引用 <see cref="CardConfigSO"/> + 权重（按权重随机抽取）。
    /// </summary>
    [Serializable]
    public sealed class DropEntry
    {
        /// <summary>
        /// 掉落的卡牌配置引用。
        /// </summary>
        [SerializeField] private CardConfigSO card;

        /// <summary>
        /// 权重（>0 才参与抽取）。
        /// </summary>
        public int weight = 1;

        /// <summary>
        /// 掉落的卡牌配置（只读）。
        /// </summary>
        public CardConfigSO Card => card;
    }

    #endregion

    #region Fields

    /// <summary>
    /// 卡池唯一 id（代码与 <see cref="BuildService.TryBuild"/> 使用，建议 ASCII）。
    /// </summary>
    [Header("卡池基础")]
    [SerializeField] private string poolId;

    /// <summary>
    /// 卡池显示名（界面展示；资产自动命名优先用此字段）。
    /// </summary>
    [SerializeField] private string displayName;

    /// <summary>
    /// 卡池阵营：非 <see cref="ShipFaction.Other"/> 时，掉落表中每张舰娘须与此处阵营一致；<see cref="ShipFaction.Other"/> 表示混池，可配置任意阵营舰娘。
    /// </summary>
    [SerializeField] private ShipFaction poolFaction = ShipFaction.Other;

    /// <summary>
    /// 卡池横幅图（建造界面大图）。
    /// </summary>
    [SerializeField] private Sprite poolBannerSprite;

    /// <summary>
    /// 建造 1 次消耗。
    /// </summary>
    [Header("建造消耗")]
    [SerializeField] private BuildCost buildOneCost = new();

    /// <summary>
    /// 建造 10 次消耗。
    /// </summary>
    [SerializeField] private BuildCost buildTenCost = new();

    /// <summary>
    /// 卡池掉落表。
    /// </summary>
    [Header("掉落")]
    [SerializeField] private List<DropEntry> dropEntries = new();

    #endregion

    #region Public API

    /// <summary>
    /// 卡池唯一 id。
    /// </summary>
    public string PoolId => poolId;

    /// <summary>
    /// 卡池显示名。
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// 卡池阵营（单一阵营池或混池）。
    /// </summary>
    public ShipFaction PoolFaction => poolFaction;

    /// <summary>
    /// 卡池展示图。
    /// </summary>
    public Sprite PoolBannerSprite => poolBannerSprite;

    /// <summary>
    /// 单抽消耗。
    /// </summary>
    public BuildCost BuildOneCost => buildOneCost;

    /// <summary>
    /// 十连消耗。
    /// </summary>
    public BuildCost BuildTenCost => buildTenCost;

    /// <summary>
    /// 掉落表。
    /// </summary>
    public IReadOnlyList<DropEntry> DropEntries => dropEntries;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：按 <c>BuildPool_{displayName}_{poolId}</c> 规则同步资产文件名（缺项则省略对应段）。
    /// </summary>
    private void OnValidate()
    {
        ValidateDropEntries();
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
    }

    /// <summary>
    /// 期望文件名：<c>BuildPool</c> + 显示名 + poolId。
    /// </summary>
    private string BuildPreferredAssetBaseName()
    {
        return ScriptableObjectAssetRenameUtility.BuildPreferredBaseNamePrefixDisplayId(
            "BuildPool",
            displayName,
            poolId,
            "BuildPool_");
    }

    /// <summary>
    /// 校验掉落表：空引用或权重无效时在控制台提示。
    /// </summary>
    private void ValidateDropEntries()
    {
        if (dropEntries == null || dropEntries.Count == 0)
        {
            return;
        }

        for (var i = 0; i < dropEntries.Count; i++)
        {
            var e = dropEntries[i];
            if (e == null)
            {
                Debug.LogWarning($"BuildPoolConfigSO: [{name}] dropEntries[{i}] 为空。", this);
                continue;
            }

            if (e.Card == null)
            {
                Debug.LogWarning($"BuildPoolConfigSO: [{name}] dropEntries[{i}] 未指定卡牌引用。", this);
                continue;
            }

            if (poolFaction != ShipFaction.Other && e.Card.Faction != poolFaction)
            {
                Debug.LogWarning(
                    $"BuildPoolConfigSO: [{name}] dropEntries[{i}] 阵营不匹配：卡池={poolFaction}，舰娘「{e.Card.DisplayName}」={e.Card.Faction}。单一阵营卡池仅可配置同阵营舰娘；混池请将卡池阵营设为 Other（其他）。",
                    this);
            }

            if (e.weight <= 0)
            {
                Debug.LogWarning(
                    $"BuildPoolConfigSO: [{name}] dropEntries[{i}] 权重<=0，将不参与抽取（卡牌={e.Card.name}）。",
                    this);
            }
        }
    }
#endif
}
