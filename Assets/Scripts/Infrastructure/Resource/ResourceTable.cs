using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 资源配置运行时查询表。Addressables 地址在 ConfigService 中硬编码。
/// </summary>
public class ResourceTable
{
    public const string DefaultConfigAddress = "ResourceConfigTable";

    private readonly Dictionary<int, ResourceConfig> _configsById = new Dictionary<int, ResourceConfig>();
    private readonly Dictionary<string, List<int>> _idsByGroup = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
    public bool IsInitialized { get; private set; }
    public int Count => _configsById.Count;

    public ResourceTable() { }

    /// <summary>从 Addressables 加载资源配置 JSON，失败时返回空表。</summary>
    public static async UniTask<ResourceTable> LoadFromAddressableAsync(string configAddress = DefaultConfigAddress)
    {
        try
        {
            var textAsset = await Addressables.LoadAssetAsync<TextAsset>(configAddress).ToUniTask();
            if (textAsset == null)
            {
                Debug.LogWarning($"[ResourceTable] 未找到资源配置: {configAddress}，使用空表");
                return new ResourceTable();
            }
            var json = textAsset.text;
            Addressables.Release(textAsset);
            var tableWrapper = JsonUtility.FromJson<ResourceConfigTable>(json);
            var resourceTable = new ResourceTable();
            if (tableWrapper != null && tableWrapper.Items != null)
            {
                resourceTable.Build(tableWrapper.Items);
                Debug.Log($"[ResourceTable] 加载 {tableWrapper.Items.Count} 条资源配置");
            }
            return resourceTable;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ResourceTable] 加载失败: {ex.Message}，使用空表继续");
            return new ResourceTable();
        }
    }

    public void Build(IReadOnlyList<ResourceConfig> configs)
    {
        if (configs == null) throw new ArgumentNullException(nameof(configs));
        _configsById.Clear();
        _idsByGroup.Clear();
        for (int i = 0; i < configs.Count; i++)
        {
            var cfg = configs[i];
            if (cfg == null || cfg.ResourceId <= 0 || string.IsNullOrWhiteSpace(cfg.Address)) continue;
            _configsById[cfg.ResourceId] = cfg;
            var group = string.IsNullOrWhiteSpace(cfg.Group) ? "Default" : cfg.Group.Trim();
            if (!_idsByGroup.TryGetValue(group, out var list))
            {
                list = new List<int>();
                _idsByGroup[group] = list;
            }
            list.Add(cfg.ResourceId);
        }
        IsInitialized = true;
    }

    public bool TryGetAddress(ResourceId resourceId, out string address)
    {
        address = null;
        if (_configsById.TryGetValue(resourceId, out var cfg)) { address = cfg.Address; return true; }
        return false;
    }

    public bool TryGetConfig(ResourceId resourceId, out ResourceConfig config)
    {
        return _configsById.TryGetValue(resourceId, out config);
    }

    public IReadOnlyList<int> GetIdsByGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group)) return Array.Empty<int>();
        if (_idsByGroup.TryGetValue(group.Trim(), out var list)) return list;
        return Array.Empty<int>();
    }
}
