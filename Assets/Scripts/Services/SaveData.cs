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
    /// <summary> 当前卡组,存instanceId的列表 </summary>
    public List<string> deckInstanceIds = new List<string>();

    public SaveData() { }
}
