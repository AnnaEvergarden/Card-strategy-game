using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Panel/Panel Config Database", fileName = "PanelConfigDatabase_")]
public class PanelConfigDatabaseSO : ScriptableObject
{
    public List<PanelConfigSO> Items = new List<PanelConfigSO>();
}


