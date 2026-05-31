using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 领卡展示用单张卡牌条目：绑定头像与名称，并支持「当前张」高亮（缩放/透明度）。
/// </summary>
public sealed class CardRevealEntryView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 卡图。
    /// </summary>
    [SerializeField] private Image icon;

    /// <summary>
    /// 卡牌名称（可选）。
    /// </summary>
    [SerializeField] private TMP_Text nameText;

    /// <summary>
    /// 当前张缩放倍数。
    /// </summary>
    [SerializeField] private float highlightedScale = 1.08f;

    /// <summary>
    /// 非当前张缩放。
    /// </summary>
    [SerializeField] private float normalScale = 1f;

    #endregion

    #region Public API

    /// <summary>
    /// 绑定展示数据。
    /// </summary>
    /// <param name="cardId">卡牌 id。</param>
    /// <param name="config">卡牌配置（可为空）。</param>
    public void Bind(string cardId, CardConfigSO config)
    {
        var display = config != null && !string.IsNullOrWhiteSpace(config.DisplayName)
            ? config.DisplayName.Trim()
            : (cardId ?? string.Empty).Trim();

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(display) ? display : cardId;
        }

        if (icon != null)
        {
            icon.preserveAspect = true;
            var sprite = LoadIcon(config, cardId);
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
    }

    /// <summary>
    /// 设为当前展示张或非当前张（视觉层级由父级子节点顺序控制）。
    /// </summary>
    /// <param name="isCurrent">是否为当前序号对应卡牌。</param>
    public void SetCurrentVisual(bool isCurrent)
    {
        var s = isCurrent ? highlightedScale : normalScale;
        transform.localScale = new Vector3(s, s, 1f);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 按统一资源路径规则加载卡牌头像。
    /// </summary>
    private static Sprite LoadIcon(CardConfigSO config, string cardId)
    {
        if (config == null)
        {
            return null;
        }

        var englishName = config.EnglishName;
        if (string.IsNullOrWhiteSpace(englishName))
        {
            return null;
        }

        return GameResourceLoader.LoadShipgirlIcon(englishName);
    }

    #endregion
}
