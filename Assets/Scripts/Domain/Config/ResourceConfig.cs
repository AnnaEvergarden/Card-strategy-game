using System.Collections.Generic;

/// <summary>
/// 资源配置数据：映射 ResourceId → Addressables 地址。
/// 由 ConfigExporter 工具从 SO 导出为 JSON，ConfigService 加载至运行时内存表。
/// 纯数据模型，不依赖 Unity API。
/// </summary>
[System.Serializable]
public class ResourceConfig : IConfigData
{
    /// <summary>配置唯一标识（对应 ResourceId）。</summary>
    public string Id => ResourceId.ToString();

    /// <summary>资源标识符（Domain 层通过此 ID 请求资源）。</summary>
    public int ResourceId;

    /// <summary>资源在 Addressables 中的加载地址。</summary>
    public string Address;

    /// <summary>资源类型标识（例如 "Sprite"、"GameObject"、"AudioClip"、"TextAsset"）。</summary>
    public string AssetType;

    /// <summary>资源所属 Addressables Group 名称。</summary>
    public string Group;
}

/// <summary>
/// 资源配置表容器：包含多条资源配置。
/// 对应导出的 JSON 文件结构。
/// </summary>
[System.Serializable]
public class ResourceConfigTable
{
    public List<ResourceConfig> Items = new List<ResourceConfig>();
}
