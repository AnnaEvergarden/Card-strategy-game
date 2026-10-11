using System.Collections.Generic;

/// <summary>
/// 单支编队数据。
/// </summary>
[System.Serializable]
public class FleetGroupData
{
    /// <summary>编队索引（0 起始）。</summary>
    public int GroupIndex;

    /// <summary>编队显示名称。</summary>
    public string GroupName;

    /// <summary>编队中卡牌实例 ID 列表（最多 6 个）。</summary>
    public List<string> CardInstanceIds = new List<string>();
}

/// <summary>
/// 编队存档数据。
/// </summary>
[System.Serializable]
public class FleetSaveData
{
    /// <summary>所有编队分组。</summary>
    public List<FleetGroupData> Groups = new List<FleetGroupData>();
}
