using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡池选择按钮视图：仅显示卡池名并响应点击。
/// </summary>
public sealed class BuildPoolButtonView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 按钮组件。
    /// </summary>
    [SerializeField] private Button button;

    /// <summary>
    /// 卡池名称文本。
    /// </summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>
    /// 绑定按钮与文案。
    /// </summary>
    public void Bind(string title, UnityEngine.Events.UnityAction onClick)
    {
        if (titleText != null) titleText.text = title;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
        }
    }

    #endregion
}
