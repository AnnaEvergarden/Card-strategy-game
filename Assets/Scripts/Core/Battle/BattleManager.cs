using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleSide
{
    Player1,
    Player2,
}


/// <summary>
/// 战斗管理器。持有战斗卡牌数据列表（可配置），负责双方对战；通过 CardStatsService 计算战力，判定胜负。
/// 数值只通过 CardStatsService 获取，遵循 PVP 平衡规则。
/// </summary>
public class BattleManager : Singleton<BattleManager>
{
    // 当前回合状态，默认 PLayer1 开始
    public BattleSide currentSide = BattleSide.Player1;
    public int turnCount = 1;

    // 对战双方卡组
    public List<CardUnit> player1Units;
    public List<CardUnit> player2Units;

    // 双方上场的卡牌（数量上限为2）
    private CardUnit[] player1Active = new CardUnit[2];
    private CardUnit[] player2Active = new CardUnit[2];

    private bool player1HasActedThisTurn;
    private bool player2HasActedThisTurn;

    /// <summary>
    /// 测试版，根据 BattleContext 开始一场战斗；isPVP 表示是否为平衡型对战。
    /// </summary>
    /*public void StartBattle(BattleContext _context)
    {
        if (_context == null)
        {
            Debug.LogWarning("[BattleManager] 战斗上下文 [BattleContext] 为空!");
            return;
        }

        bool useEnhancement = !_context.isPVP;
        int playerPower = GetDeckTotalPower(_context.Player1Deck, useEnhancement);
        int enemyPower = GetDeckTotalPower(_context.Player2Deck, useEnhancement);

        Debug.Log($"[BattleManager] 是否 PVP [{_context.isPVP}], 使用强化 [{!useEnhancement}], 我方战力 [{playerPower}], 敌方战力 [{enemyPower}]");

        if(playerPower > enemyPower)
        {
            Debug.Log("[BattleManager] 我方胜利!");
        }else if (playerPower < enemyPower)
        {
            Debug.Log("[BattleManager] 敌方胜利!");
        }
        else
        {
            Debug.Log("[BattleManager] 平局!");
        }

    }*/

    /// <summary>
    /// 测试版，简化算总战力
    /// </summary>
    public int GetDeckTotalPower(List<CardInstance> _deck, bool _useEnhancement)
    {
        int total = 0;
        if(_deck == null)
        {
            Debug.LogWarning("[BattleManager] 卡组为空!");
            return total;
        }

        foreach (var _instance in _deck)
        {
            var data = CardDataBase.Instance.GetCardData(_instance.cardId);
            if (data == null)
            {
                Debug.LogWarning($"[BattleManager] 没有找到该卡牌数据 => {_instance.cardId}");
                continue;
            }
            Debug.Log($"[BattleManager] 卡牌战斗数据: " +
                    $"\n 卡名: {data.cardName}" +
                    $"\n 卡ID: {data.cardId}" +
                    $"\n 基础攻击: {data.baseAttack}" +
                    $"\n 生命值: {data.baseHealth}" +
                    $"\n 费用: {data.baseCost}");
            int attack = CardStatsService.GetAttack(_instance, data, _useEnhancement);
            int health = CardStatsService.GetHealth(_instance, data, _useEnhancement);
            total += attack + health;
        }
        return total;
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartBattle(BattleContext _ctx)
    {
        if(_ctx == null)
        {
            Debug.LogWarning("Context 为空！");
            return;
        }

        if(_ctx.Player1Deck.Count == 0 || _ctx.Player2Deck.Count == 0)
        {
            Debug.LogWarning("_ctx中的玩家卡组为空!");
        }

        // 根据 Context里的卡组决定对战卡组
        player1Units = CreateUnitsFromDeck(_ctx.Player1Deck, _ctx);
        player2Units = CreateUnitsFromDeck(_ctx.Player2Deck, _ctx);

        // 选出双方当前上场的卡牌
        SelectActiveUnitsForSide(BattleSide.Player1);
        SelectActiveUnitsForSide(BattleSide.Player2);

        // 生成双方卡牌预制体
        BattleFieldView.Instance.SetUpCards(player1Units, player2Units);

        // 初始化回合状态
        turnCount = 1;
        currentSide = BattleSide.Player1;
        player1HasActedThisTurn = false;
        player2HasActedThisTurn = false;
        ResetUnitsTurnFlags();

        Debug.Log($"战斗开始，当前为第 {turnCount} 回合，当前行动方 {currentSide.ToString()} 。");
    }

    private List<CardUnit> CreateUnitsFromDeck(List<CardInstance> _deck, BattleContext _ctx)
    {
        var list = new List<CardUnit>();

        if(_deck == null || _deck.Count == 0)
        {
            Debug.LogWarning("当前卡组为空！");
            return list;
        }

        foreach (var _instance in _deck)
        {
            if (_instance == null) continue;
            var unit = new CardUnit(_instance, _ctx);
            if(unit.data != null)
            {
                list.Add(unit);
            }
        }

        return list;
    }

    /// <summary>
    /// 当前为简单版本-----------------------------待修改
    /// </summary>
    private void SelectActiveUnitsForSide(BattleSide _side)
    {
        var source = _side == BattleSide.Player1 ? player1Units : player2Units;
        var target = _side == BattleSide.Player1 ? player1Active : player2Active;

        // 简单版本，取前两张卡牌上场
        int filled = 0;
        for (int i = 0; i < source.Count && filled < 2; i++)
        {
            if (source[i].IsDead)
            {
                Debug.Log($"{source[i].data.cardName} 已阵亡！");
                continue;
            }

            target[filled] = source[i];
            filled++;
        }

        // 如果存活卡牌数不足2张，槽位为空
        while(filled < 2)
        {
            target[filled] = null;
            filled++;
        }
    }

    private void ResetUnitsTurnFlags()
    {
        void ResetList(List<CardUnit> list)
        {
            foreach(var u in list)
            {
                u.hasActedThisTurn = false;
            }
        }
        ResetList(player1Units);
        ResetList(player2Units);
    }

    public CardUnit GetActiveUnit(BattleSide _side, int _index)
    {
        return _side == BattleSide.Player1 ? player1Active[_index] : player2Active[_index];
    }

    public bool IsAllActiveUnitsActedThisTurn(BattleSide _side)
    {
        var actives = _side == BattleSide.Player1 ? player1Active : player2Active;

        bool any = false;
        foreach (var u in actives)
        {
            if (u == null || u.IsDead) continue;
            any = true;
            if (!u.hasActedThisTurn) return false;
        }
        //没有任何存活单位，则认为不需要行动
        return any;
    }

    public void EndCurrentSideTurn(bool _auto)
    {
        if(currentSide == BattleSide.Player1)
        {
            player1HasActedThisTurn = true;
        }
        else
        {
            player2HasActedThisTurn = true;
        }

        Debug.Log($"{currentSide} 结束回合 (_auto = {_auto})");

        // 切换行动方
        currentSide = currentSide == BattleSide.Player1 ? BattleSide.Player2 : BattleSide.Player1;

        //双方结束后进入下一回合
        if(player1HasActedThisTurn && player2HasActedThisTurn)
        {
            turnCount++;
            player1HasActedThisTurn = false;
            player2HasActedThisTurn = false;
            ResetUnitsTurnFlags();

            Debug.Log($"进入第 {turnCount} 回合");
        }
    }

    private void CheckBattleEnd()
    {
        bool player1AllDead = HasAllDead(player1Units);
        bool player2AllDead = HasAllDead(player2Units);

        if(player1AllDead || player2AllDead)
        {
            if (player1AllDead)
            {
                Debug.Log("Player2 获得胜利！");
            }else if (player2AllDead)
            {
                Debug.Log("Player1 获得胜利！");
            }
        }

        // 通知对战结束-------------------------------待添加
    }

    private bool HasAllDead(List<CardUnit> _units)
    {
        foreach(var u in _units)
        {
            if(!u.IsDead) return false;
        }
        return true;
    }
}
