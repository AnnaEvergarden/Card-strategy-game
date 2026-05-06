using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 射线调试器：运行时按 F8 开关，开启后点击鼠标左键打印当前 UI 命中链。
/// 该组件会在启动时自动创建并跨场景常驻，便于排查按钮点击无响应问题。
/// </summary>
public sealed class UIRaycastDebugHotkey : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 调试器常驻对象名称。
    /// </summary>
    private const string RuntimeObjectName = "[Debug] UI Raycast Hotkey";

    /// <summary>
    /// 调试开关键（F8）。
    /// </summary>
    private const KeyCode ToggleKey = KeyCode.F8;

    /// <summary>
    /// 是否启用 UI 点击命中链打印。
    /// </summary>
    private static bool _isEnabled;

    /// <summary>
    /// 单例实例（用于防重复创建）。
    /// </summary>
    private static UIRaycastDebugHotkey _instance;

    /// <summary>
    /// PointerEventData 复用实例，避免每次点击分配。
    /// </summary>
    private PointerEventData _pointerEventData;

    /// <summary>
    /// 射线命中结果复用列表。
    /// </summary>
    private readonly List<RaycastResult> _raycastResults = new();

    #endregion

    #region Runtime Bootstrap

    /// <summary>
    /// 首场景加载前自动创建调试器对象，并设置为跨场景常驻。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
        {
            return;
        }

        var go = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UIRaycastDebugHotkey>();
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 保护单例：若外部重复挂载该组件，仅保留第一份。
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 监听开关键并在启用状态下输出点击命中链。
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey))
        {
            _isEnabled = !_isEnabled;
            Debug.Log($"[UIRaycastDebug] 状态已切换：{(_isEnabled ? "开启" : "关闭")}（快捷键 F8）。");
        }

        if (!_isEnabled)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            LogCurrentPointerRaycastResults();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 打印当前鼠标位置的 UI 射线命中链（从最上层到最下层）。
    /// </summary>
    private void LogCurrentPointerRaycastResults()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[UIRaycastDebug] 当前场景无 EventSystem，无法执行 UI 射线检测。");
            return;
        }

        _pointerEventData ??= new PointerEventData(EventSystem.current);
        _pointerEventData.position = Input.mousePosition;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

        Debug.Log($"[UIRaycastDebug] 点击坐标={_pointerEventData.position}，命中数量={_raycastResults.Count}。");
        for (var i = 0; i < _raycastResults.Count; i++)
        {
            var hit = _raycastResults[i];
            var path = hit.gameObject != null ? BuildTransformPath(hit.gameObject.transform) : "<null>";
            Debug.Log($"[UIRaycastDebug] [{i}] {path}");
        }
    }

    /// <summary>
    /// 生成层级路径，便于在 Hierarchy 中定位拦截点击的对象。
    /// </summary>
    /// <param name="target">目标节点。</param>
    /// <returns>从根到叶子的层级路径字符串。</returns>
    private static string BuildTransformPath(Transform target)
    {
        if (target == null)
        {
            return "<null>";
        }

        var path = target.name;
        var current = target.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }

    #endregion
}
