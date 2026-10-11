using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Level/Stage Config", fileName = "LevelStage_")]
public class LevelStageConfigSO : ScriptableObject, IConfigData
{
    [SerializeField] private string _stageId;
    [SerializeField] private string _areaId;
    [SerializeField] [HideInInspector] private LevelAreaConfigSO _areaRef;
    [SerializeField] private string _displayName;
    [SerializeField] private int _stageIndexInArea;
    [SerializeField] private bool _isUnlocked = true;
    [Tooltip("敌方上阵卡牌（按顺序拖动 CardConfigSO；前 2 名首发、其余替补，最多 6 张）")]
    [SerializeField] private List<CardConfigSO> _enemyCards = new List<CardConfigSO>();
    [Tooltip("旧版敌方卡牌 ID 列表（仅在未拖入 CardConfigSO 时作为回退，请勿手动编辑）")]
    [SerializeField] private List<string> _enemyCardIds = new List<string>();

    public string Id => StageId;
    public string StageId => _stageId;
    public string AreaId => _areaId;
    public string DisplayName => _displayName;
    public int StageIndexInArea => _stageIndexInArea;
    public bool IsUnlocked => _isUnlocked;

    /// <summary>敌方上阵卡牌配置（编辑器拖拽赋值）。</summary>
    public IReadOnlyList<CardConfigSO> EnemyCards => _enemyCards;

    /// <summary>
    /// 敌方卡牌 ID 列表（导出 JSON 与运行时使用）：
    /// 优先取拖入的 CardConfigSO；未拖入时回退旧版 ID 列表，避免历史配置与已导出的 JSON 数据丢失。
    /// </summary>
    public IReadOnlyList<string> EnemyCardIds
    {
        get
        {
            var ids = new List<string>(_enemyCards.Count);
            for (var i = 0; i < _enemyCards.Count; i++)
            {
                var card = _enemyCards[i];
                if (card != null && !string.IsNullOrEmpty(card.CardId))
                    ids.Add(card.CardId);
            }

            if (ids.Count == 0 && _enemyCardIds != null)
                ids.AddRange(_enemyCardIds);

            return ids;
        }
    }

    public LevelStageConfig ToDomain() => new LevelStageConfig
    {
        StageId = _stageId, AreaId = _areaId, DisplayName = _displayName,
        StageIndexInArea = _stageIndexInArea, IsUnlocked = _isUnlocked,
        EnemyCardIds = new List<string>(EnemyCardIds)
    };
}


