using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Level/Area Database", fileName = "LevelAreaDatabase_")]
public class LevelAreaDatabaseSO : ScriptableObject
{
    public List<LevelAreaConfigSO> Areas = new List<LevelAreaConfigSO>();

    /// <summary>按分类筛选区域列表。</summary>
    public List<LevelAreaConfigSO> GetAreasByCategory(LevelAreaCategory category)
    {
        var result = new List<LevelAreaConfigSO>(Areas.Count);
        for (int i = 0; i < Areas.Count; i++)
        {
            if (Areas[i] != null && Areas[i].Category == category)
                result.Add(Areas[i]);
        }
        return result;
    }
}