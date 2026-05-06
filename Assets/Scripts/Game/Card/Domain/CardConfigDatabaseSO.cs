using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌配置表：按 cardId 查询名称与图标路径。
/// </summary>
[CreateAssetMenu(menuName = "Game/Card/Card Config Database", fileName = "CardConfigDatabase")]
public sealed class CardConfigDatabaseSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 全部卡牌配置。
    /// </summary>
    [SerializeField] private List<CardConfigSO> cards = new();

    #endregion

    #region Public API

    /// <summary>
    /// 只读配置列表。
    /// </summary>
    public IReadOnlyList<CardConfigSO> Cards => cards;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：聚合表无显示名/id，固定同步为默认数据库文件名。
    /// </summary>
    private void OnValidate()
    {
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
    }

    /// <summary>
    /// 期望文件名：与 CreateAssetMenu 默认一致（多份同类资产时需自行区分路径避免重名冲突）。
    /// </summary>
    private string BuildPreferredAssetBaseName()
    {
        return "CardConfigDatabase";
    }
#endif
}
