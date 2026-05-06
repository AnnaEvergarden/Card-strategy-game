using UnityEngine;

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

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：按 <c>LevelStage_{displayName}_{StageId}</c> 规则同步资产文件名（缺项则省略对应段）。
    /// </summary>
    private void OnValidate()
    {
        if (stageIndexInArea < 1)
        {
            stageIndexInArea = 1;
        }

        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, BuildPreferredAssetBaseName());
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
#endif
}
