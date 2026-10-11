using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置导出工具：从 SO 资产读取数据，导出为 JSON 供 ConfigService 运行时加载。
/// 输出路径固定为 Assets/Config/Runtime。
/// </summary>
public static partial class ConfigExportUtility
{
    private const string DefaultOutputRelPath = "Assets/Config/Runtime";

    public static string ConfigOutputPath => GetFullPath(DefaultOutputRelPath);
    public static string ConfigOutputAssetPath => DefaultOutputRelPath;

    // ── 主导出入口 ──

    /// <summary>主导出入口：从所有已有的 SO Database 资产中读取并导出为 JSON。</summary>
    public static void ExportFromSOAssets()
    {
        EnsureOutputDirectory();

        // 按依赖/业务顺序依次导出
        ExportPanelConfigsFromSO();
        ExportCardsFromSO();
        ExportSkillsFromSO();
        ExportPoolsFromSO();
        ExportItemsFromSO();
        ExportLevelAreasFromSO();
        ExportScenesFromSO();

        AssetDatabase.Refresh();
        Debug.Log("[ConfigExport] 从 SO 资产导出完成");
    }

    public static List<PanelConfig> GetPanelConfigs()
    {
        var fromSO = GetPanelConfigsFromSO();
        if (fromSO != null) return fromSO;

        string path = Path.Combine(GetFullPath(DefaultOutputRelPath), "PanelConfigTable.json");
        if (!File.Exists(path)) return new List<PanelConfig>();
        string json = File.ReadAllText(path);
        var table = JsonUtility.FromJson<PanelConfigTable>(json);
        return table?.Items ?? new List<PanelConfig>();
    }

    private static List<PanelConfig> GetPanelConfigsFromSO()
    {
        var guids = AssetDatabase.FindAssets("t:PanelConfigDatabaseSO");
        if (guids.Length == 0) return null;
        var db = AssetDatabase.LoadAssetAtPath<PanelConfigDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (db?.Items == null) return null;
        var list = new List<PanelConfig>();
        foreach (var so in db.Items) { if (so != null) list.Add(so.ToDomain()); }
        return list;
    }

    // ── 导出方法：从 SO Database 导出 ──

    public static void ExportPanelConfigsFromSO()
    {
        var configs = GetPanelConfigsFromSO();
        if (configs != null) WriteEntry("PanelConfigTable.json", new PanelConfigTable { Items = configs });
    }

    public static void ExportCardsFromSO()
    {
        var guids = AssetDatabase.FindAssets("t:CardConfigDatabaseSO");
        if (guids.Length == 0) { Debug.Log("[ConfigExport] 未找到 CardConfigDatabaseSO，跳过"); return; }
        var db = AssetDatabase.LoadAssetAtPath<CardConfigDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (db?.Cards == null) return;
        var list = new List<CardConfig>();
        foreach (var so in db.Cards) { if (so != null) list.Add(so.ToDomain()); }
        WriteEntry("CardConfigTable.json", new CardConfigTable { Items = list });
        Debug.Log($"[ConfigExport] 导出 CardConfig: {list.Count} 条");
    }

    public static void ExportSkillsFromSO()
    {
        var guids = AssetDatabase.FindAssets("t:SkillConfigDatabaseSO");
        if (guids.Length == 0) { Debug.Log("[ConfigExport] 未找到 SkillConfigDatabaseSO，跳过"); return; }
        var db = AssetDatabase.LoadAssetAtPath<SkillConfigDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (db?.Skills == null) return;
        var list = new List<SkillConfig>();
        foreach (var so in db.Skills) { if (so != null) list.Add(so.ToDomain()); }
        WriteEntry("SkillConfigTable.json", new SkillConfigTable { Items = list });
    }

    public static void ExportPoolsFromSO()
    {
        var guids = AssetDatabase.FindAssets("t:BuildPoolDatabaseSO");
        if (guids.Length == 0) { Debug.LogWarning("[ConfigExport] 未找到 BuildPoolDatabaseSO"); return; }
        var db = AssetDatabase.LoadAssetAtPath<BuildPoolDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (db?.Pools == null) return;
        var list = new List<BuildPoolConfig>();
        foreach (var so in db.Pools) { if (so != null) list.Add(so.ToDomain()); }
        WriteEntry("BuildPoolConfigTable.json", new BuildPoolConfigTable { Items = list });
        Debug.Log($"[ConfigExport] 导出 BuildPoolConfig: {list.Count} 条");
    }

    public static void ExportItemsFromSO()
    {
        var guids = AssetDatabase.FindAssets("t:ItemConfigDatabaseSO");
        if (guids.Length == 0) { Debug.Log("[ConfigExport] 未找到 ItemConfigDatabaseSO，跳过"); return; }
        var db = AssetDatabase.LoadAssetAtPath<ItemConfigDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (db?.Items == null) return;
        var list = new List<ItemConfig>();
        foreach (var so in db.Items) { if (so != null) list.Add(so.ToDomain()); }
        WriteEntry("ItemConfigTable.json", new ItemConfigTable { Items = list });
    }

    public static void ExportLevelAreasFromSO()
    {
        var guids = AssetDatabase.FindAssets("t:LevelAreaDatabaseSO");
        if (guids.Length == 0) { Debug.LogWarning("[ConfigExport] 未找到 LevelAreaDatabaseSO"); return; }
        var db = AssetDatabase.LoadAssetAtPath<LevelAreaDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (db?.Areas == null) return;
        var list = new List<LevelAreaConfig>();
        var allStages = new List<LevelStageConfig>();
        foreach (var so in db.Areas)
        {
            if (so != null)
            {
                var domain = so.ToDomain();
                list.Add(domain);
                if (domain.Stages != null)
                    allStages.AddRange(domain.Stages);
            }
        }
        WriteEntry("LevelAreaConfigTable.json", new LevelAreaConfigTable { Items = list });
        WriteEntry("LevelStageConfigTable.json", new LevelStageConfigTable { Items = allStages });
        Debug.Log($"[ConfigExport] 导出 LevelAreaConfig: {list.Count} 条区域，LevelStageConfig: {allStages.Count} 条关卡");
    }

    public static void ExportScenesFromSO()
    {
        var guids = AssetDatabase.FindAssets("t:SceneConfigDatabaseSO");
        if (guids.Length == 0) { Debug.Log("[ConfigExport] 未找到 SceneConfigDatabaseSO，跳过"); return; }
        var db = AssetDatabase.LoadAssetAtPath<SceneConfigDatabaseSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (db?.Items == null) return;
        var list = new List<SceneConfig>();
        foreach (var so in db.Items) { if (so != null) list.Add(so.ToDomain()); }
        WriteEntry("SceneConfigTable.json", new SceneConfigTable { Items = list });
        Debug.Log($"[ConfigExport] 导出 SceneConfig: {list.Count} 条");
    }

    // ── 写入核心 ──

    /// <summary>将配置表写入 DefaultOutputRelPath 目录下的 JSON 文件。</summary>
    private static void WriteEntry<T>(string fileName, T table)
    {
        string fullDir = GetFullPath(DefaultOutputRelPath);
        if (!Directory.Exists(fullDir)) Directory.CreateDirectory(fullDir);
        string json = JsonUtility.ToJson(table, true);
        string path = Path.Combine(fullDir, fileName);
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        Debug.Log($"[ConfigExport] 已导出: {DefaultOutputRelPath}/{fileName}");
    }

    // ── 工具方法 ──

    private static string GetFullPath(string relativePath)
    {
        var fp = Path.GetFullPath(Path.Combine(Application.dataPath, "../", relativePath));
        return fp.Replace('\\', '/');
    }

    private static void EnsureOutputDirectory()
    {
        string full = GetFullPath(DefaultOutputRelPath);
        if (!Directory.Exists(full)) Directory.CreateDirectory(full);
    }
}

