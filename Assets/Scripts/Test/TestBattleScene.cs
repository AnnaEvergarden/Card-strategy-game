using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ≤‚ ‘’Ω∂∑≥°æ∞¬ﬂº≠
/// </summary>
public class TestBattleScene : MonoBehaviour
{
    [SerializeField] private Button startBattleBtn;

    public bool isPVP;

    BattleManager battleManager;

    BattleContext ctx = new BattleContext();

    private void Start()
    {
        startBattleBtn.onClick.AddListener(OnClickStartBattleButton);

        battleManager = BattleManager.Instance;

        ctx.Player1Deck.Add(new CardInstance(CardDataBase.Instance.defaultCardData));
        ctx.Player2Deck.Add(new CardInstance(CardDataBase.Instance.defaultCardData));
    }

    private void OnClickStartBattleButton()
    {
        if (startBattleBtn != null)
        {
            battleManager.StartBattle(ctx);
        }
    }
}
