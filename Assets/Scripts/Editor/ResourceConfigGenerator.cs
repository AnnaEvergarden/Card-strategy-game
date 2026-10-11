using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class ResourceConfigGenerator
{
    private const string ConfigLabel = "NeedResource_Config";
    private const string ResIdPrefix = "ResId_";
    private const string RangeSettingsRelPath = "Assets/Config/Editor/ResourceGroupRanges.asset";
    private const string DefaultOutputPath = "Assets/Config/Runtime";

    public static void Generate()
    {
        var ranges = LoadOrCreateRangeSettings();
        if (ranges == null) { EditorUtility.DisplayDialog("错误", "无法加载 ResourceGroupRangeSO", "确定"); return; }

        var addrSettings = AddressableAssetSettingsDefaultObject.Settings;
        if (addrSettings == null) { EditorUtility.DisplayDialog("错误", "未找到 Addressables 设置", "确定"); return; }

        string outputDir = ResolveOutputDir();
        Generate(addrSettings, ranges, outputDir);
    }

    public static void Generate(AddressableAssetSettings addrSettings, ResourceGroupRangeSO ranges, string outputDir)
    {
        if (addrSettings == null || ranges == null || string.IsNullOrEmpty(outputDir)) return;

        // ── 1. 扫描 Addressables，按 Group 分组 ──
        var entriesByGroup = new Dictionary<string, List<AddressableAssetEntry>>();
        foreach (var group in addrSettings.groups)
        {
            if (group == null) continue;
            foreach (var entry in group.entries)
            {
                if (entry == null || !entry.labels.Contains(ConfigLabel)) continue;
                string groupName = entry.parentGroup.Name;
                if (!entriesByGroup.TryGetValue(groupName, out var list))
                {
                    list = new List<AddressableAssetEntry>();
                    entriesByGroup[groupName] = list;
                }
                list.Add(entry);
            }
        }

        if (entriesByGroup.Count == 0)
        {
            Debug.Log("[ResourceConfigGenerator] 未找到标签为 NeedResource_Config 的条目");
            return;
        }

        // 排序：组内按地址字母顺序，确保 ID 分配顺序稳定
        foreach (var kvp in entriesByGroup)
            kvp.Value.Sort((a, b) => string.Compare(a.address, b.address, System.StringComparison.OrdinalIgnoreCase));


        // ── 3. 读取已有 JSON ──
        string jsonPath = Path.Combine(outputDir, "ResourceConfigTable.json");
        var existingByAddress = new Dictionary<string, ResourceConfig>();
        if (File.Exists(jsonPath))
        {
            string oldJson = File.ReadAllText(jsonPath);
            var oldTable = JsonUtility.FromJson<ResourceConfigTable>(oldJson);
            if (oldTable?.Items != null)
                foreach (var cfg in oldTable.Items)
                    if (!string.IsNullOrEmpty(cfg.Address))
                        existingByAddress[cfg.Address] = cfg;
        }

        // ── 4. 全局 ID 冲突检测（按分组字母顺序遍历） ──
        var sortedGroupNames = entriesByGroup.Keys.OrderBy(k => k, System.StringComparer.OrdinalIgnoreCase).ToList();
        var globalUsedIds = new HashSet<int>();

        foreach (var groupName in sortedGroupNames)
        {
            var entries = entriesByGroup[groupName];
            var capacity = ranges.FindByGroupName(groupName);
            if (capacity == null) continue;

            int minId = ranges.GetMinId(capacity);
            int maxId = ranges.GetMaxId(capacity);

            foreach (var entry in entries)
            {
                if (existingByAddress.TryGetValue(entry.address, out var existing) && existing.ResourceId > 0)
                    globalUsedIds.Add(existing.ResourceId);

                int eid = ParseExplicitId(entry);
                if (eid > 0 && eid >= minId && eid <= maxId)
                    globalUsedIds.Add(eid);
            }
        }

        // ── 5. 带全局冲突检测的分组分配 ──
        var results = new List<ResourceConfig>();
        var warnings = new List<string>();

        foreach (var groupName in sortedGroupNames)
        {
            var entries = entriesByGroup[groupName];
            var capacity = ranges.FindByGroupName(groupName);
            if (capacity == null)
            {
                warnings.Add($"Group \"{groupName}\" 未定义容量（已跳过 {entries.Count} 项），请在 ResourceConfig 设置中刷新分组");
                continue;
            }

            int minId = ranges.GetMinId(capacity);
            int maxId = ranges.GetMaxId(capacity);
            var matchedExisting = new HashSet<string>();

            foreach (var entry in entries)
            {
                if (existingByAddress.TryGetValue(entry.address, out var existing))
                {
                    matchedExisting.Add(entry.address);
                    if (existing.ResourceId > 0 && (existing.ResourceId < minId || existing.ResourceId > maxId))
                        warnings.Add($"\"{entry.address}\" 的 ID={existing.ResourceId} 不在范围 [{minId},{maxId}] 内");
                }
            }

            // 新添加的资源往后排：从该组已有最大 ID + 1 开始分配
            int nextAutoId = minId;
            foreach (var entry in entries)
            {
                if (matchedExisting.Contains(entry.address) && existingByAddress.TryGetValue(entry.address, out var existing))
                    if (existing.ResourceId >= nextAutoId)
                        nextAutoId = existing.ResourceId + 1;
            }
            foreach (var entry in entries)
            {
                if (matchedExisting.Contains(entry.address))
                {
                    results.Add(MakeConfig(existingByAddress[entry.address].ResourceId, entry, groupName));
                    continue;
                }

                int eid = ParseExplicitId(entry);
                if (eid > 0 && eid >= minId && eid <= maxId)
                {
                    results.Add(MakeConfig(eid, entry, groupName));
                    continue;
                }
                if (eid > 0)
                    warnings.Add($"\"{entry.address}\" ResId_{eid} 超出范围 [{minId},{maxId}]，忽略");

                while (nextAutoId <= maxId && globalUsedIds.Contains(nextAutoId))
                    nextAutoId++;

                if (nextAutoId > maxId)
                {
                    warnings.Add($"Group \"{groupName}\" 容量已满，\"{entry.address}\" 无法分配");
                    continue;
                }

                results.Add(MakeConfig(nextAutoId, entry, groupName));
                globalUsedIds.Add(nextAutoId);
                nextAutoId++;
            }
        }

        if (results.Count == 0) { Debug.Log("[ResourceConfigGenerator] 没有可导出的条目"); return; }

        // ── 6. 写出 JSON ──
        results = results.OrderBy(r => r.ResourceId).ToList();

        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        File.WriteAllText(jsonPath, JsonUtility.ToJson(new ResourceConfigTable { Items = results }, true), System.Text.Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"[ResourceConfigGenerator] 已生成 {results.Count} 条");
        if (warnings.Count > 0)
        {
            var firstFew = string.Join("\n", warnings.Take(5));
            Debug.LogWarning($"[ResourceConfigGenerator] {warnings.Count} 个警告:\n{firstFew}");
        }
    }

    private static ResourceConfig MakeConfig(int id, AddressableAssetEntry entry, string group)
    {
        string assetType = "Unknown";
        try
        {
            string path = AssetDatabase.GUIDToAssetPath(entry.guid);
            var loaded = AssetDatabase.LoadMainAssetAtPath(path);
            if (loaded != null) assetType = loaded.GetType().Name;
        }
        catch { }
        return new ResourceConfig
        {
            ResourceId = id,
            Address = entry.address,
            AssetType = assetType,
            Group = group,
        };
    }

    private static int ParseExplicitId(AddressableAssetEntry entry)
    {
        foreach (var lbl in entry.labels)
            if (lbl.StartsWith(ResIdPrefix) && int.TryParse(lbl.Substring(ResIdPrefix.Length), out int id))
                return id;
        return 0;
    }

    private static ResourceGroupRangeSO LoadOrCreateRangeSettings()
    {
        var so = AssetDatabase.LoadAssetAtPath<ResourceGroupRangeSO>(RangeSettingsRelPath);
        if (so != null) return so;
        string dir = Path.GetDirectoryName(
            Path.GetFullPath(Path.Combine(Application.dataPath, "../", RangeSettingsRelPath)));
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        so = ResourceGroupRangeSO.CreateWithDefault();
        AssetDatabase.CreateAsset(so, RangeSettingsRelPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return so;
    }

    private static string ResolveOutputDir()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../", DefaultOutputPath)).Replace('\\', '/');
    }
}
