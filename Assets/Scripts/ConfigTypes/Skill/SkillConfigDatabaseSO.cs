using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Skill/Skill Config Database", fileName = "SkillConfigDatabase_")]
public class SkillConfigDatabaseSO : ScriptableObject
{
    public List<SkillConfigSO> Skills = new List<SkillConfigSO>();
}

