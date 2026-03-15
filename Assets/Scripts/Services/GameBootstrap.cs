using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 测试用例，（暂时废弃）
/// 游戏入口，挂在「我的项目」中的空物体（如 GameBootstrap）上。
/// 负责 Load 存档、选择槽位后第一次进入战斗，验证 PVP/PVE 流程逻辑。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("引用（若为空则尝试 FindObjectOfType）")]
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private AccountManager accountManager;
    [SerializeField] private CardData testCard;

    private void Awake()
    {
        if(saveManager == null) saveManager = FindObjectOfType<SaveManager>();
        if (accountManager == null) accountManager = FindObjectOfType<AccountManager>();
        if (saveManager != null) saveManager.Load();
        else
        {
            Debug.LogWarning("[GameBootstrap] SaveManager 为空，请创建一个并挂到场景 SaveManager!");
        }

        if (battleManager == null) battleManager = FindObjectOfType<BattleManager>();
        if(battleManager != null && testCard != null)
        {
            CardDataBase.Instance.RegisterCardData(testCard);
        }
    }

    private void Start()
    {
        if (accountManager == null || !accountManager.IsLoggedIn())
        {
            // 未登录时，停留在标题/转登录界面
        }
        else
        {
            // 已登录，选存档槽位后进入游戏
            int _slotIndex = 0;
            saveManager.SelectSlot(_slotIndex);
        }

        //RunTestBattleIfNeeded();
    }

    /// <summary> 
    /// 向 battleManager 注册测试卡牌数据，跑一次 PVE 和 PVP 战斗。
    /// </summary>
    private void RunTestBattleIfNeeded()
    {
        if(battleManager == null || testCard == null)
        {
            Debug.LogWarning("[GameBootstrap] BattleManager 或 TestCard 为空!");
            return;
        }

        List<CardInstance> playerDeck = saveManager.GetCurrentDeck();
        // 卡组逻辑示例，正式逻辑可从关卡配置读取
        List<CardInstance> enemyDeck = new List<CardInstance>();
        if (playerDeck == null || playerDeck.Count == 0)
        {
            Debug.LogWarning("[GameBootstrap] 当前卡组为空，使用测试卡组。");

            var playerCard = CardInstance.CreateNew(testCard.cardId);
            playerCard.enhanceAttackBonus = 2;
            playerCard.enhanceHealthBonus = 2;
            playerCard.enhanceLevel = 1;

            var enemyCard = CardInstance.CreateNew(testCard.cardId);

            playerDeck = new List<CardInstance> { playerCard };
            enemyDeck = new List<CardInstance> { enemyCard };
        }

        var ctxPVE = new BattleContext(false, playerDeck, enemyDeck);
        /*Debug.Log($"------- 是否为 PVP ({ctxPVE.isPVP})------ " +
            $"\n================================================" +
            $"\n Player 强化数据:" +
            $"\n 攻击加成 : {playerCard.enhanceAttackBonus} " +
            $"\n 生命加成 : {playerCard.enhanceHealthBonus} " +
            $"\n 强化等级 : {playerCard.enhanceLevel}" +
            $"\n================================================" +
            $"\n Enemy 强化数据:" +
            $"\n 攻击加成 : {enemyCard.enhanceAttackBonus} " +
            $"\n 生命加成 : {enemyCard.enhanceHealthBonus} " +
            $"\n 强化等级 : {enemyCard.enhanceLevel}");*/

        battleManager.StartBattle(ctxPVE);
    }
}
