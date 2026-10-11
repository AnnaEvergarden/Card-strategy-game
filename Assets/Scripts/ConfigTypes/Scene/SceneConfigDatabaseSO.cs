using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Scene/Scene Config Database", fileName = "SceneConfigDatabase_")]
public class SceneConfigDatabaseSO : ScriptableObject
{
    public List<SceneConfigSO> Items = new List<SceneConfigSO>();
}

