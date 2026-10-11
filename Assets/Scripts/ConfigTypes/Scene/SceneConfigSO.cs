using UnityEngine;

/// <summary>
/// 场景配置 SOAsset —— 映射 SceneType → Addressables 地址 → 加载策略。
/// 通过 CreateAssetMenu → CardGame/Config/Scene/Scene Config 创建。
/// 数据由 ConfigExporter 导出为 SceneConfigTable.json，运行时通过 ConfigService 查询。
/// </summary>
[CreateAssetMenu(menuName = "CardGame/Config/Scene/Scene Config", fileName = "SceneConfig_")]
public class SceneConfigSO : ScriptableObject, IConfigData
{
    [Header("场景信息")]
    [SerializeField]
    [Tooltip("场景类型标识")]
    private SceneType _sceneType;

    [SerializeField]
    [Tooltip("场景在 Addressables 中的加载地址（例如 Assets/Scenes/LobbyScene.scene）")]
    private string _sceneAddress;

    [SerializeField]
    [Tooltip("是否在游戏启动时预加载此场景")]
    private bool _isPreload;

    [SerializeField]
    [Tooltip("切换到该场景时是否需要主动释放旧场景占用的资源")]
    private bool _needReleaseOldScene;

    [SerializeField]
    [Tooltip("进入场景后自动打开的默认面板标识（如 \"Lobby\"）,非Address")]
    private string _defaultPanelId;

    public string Id => ((int)_sceneType).ToString();
    public SceneType SceneType => _sceneType;
    public string SceneAddress => _sceneAddress;
    public bool IsPreload => _isPreload;
    public bool NeedReleaseOldScene => _needReleaseOldScene;
    public string DefaultPanelId => _defaultPanelId;

    public SceneConfig ToDomain() => new SceneConfig
    {
        SceneType = _sceneType,
        SceneAddress = _sceneAddress,
        IsPreload = _isPreload,
        NeedReleaseOldScene = _needReleaseOldScene,
        DefaultPanelId = _defaultPanelId,
    };
}



