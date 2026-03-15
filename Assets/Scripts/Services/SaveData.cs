using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string playerName = "Player";
    public int gold;
    /// <summary> 背包: 所有拥有的卡牌实例 </summary>
    public List<CardInstance> inventory = new List<CardInstance>();

    //多套卡组
    public List<DeckSlot> decks = new List<DeckSlot>();
    /// <summary>
    /// 当前选中卡组在decks里的下标.
    /// </summary>
    public int currentDeckIndex = 0;

    public SaveData()
    {
        if(decks == null || decks.Count == 0)
        {
            decks = new List<DeckSlot> { new DeckSlot("默认卡组")};
        }
    }
}

[Serializable]
public class DeckSlot
{
    public string deckName = "默认卡组";
    /// <summary>
    /// 一套卡组中的卡牌序号集合
    /// </summary>
    public List<string> instanceIds = new List<string>();

    public DeckSlot() { }

    public DeckSlot(string _name)
    {
        deckName = _name;
        instanceIds = new List<string>();
    }
}
