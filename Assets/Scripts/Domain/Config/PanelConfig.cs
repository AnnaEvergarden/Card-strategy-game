/// <summary>
/// 面板配置数据：映射 PanelId → ResourceId（预制体资源）→ 层级 → 缓存策略。
/// 由 ConfigExporter 工具从 SO 导出为 JSON，运行时通过 UIService 加载使用。
/// 纯数据模型，不依赖 Unity API。
/// </summary>
[System.Serializable]
public class PanelConfig : IConfigData
{
    public string Id => PanelId;
    public string PanelId;
    public int ResourceId;
    public int Layer;
    public bool CacheOnClose;
}

[System.Serializable]
public class PanelConfigTable
{
    public System.Collections.Generic.List<PanelConfig> Items = new System.Collections.Generic.List<PanelConfig>();
}
