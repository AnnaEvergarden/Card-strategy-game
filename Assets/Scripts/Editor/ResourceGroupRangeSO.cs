using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

/// <summary>
/// 单个分组的容量与 ID 范围定义。
/// </summary>
[System.Serializable]
public class ResourceGroupCapacityEntry
{
    public string GroupName;
    public string DisplayName;
    public int Capacity = 500;
    public string Description;
    public string _sourceGroupName;
}

/// <summary>
/// Addressables Group 容量与 ID 范围配置资产。
/// 由 ResourceConfigGenerator 用于自动分配 ResourceId。
/// 支持从 Addressables 设置中同步分组列表。
/// </summary>
[CreateAssetMenu(menuName = "CardGame/Resource Group Ranges", fileName = "ResourceGroupRanges")]
public class ResourceGroupRangeSO : ScriptableObject
{
    public List<ResourceGroupCapacityEntry> Groups = new List<ResourceGroupCapacityEntry>();

    public ResourceGroupCapacityEntry FindByGroupName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return Groups.FirstOrDefault(g => g.GroupName == name);
    }

    /// <summary>获取该组的起始 ID（前序组容量累计）。</summary>
    public int GetMinId(ResourceGroupCapacityEntry group)
    {
        if (group == null) return 0;
        int id = 1; // 0 保留给未分配资源
        foreach (var g in Groups)
        {
            if (g == group) return id;
            id += g.Capacity;
        }
        return id;
    }

    /// <summary>获取该组的终止 ID（含）。</summary>
    public int GetMaxId(ResourceGroupCapacityEntry group)
    {
        if (group == null) return 0;
        return GetMinId(group) + group.Capacity - 1;
    }

    /// <summary>从 Addressables 设置中同步未在列表中出现的分组。</summary>
    public void SyncFromAddressables()
    {
        if (Groups == null) Groups = new List<ResourceGroupCapacityEntry>();
        var knownNames = new HashSet<string>(Groups.Select(g => g.GroupName).Where(n => !string.IsNullOrEmpty(n)));

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        foreach (var group in settings.groups)
        {
            if (group == null) continue;
            if (!knownNames.Contains(group.Name))
            {
                Groups.Add(new ResourceGroupCapacityEntry
                {
                    GroupName = group.Name,
                    DisplayName = group.Name,
                    Capacity = 500,
                    _sourceGroupName = group.Name,
                });
                knownNames.Add(group.Name);
            }
        }
    }

    public static ResourceGroupRangeSO CreateWithDefault()
    {
        var so = CreateInstance<ResourceGroupRangeSO>();
        so.Groups = new List<ResourceGroupCapacityEntry>
        {
            new ResourceGroupCapacityEntry { GroupName = "Background",     DisplayName = "Background",      Capacity = 1000 },
            new ResourceGroupCapacityEntry { GroupName = "Config",         DisplayName = "Config",          Capacity = 100 },
            new ResourceGroupCapacityEntry { GroupName = "Icon_Shipgirl",  DisplayName = "Icon Shipgirl",   Capacity = 500 },
            new ResourceGroupCapacityEntry { GroupName = "Icon_Skills",    DisplayName = "Icon Skills",     Capacity = 500 },
            new ResourceGroupCapacityEntry { GroupName = "Prefab_Panel",   DisplayName = "Prefab Panel",    Capacity = 500 },
            new ResourceGroupCapacityEntry { GroupName = "Scene",          DisplayName = "Scene",           Capacity = 500 },
            new ResourceGroupCapacityEntry { GroupName = "Shared_UI",      DisplayName = "Shared UI",       Capacity = 1000 },
        };
        return so;
    }
}
