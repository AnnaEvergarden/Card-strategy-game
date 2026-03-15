using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : BaseUIPanel
{
    [Header("回合相关按钮")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button card1Button;
    [SerializeField] private Button card2Button;

    BattleManager battleManager;

    protected override void Awake()
    {
        base.Awake();

        battleManager = BattleManager.Instance;

        if(endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnClickEndTurnButton);
        }

        if(card1Button != null)
        {
            card1Button.onClick.AddListener(OnClickCard1Button);
        }

        if(card2Button != null)
        {
            card2Button.onClick.AddListener(OnClickCard2Button);
        }

    }

    public void OnClickEndTurnButton()
    {
        battleManager.EndCurrentSideTurn(_auto: false);
    }

    public void OnClickCard1Button()
    {
        //弹出卡牌操作界面
    }

    public void OnClickCard2Button()
    {
        //弹出卡牌操作界面
    }
}
