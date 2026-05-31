using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌配置表：按 cardId 查询名称与图标路径。
/// </summary>
[CreateAssetMenu(menuName = "Game/Card/Card Config Database", fileName = "CardConfigDatabase_Other")]
public sealed class CardConfigDatabaseSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 全部卡牌配置。
    /// </summary>
    [SerializeField] private List<CardConfigSO> cards = new();

    /// <summary>
    /// 本数据库侧重阵营（策划标注，用于支持分阵营资产维护）；资产命名约定：<c>CardConfigDatabase_{阵营枚举名}</c>。
    /// </summary>
    [SerializeField] private ShipFaction databaseFaction = ShipFaction.Other;

    #endregion

    #region Public API

    /// <summary>
    /// 只读配置列表。
    /// </summary>
    public IReadOnlyList<CardConfigSO> Cards => cards;

    /// <summary>
    /// 本库标注阵营。
    /// </summary>
    public ShipFaction DatabaseFaction => databaseFaction;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：按所选阵营同步数据库文件名。
    /// </summary>
    private void OnValidate()
    {
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, $"CardConfigDatabase_{databaseFaction}");
    }
#endif
}
