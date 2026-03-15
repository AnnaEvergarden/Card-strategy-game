using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CardUnit
{
    public CardInstance instance { get; private set; }
    public CardData data { get; private set; }

    public int currentHP { get; private set; }
    public int currentAttack { get; private set; }
    public int currentCost { get; private set; }

    /// <summary>
    /// 本回合是否已经使用过技能
    /// </summary>
    public bool hasActedThisTurn;
    public bool IsDead => currentHP <= 0;

    public List<CardEffect> effects = new List<CardEffect>();

    public void TakeDamage(int _damage)
    {
        currentHP -= _damage;
        if(currentHP < 0) currentHP = 0;
    }

    public void Heal(int _amount)
    {
        currentHP += _amount;
    }

    public CardUnit(CardInstance _card, BattleContext _ctx)
    {
        instance = _card;
        data = CardDataBase.Instance.GetCardData(_card.cardId);

        if(data == null)
        {
            Debug.LogWarning($"CardData 为空，无法创建CardUnit！");
            return;
        }

        currentAttack = CardStatsService.GetAttack(instance, data, _ctx.isPVP);
        currentHP = CardStatsService.GetHealth(instance, data, _ctx.isPVP);
        hasActedThisTurn = false;
    }
}
