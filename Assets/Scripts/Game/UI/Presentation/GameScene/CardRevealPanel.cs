using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 领卡展示面板：按卡牌数量生成条目预制体并横向错位叠放（右侧在上）；点击将最上层卡牌向右滑出，最后一张滑出后关闭。
/// </summary>
public sealed class CardRevealPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 点击继续按钮（可选；为空时可由外部按钮直接调 OnClickNext）。
    /// </summary>
    [SerializeField] private Button nextBtn;

    /// <summary>
    /// 卡牌序号文本（例如：1/10）。
    /// </summary>
    [SerializeField] private TMP_Text cardIndexText;

    /// <summary>
    /// 堆叠卡牌的父节点（RectTransform）；与 <see cref="cardRevealEntryPrefab"/> 同时配置时启用堆叠布局。
    /// </summary>
    [SerializeField] private RectTransform cardStackRoot;

    /// <summary>
    /// 单张领卡条目预制体（挂 <see cref="CardRevealEntryView"/>）。
    /// </summary>
    [SerializeField] private GameObject cardRevealEntryPrefab;

    /// <summary>
    /// 第一张卡（最左侧、最下）相对 <see cref="cardStackRoot"/> 的锚点坐标。
    /// </summary>
    [SerializeField] private Vector2 cardStackBaseOffset;

    /// <summary>
    /// 每张相对上一张的水平位移：向右错开，右侧卡牌叠在更上层（子节点顺序越后越后绘制）。
    /// Y 轴由 <see cref="cardStackBaseOffset"/> 统一控制，所有卡牌保持同一 Y。
    /// </summary>
    [SerializeField] private Vector2 cardStackStep = new Vector2(24f, 0f);

    /// <summary>
    /// 切换下一张时滑动动画时长（秒，受 <see cref="Time.unscaledDeltaTime"/> 驱动）。
    /// </summary>
    /// <remarks>
    /// TODO: 引入 DOTween 后，可改为 <c>DOAnchorPos</c> 的 Duration，并配合 <c>Sequence</c> / <c>Ease</c> 统一手感。
    /// </remarks>
    [SerializeField] private float slideDuration = 0.22f;

    /// <summary>
    /// 点击下一张时：当前最上层卡牌向右滑出屏幕的水平位移量（像素）。
    /// </summary>
    /// <remarks>
    /// TODO: 迁移 DOTween 时可用 <c>DOAnchorPos</c> 的相对位移或 <c>DOLocalMove</c> 替代本字段与协程插值。
    /// </remarks>
    [SerializeField] private float slideOffsetX = 200f;

    /// <summary>
    /// 本次待展示卡牌 id 列表。
    /// </summary>
    private readonly List<string> _revealedCardIds = new();

    /// <summary>
    /// 当前展示下标。
    /// </summary>
    private int _currentIndex;

    /// <summary>
    /// cardId -> 配置映射。
    /// </summary>
    private readonly Dictionary<string, CardConfigSO> _configMap = new();

    /// <summary>
    /// 运行时生成的堆叠条目。
    /// </summary>
    private readonly List<CardRevealEntryView> _spawnedStackEntries = new();

    /// <summary>
    /// 是否正在播放切换动画（防止连点）。
    /// </summary>
    private bool _isSliding;

    /// <summary>
    /// 当前滑动协程句柄（禁用时停止）。
    /// </summary>
    private Coroutine _slideCoroutine;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中已放置的领卡面板；若不存在则打调试警告并返回 null（不运行时创建默认物体）。
    /// </summary>
    public static CardRevealPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<CardRevealPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.CardRevealPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "CardRevealPanel: 场景中未找到 CardRevealPanel，请在场景或 UI 预制体中挂载该组件；已取消运行时自动搭建界面。");
        return null;
    }

    /// <summary>
    /// 打开并开始逐张展示卡牌。
    /// </summary>
    public void OpenWithCards(IReadOnlyList<string> cardIds)
    {
        StopSlideIfAny();
        _revealedCardIds.Clear();
        if (cardIds != null)
        {
            for (var i = 0; i < cardIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(cardIds[i]))
                {
                    _revealedCardIds.Add(cardIds[i].Trim());
                }
            }
        }

        _currentIndex = _revealedCardIds.Count - 1;
        LoadConfigMap();
        RebuildCardStack();
        RefreshCardIndexText();
        RefreshStackHighlight();
    }

    /// <summary>
    /// 点击下一张：最后一张后关闭领卡面板并回到上层。
    /// </summary>
    public void OnClickNext()
    {
        if (_isSliding)
        {
            return;
        }

        if (_revealedCardIds.Count == 0)
        {
            UIPanelRegistry.TryPop();
            return;
        }

        var canSlide = UseStackLayout()
                       && slideDuration > 0.001f
                       && slideOffsetX > 0.01f
                       && _currentIndex >= 0
                       && _currentIndex < _spawnedStackEntries.Count;

        if (canSlide)
        {
            // TODO: 替换为 DOTween 时，在此构建“当前顶层卡牌右移离场”动画，并统一用 Kill/Complete 处理打断。
            _slideCoroutine = StartCoroutine(CoSlideOutTop(_currentIndex));
        }
        else
        {
            // 无滑动条件时，直接视为移除一张。
            RemoveTopCardAndContinue(_currentIndex);
        }
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时订阅下一张按钮。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        if (nextBtn != null) nextBtn.onClick.AddListener(OnClickNext);
    }

    /// <summary>
    /// 禁用时取消订阅并停止滑动。
    /// </summary>
    protected override void OnDisable()
    {
        if (nextBtn != null) nextBtn.onClick.RemoveListener(OnClickNext);
        StopSlideIfAny();
        base.OnDisable();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 是否使用堆叠预制体布局。
    /// </summary>
    private bool UseStackLayout()
    {
        return cardStackRoot != null && cardRevealEntryPrefab != null;
    }

    /// <summary>
    /// 停止滑动协程并恢复按钮可点状态。
    /// </summary>
    private void StopSlideIfAny()
    {
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }

        _isSliding = false;
        if (nextBtn != null)
        {
            nextBtn.interactable = true;
        }
    }

    /// <summary>
    /// 第 i 张堆叠卡牌的锚点坐标（与生成时一致）。
    /// </summary>
    private Vector2 StackSlotPosition(int index)
    {
        return new Vector2(cardStackBaseOffset.x + cardStackStep.x * index, cardStackBaseOffset.y);
    }

    /// <summary>
    /// 取堆叠条目的 RectTransform。
    /// </summary>
    private RectTransform GetStackRect(int index)
    {
        if (index < 0 || index >= _spawnedStackEntries.Count)
        {
            return null;
        }

        var e = _spawnedStackEntries[index];
        return e != null ? e.transform as RectTransform : null;
    }

    /// <summary>
    /// 当前最上层卡牌向右滑出；使用协程插值。
    /// </summary>
    /// <remarks>
    /// TODO: 后续改为 DOTween：例如 <c>outRt.DOAnchorPos(outEnd, duration).SetEase(Ease.InQuad).SetUpdate(true)</c>，
    /// 完成后回调 <see cref="RemoveTopCardAndContinue"/> 并移除本协程。
    /// </remarks>
    private IEnumerator CoSlideOutTop(int topIndex)
    {
        _isSliding = true;
        if (nextBtn != null)
        {
            nextBtn.interactable = false;
        }

        var outRt = GetStackRect(topIndex);
        var baseOut = StackSlotPosition(topIndex);
        var outEnd = baseOut + new Vector2(slideOffsetX, 0f);

        var duration = Mathf.Max(0.01f, slideDuration);
        if (outRt != null)
        {
            outRt.anchoredPosition = baseOut;
        }

        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var k = Mathf.Clamp01(t / duration);
            var s = Mathf.SmoothStep(0f, 1f, k);
            if (outRt != null)
            {
                outRt.anchoredPosition = Vector2.LerpUnclamped(baseOut, outEnd, s);
            }

            yield return null;
        }

        if (outRt != null)
        {
            outRt.anchoredPosition = outEnd;
        }

        RemoveTopCardAndContinue(topIndex);
        _slideCoroutine = null;
    }

    /// <summary>
    /// 移除当前顶层卡牌并推进状态：若已全部划出则关闭面板，否则刷新高亮与序号。
    /// </summary>
    /// <param name="topIndex">本次被移除的顶层卡牌索引。</param>
    private void RemoveTopCardAndContinue(int topIndex)
    {
        if (topIndex >= 0 && topIndex < _spawnedStackEntries.Count)
        {
            var top = _spawnedStackEntries[topIndex];
            if (top != null)
            {
                Destroy(top.gameObject);
            }
        }

        _currentIndex = topIndex - 1;
        if (_currentIndex < 0)
        {
            _isSliding = false;
            if (nextBtn != null)
            {
                nextBtn.interactable = true;
            }

            UIPanelRegistry.TryPop();
            return;
        }

        RefreshCardIndexText();
        RefreshStackHighlight();
        _isSliding = false;
        if (nextBtn != null)
        {
            nextBtn.interactable = true;
        }
    }

    /// <summary>
    /// 销毁并清空堆叠实例。
    /// </summary>
    private void ClearCardStack()
    {
        for (var i = 0; i < _spawnedStackEntries.Count; i++)
        {
            var v = _spawnedStackEntries[i];
            if (v != null)
            {
                Destroy(v.gameObject);
            }
        }

        _spawnedStackEntries.Clear();
    }

    /// <summary>
    /// 按当前 id 列表在 <see cref="cardStackRoot"/> 下生成条目并错位排布。
    /// </summary>
    private void RebuildCardStack()
    {
        ClearCardStack();
        if (!UseStackLayout())
        {
            return;
        }

        for (var i = 0; i < _revealedCardIds.Count; i++)
        {
            var id = _revealedCardIds[i];
            var go = Instantiate(cardRevealEntryPrefab, cardStackRoot, false);
            go.name = $"CardReveal_{i + 1}_{id}";

            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = StackSlotPosition(i);
            }

            var entry = go.GetComponent<CardRevealEntryView>() ?? go.GetComponentInChildren<CardRevealEntryView>(true);
            _configMap.TryGetValue(id, out var cfg);
            if (entry != null)
            {
                entry.Bind(id, cfg);
                _spawnedStackEntries.Add(entry);
            }
        }
    }

    /// <summary>
    /// 刷新堆叠条目的「当前张」高亮。
    /// </summary>
    private void RefreshStackHighlight()
    {
        for (var i = 0; i < _spawnedStackEntries.Count; i++)
        {
            var e = _spawnedStackEntries[i];
            if (e != null)
            {
                e.SetCurrentVisual(i == _currentIndex);
            }
        }
    }

    /// <summary>
    /// 刷新序号文本。
    /// </summary>
    private void RefreshCardIndexText()
    {
        if (cardIndexText == null)
        {
            return;
        }

        if (_revealedCardIds.Count == 0 || _currentIndex < 0 || _currentIndex >= _revealedCardIds.Count)
        {
            cardIndexText.text = "0/0";
            return;
        }

        cardIndexText.text = $"{_currentIndex + 1}/{_revealedCardIds.Count}";
    }

    /// <summary>
    /// 加载卡牌配置映射。
    /// </summary>
    private void LoadConfigMap()
    {
        _configMap.Clear();
        var db = GameResourceLoader.LoadCardConfigDatabase(logOnMissing: false);
        if (db == null || db.Cards == null)
        {
            return;
        }

        var cards = db.Cards;
        for (var i = 0; i < cards.Count; i++)
        {
            var cfg = cards[i];
            if (cfg != null && !string.IsNullOrWhiteSpace(cfg.CardId))
            {
                _configMap[cfg.CardId] = cfg;
            }
        }
    }

    #endregion
}
