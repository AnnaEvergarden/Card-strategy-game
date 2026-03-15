using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CardStatsService : Singleton<CardStatsService>
{
    protected override bool PersistAcrossScenes => false;

    /// <summary>
    /// 战斗用于战斗的攻击力。
    /// </summary>
    /// <param name="_instance"> 玩家持有的卡牌实例(可含强化) </param>
    /// <param name="_data"> 卡牌配置(基础属性) </param>
    /// <param name="_useEnhancement"> true = PVE 计入强化, false = PVP 不计入强化 </param>
    public static int GetAttack(CardInstance _instance, CardData _data, bool _useEnhancement)
    {
        if (_data == null)
        {
            Debug.LogWarning($"[CardStatsService] 没有找到该卡牌的信息!");
            return 0;
        }

        int attack = _data.baseAttack;
        if (_useEnhancement && _instance != null)
            attack += _instance.enhanceAttackBonus * _instance.enhanceLevel;
        return Mathf.Max(0, attack);
    }

    /// <summary>
    /// 获取用于战斗的生命值</summary>
    public static int GetHealth(CardInstance _instance, CardData _data, bool _useEnhancement)
    {
        if(_data == null)
        {
            Debug.LogWarning($"[CardStatsService] 没有找到该卡牌的信息!");
            return 0;
        }

        int health = _data.baseHealth;
        if(_useEnhancement && _instance != null)
            health += _instance.enhanceHealthBonus * _instance.enhanceLevel;
        return Mathf.Max(0, health);
    }

    /// <summary>
    /// 收取费用(可被其他卡牌影响)
    /// </summary>
    public static int GetCost(CardData _data)
    {
        return _data == null ? 0 : _data.baseCost;
    }
}
