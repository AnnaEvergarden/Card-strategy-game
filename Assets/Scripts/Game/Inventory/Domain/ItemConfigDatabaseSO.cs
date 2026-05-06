using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 道具配置库：集中管理所有 ItemConfigSO，用于按 itemId 查询配置。
/// </summary>
[CreateAssetMenu(menuName = "Game/Inventory/Item Config Database", fileName = "ItemConfigDatabase")]
public sealed class ItemConfigDatabaseSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 道具配置列表。
    /// </summary>
    [SerializeField] private List<ItemConfigSO> items = new();

    #endregion

    #region Public API

    /// <summary>
    /// 获取配置列表。
    /// </summary>
    public IReadOnlyList<ItemConfigSO> Items => items;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：聚合表固定同步为默认数据库文件名。
    /// </summary>
    private void OnValidate()
    {
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
    }

    /// <summary>
    /// 期望文件名：与 CreateAssetMenu 默认一致。
    /// </summary>
    private string BuildPreferredAssetBaseName()
    {
        return "ItemConfigDatabase";
    }
#endif
}

