using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SO Database 自动收纳工具：搜索指定类型的所有 Config 资产，自动填充到 Database 中。
/// 避免手动拖拽遗漏。
/// </summary>
public static class DatabasePopulator
{
    [MenuItem("Tools/刷新 SO Database（全部）")]
    public static void RefreshAllDatabases()
    {
        RefreshCardDatabase();
        RefreshSkillDatabase();
        RefreshBuildPoolDatabase();
        RefreshItemDatabase();
        RefreshLevelAreaDatabase();
        RefreshSceneDatabase();
        RefreshPanelDatabase();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DatabasePopulator] 所有 Database 已刷新");
    }

    // ── 单类型刷新 ──

    public static void RefreshCardDatabase()
    {
        var db = FindSingleAsset<CardConfigDatabaseSO>();
        if (db == null) { Debug.LogWarning("[DatabasePopulator] 未找到 CardConfigDatabaseSO"); return; }
        db.Cards = FindAllAssets<CardConfigSO>();
        EditorUtility.SetDirty(db);
        Debug.Log($"[DatabasePopulator] CardConfigDatabase: {db.Cards.Count} 条");
    }

    public static void RefreshSkillDatabase()
    {
        var db = FindSingleAsset<SkillConfigDatabaseSO>();
        if (db == null) { Debug.LogWarning("[DatabasePopulator] 未找到 SkillConfigDatabaseSO"); return; }
        db.Skills = FindAllAssets<SkillConfigSO>();
        EditorUtility.SetDirty(db);
        Debug.Log($"[DatabasePopulator] SkillConfigDatabase: {db.Skills.Count} 条");
    }

    public static void RefreshBuildPoolDatabase()
    {
        var db = FindSingleAsset<BuildPoolDatabaseSO>();
        if (db == null) { Debug.LogWarning("[DatabasePopulator] 未找到 BuildPoolDatabaseSO"); return; }
        db.Pools = FindAllAssets<BuildPoolConfigSO>();
        EditorUtility.SetDirty(db);
        Debug.Log($"[DatabasePopulator] BuildPoolDatabase: {db.Pools.Count} 条");
    }

    public static void RefreshItemDatabase()
    {
        var db = FindSingleAsset<ItemConfigDatabaseSO>();
        if (db == null) { Debug.LogWarning("[DatabasePopulator] 未找到 ItemConfigDatabaseSO"); return; }
        db.Items = FindAllAssets<ItemConfigSO>();
        EditorUtility.SetDirty(db);
        Debug.Log($"[DatabasePopulator] ItemConfigDatabase: {db.Items.Count} 条");
    }

    public static void RefreshLevelAreaDatabase()
    {
        var db = FindSingleAsset<LevelAreaDatabaseSO>();
        if (db == null) { Debug.LogWarning("[DatabasePopulator] 未找到 LevelAreaDatabaseSO"); return; }
        db.Areas = FindAllAssets<LevelAreaConfigSO>();
        EditorUtility.SetDirty(db);
        Debug.Log($"[DatabasePopulator] LevelAreaDatabase: {db.Areas.Count} 条");
    }

    public static void RefreshSceneDatabase()
    {
        var db = FindSingleAsset<SceneConfigDatabaseSO>();
        if (db == null) { Debug.LogWarning("[DatabasePopulator] 未找到 SceneConfigDatabaseSO"); return; }
        db.Items = FindAllAssets<SceneConfigSO>();
        EditorUtility.SetDirty(db);
        Debug.Log($"[DatabasePopulator] SceneConfigDatabase: {db.Items.Count} 条");
    }


    public static void RefreshPanelDatabase()
    {
        var db = FindSingleAsset<PanelConfigDatabaseSO>();
        if (db == null) { Debug.LogWarning("[DatabasePopulator] 未找到 PanelConfigDatabaseSO"); return; }
        db.Items = FindAllAssets<PanelConfigSO>();
        EditorUtility.SetDirty(db);
        Debug.Log($"[DatabasePopulator] PanelConfigDatabase: {db.Items.Count} 条");
    }

    // ── 工具方法 ──

    private static T FindSingleAsset<T>() where T : ScriptableObject
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static List<T> FindAllAssets<T>() where T : ScriptableObject
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        var results = new List<T>();
        foreach (var guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null && string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(asset)) == false)
                results.Add(asset);
        }
        return results;
    }
}
