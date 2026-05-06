using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡池数据库：维护可选建造卡池列表。
/// </summary>
[CreateAssetMenu(menuName = "Game/Card/Build Pool Database", fileName = "BuildPoolDatabase")]
public sealed class BuildPoolDatabaseSO : ScriptableObject
{
    #region Fields

    /// <summary>
    /// 全部可选建造卡池。
    /// </summary>
    [SerializeField] private List<BuildPoolConfigSO> pools = new();

    #endregion

    #region Public API

    /// <summary>
    /// 卡池只读列表。
    /// </summary>
    public IReadOnlyList<BuildPoolConfigSO> Pools => pools;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器内：聚合表固定同步为默认数据库文件名。
    /// </summary>
    private void OnValidate()
    {
        ScriptableObjectAssetRenameUtility.TryRenameAsset(this, "BuildPoolDatabase");
    }
#endif
}
