using System;
using UnityEngine;

/// <summary>
/// 道具配置：定义道具基础信息与图标资源路径。
/// </summary>
[CreateAssetMenu(menuName = "Game/Inventory/Item Config", fileName = "ItemConfig_")]
public sealed class ItemConfigSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 道具唯一 ID（需与仓库数据 itemId 一致）。
    /// </summary>
    [SerializeField] private string itemId;

    /// <summary>
    /// 道具显示名称。
    /// </summary>
    [SerializeField] private string itemName;

    /// <summary>
    /// 图标路径（Resources 相对路径，不含扩展名）。
    /// 例如：Icons/Items/Potion_01
    /// </summary>
    [SerializeField] private string iconResourcePath;

    #endregion

    #region Public API

    /// <summary>
    /// 道具唯一 ID。
    /// </summary>
    public string ItemId => itemId;

    /// <summary>
    /// 道具显示名称。
    /// </summary>
    public string ItemName => itemName;

    /// <summary>
    /// 图标资源路径。
    /// </summary>
    public string IconResourcePath => iconResourcePath;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：按 <c>Item_{itemName}_{itemId}</c> 规则同步资产文件名（缺项则省略对应段）。
    /// </summary>
    private void OnValidate()
    {
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
    }

    /// <summary>
    /// 期望文件名：<c>Item</c> + 道具名 + itemId。
    /// </summary>
    private string BuildPreferredAssetBaseName()
    {
        return ScriptableObjectAssetRenameUtility.BuildPreferredBaseNamePrefixDisplayId(
            "Item",
            itemName,
            itemId,
            "Item_");
    }
#endif
}

