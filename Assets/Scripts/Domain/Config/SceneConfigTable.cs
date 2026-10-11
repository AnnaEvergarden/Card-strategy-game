using System;
using System.Collections.Generic;

/// <summary>
/// 场景配置表容器：包含多条场景配置。
/// 对应导出的 JSON 文件结构，由 ConfigService 加载至运行时内存表。
/// </summary>
[Serializable]
public class SceneConfigTable
{
    public List<SceneConfig> Items = new List<SceneConfig>();
}
