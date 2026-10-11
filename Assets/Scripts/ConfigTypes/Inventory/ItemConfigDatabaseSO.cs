using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Inventory/Item Database", fileName = "ItemConfigDatabase_")]
public class ItemConfigDatabaseSO : ScriptableObject
{
    public List<ItemConfigSO> Items = new List<ItemConfigSO>();
}

