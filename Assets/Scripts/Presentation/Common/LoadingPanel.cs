using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 加载面板：作为 PanelBase 子类，通过 IUIService 统一管理。
/// 显示当前加载任务描述、进度条、百分比 Loading 提示。
/// 由 LoadingController 订阅 SceneService 事件驱动刷新，不与 SceneService 直接耦合。
/// </summary>
public class LoadingPanel : PanelBase
{
    [Header("进度显示")]
    [SerializeField]
    [Tooltip("当前加载任务描述文本")]
    private TMP_Text _taskDescriptionText;

    [SerializeField]
    [Tooltip("加载进度条")]
    private Slider _progressSlider;

    [SerializeField]
    [Tooltip("进度百分比文本")]
    private TMP_Text _progressText;

    [SerializeField]
    [Tooltip("加载状态提示文本")]
    private TMP_Text _loadingMessageText;

    public override UniTask OnOpenAsync(object param = null)
    {
        gameObject.SetActive(true);
        UpdateProgress(0f, "正在准备...");
        SetTaskDescription(param as string ?? string.Empty);
        return UniTask.CompletedTask;
    }

    public override UniTask OnCloseAsync()
    {
        gameObject.SetActive(false);
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// 设置当前加载任务描述（例如："正在加载场景配置..."）。
    /// </summary>
    public void SetTaskDescription(string description)
    {
        if (_taskDescriptionText != null)
        {
            _taskDescriptionText.text = description ?? string.Empty;
        }
    }

    /// <summary>
    /// 更新加载进度与状态文本。
    /// </summary>
    /// <param name="progress">进度值（0～1）</param>
    /// <param name="message">当前加载状态描述文本</param>
    public void UpdateProgress(float progress, string message)
    {
        if (_progressSlider != null)
        {
            _progressSlider.value = Mathf.Clamp01(progress);
        }
        if (_progressText != null)
        {
            _progressText.text = $"{(int)(Mathf.Clamp01(progress) * 100)}%";
        }
        if (_loadingMessageText != null)
        {
            _loadingMessageText.text = message ?? string.Empty;
        }
    }
}
