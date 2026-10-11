using Cysharp.Threading.Tasks;

/// <summary>
/// 战斗入口服务：编排从关卡选择到战斗场景的启动流程，
/// 以及结算后的"再次挑战 / 返回关卡选择"回退流程。
/// </summary>
public class BattleEntryService
{
    /// <summary>关卡选择面板 ID（返回关卡选择时打开）。</summary>
    private const string LevelStageSelectPanelId = "LevelStageSelect";

    private readonly ISceneService _sceneService;
    private readonly IUIService _uiService;
    private readonly ISaveService _saveService;
    private readonly IConfigService _configService;

    /// <summary>当前战斗上下文（跨场景传递）。</summary>
    public BattleStartContext CurrentContext { get; private set; }

    public BattleEntryService(ISceneService sceneService, IUIService uiService,
        ISaveService saveService, IConfigService configService)
    {
        _sceneService = sceneService;
        _uiService = uiService;
        _saveService = saveService;
        _configService = configService;
    }

    /// <summary>从关卡选择进入战斗。</summary>
    public async UniTask StartBattleAsync(string stageId, LevelMode mode)
    {
        CurrentContext = new BattleStartContext(stageId, mode);
        await _sceneService.LoadSceneAsync(SceneType.Battle);
    }

    /// <summary>从战斗结算返回主游戏场景。</summary>
    public async UniTask ReturnToGameScene()
    {
        CurrentContext = null;
        await _sceneService.LoadSceneAsync(SceneType.Lobby);
    }

    /// <summary>
    /// 再次挑战：保留当前关卡，清空编队与首发选择，重新从卡组选择流程开始。
    /// </summary>
    public async UniTask RetryCurrentStageAsync()
    {
        var context = CurrentContext;
        if (context == null || string.IsNullOrEmpty(context.StageId))
        {
            await ReturnToGameScene();
            return;
        }

        await StartBattleAsync(context.StageId, context.Mode);
    }

    /// <summary>
    /// 返回关卡选择：回到主场景并重新打开进入战斗前的区域关卡列表。
    /// </summary>
    public async UniTask ReturnToStageSelectAsync()
    {
        var area = ResolveCurrentArea();

        CurrentContext = null;
        await _sceneService.LoadSceneAsync(SceneType.Lobby);

        if (area != null && _uiService != null)
            await _uiService.OpenAsync<PanelBase>(LevelStageSelectPanelId, area);
    }

    /// <summary>由当前关卡的 StageId 反查所属区域配置（用于返回关卡选择）。</summary>
    private LevelAreaConfig ResolveCurrentArea()
    {
        var context = CurrentContext;
        if (context == null || string.IsNullOrEmpty(context.StageId) || _configService == null)
            return null;

        var stage = _configService.Get<LevelStageConfig>(context.StageId);
        if (stage == null || string.IsNullOrEmpty(stage.AreaId))
            return null;

        return _configService.Get<LevelAreaConfig>(stage.AreaId);
    }
}
