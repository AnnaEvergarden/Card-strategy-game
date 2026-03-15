using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 挂在单张卡牌上，负责UI交互
/// </summary>
public class CardUI : MonoBehaviour,IPointerClickHandler
{
    [Header("显示信息")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text AttackText;
    [SerializeField] private TMP_Text HealthText;

    [Header("操作面板")]
    [SerializeField] private string cardActionPanel;

    private CardUnit cardUnit;

    /// <summary>
    /// 点击卡牌弹出操作面板
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 实现卡牌点击功能，（弹出操作面板）
        if(cardUnit == null)
        {
            Debug.LogWarning($"当前槽位没有卡牌！");
            return;
        }

        var panel = UIPanelManager.Instance?.GetPanel(cardActionPanel) as CardActionPanel; 
        if(panel != null)
        {
            UIPanelManager.Instance.PushPanel(cardActionPanel);
        }
        else
        {
            Debug.LogWarning($"未找到 {cardActionPanel} 面板！");
        }
    }

    /// <summary>
    /// 绑定CardUnit和CardUI
    /// </summary>
    public void Bind(CardUnit _unit)
    {
        cardUnit = _unit;
        RefreshDisplay();
    }

    // 刷新卡牌牌面信息
    public void RefreshDisplay()
    {
        if(cardUnit == null || cardUnit.data == null)
        {
            icon.sprite = null;
            nameText.text = "";
            AttackText.text = "";
            HealthText.text = "";
            return;
        }

        var data = cardUnit.data;

        icon = data.icon;
        nameText.text = data.cardName;
        AttackText.text = cardUnit.currentAttack.ToString();
        HealthText.text = cardUnit.currentHP.ToString();
    }

    public CardUnit GetBoundUnit() => cardUnit;
}
