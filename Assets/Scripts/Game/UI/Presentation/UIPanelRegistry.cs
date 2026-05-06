using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UI 面板注册表：同一场景内用栈管理「前进打开 / 返回弹出 / 主界面清空再压栈」；换场景后清空栈并压入该场景默认根面板。
/// </summary>
public static class UIPanelRegistry
{
    #region Fields

    /// <summary>
    /// 面板字典：key 为面板名，value 为面板实例。
    /// </summary>
    private static readonly Dictionary<string, BasePanel> Panels = new();

    /// <summary>
    /// 当前场景内的面板导航栈（底到顶）；仅栈顶对应 GameObject 为激活状态（由 Push/Pop/ClearAndPush 维护）。
    /// </summary>
    private static readonly List<string> PanelStack = new();

    /// <summary>
    /// 是否已订阅场景加载（避免重复订阅）。
    /// </summary>
    private static bool _sceneHooked;

    #endregion

    #region Static Constructor

    /// <summary>
    /// 订阅场景加载以在换场景后重置栈并打开默认面板。
    /// </summary>
    static UIPanelRegistry()
    {
        HookSceneLoaded();
    }

    #endregion

    #region Public API — 注册

    /// <summary>
    /// 注册面板到字典，若名称为空则回退使用物体名。
    /// </summary>
    public static void Register(BasePanel panel)
    {
        if (panel == null)
        {
            return;
        }

        var key = NormalizeKey(panel.PanelName);
        if (string.IsNullOrEmpty(key))
        {
            key = NormalizeKey(panel.gameObject.name);
            panel.PanelName = key;
        }

        Panels[key] = panel;
    }

    /// <summary>
    /// 注销面板，只有字典中当前实例匹配时才移除。
    /// </summary>
    public static void Unregister(BasePanel panel)
    {
        if (panel == null)
        {
            return;
        }

        var key = NormalizeKey(panel.PanelName);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (Panels.TryGetValue(key, out var cur) && ReferenceEquals(cur, panel))
        {
            Panels.Remove(key);
        }
    }

    /// <summary>
    /// 按名称尝试获取面板实例。
    /// </summary>
    public static bool TryGet(string panelName, out BasePanel panel)
        => Panels.TryGetValue(NormalizeKey(panelName), out panel);

    #endregion

    #region Public API — 栈导航

    /// <summary>
    /// 打开目标面板并压栈：隐藏当前栈顶，将目标压为新的栈顶并显示。
    /// </summary>
    /// <param name="panelName">要打开的面板名。</param>
    public static void Push(string panelName)
    {
        var key = NormalizeKey(panelName);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (PanelStack.Count > 0 && StackTop() == key)
        {
            return;
        }

        if (PanelStack.Count > 0)
        {
            Hide(StackTop());
        }

        PanelStack.Add(key);
        Show(key);
    }

    /// <summary>
    /// 弹出当前栈顶并恢复上一层面板；栈中只有一层时无法弹出。
    /// </summary>
    /// <returns>是否成功弹出。</returns>
    public static bool TryPop()
    {
        if (PanelStack.Count <= 1)
        {
            return false;
        }

        var current = PanelStack[^1];
        Hide(current);
        PanelStack.RemoveAt(PanelStack.Count - 1);
        var prev = PanelStack[^1];
        Show(prev);
        return true;
    }

    /// <summary>
    /// 清空栈中记录并隐藏这些面板，再隐藏其余已注册面板，最后将指定面板作为唯一栈元素显示（主界面按钮）。
    /// </summary>
    /// <param name="panelName">清空后要打开并压栈的面板名（游戏场景一般为 <see cref="PanelNames.MainPanel"/>）。</param>
    public static void ClearAndPush(string panelName)
    {
        var key = NormalizeKey(panelName);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        while (PanelStack.Count > 0)
        {
            Hide(PanelStack[^1]);
            PanelStack.RemoveAt(PanelStack.Count - 1);
        }

        HideAllRegisteredExcept(key);
        PanelStack.Add(key);
        Show(key);
    }

    /// <summary>
    /// 若栈为空则按当前场景压入默认根面板（补偿部分环境下首场景未触发 sceneLoaded 的顺序问题）。
    /// </summary>
    public static void EnsureDefaultStackForActiveScene()
    {
        if (PanelStack.Count > 0)
        {
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.name == SceneNames.TitleScene)
        {
            ApplySceneDefaultStack(PanelNames.TitlePanel);
        }
        else if (scene.name == SceneNames.GameScene)
        {
            ApplySceneDefaultStack(PanelNames.MainPanel);
        }
    }

    #endregion

    #region Public API — 底层显示（场景初始化等可配合栈外使用）

    /// <summary>
    /// 显示指定面板（不修改栈；一般用于与栈逻辑无关的显隐）。
    /// </summary>
    public static void Show(string panelName)
    {
        var p = ResolvePanel(panelName);
        if (p != null)
        {
            p.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏指定面板（不修改栈）。
    /// </summary>
    public static void Hide(string panelName)
    {
        var p = ResolvePanel(panelName);
        if (p != null)
        {
            p.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Public API — 场景

    /// <summary>
    /// 加载目标场景；换场景前清空栈（新场景加载后会由 sceneLoaded 压入默认面板）。
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        PanelStack.Clear();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 退出游戏（编辑器下停止播放，打包后退出应用）。
    /// </summary>
    public static void QuitGame()
    {
        PanelStack.Clear();
        GameDataSaveService.SaveAll();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Private Methods

    private static void HookSceneLoaded()
    {
        if (_sceneHooked)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        _sceneHooked = true;
    }

    /// <summary>
    /// 非叠加加载完成后：清空栈并按场景压入默认根面板。
    /// </summary>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
        {
            return;
        }

        PanelStack.Clear();

        if (scene.name == SceneNames.TitleScene)
        {
            ApplySceneDefaultStack(PanelNames.TitlePanel);
        }
        else if (scene.name == SceneNames.GameScene)
        {
            ApplySceneDefaultStack(PanelNames.MainPanel);
        }
        else
        {
            HideAllRegisteredExcept(string.Empty);
        }
    }

    /// <summary>
    /// 隐藏除指定键以外的所有已注册面板，再显示默认面板并压入栈底（此时栈仅此一层）。
    /// </summary>
    private static void ApplySceneDefaultStack(string defaultPanelName)
    {
        var key = NormalizeKey(defaultPanelName);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        HideAllRegisteredExcept(key);
        PanelStack.Clear();
        PanelStack.Add(key);
        Show(key);
    }

    private static void HideAllRegisteredExcept(string keepKeyNormalized)
    {
        foreach (var kv in Panels)
        {
            if (kv.Value == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(keepKeyNormalized))
            {
                kv.Value.gameObject.SetActive(false);
                continue;
            }

            if (NormalizeKey(kv.Key) != keepKeyNormalized)
            {
                kv.Value.gameObject.SetActive(false);
            }
        }
    }

    private static string StackTop() => PanelStack.Count == 0 ? string.Empty : PanelStack[^1];

    private static string NormalizeKey(string key) => (key ?? string.Empty).Trim();

    private static BasePanel ResolvePanel(string panelName)
    {
        if (TryGet(panelName, out var p) && p != null)
        {
            return p;
        }

        var key = NormalizeKey(panelName);
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        p = FindPanelInScene(key);
        if (p != null)
        {
            Register(p);
        }

        return p;
    }

    private static BasePanel FindPanelInScene(string key)
    {
        var all = Object.FindObjectsOfType<BasePanel>(true);
        foreach (var bp in all)
        {
            var pk = NormalizeKey(bp.PanelName);
            if (!string.IsNullOrEmpty(pk) && pk == key)
            {
                return bp;
            }

            if (NormalizeKey(bp.gameObject.name) == key)
            {
                return bp;
            }
        }

        return null;
    }

    #endregion
}
