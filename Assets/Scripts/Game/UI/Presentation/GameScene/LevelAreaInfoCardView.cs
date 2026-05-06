using TMPro;
using UnityEngine;

/// <summary>
/// 区域信息预制体视图：由 <see cref="LevelAreaSelectPanel"/> 调用 Bind 刷新文本。
/// </summary>
public sealed class LevelAreaInfoCardView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 模式标题文本（常驻关卡/活动关卡）。
    /// </summary>
    [SerializeField] private TMP_Text modeTitleText;

    /// <summary>
    /// 区域标题文本。
    /// </summary>
    [SerializeField] private TMP_Text areaTitleText;

    /// <summary>
    /// 区域描述文本。
    /// </summary>
    [SerializeField] private TMP_Text areaDescriptionText;

    /// <summary>
    /// 区域可挑战状态文本。
    /// </summary>
    [SerializeField] private TMP_Text areaStateText;

    #endregion

    #region Public API

    /// <summary>
    /// 绑定当前模式与区域信息到 UI。
    /// </summary>
    /// <param name="mode">当前选择模式。</param>
    /// <param name="area">当前区域配置（ScriptableObject）。</param>
    /// <param name="index">当前区域下标。</param>
    /// <param name="total">当前模式区域总数。</param>
    public void Bind(LevelMode mode, LevelAreaConfigSO area, int index, int total)
    {
        if (modeTitleText != null)
        {
            modeTitleText.text = mode == LevelMode.Activity ? "活动关卡" : "常驻关卡";
        }

        if (areaTitleText != null)
        {
            var fallback = $"区域 {index + 1}/{Mathf.Max(total, 1)}";
            areaTitleText.text = area == null || string.IsNullOrWhiteSpace(area.DisplayName)
                ? fallback
                : area.DisplayName;
        }

        if (areaDescriptionText != null)
        {
            areaDescriptionText.text = area == null || string.IsNullOrWhiteSpace(area.Description)
                ? "暂无区域描述。"
                : area.Description;
        }

        if (areaStateText != null)
        {
            areaStateText.text = area != null && area.IsUnlocked ? "可挑战" : "未解锁";
        }
    }

    /// <summary>
    /// 当区域列表为空时显示占位文案。
    /// </summary>
    /// <param name="mode">当前模式。</param>
    public void BindEmpty(LevelMode mode)
    {
        if (modeTitleText != null)
        {
            modeTitleText.text = mode == LevelMode.Activity ? "活动关卡" : "常驻关卡";
        }

        if (areaTitleText != null)
        {
            areaTitleText.text = "暂无可用区域";
        }

        if (areaDescriptionText != null)
        {
            areaDescriptionText.text = "请先在 LevelAreaDatabase 配置区域数据。";
        }

        if (areaStateText != null)
        {
            areaStateText.text = "不可挑战";
        }
    }

    #endregion
}
