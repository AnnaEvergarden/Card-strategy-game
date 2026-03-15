using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card Strategy/Card Data")]
public class CardData : ScriptableObject
{
    [Header("基础信息")]
    public Image icon;
    public string cardId;
    public string cardName;
    [TextArea(2,4)]
    public string description;

    [Header("基础属性 (PVP 与 PVE 共用)")]
    public int baseAttack = 1;
    public int baseHealth = 1;
    public int baseCost = 1;

    /// <summary>
    /// 仅基础属性，不含强化，根据 isPVP 决定是否计入卡牌强化属性
    /// </summary>
    public int GetBaseAttack() => baseAttack;
    public int GetBaseHealth() => baseHealth;
}
