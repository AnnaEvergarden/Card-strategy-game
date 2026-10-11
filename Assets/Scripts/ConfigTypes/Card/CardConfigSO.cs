using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Config/Card/Card Config", fileName = "CardConfig_")]
public class CardConfigSO : ScriptableObject, IConfigData
{
    [Header("基础信息")]
    [SerializeField] private string _cardId;
    [SerializeField] private string _displayName;
    [SerializeField] private string _englishName;
    [SerializeField] private ShipFaction _faction;
    [SerializeField] private CardRarity _rarity;
    [SerializeField] private int _baseHP;
    [SerializeField] private int _baseAttack;
    [SerializeField] private int _baseDefense;

    [Header("技能")]
    [SerializeField] private List<SkillEntrySO> _skills = new List<SkillEntrySO>();

    [Header("资源")]
    [SerializeField] private int _iconResourceId;
    [SerializeField] private int _portraitResourceId;

    public string Id => CardId;
    public string CardId => _cardId;
    public string DisplayName => _displayName;
    public string EnglishName => _englishName;
    public ShipFaction Faction => _faction;
    public CardRarity Rarity => _rarity;
    public int BaseHP => _baseHP;
    public int BaseAttack => _baseAttack;
    public int BaseDefense => _baseDefense;
    public IReadOnlyList<SkillEntrySO> Skills => _skills;
    public int IconResourceId => _iconResourceId;
    public int PortraitResourceId => _portraitResourceId;

    [System.Serializable]
    public class SkillEntrySO
    {
        /// <summary>
        /// 关联的技能配置（拖入后自动补充 SkillId）。
        /// </summary>
        public SkillConfigSO Config;
        public string SkillId;
    }

    public CardConfig ToDomain()
    {
        var skillIdList = new List<string>();
        if (_skills != null)
            for (int i = 0; i < _skills.Count; i++)
                if (_skills[i] != null && !string.IsNullOrWhiteSpace(_skills[i].SkillId))
                    skillIdList.Add(_skills[i].SkillId.Trim());

        return new CardConfig
        {
            CardId = _cardId,
            DisplayName = _displayName,
            EnglishName = _englishName,
            Faction = _faction,
            Rarity = _rarity,
            BaseHP = _baseHP,
            BaseAttack = _baseAttack,
            BaseDefense = _baseDefense,
            SkillIds = skillIdList,
            IconResourceId = _iconResourceId,
            PortraitResourceId = _portraitResourceId
        };
    }
}



