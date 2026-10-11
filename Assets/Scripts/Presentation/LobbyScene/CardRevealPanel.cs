using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 抽卡揭示面板：接收已抽好的卡牌列表，逐张展示，点击切换下一张（DOTween 切出动画），
/// 最后一张切完后将所有卡牌展示在 Grid Layout Group 容器中，
/// 点击返回时保存所有抽到的卡牌到收藏。
/// </summary>
public sealed class CardRevealPanel : PanelBase
{
    [Header("Single Card View")]
    [SerializeField] private RectTransform _singleCardRoot;
    [SerializeField] private CanvasGroup _singleCardGroup;

    [Header("Grid Summary")]
    [SerializeField] private Transform _gridRoot;

    [Header("Card Prefab")]
    [SerializeField] private GameObject _cardItemPrefab;

    [Header("Navigation")]
    [SerializeField] private Button _clickBtn;
    [SerializeField] private Button _backBtn;
    [SerializeField] private TMP_Text _cardIndexText;

    private readonly List<CardConfig> _drawnCards = new List<CardConfig>();
    private int _currentIndex;
    private bool _isAnimating;
    private bool _cardsSaved;

    protected override void OnEnable() { base.OnEnable(); Subscribe(); }
    protected override void OnDisable() { Unsubscribe(); base.OnDisable(); }

    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);

        _drawnCards.Clear();
        _cardsSaved = false;
        if (param is IReadOnlyList<CardConfig> cards && cards.Count > 0)
            _drawnCards.AddRange(cards);

        if (_drawnCards.Count == 0) return;

        // 状态重置：上一次开启可能在结束或动画中途改变了激活态/缩放/位移，二次开启必须全部还原。
        _currentIndex = 0;
        _isAnimating = false;
        if (_backBtn != null) _backBtn.gameObject.SetActive(false);
        if (_clickBtn != null) _clickBtn.gameObject.SetActive(true);
        if (_gridRoot != null) _gridRoot.gameObject.SetActive(false);
        if (_singleCardRoot != null)
        {
            _singleCardRoot.gameObject.SetActive(true);
            _singleCardRoot.localScale = Vector3.one;
            _singleCardRoot.anchoredPosition = Vector2.zero;
        }
        if (_singleCardGroup != null) _singleCardGroup.alpha = 1f;
        ShowCard(_currentIndex);
    }

    private void ShowCard(int index)
    {
        if (index < 0 || index >= _drawnCards.Count) return;

        for (int i = _singleCardRoot.childCount - 1; i >= 0; i--)
            Destroy(_singleCardRoot.GetChild(i).gameObject);

        if (_cardItemPrefab == null) return;

        var go = Instantiate(_cardItemPrefab, _singleCardRoot, false);
        SetupCardPrefab(go, _drawnCards[index]);

        // 序号文本：当前张数/总张数（例如 1/10、2/10）
        if (_cardIndexText != null)
        {
            _cardIndexText.gameObject.SetActive(true);
            _cardIndexText.text = $"{index + 1}/{_drawnCards.Count}";
        }
    }

    public async void OnClickAdvance()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        var tcs = new UniTaskCompletionSource();
        var seq = DOTween.Sequence();
        seq.Join(_singleCardRoot.DOScale(0.7f, 0.25f).SetEase(Ease.InBack));
        seq.Join(_singleCardGroup.DOFade(0f, 0.2f));
        seq.Join(_singleCardRoot.DOAnchorPosX(-300f, 0.25f).SetEase(Ease.InCubic));
        seq.OnComplete(() => tcs.TrySetResult());
        seq.Play();
        await tcs.Task;

        _currentIndex++;

        if (_currentIndex >= _drawnCards.Count)
        {
            _singleCardRoot.gameObject.SetActive(false);
            if (_clickBtn != null) _clickBtn.gameObject.SetActive(false);
            ShowGridSummary();
        }
        else
        {
            _singleCardRoot.localScale = Vector3.one;
            _singleCardRoot.anchoredPosition = Vector2.zero;
            _singleCardGroup.alpha = 1f;
            ShowCard(_currentIndex);
        }

        _isAnimating = false;
    }

    private void ShowGridSummary()
    {
        _gridRoot.gameObject.SetActive(true);
        if (_cardIndexText != null)
            _cardIndexText.gameObject.SetActive(false);

        for (int i = _gridRoot.childCount - 1; i >= 0; i--)
            Destroy(_gridRoot.GetChild(i).gameObject);

        if (_cardItemPrefab == null || _drawnCards.Count == 0) return;

        for (int i = 0; i < _drawnCards.Count; i++)
        {
            var go = Instantiate(_cardItemPrefab, _gridRoot, false);
            SetupCardPrefab(go, _drawnCards[i]);
        }

        if (_backBtn != null) _backBtn.gameObject.SetActive(true);
    }

    private void SetupCardPrefab(GameObject go, CardConfig card)
    {
        var view = go.GetComponent<CardItemView>();
        if (view != null)
            view.SetData(card, ResourceService).Forget();
    }

    /// <summary>返回时保存抽到的卡牌到收藏。</summary>
    public async void OnClickBack()
    {
        if (!_cardsSaved)
        {
            _cardsSaved = true;
            var collectionService = ServiceContainer?.TryResolve<ICardCollectionService>();
            var uow = ServiceContainer?.TryResolve<IUnitOfWork>();
            if (collectionService != null && uow != null)
            {
                // 用 UnitOfWork 包住所有添加操作，保证原子性
                await uow.BeginAsync();
                try
                {
                    for (int i = 0; i < _drawnCards.Count; i++)
                    {
                        var instanceId = System.Guid.NewGuid().ToString();
                        await collectionService.AddCardAsync(instanceId, _drawnCards[i].CardId);
                    }
                    await uow.CommitAsync();
                }
                catch
                {
                    await uow.RollbackAsync();
                    throw;
                }
            }
        }

        UIService?.GoBackAsync().Forget();
    }

    private void Subscribe()
    {
        SafeSubscribe(_clickBtn, OnClickAdvance);
        SafeSubscribe(_backBtn, OnClickBack);
    }

    private void Unsubscribe()
    {
        SafeUnsubscribe(_clickBtn, OnClickAdvance);
        SafeUnsubscribe(_backBtn, OnClickBack);
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