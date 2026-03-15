using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 负责生成战斗场景中的卡牌Prefab
/// </summary>
public class BattleFieldView : Singleton<BattleFieldView>
{
    [Header("卡牌容器与预制体")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform player1CardContainer;
    [SerializeField] private Transform player2CardContainer;

    private readonly List<CardUI> player1CardUIs = new List<CardUI>();
    private readonly List<CardUI> player2CardUIs = new List<CardUI>();

    /// <summary>
    /// 根据双方上场的 CardUnit ，生成对应Prefab
    /// 由 BattleManager 开局生成
    /// </summary>
    public void SetUpCards(IReadOnlyList<CardUnit> _player1Units, IReadOnlyList<CardUnit> _player2Units)
    {
        if(_player1Units == null || _player2Units == null)
        {
            Debug.LogWarning($"某一方 _playerUnits 为空！");
            return;
        }

        ClearCards();

        InstantiatePrefabs(_player1Units, player1CardUIs, player1CardContainer);
        InstantiatePrefabs(_player2Units, player2CardUIs, player2CardContainer);

        //Debug.Log("生成双方卡牌！");
    }

    private void InstantiatePrefabs(IReadOnlyList<CardUnit> _playerUnits, List<CardUI> _cardUIs, Transform _container)
    {
        if (_container != null && _playerUnits != null)
        {
            for (int i = 0; i < _playerUnits.Count; i++)
            {
                CardUnit unit = _playerUnits[i];
                Debug.Log($"生成 {unit.data.cardName}");
                if (unit == null) continue;

                GameObject go = Instantiate(cardPrefab, _container);
                go.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                go.name = $"PlayerCard_{i}";
                Debug.Log($"生成 {go.name}");

                CardUI cardUI = go.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    cardUI.Bind(unit);
                    _cardUIs.Add(cardUI);
                }
            }
        }
        else
        {
            Debug.LogWarning($"卡牌预制体容器或CardUnit为空！");
        }

        if(_playerUnits.Count == 0)
        {
            Debug.LogWarning($"_playerUnits 里没有物体！");
        }
    }

    /// <summary>
    /// 刷新绑定的卡牌显示
    /// </summary>
    public void RefreshAllCards()
    {
        foreach (var ui in player1CardUIs)
            ui.RefreshDisplay();
        foreach (var ui in player2CardUIs)
            ui.RefreshDisplay();
    }

    public void ClearCards()
    {
        if(player1CardContainer != null)
        {
            for(int i = player1CardContainer.childCount - 1; i > 0; i--)
            {
                Destroy(player1CardContainer.GetChild(i).gameObject);
            }
            for(int i = player2CardContainer.childCount - 1; i > 0; i--)
            {
                Destroy(player2CardContainer.GetChild(i).gameObject);
            }
        }
        player1CardUIs.Clear();
        player2CardUIs.Clear();
    }
}
