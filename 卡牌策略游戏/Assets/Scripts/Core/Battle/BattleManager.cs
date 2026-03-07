using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗流程驱动，极简版,只根据双方卡组通过CardService进行一次总战力比拼输赢
/// 所有数值只通过CardService获取，以遵循PVP忽略强化
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Tooltip("卡牌配置表: cardId -> CardData, 可从Resources或配置加载")]
    [SerializeField] private List<CardData> cardDataList = new List<CardData>();

    private Dictionary<string, CardData> _cardDataById;

    private void Awake()
    {
        _cardDataById = new Dictionary<string, CardData>();
        foreach (var _data in cardDataList)
        {
            if(_data != null && !string.IsNullOrEmpty(_data.cardId))
                _cardDataById[_data.cardId] = _data;
        }
    }

    /// <summary>
    /// 开始一局战斗，根据isPVP决定是否考虑强化
    /// </summary>
    public void StartBattle(BattleContext _context)
    {
        if (_context == null)
        {
            Debug.LogWarning("战斗上下文[BattleContext]为空"!);
            return;
        }

        bool useEnhancement = !_context.isPVP;
        int playerPower = GetDeckTotalPower(_context.PlayerDeck, useEnhancement);
        int enemyPower = GetDeckTotalPower(_context.EnemyDeck, useEnhancement);

        Debug.Log($"[战斗信息] : 是否为PVP [{_context.isPVP}], 是否启用强化 [{!useEnhancement}],玩家方总战力 [{playerPower}] ,敌方战力 [{enemyPower}]");

        if(playerPower > enemyPower)
        {
            Debug.Log("你获得了胜利!");
        }else if (playerPower < enemyPower)
        {
            Debug.Log("敌方获得了胜利!");
        }
        else
        {
            Debug.Log("平局！");
        }

    }
    public int GetDeckTotalPower(List<CardInstance> _deck, bool _useEnhancement)
    {;
        int total = 0;
        if(_deck == null)
        {
            Debug.LogWarning($"卡组为空！");
            return total;
        }

        foreach (var _instance in _deck)
        {
            var data = GetCardData(_instance.cardId);
            if (data == null)
            {
                Debug.LogWarning($"未找到该卡牌的信息 => {data.cardId} {data.cardName}");
                continue;
            }
            Debug.Log($"[CardStatsService] 该卡牌信息: " +
                    $"\n 名称: {data.cardName}" +
                    $"\n 序号: {data.cardId}" +
                    $"\n 基础攻击力: {data.baseAttack}" +
                    $"\n 基础生命值: {data.baseHealth}" +
                    $"\n 基础费用: {data.baseCost}");
            int attack = CardStatsService.GetAttack(_instance, data, _useEnhancement);
            int health = CardStatsService.GetHealth(_instance, data, _useEnhancement);
            total += attack + health;
        }
        return total;
    }

    private CardData GetCardData(string _cardId)
    {
        if(_cardDataById != null && _cardDataById.TryGetValue(_cardId, out var _data))
        {
            return _data;
        }
        Debug.LogError($"未找到该卡牌 => {_cardId}");
        return null;
    }

    /// <summary>
    /// 运行时加载卡牌配置（从Resources或其他地方）
    /// </summary>
    public void RegisterCardData(CardData _data)
    {
        if (_data == null || string.IsNullOrEmpty(_data.cardId)) return;
        if(_cardDataById == null) _cardDataById = new Dictionary<string, CardData>();
        _cardDataById[_data.cardId] = _data;
    }
}
