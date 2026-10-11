using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 服务实现：统一管理面板的加载、打开、关闭、缓存与栈导航。
/// 面板打开顺序：缓存 → 场景已有实例 → Addressables 加载预制体
///
/// [P1 FIX] EnsureCanvas 不再缓存 Canvas 引用。场景切换后旧 Canvas 被销毁，
/// 每次打开面板时重新搜索当前场景的 Canvas，防止使用垂悬指针。
/// </summary>
public class UIService : IUIService, IDisposable
{
    private readonly Dictionary<string, PanelBase> _panelCache = new Dictionary<string, PanelBase>();
    private readonly List<string> _navigationStack = new List<string>();
    private readonly HashSet<string> _openingPanels = new HashSet<string>();
    private readonly IResourceService _resourceService;
    private ServiceContainer _container;

    public UIService(IResourceService resourceService)
    {
        _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
    }

    public void SetContainer(ServiceContainer container) { _container = container; }

    public async UniTask<TPanel> OpenAsync<TPanel>(string panelId, object param = null) where TPanel : PanelBase
    {
        if (string.IsNullOrEmpty(panelId)) return null;
        EnsureCanvas();

        // 并发保护：同一 panelId 正在加载时等待
        while (_openingPanels.Contains(panelId))
            await UniTask.Yield();
        _openingPanels.Add(panelId);
        try
        {
        // 1. 查缓存——已打开的面板直接激活
        if (_panelCache.TryGetValue(panelId, out var cached) && cached != null)
        {
            cached.UIService = this;
            cached.ResourceService = _resourceService;
            cached.ServiceContainer = _container;
            await cached.OnOpenAsync(param);
            PushToStack(panelId);
            return cached as TPanel;
        }

        // 2. 查场景中是否已有实例（开发阶段 panel 直接放在场景中）
        var allPanels = UnityEngine.Object.FindObjectsOfType<TPanel>(true);
        var existing = Array.Find(allPanels, p => p.PanelId == panelId);

        // [fallback] 精确匹配 PanelId 失败时，查找 PanelId 未设置的场景面板
        // 防止 Addressables 加载产生重复实例
        if (existing == null)
        {
            PanelBase unmatched = null;
            for (int i = 0; i < allPanels.Length; i++)
            {
                if (string.IsNullOrEmpty(allPanels[i].PanelId) && !_panelCache.ContainsValue(allPanels[i]))
                {
                    if (unmatched != null) { unmatched = null; break; }
                    unmatched = allPanels[i];
                }
            }
            if (unmatched != null)
            {
                existing = unmatched as TPanel;
                Debug.LogWarning($"[UIService] 面板 \"{existing.name}\" 的 PanelId 未设置，自动匹配为 \"{panelId}\"。请在 Inspector 中设置 _panelId。");
            }
        }

        if (existing != null)
        {
            _panelCache[panelId] = existing;
            existing.UIService = this;
            existing.ResourceService = _resourceService;
            existing.ServiceContainer = _container;
            await existing.OnOpenAsync(param);
            PushToStack(panelId);
            return existing;
        }

        // 3. 获取面板配置
        PanelConfig config = ResolvePanelConfig(panelId);
        if (config.ResourceId <= 0)
        {
            Debug.LogError($"[UIService] PanelId={panelId} 未配置 ResourceId");
            return null;
        }

        // 4. 加载预制体
        var prefab = await _resourceService.LoadAsync<GameObject>(config.ResourceId);
        if (prefab == null)
        {
            Debug.LogError($"[UIService] 面板预制体加载失败 ResourceId={config.ResourceId}, PanelId={panelId}");
            return null;
        }

       // 5. 实例化到当前场景的 Canvas 下
       var rootCanvas = GameObject.FindObjectOfType<Canvas>()?.transform;
       if (rootCanvas == null)
       {
           Debug.LogError("[UIService] 当前场景中未找到 Canvas，无法实例化面板");
           return null;
       }

        // 先将预制体设为非激活，避免 Instantiate 时触发 OnEnable 而服务字段尚未赋值
        bool prefabWasActive = prefab.activeSelf;
        if (prefabWasActive)
            prefab.SetActive(false);

       var instance = GameObject.Instantiate(prefab, rootCanvas, false);
        if (prefabWasActive)
            prefab.SetActive(true);

       instance.name = "Panel_" + panelId;

       var panel = instance.GetComponent<TPanel>();
       if (panel == null)
       {
           Debug.LogError($"[UIService] 预制体上未找到 {typeof(TPanel).Name} 组件, PanelId={panelId}");
           GameObject.Destroy(instance);
           return null;
       }

       // 6. 初始化面板
       panel.UIService = this;
       panel.ResourceService = _resourceService;
       panel.ServiceContainer = _container;
       SetPanelLayer(panel, config.Layer);

       // 7. 缓存并打开
       _panelCache[panelId] = panel;
       PushToStack(panelId);
        // 激活面板 — OnEnable 此时才触发，Service 已就绪
        instance.SetActive(true);
       await panel.OnOpenAsync(param);

        return panel;
        }
        finally
        {
            _openingPanels.Remove(panelId);
        }
    }

    public async UniTask CloseAsync(string panelId)
    {
        if (string.IsNullOrEmpty(panelId)) return;
        if (!_panelCache.TryGetValue(panelId, out var panel) || panel == null) return;

        await panel.OnCloseAsync();
        PanelConfig config = ResolvePanelConfig(panelId);
        if (config.CacheOnClose) { panel.gameObject.SetActive(false); }
        else { _panelCache.Remove(panelId); GameObject.Destroy(panel.gameObject); }

        // 从栈中移除当前面板后，自动回到上一个面板
        _navigationStack.Remove(panelId);
        if (_navigationStack.Count > 0)
        {
            var prevTop = _navigationStack[^1];
            if (_panelCache.TryGetValue(prevTop, out var prevPanel) && prevPanel != null)
            {
                prevPanel.UIService = this;
                prevPanel.ResourceService = _resourceService;
                prevPanel.ServiceContainer = _container;
                prevPanel.gameObject.SetActive(true);
                await prevPanel.OnOpenAsync();
            }
        }
    }

    public async UniTask GoBackAsync()
    {
        if (_navigationStack.Count <= 1) return;
        var currentTop = _navigationStack[^1];
        _navigationStack.RemoveAt(_navigationStack.Count - 1);
        if (_panelCache.TryGetValue(currentTop, out var cp) && cp != null)
        { await cp.OnCloseAsync(); cp.gameObject.SetActive(false); }
        var newTop = _navigationStack[^1];
        if (_panelCache.TryGetValue(newTop, out var pp) && pp != null)
        {
            pp.UIService = this;
            pp.ResourceService = _resourceService;
            pp.ServiceContainer = _container;
            pp.gameObject.SetActive(true);
            await pp.OnOpenAsync();
        }
    }

    public async UniTask GoHomeAsync()
    {
        for (int i = _navigationStack.Count - 1; i >= 0; i--)
        {
            var pid = _navigationStack[i];
            if (_panelCache.TryGetValue(pid, out var p) && p != null) p.gameObject.SetActive(false);
        }
        _navigationStack.Clear();
        await OpenAsync<PanelBase>("LobbyPanel");
    }

    public void Dispose()
    {
        foreach (var kvp in _panelCache)
        { if (kvp.Value != null && kvp.Value.gameObject != null) GameObject.Destroy(kvp.Value.gameObject); }
        _panelCache.Clear(); _navigationStack.Clear();

    }

    /// <summary>
    /// 确保当前场景中存在 Canvas。每次打开面板时重新搜索，
    /// 防止旧 Canvas 被销毁后使用垂悬指针（[P1 FIX]）。
    /// </summary>
    private void EnsureCanvas()
    {
        // [P1 FIX] 不再缓存 _canvasFound，场景切换后旧 Canvas 会被销毁，
        // 每次 OpenAsync 重新搜索当前场景有效的 Canvas。
        var canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[UIService] 当前场景中未找到 Canvas，请在场景中放置 Canvas");
        }
    }

    private void PushToStack(string panelId) { _navigationStack.Remove(panelId); _navigationStack.Add(panelId); }
    private static void SetPanelLayer(PanelBase panel, int layer) { panel.transform.SetSiblingIndex(layer * 100); }

    private PanelConfig ResolvePanelConfig(string panelId)
    {
        if (_container != null)
        {
            var configService = _container.TryResolve<IConfigService>();
            var config = configService?.Get<PanelConfig>(panelId);
            if (config != null) return config;
        }
        return new PanelConfig { PanelId = panelId, ResourceId = 0, Layer = 1, CacheOnClose = true };
    }
}