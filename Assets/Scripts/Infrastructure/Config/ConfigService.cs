using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 配置服务：JSON 文件加载优先，SO 加载为备选。
/// 所有地址与 ConfigExporter 导出的文件名一致（Config/Runtime/{TypeName}Table）。
/// </summary>
public class ConfigService : IConfigService, IDisposable
{
    private static readonly Dictionary<string, string> ConfigAddressMap = new Dictionary<string, string>
    {
        { "ResourceConfig",   "ResourceConfigTable" },
        { "PanelConfig",      "PanelConfigTable" },
        { "CardConfig",       "CardConfigTable" },
        { "SkillConfig",      "SkillConfigTable" },
        { "BuildPoolConfig",  "BuildPoolConfigTable" },
        { "ItemConfig",       "ItemConfigTable" },
        { "LevelAreaConfig",  "LevelAreaConfigTable" },
        { "LevelStageConfig", "LevelStageConfigTable" },
        { "SceneConfig",      "SceneConfigTable" },
    };

    private readonly Dictionary<Type, object> _tables = new Dictionary<Type, object>();
    private readonly Dictionary<Type, Dictionary<string, IConfigData>> _indexes = new Dictionary<Type, Dictionary<string, IConfigData>>();
    public bool IsInitialized { get; private set; }

    public async UniTask InitializeAsync(IProgress<float> progress = null)
    {
        if (IsInitialized) return;
        int total = ConfigAddressMap.Count;
        int completed = 0;
        foreach (var kvp in ConfigAddressMap)
        {
            try { await LoadConfigAsync(kvp.Key, kvp.Value); }
            catch (Exception ex) { Debug.LogWarning($"[ConfigService] 加载失败 {kvp.Key}: {ex.Message}"); }
            completed++;
            progress?.Report((float)completed / total);
        }
        IsInitialized = true;
        Debug.Log($"[ConfigService] 加载完成: {_tables.Count} 个表");
    }

    public T Get<T>(string id) where T : class, IConfigData
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (_indexes.TryGetValue(typeof(T), out var index) && index.TryGetValue(id, out var data))
            return data as T;
        return null;
    }

    public IReadOnlyList<T> GetAll<T>() where T : class, IConfigData
    {
        if (_tables.TryGetValue(typeof(T), out var list) && list is List<T> typedList)
            return typedList;
        return Array.Empty<T>();
    }

    public async UniTask ReloadAsync<T>() where T : class, IConfigData
    {
        string typeName = typeof(T).Name;
        if (!ConfigAddressMap.TryGetValue(typeName, out var address)) return;
        _tables.Remove(typeof(T));
        _indexes.Remove(typeof(T));
        await LoadConfigAsync(typeName, address);
    }

    public void Dispose() { _tables.Clear(); _indexes.Clear(); IsInitialized = false; }

    private async UniTask LoadConfigAsync(string typeName, string address)
    {

        var textAsset = await Addressables.LoadAssetAsync<TextAsset>(address).ToUniTask();
        if (textAsset == null) return;
        string json = textAsset.text;
        Addressables.Release(textAsset);
        if (string.IsNullOrWhiteSpace(json)) return;

        object table = typeName switch
        {
            "ResourceConfig" => JsonUtility.FromJson<ResourceConfigTable>(json),
            "PanelConfig" => JsonUtility.FromJson<PanelConfigTable>(json),
            "CardConfig" => JsonUtility.FromJson<CardConfigTable>(json),
            "SkillConfig" => JsonUtility.FromJson<SkillConfigTable>(json),
            "BuildPoolConfig" => JsonUtility.FromJson<BuildPoolConfigTable>(json),
            "ItemConfig" => JsonUtility.FromJson<ItemConfigTable>(json),
            "LevelAreaConfig" => JsonUtility.FromJson<LevelAreaConfigTable>(json),
            "LevelStageConfig" => JsonUtility.FromJson<LevelStageConfigTable>(json),
            "SceneConfig" => JsonUtility.FromJson<SceneConfigTable>(json),
            _ => null
        };
        if (table == null) return;
        var items = ExtractItemsFromJson(table);
        if (items == null || items.Count == 0) return;
        StoreConfigItems(items);
    }

    private void StoreConfigItems(List<IConfigData> items)
    {
        if (items == null || items.Count == 0) return;
        Type itemType = items[0].GetType();
        var listType = typeof(List<>).MakeGenericType(itemType);
        var typedList = Activator.CreateInstance(listType) as System.Collections.IList;
        for (int i = 0; i < items.Count; i++) typedList.Add(items[i]);
        _tables[itemType] = typedList;
        var index = new Dictionary<string, IConfigData>();
        for (int i = 0; i < items.Count; i++)
            if (!string.IsNullOrWhiteSpace(items[i].Id))
                index[items[i].Id] = items[i];
        _indexes[itemType] = index;
    }

    private static List<IConfigData> ExtractItemsFromJson(object table)
    {
        if (table is ResourceConfigTable rct && rct.Items != null)
            return rct.Items.ConvertAll(x => (IConfigData)x);
        if (table is PanelConfigTable pct && pct.Items != null)
            return pct.Items.ConvertAll(x => (IConfigData)x);
        if (table is CardConfigTable cct && cct.Items != null)
            return cct.Items.ConvertAll(x => (IConfigData)x);
        if (table is SkillConfigTable skt && skt.Items != null)
            return skt.Items.ConvertAll(x => (IConfigData)x);
        if (table is BuildPoolConfigTable bpt && bpt.Items != null)
            return bpt.Items.ConvertAll(x => (IConfigData)x);
        if (table is ItemConfigTable ict && ict.Items != null)
            return ict.Items.ConvertAll(x => (IConfigData)x);
        if (table is LevelAreaConfigTable lact && lact.Items != null)
            return lact.Items.ConvertAll(x => (IConfigData)x);
        if (table is LevelStageConfigTable lsct && lsct.Items != null)
            return lsct.Items.ConvertAll(x => (IConfigData)x);
        if (table is SceneConfigTable sct && sct.Items != null)
            return sct.Items.ConvertAll(x => (IConfigData)x);
        return null;
    }
}
