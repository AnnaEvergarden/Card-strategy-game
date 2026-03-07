using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CardInstance 
{
    /// <summary> /// 实例唯一 ID ，用于存档和卡组 /// </summary>
    public string instanceId;
    /// <summary> /// 对应 CardData 的 cardId  /// </summary>
    public string cardId;
    /// <summary> /// 卡牌当前的强化等级,pvp 忽略 /// </summary>
    public int enhanceLevel;
    /// <summary> /// 强化带来的攻击加成,pvp 忽略 /// </summary>
    public int enhanceAttackBonus;
    /// <summary> /// 强化带来的生命加成,pvp 忽略 /// </summary>
    public int enhanceHealthBonus;

    public  CardInstance(string _cardId)
    {
        instanceId = Guid.NewGuid().ToString();
        this.cardId = _cardId;
        enhanceLevel = 0;
        enhanceAttackBonus = 0;
        enhanceHealthBonus = 0;
    }

    public static CardInstance CreateNew(string _cardId)
    {
        return new CardInstance(_cardId);
    }
}
