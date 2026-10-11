using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Build/Build Pool Config", fileName = "BuildPool_")]
public class BuildPoolConfigSO : ScriptableObject, IConfigData
{
    [SerializeField] private string _poolId;
    [SerializeField] private string _displayName;
    [SerializeField] private ShipFaction _poolFaction;
    [SerializeField] private bool _isOpen = true;
    [SerializeField] private int _bannerResourceId;
    [SerializeField] private int _buttonIconResourceId;
    [SerializeField] private BuildCostData _buildOneCost = new BuildCostData();
    [SerializeField] private BuildCostData _buildTenCost = new BuildCostData();
    [SerializeField] private List<BuildDropEntrySO> _dropEntries = new List<BuildDropEntrySO>();

    public string Id => PoolId;
    public string PoolId => _poolId;
    public string DisplayName => _displayName;
    public ShipFaction PoolFaction => _poolFaction;
    public bool IsOpen => _isOpen;
    public int BannerResourceId => _bannerResourceId;
    public int ButtonIconResourceId => _buttonIconResourceId;
    public BuildCostData BuildOneCost => _buildOneCost;
    public BuildCostData BuildTenCost => _buildTenCost;
    public IReadOnlyList<BuildDropEntrySO> DropEntries => _dropEntries;

    [System.Serializable]
    public class BuildDropEntrySO
    {
        /// <summary>
        /// 关联的卡牌配置（拖入后自动补充 CardId）。
        /// </summary>
        public CardConfigSO CardConfig;

        public string CardId;
        public int Weight = 1;
    }

    public BuildPoolConfig ToDomain()
    {
        var config = new BuildPoolConfig
        {
            PoolId = _poolId,
            DisplayName = _displayName,
            PoolFaction = _poolFaction,
            IsOpen = _isOpen,
            BannerResourceId = _bannerResourceId,
            ButtonIconResourceId = _buttonIconResourceId,
            BuildOneCost = _buildOneCost ?? new BuildCostData(),
            BuildTenCost = _buildTenCost ?? new BuildCostData()
        };
        if (_dropEntries != null)
        {
            config.DropEntries = new List<BuildDropEntryData>();
            for (int i = 0; i < _dropEntries.Count; i++)
            {
                var entrySO = _dropEntries[i];
                if (entrySO == null) continue;

                var cardId = !string.IsNullOrWhiteSpace(entrySO.CardId)
                    ? entrySO.CardId
                    : (entrySO.CardConfig != null ? entrySO.CardConfig.CardId : null);

                config.DropEntries.Add(new BuildDropEntryData
                {
                    CardId = cardId,
                    Weight = entrySO.Weight
                });
            }
        }
        return config;
    }
}