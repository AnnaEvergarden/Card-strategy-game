using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 
/// 单局战斗的上下文，进入战斗前判断 isPVP，战斗时只读
/// </summary>
public class BattleContext
{ 
    /// <summary>
    /// true = 匹配/排位/工会战等，忽略强化, false = PVE，计入强化 </summary>
    public bool isPVP { get; private set; }

    ///<summary> 玩家方卡牌组 (CardInstance 列表) </summary>
    public List<CardInstance> Player1Deck { get; private set; }

    ///<summary> 敌方卡牌组 (CardInstance 列表) </summary>
    public List<CardInstance> Player2Deck { get; private set; }

    public BattleContext()
    {
        Player1Deck = new List<CardInstance>();
        Player2Deck = new List<CardInstance>();
    }

    public BattleContext(bool _isPVP, List<CardInstance> _playerDeck, List<CardInstance> _enemyDeck)
    {
        this.isPVP = _isPVP;
        Player1Deck = _playerDeck;
        Player2Deck = _enemyDeck;
    }
}
