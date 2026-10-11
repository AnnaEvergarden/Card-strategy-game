using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编队选择面板：从已收集卡牌列表选择最多 6 张加入编队。
/// 选中状态带序号遮罩，取消中间序号自动递减。
/// </summary>
public sealed class FleetPickPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;
    [SerializeField] private Button _confirmBtn;

    [Header("Card List")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _cardsRoot;
    [SerializeField] private GameObject _cardItemPrefab;

    [Header("提示")]
    [SerializeField] private TMP_Text _hintText;

    private const int MaxFleetSize = 6;

    // 所有已收集卡牌
    private readonly List<CardInstance> _allCards = new List<CardInstance>();
    private readonly List<CardConfig> _allConfigs = new List<CardConfig>();
    private readonly List<FleetCardItemView> _allViews = new List<FleetCardItemView>();

    // 当前选中的卡牌实例 ID（顺序即编号）
    private readonly List<string> _selectedInstanceIds = new List<string>();
    private int _currentGroupIndex;

    protected override void OnEnable() { base.OnEnable(); Subscribe(); }
    protected override void OnDisable() { Unsubscribe(); base.OnDisable(); }

    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);

        _currentGroupIndex = param is int index ? index : 0;

        // 读取玩家已收集卡牌
        var collectionService = ServiceContainer?.TryResolve<ICardCollectionService>();
        var configService = ServiceContainer?.TryResolve<IConfigService>();
        var fleetService = ServiceContainer?.TryResolve<IFleetService>();

        _allCards.Clear();
        _allConfigs.Clear();
        _allViews.Clear();

        if (collectionService != null)
        {
            var cards = await collectionService.GetCardsAsync();
            if (cards != null)
                _allCards.AddRange(cards);
        }

        // 读取 config 和已选
        for (int i = 0; i < _allCards.Count; i++)
        {
            var cfg = configService?.Get<CardConfig>(_allCards[i].CardId);
            _allConfigs.Add(cfg);
        }

        _selectedInstanceIds.Clear();
        if (fleetService != null)
        {
            var ids = await fleetService.LoadGroupAsync(_currentGroupIndex);
            if (ids != null)
                _selectedInstanceIds.AddRange(ids);
        }

        PopulateCards();
    }

    private void PopulateCards()
    {
        // 清空旧条目
        for (int i = _cardsRoot.childCount - 1; i >= 0; i--)
            Destroy(_cardsRoot.GetChild(i).gameObject);
        _allViews.Clear();

        if (_cardItemPrefab == null) return;

        for (int i = 0; i < _allCards.Count; i++)
        {
            var instance = _allCards[i];
            var config = _allConfigs[i];
            if (config == null) continue;

            var index = i; // capture
            var go = Instantiate(_cardItemPrefab, _cardsRoot, false);
            var view = go.GetComponent<FleetCardItemView>();
            if (view == null) continue;

            view.SetData(instance, config, ResourceService);
            view.SetListener(() => OnToggleCard(index));

            _allViews.Add(view);
        }

        // 恢复选中状态
        UpdateAllSelectionStates();
    }

    private void OnToggleCard(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= _allCards.Count) return;

        var instance = _allCards[cardIndex];
        var pos = _selectedInstanceIds.IndexOf(instance.InstanceId);

        if (pos >= 0)
        {
            // 取消选中
            _selectedInstanceIds.RemoveAt(pos);
        }
        else
        {
            if (_selectedInstanceIds.Count >= MaxFleetSize)
            {
                if (_hintText != null) _hintText.text = "编队已满（最多 6 张）";
                return;
            }
            _selectedInstanceIds.Add(instance.InstanceId);
            if (_hintText != null) _hintText.text = "";
        }

        UpdateAllSelectionStates();
    }

    private void UpdateAllSelectionStates()
    {
        for (int i = 0; i < _allViews.Count; i++)
        {
            var instance = _allCards[i];
            var pos = _selectedInstanceIds.IndexOf(instance.InstanceId);

            if (pos >= 0)
                _allViews[i].SetSelected(pos + 1); // 序号从 1 开始
            else
                _allViews[i].SetDeselected();
        }
    }

    public async void OnClickConfirm()
    {
        var fleetService = ServiceContainer?.TryResolve<IFleetService>();
        if (fleetService != null)
            await fleetService.SaveGroupAsync(_currentGroupIndex, _selectedInstanceIds);

        UIService?.GoBackAsync().Forget();
    }

    public void OnClickBack() { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome() { UIService?.GoHomeAsync().Forget(); }

    private void Subscribe()
    {
        SafeSubscribe(_backBtn, OnClickBack);
        SafeSubscribe(_homeBtn, OnClickHome);
        SafeSubscribe(_confirmBtn, OnClickConfirm);
    }

    private void Unsubscribe()
    {
        SafeUnsubscribe(_backBtn, OnClickBack);
        SafeUnsubscribe(_homeBtn, OnClickHome);
        SafeUnsubscribe(_confirmBtn, OnClickConfirm);
    }

    private static void SafeSubscribe(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    private static void SafeUnsubscribe(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(action);
    }
}