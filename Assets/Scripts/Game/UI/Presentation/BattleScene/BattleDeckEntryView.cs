using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗准备：单套卡组条目视图（卡组名 + 至多 6 个舰娘头像占位）。
/// </summary>
public sealed class BattleDeckEntryView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 卡组名称文本。
    /// </summary>
    [SerializeField] private TMP_Text deckNameText;

    /// <summary>
    /// 至多 6 个头像位（下标 0～5 与编队槽位一一对应；空槽对应 Image 隐藏）。
    /// </summary>
    [SerializeField] private Image[] shipIcons;

    /// <summary>
    /// 选择本套卡组按钮；为空时在 <see cref="Bind"/> 内尝试 <see cref="GetComponent{Button}"/>。
    /// </summary>
    [SerializeField] private Button selectButton;

    /// <summary>
    /// 当前条目对应卡组下标。
    /// </summary>
    private int _groupIndex;

    /// <summary>
    /// 点击回调（传入卡组下标）。
    /// </summary>
    private Action<int> _onSelect;

    #endregion

    #region Public API

    /// <summary>
    /// 绑定一套编队数据与点击回调。
    /// </summary>
    /// <param name="group">编队数据。</param>
    /// <param name="groupIndex">在 <see cref="FleetStore.FleetData.groups"/> 中的下标。</param>
    /// <param name="configMap">cardId 到配置的映射。</param>
    /// <param name="onSelect">点击时回调卡组下标。</param>
    public void Bind(
        FleetStore.FleetGroupData group,
        int groupIndex,
        Dictionary<string, CardConfigSO> configMap,
        Action<int> onSelect)
    {
        _groupIndex = groupIndex;
        _onSelect = onSelect;

        if (deckNameText != null)
        {
            deckNameText.text = group != null && !string.IsNullOrWhiteSpace(group.groupName)
                ? group.groupName.Trim()
                : $"卡组 {groupIndex + 1}";
        }

        ClearIcons();
        if (shipIcons == null || shipIcons.Length == 0)
        {
            WireButton();
            return;
        }

        group.cardIds ??= new List<string>();
        // 与 FleetPanel 一致：按槽位 0～5 对齐头像位，空槽清空对应 Image，避免跳过空位导致头像错位。
        for (var slot = 0; slot < FleetStore.MaxCardsPerFleet && slot < shipIcons.Length; slot++)
        {
            var img = shipIcons[slot];
            if (img == null)
            {
                continue;
            }

            var rawId = slot < group.cardIds.Count ? group.cardIds[slot] : null;
            var id = string.IsNullOrWhiteSpace(rawId) ? string.Empty : rawId.Trim();
            if (string.IsNullOrEmpty(id))
            {
                img.sprite = null;
                img.enabled = false;
                continue;
            }

            CardConfigSO cfg = null;
            if (configMap != null)
            {
                configMap.TryGetValue(id, out cfg);
            }

            var englishName = cfg != null ? cfg.EnglishName : null;
            GameResourceLoader.ApplyShipgirlIconToImage(img, englishName, logOnMissing: false);
        }

        WireButton();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 清空头像位。
    /// </summary>
    private void ClearIcons()
    {
        if (shipIcons == null)
        {
            return;
        }

        for (var i = 0; i < shipIcons.Length; i++)
        {
            var img = shipIcons[i];
            if (img == null)
            {
                continue;
            }

            img.sprite = null;
            img.enabled = false;
        }
    }

    /// <summary>
    /// 绑定选择按钮监听。
    /// </summary>
    private void WireButton()
    {
        var btn = selectButton != null ? selectButton : GetComponent<Button>();
        if (btn == null)
        {
            btn = gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
        }

        selectButton = btn;
        var graphic = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);
        btn.targetGraphic = graphic;
        btn.interactable = graphic != null;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => _onSelect?.Invoke(_groupIndex));
    }

    #endregion
}
