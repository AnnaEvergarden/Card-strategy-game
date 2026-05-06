using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编队卡牌槽位视图：绑定卡牌名称、头像与编号。
/// </summary>
public sealed class FleetCardSlotView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 卡图。
    /// </summary>
    [SerializeField] private Image icon;

    /// <summary>
    /// 卡牌名称文本。
    /// </summary>
    [SerializeField] private TMP_Text nameText;

    /// <summary>
    /// 槽位序号文本（可选）。
    /// </summary>
    [SerializeField] private TMP_Text slotIndexText;

    #endregion

    #region Public API

    /// <summary>
    /// 绑定单个卡槽显示数据。
    /// </summary>
    /// <param name="slotIndex">槽位下标（从 0 开始）。</param>
    /// <param name="cardId">卡牌配置 id。</param>
    /// <param name="config">卡牌配置（可为空）。</param>
    public void Bind(int slotIndex, string cardId, CardConfigSO config)
    {
        var display = config != null && !string.IsNullOrWhiteSpace(config.DisplayName)
            ? config.DisplayName
            : (string.IsNullOrWhiteSpace(cardId) ? $"卡牌 {slotIndex + 1}" : cardId);

        if (nameText != null)
        {
            nameText.text = display;
        }

        if (slotIndexText != null)
        {
            slotIndexText.text = $"{slotIndex + 1:D2}";
        }

        if (icon != null)
        {
            icon.preserveAspect = true;
            icon.sprite = LoadIcon(config, cardId);
            icon.enabled = icon.sprite != null;
        }
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
