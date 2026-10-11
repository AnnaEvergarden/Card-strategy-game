using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Level/Area Config", fileName = "LevelArea_")]
public class LevelAreaConfigSO : ScriptableObject, IConfigData
{
    [SerializeField] private string _areaId;
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private ShipFaction _faction;
    [SerializeField] private LevelAreaCategory _category;
    [SerializeField] private bool _isUnlocked = true;
    [SerializeField] private List<LevelStageConfigSO> _stages = new List<LevelStageConfigSO>();

    public string Id => AreaId;
    public string AreaId => _areaId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public ShipFaction Faction => _faction;
    public LevelAreaCategory Category => _category;
    public bool IsUnlocked => _isUnlocked;
    public IReadOnlyList<LevelStageConfigSO> Stages => _stages;

    public LevelAreaConfig ToDomain()
    {
        var config = new LevelAreaConfig
        {
            AreaId = _areaId,
            DisplayName = _displayName,
            Description = _description,
            Faction = _faction,
            Category = _category,
            IsUnlocked = _isUnlocked
        };
        if (_stages != null)
        {
            config.Stages = new List<LevelStageConfig>();
            for (int i = 0; i < _stages.Count; i++)
                if (_stages[i] != null) config.Stages.Add(_stages[i].ToDomain());
        }
        return config;
    }
}