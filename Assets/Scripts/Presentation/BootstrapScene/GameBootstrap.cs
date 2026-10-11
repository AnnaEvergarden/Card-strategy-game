using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private const int MaxHotUpdateRetries = 3;

    private static GameBootstrap _instance;
    private static bool _hasInitialized;

    private ServiceContainer _container;
    private LoadingController _loadingController;

    public static ServiceContainer Container => _instance?._container;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async UniTaskVoid Start()
    {
        if (_hasInitialized) return;
        _hasInitialized = true;

        try
        {
            Debug.Log("[GameBootstrap] 启动初始化流程");
            await CreateServicesAsync();
            await InitializeSubsystemsAsync();

            _loadingController.StartBootstrap();
            _loadingController.ReportBootstrapProgress(0.10f, "正在检查资源更新...");
            await RunHotUpdateWithProgressAsync();
            _loadingController.ReportBootstrapProgress(0.70f, "准备进入游戏...");
            await LoadEntrySceneAsync();
            Debug.Log("[GameBootstrap] 初始化流程完成");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameBootstrap] 启动失败: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async UniTask CreateServicesAsync()
    {
        _container = new ServiceContainer();

        // 1. ResourceService
        var resourceTable = await ResourceTable.LoadFromAddressableAsync();
        var resourceService = new ResourceService(resourceTable);
        _container.Register<IResourceService>(resourceService);

        // 2. ConfigService
        var configService = new ConfigService();
        _container.Register<IConfigService>(configService);

        // 3. SceneService
        var sceneService = new SceneService(configService, resourceService);
        _container.Register<ISceneService>(sceneService);

        // 4. UIService
        var uiService = new UIService(resourceService);
        uiService.SetContainer(_container);
        _container.Register<IUIService>(uiService);

        // 5. SaveService
        var saveService = new SaveService();
        _container.Register<ISaveService>(saveService);

        // 6. AudioService
        var audioService = new AudioService();
        _container.Register<IAudioService>(audioService);

        // 7. HotUpdateService
        var hotUpdateService = new HotUpdateService();
        _container.Register<IHotUpdateService>(hotUpdateService);

        // 8. EventBus — 数据变更事件总线
        var eventBus = new EventBus();
        _container.Register<IEventBus>(eventBus);

        // 9. Repositories
        var inventoryRepo = new InventoryRepository(saveService);
        _container.Register<IInventoryRepository>(inventoryRepo);

        var cardCollectionRepo = new CardCollectionRepository(saveService);
        _container.Register<ICardCollectionRepository>(cardCollectionRepo);

        var fleetRepo = new FleetRepository(saveService);
        _container.Register<IFleetRepository>(fleetRepo);

        var accountRepo = new AccountRepository(saveService);
        _container.Register<IAccountRepository>(accountRepo);

        var playerProfileRepo = new PlayerProfileRepository(saveService);
        _container.Register<IPlayerProfileRepository>(playerProfileRepo);

        // 10. UnitOfWork — 所有服务共享同一个事务实例，实现跨 Repository 原子写入
        var uow = new UnitOfWork();
        _container.Register<IUnitOfWork>(uow);

        // 11. Application Services（数据层）
        var inventoryService = new InventoryService(inventoryRepo, eventBus, uow);
        _container.Register<IInventoryService>(inventoryService);

        var cardCollectionService = new CardCollectionService(cardCollectionRepo, eventBus, uow);
        _container.Register<ICardCollectionService>(cardCollectionService);

        var fleetService = new FleetService(fleetRepo);
        _container.Register<IFleetService>(fleetService);

        var accountService = new AccountService(accountRepo, eventBus, uow);
        _container.Register<IAccountService>(accountService);

        var playerProfileService = new PlayerProfileService(playerProfileRepo, uow);
        _container.Register<IPlayerProfileService>(playerProfileService);

        // 11. 业务服务（跨领域编排）
        var loginService = new LoginService(accountService, sceneService);
        _container.Register(loginService);

        var battleEntryService = new BattleEntryService(sceneService, uiService, saveService, configService);
        _container.Register(battleEntryService);

        // 12. BattleService — 战斗用例编排（配置/编队/收藏 → 会话构建与操作）
        var battleService = new BattleService(configService, fleetService, cardCollectionService);
        _container.Register<IBattleService>(battleService);

        // 12. LoadingController
        _loadingController = new LoadingController(sceneService, uiService);
        Debug.Log("[GameBootstrap] 全局服务创建完成");
    }

    private async UniTask InitializeSubsystemsAsync()
    {
        var configService = _container.Resolve<IConfigService>();
        await configService.InitializeAsync();
        Debug.Log("[GameBootstrap] 配置表加载完成");
        var audioService = _container.Resolve<IAudioService>();
        await audioService.InitializeAsync();
        Debug.Log("[GameBootstrap] 音频系统初始化完成");
    }

    private async UniTask RunHotUpdateWithProgressAsync()
    {
        for (int attempt = 1; attempt <= MaxHotUpdateRetries; attempt++)
        {
            bool succeeded = await TryRunHotUpdateOnceAsync();
            if (succeeded)
                return;

            _loadingController.ReportBootstrapProgress(0.25f, "资源更新失败，请检查网络");

            if (attempt >= MaxHotUpdateRetries)
            {
                await NetworkErrorDialog.ShowAsync("多次资源更新失败，已跳过本次更新", null, "知道了");
                return;
            }

            bool retry = await NetworkErrorDialog.ShowAsync(
                "网络连接异常，请检查网络后重试",
                "重试",
                "跳过更新");
            if (!retry)
                return;
        }
    }

    private async UniTask<bool> TryRunHotUpdateOnceAsync()
    {
        var hotUpdateService = _container.Resolve<IHotUpdateService>();
        _loadingController.ReportBootstrapProgress(0.10f, "正在检查资源更新...");
        var catalogs = await hotUpdateService.CheckForCatalogUpdatesAsync();
        if (catalogs == null || catalogs.Count == 0)
        {
            _loadingController.ReportBootstrapProgress(0.45f, "资源已是最新，无需更新");
            await UniTask.Delay(TimeSpan.FromSeconds(0.3f));
            return true;
        }

        _loadingController.ReportBootstrapProgress(0.25f, "发现更新内容，正在下载...");
        var progress = new Progress<float>(p =>
        {
            float mapped = 0.25f + p * 0.40f;
            _loadingController.ReportBootstrapProgress(mapped, $"正在下载更新...{(int)(p * 100)}%");
        });

        HotUpdateResult result = await hotUpdateService.DownloadUpdatesAsync(catalogs, progress);
        if (result != HotUpdateResult.Succeeded)
            return false;

        _loadingController.ReportBootstrapProgress(0.65f, "资源更新完成");
        return true;
    }

    private async UniTask LoadEntrySceneAsync()
    {
        var sceneService = _container.Resolve<ISceneService>();
        Debug.Log("[GameBootstrap] 加载主场景...");
        await sceneService.LoadSceneAsync(SceneType.Title);
    }
}
