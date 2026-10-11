using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Build/Build Pool Database", fileName = "BuildPoolDatabase_")]
public class BuildPoolDatabaseSO : ScriptableObject
{
    public List<BuildPoolConfigSO> Pools = new List<BuildPoolConfigSO>();
}

