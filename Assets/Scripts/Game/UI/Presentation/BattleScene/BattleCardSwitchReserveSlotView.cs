using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗换牌面板：替补（未上场）舰娘槽位，展示 HP 与选中态。
/// </summary>
public sealed class BattleCardSwitchReserveSlotView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 当前绑定的 cardId。
    /// </summary>
    public string BoundCardId { get; private set; } = string.Empty;

    /// <summary>
    /// 当前绑定的 UnitId。
    /// </summary>
    public string BoundUnitId { get; private set; } = string.Empty;

    /// <summary>
    /// 舰娘头像。
    /// </summary>
    [SerializeField] private Image iconImage;

    /// <summary>
    /// 舰娘名称。
    /// </summary>
    [SerializeField] private TMP_Text nameText;

    /// <summary>
    /// 生命值文本。
    /// </summary>
    [SerializeField] private TMP_Text hpText;

    /// <summary>
    /// 选中高亮根节点（可选）。
    /// </summary>
    [SerializeField] private GameObject selectedHighlight;

    /// <summary>
    /// HP 为 0 时显示的不可用遮罩（可选）。
    /// </summary>
    [SerializeField] private GameObject disabledOverlay;

    #endregion

    #region Public API

    /// <summary>
    /// 绑定替补舰娘展示（图标、名称与运行时生命）。
    /// </summary>
    /// <param name="cardId">卡牌 id。</param>
    /// <param name="config">配置（可为 null）。</param>
    /// <param name="unit">本局战斗单位（可为 null，回退配置）。</param>
    public void Bind(string cardId, CardConfigSO config, BattleUnit unit)
    {
        BoundCardId = string.IsNullOrWhiteSpace(cardId) ? string.Empty : cardId.Trim();
        BoundUnitId = unit?.UnitId ?? string.Empty;

        var display = config != null && !string.IsNullOrWhiteSpace(config.DisplayName)
            ? config.DisplayName
            : BoundCardId;

        if (nameText != null)
        {
            nameText.text = display;
        }

        var hp = unit?.Hp ?? config?.HP ?? 0;

        if (hpText != null)
        {
            hpText.text = hp.ToString();
        }

        if (iconImage != null)
        {
            var englishName = config != null ? config.EnglishName : null;
            GameResourceLoader.ApplyShipgirlIconToImage(iconImage, englishName, logOnMissing: false);
        }

        var deployable = hp > 0;
        if (disabledOverlay != null)
        {
            disabledOverlay.SetActive(!deployable);
        }
    }

    /// <summary>
    /// 更新选中高亮。
    /// </summary>
    /// <param name="selected">是否选中。</param>
    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(selected);
        }
    }

    #endregion
}
