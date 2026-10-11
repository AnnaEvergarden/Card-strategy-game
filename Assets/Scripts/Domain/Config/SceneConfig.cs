using System;

/// <summary>
/// 场景配置数据：映射 SceneType → Addressables 地址 → 加载策略。
/// 由 ConfigExporter 从 SO 导出为 JSON，ConfigService 加载至运行时内存表。
/// 纯数据模型，不依赖 Unity API。
/// 业务层通过 SceneType 请求场景切换，SceneService 内部查询此配置获取场景地址。
/// </summary>
[Serializable]
public class SceneConfig : IConfigData
{
    /// <summary>
    /// 配置唯一标识（对应 SceneType 的 int 值）。
    /// </summary>
    public string Id => ((int)SceneType).ToString();

    /// <summary>
    /// 场景类型。
    /// </summary>
    public SceneType SceneType;

    /// <summary>
    /// 场景在 Addressables 中的加载地址（例如 "Assets/Scenes/TitleScene.scene"）。
    /// </summary>
    public string SceneAddress;

    /// <summary>
    /// 是否在游戏启动时预加载此场景。
    /// </summary>
    public bool IsPreload;

    /// <summary>
    /// 切换到该场景时是否需要主动释放旧场景占用的资源。
    /// </summary>
    public bool NeedReleaseOldScene;

    /// <summary>
    /// 进入该场景后自动打开的默认面板标识。
    /// 场景控制器的 InitializeAsync 通过此字段决定打开哪个面板。
    /// 为空字符串表示不自动打开面板。
    /// </summary>
    public string DefaultPanelId;
}

