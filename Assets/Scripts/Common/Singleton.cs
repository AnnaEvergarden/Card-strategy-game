using UnityEngine;

/// <summary>
/// 泛型单例基类，供需要跨场景存在的 Manager 等继承。
/// 子类在 Awake 时若已存在其它实例则销毁自身，否则保留并可选 DontDestroyOnLoad。
/// </summary>
/// <typeparam name="T">继承本类的 MonoBehaviour 类型（如 UIPanelManager）</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();

    /// <summary>
    /// 是否在切换场景时保留该物体不销毁。子类可重写为 false 表示仅当前场景单例。
    /// </summary>
    protected virtual bool PersistAcrossScenes => true;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();
                        if (_instance == null)
                        {
                            Debug.LogWarning($"[Singleton] 场景中未找到 {typeof(T).Name}，请确保场景内已挂载或先访问 Instance 前已加载对应场景。");
                        }
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 当前是否存在有效单例（可用于判断是否已初始化）。
    /// </summary>
    public static bool HasInstance => _instance != null;

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this as T;
        if (PersistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
