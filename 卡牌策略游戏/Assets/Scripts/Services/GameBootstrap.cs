using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏启动入口。挂到场景中的空物体（如 GameBootstrap）上。
/// 负责：Load 存档、可选地跑一次测试战斗以验证 PVP/PVE 属性逻辑。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("引用（若为空则尝试 FindObjectOfType）")]
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private CardData testCard;

    private void Awake()
    {
        if(saveManager == null) saveManager = FindObjectOfType<SaveManager>();
        if (saveManager != null) saveManager.Load();
        else
        {
            Debug.LogWarning($"[GameBootsrap] SaveManager 为空，请创建一个空物体挂载SaveManager!");
        }

        if (battleManager == null) battleManager = FindObjectOfType<BattleManager>();
        if(battleManager != null && testCard != null)
        {
            battleManager.RegisterCardData(testCard);
        } 
    }

    private void Start()
    {
        RunTestBattleIfNeeded();
    }

    /// <summary> 
    /// 若存在battleManager 和 测试卡数据，则跑一局PVE 和 PVP 测试
    /// </summary>
    private void RunTestBattleIfNeeded()
    {
        if(battleManager == null || testCard == null)
        {
            Debug.LogWarning($"BattleManager 或者 TestCard 为空!");
            return;
        }

        var playerCard = CardInstance.CreateNew(testCard.cardId);
        playerCard.enhanceAttackBonus = 2;
        playerCard.enhanceHealthBonus = 2;
        playerCard.enhanceLevel = 1;

        var enemyCard = CardInstance.CreateNew(testCard.cardId);

        var playerDeck = new List<CardInstance> { playerCard };
        var enemyDeck = new List<CardInstance> { enemyCard };

        var ctxPVE = new BattleContext(false, playerDeck, enemyDeck);
        Debug.Log($"------- 是否为PVP ({ctxPVE.isPVP})------ " +
            $"\n================================================" +
            $"\n PLayer 强化详情:" +
            $"\n 攻击力加成 : {playerCard.enhanceAttackBonus} " +
            $"\n 生命值加成 : {playerCard.enhanceHealthBonus} " +
            $"\n 强化等级 : {playerCard.enhanceLevel}" +
            $"\n================================================" +
            $"\n Enemy 强化详情:" +
            $"\n 攻击力加成 : {enemyCard.enhanceAttackBonus} " +
            $"\n 生命值加成 : {enemyCard.enhanceHealthBonus} " +
            $"\n 强化等级 : {enemyCard.enhanceLevel}");

        battleManager.StartBattle(ctxPVE);
    }
}
