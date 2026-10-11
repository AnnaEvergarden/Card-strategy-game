using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Card/Card Config Database", fileName = "CardConfigDatabase_")]
public class CardConfigDatabaseSO : ScriptableObject
{
    public List<CardConfigSO> Cards = new List<CardConfigSO>();
}

