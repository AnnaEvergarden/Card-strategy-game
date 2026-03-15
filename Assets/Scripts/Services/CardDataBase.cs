using System.Collections.Generic;
using UnityEngine;

public class CardDataBase : Singleton<CardDataBase>
{
    [Tooltip("卡牌数据列表: cardId -> CardData, 也可从 Resources 加载配置")]
    [SerializeField] private List<CardData> cardDataList = new List<CardData>();
    [SerializeField] public CardData defaultCardData;

    private Dictionary<string, CardData> _cardDataById;

    protected override void Awake()
    {
        base.Awake();

        _cardDataById = new Dictionary<string, CardData>();
        foreach (var _data in cardDataList)
        {
            if (_data != null && !string.IsNullOrEmpty(_data.cardId))
                _cardDataById[_data.cardId] = _data;
        }
    }

    public CardData GetCardData(string _cardId)
    {
        if (_cardDataById != null && _cardDataById.TryGetValue(_cardId, out var _data))
        {
            return _data;
        }
        Debug.LogError($"[BattleManager] 未从图集中找到该卡牌信息 => {_cardId}");
        return null;
    }


    /// <summary>
    /// 注册卡牌数据；运行时也可从 Resources 加载配置。
    /// </summary>
    public void RegisterCardData(CardData _data)
    {
        if (_data == null || string.IsNullOrEmpty(_data.cardId)) return;
        if (_cardDataById == null) _cardDataById = new Dictionary<string, CardData>();
        _cardDataById[_data.cardId] = _data;
    }
}
