using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个可挑战区域的 ScriptableObject 配置：可被数据库列表引用，便于复用与单独版本管理。
/// </summary>
[CreateAssetMenu(
    fileName = "LevelAreaConfig",
    menuName = "CardGame/Level/Level Area Config")]
public sealed class LevelAreaConfigSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 区域唯一 id（用于存档进度、埋点等）。
    /// </summary>
    [SerializeField] private string areaId;

    /// <summary>
    /// 区域显示名称（例如：北方海域 I）。
    /// </summary>
    [SerializeField] private string displayName;

    /// <summary>
    /// 区域所属阵营（与 <see cref="CardConfigSO.Faction"/> 使用同一 <see cref="ShipFaction"/> 枚举）。
    /// </summary>
    [SerializeField] private ShipFaction shipFaction = ShipFaction.Other;

    /// <summary>
    /// 区域简介（显示在区域选择界面）。
    /// </summary>
    [TextArea(2, 5)]
    [SerializeField] private string description;

    /// <summary>
    /// 是否可挑战（不可挑战时仍可展示但不可进入）。
    /// </summary>
    [SerializeField] private bool isUnlocked = true;

    /// <summary>
    /// 区域内的关卡配置（每项为 <see cref="LevelStageConfigSO"/> 资产引用）。
    /// </summary>
    [SerializeField] private List<LevelStageConfigSO> stages = new();

    #endregion

    #region Public API

    /// <summary>
    /// 区域唯一 id。
    /// </summary>
    public string AreaId => areaId;

    /// <summary>
    /// 区域显示名称。
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// 区域阵营。
    /// </summary>
    public ShipFaction Faction => shipFaction;

    /// <summary>
    /// 区域简介。
    /// </summary>
    public string Description => description;

    /// <summary>
    /// 是否可挑战。
    /// </summary>
    public bool IsUnlocked => isUnlocked;

    /// <summary>
    /// 区域内关卡配置引用列表（只读）。
    /// </summary>
    public IReadOnlyList<LevelStageConfigSO> Stages => stages;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：按 <c>LevelArea_{displayName}_{areaId}</c> 规则同步资产文件名（缺项则省略对应段）。
    /// </summary>
    private void OnValidate()
    {
        ValidateStageAreaBinding();
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
    }

    /// <summary>
    /// 校验区域内关卡是否标记到同一 areaId，避免配置错拖。
    /// </summary>
    private void ValidateStageAreaBinding()
    {
        if (stages == null || stages.Count == 0 || string.IsNullOrWhiteSpace(areaId))
        {
            return;
        }

        for (var i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            if (stage == null || string.IsNullOrWhiteSpace(stage.AreaId))
            {
                continue;
            }

            if (stage.AreaId != areaId)
            {
                Debug.LogWarning(
                    $"LevelAreaConfigSO: 区域[{areaId}] 的 stages[{i}] 引用了关卡[{stage.name}]，但其 AreaId={stage.AreaId}，请确认是否拖错。",
                    this);
            }
        }
    }

    /// <summary>
    /// 期望文件名：<c>LevelArea</c> + 显示名 + areaId。
    /// </summary>
    private string BuildPreferredAssetBaseName()
    {
        return ScriptableObjectAssetRenameUtility.BuildPreferredBaseNamePrefixDisplayId(
            "LevelArea",
            AreaId,
            displayName,
            "LevelArea_");
    }
#endif
}
