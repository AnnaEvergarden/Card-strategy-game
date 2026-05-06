using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡区域数据库：分别维护常驻与活动模式可展示的区域配置列表。
/// </summary>
[CreateAssetMenu(
    fileName = "LevelAreaDatabase",
    menuName = "CardGame/Level/Level Area Database")]
public sealed class LevelAreaDatabaseSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 常驻关卡模式下可展示的区域配置（每项为 <see cref="LevelAreaConfigSO"/> 资产引用）。
    /// </summary>
    [SerializeField] private List<LevelAreaConfigSO> permanentAreas = new();

    /// <summary>
    /// 活动关卡模式下可展示的区域配置（每项为 <see cref="LevelAreaConfigSO"/> 资产引用）。
    /// </summary>
    [SerializeField] private List<LevelAreaConfigSO> activityAreas = new();

    #endregion

    #region Public API

    /// <summary>
    /// 读取指定模式下的区域配置集合。
    /// </summary>
    /// <param name="mode">关卡模式（常驻或活动）。</param>
    /// <returns>对应模式的区域配置引用列表；未配置时返回空列表。</returns>
    public IReadOnlyList<LevelAreaConfigSO> GetAreas(LevelMode mode)
    {
        return mode == LevelMode.Activity
            ? activityAreas
            : permanentAreas;
    }

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
        return "LevelAreaDatabase";
    }
#endif
}

/// <summary>
/// 关卡模式：用于在同一 UI 中切换常驻与活动两套配置。
/// </summary>
public enum LevelMode
{
    /// <summary>
    /// 常驻关卡。
    /// </summary>
    Permanent = 0,

    /// <summary>
    /// 活动关卡。
    /// </summary>
    Activity = 1
}
