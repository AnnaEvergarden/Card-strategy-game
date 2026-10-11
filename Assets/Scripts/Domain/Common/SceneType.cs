/// <summary>
/// 场景类型枚举：标识游戏中的不同场景。
/// 所有场景加载操作必须通过此枚举，禁止直接使用场景名称字符串。
/// SceneService 内部通过 SceneConfig 配置表将 SceneType 映射为 Addressables 加载地址。
/// 新增场景时在此枚举添加条目，并在 SceneConfigSO 中补充对应配置。
/// </summary>
public enum SceneType
{
    /// <summary>
    /// 未指定。
    /// </summary>
    None = 0,

    /// <summary>
    /// 主界面场景（Lobby/大厅）：显示游戏主功能入口。
    /// </summary>
    Lobby,

    /// <summary>
    /// 战斗场景：执行回合制对战逻辑。
    /// </summary>
    Battle,

    /// <summary>
    /// 登录/标题场景：显示登录、注册与游戏标题。
    /// </summary>
    Title,
}
