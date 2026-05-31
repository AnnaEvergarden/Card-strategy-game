using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗场地单行舰娘占位：立绘、名称与生命值；支持回合行动态（Scale 缩放/禁点）与 DOTween 选中放大。
/// </summary>
public sealed class BattleShipFieldSlotView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 舰娘头像。
    /// </summary>
    [SerializeField] private Image portraitImage;

    /// <summary>
    /// 展示名称。
    /// </summary>
    [SerializeField] private TMP_Text displayNameText;

    /// <summary>
    /// 生命值文本。
    /// </summary>
    [SerializeField] private TMP_Text hpText;

    /// <summary>
    /// 做选中放大动画的根节点（未绑定时使用本物体 Transform）。
    /// </summary>
    [SerializeField] private RectTransform scaleRoot;

    /// <summary>
    /// 用于统一禁用交互（未绑定时自动 GetComponent）。
    /// </summary>
    [SerializeField] private CanvasGroup canvasGroup;

    // ── 策划可调参数 ──────────────────────────────────────────────

    /// <summary>
    /// 选中放大倍率（操作菜单打开时）。
    /// </summary>
    [SerializeField] private float selectedScale = 1.08f;

    /// <summary>
    /// 对方回合时本槽位的缩放倍率（0.85 = 缩小 15%）。
    /// </summary>
    [Header("回合缩放动画")]
    [SerializeField] private float opponentTurnScale = 0.85f;

    /// <summary>
    /// 从正常缩放到对方回合缩放的动画时长（秒）。
    /// </summary>
    [SerializeField] private float scaleDownDuration = 0.3f;

    /// <summary>
    /// 首次进入战斗后，缩小动画的延迟（秒），让玩家看到缩小过程。
    /// </summary>
    [SerializeField] private float scaleDownDelay = 0.2f;

    /// <summary>
    /// 恢复到正常缩放的动画时长（秒）。
    /// </summary>
    [SerializeField] private float scaleRestoreDuration = 0.3f;

    // ── 运行时状态 ──────────────────────────────────────────────────

    /// <summary>
    /// 当前生命值。
    /// </summary>
    private int _hp;

    /// <summary>
    /// 批量赋值时抑制逐字段刷新，最后统一 <see cref="RefreshAllUi"/>。
    /// </summary>
    private bool _suppressStatsRefresh;

    /// <summary>
    /// 打开操作菜单按钮（可选；仅玩家槽位需要绑定）。
    /// </summary>
    [SerializeField] private Button openActionMenuBtn;

    /// <summary>
    /// 技能选目标点击按钮（可选；未绑定时回退为 <see cref="portraitImage"/> 上的 Button）。
    /// </summary>
    [SerializeField] private Button skillTargetPickBtn;

    /// <summary>
    /// 当前绑定 cardId（打开菜单时使用）。
    /// </summary>
    private string _boundCardId = string.Empty;

    /// <summary>
    /// 当前绑定 UnitId（全局唯一，技能选目标用）。
    /// </summary>
    private string _boundUnitId = string.Empty;

    /// <summary>
    /// 所属阵营（P1 = 玩家方，P2 = 对手方）。
    /// </summary>
    private BattleSide _side;

    /// <summary>
    /// 当前缩放 Tween。
    /// </summary>
    private Tween _scaleTween;

    /// <summary>
    /// 记录上次应用的缩放目标值，避免重复触发动画。
    /// </summary>
    private float _lastAppliedScale = 1f;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 订阅打开菜单按钮。
    /// </summary>
    private void Awake()
    {
        if (scaleRoot == null)
        {
            scaleRoot = transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (openActionMenuBtn != null)
        {
            openActionMenuBtn.onClick.AddListener(OnClickOpenActionMenu);
        }

        WireSkillTargetPickButton();
    }

    /// <summary>
    /// 取消订阅并停止 Tween。
    /// </summary>
    private void OnDestroy()
    {
        _scaleTween?.Kill();

        if (openActionMenuBtn != null)
        {
            openActionMenuBtn.onClick.RemoveListener(OnClickOpenActionMenu);
        }

        if (skillTargetPickBtn != null)
        {
            skillTargetPickBtn.onClick.RemoveListener(OnClickSkillTargetPick);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 当前绑定的 cardId。
    /// </summary>
    public string BoundCardId => _boundCardId;

    /// <summary>
    /// 当前绑定的 UnitId。
    /// </summary>
    public string BoundUnitId => _boundUnitId;

    /// <summary>
    /// 当前生命值。
    /// </summary>
    public int HP
    {
        get => _hp;
        set
        {
            if (_hp == value)
            {
                return;
            }

            _hp = value;
            OnStatFieldChanged();
        }
    }

    /// <summary>
    /// 绑定舰娘配置（名称、头像与初始生命）；配置为 null 时清空展示。
    /// </summary>
    /// <param name="config">卡牌配置。</param>
    public void Bind(CardConfigSO config)
    {
        Bind(config, config != null ? config.CardId : string.Empty);
    }

    /// <summary>
    /// 绑定舰娘配置与 cardId（用于打开操作菜单）。
    /// </summary>
    /// <param name="config">卡牌配置。</param>
    /// <param name="cardId">卡牌 id。</param>
    /// <param name="side">阵营 <see cref="BattleSide"/>。</param>
    /// <param name="unitId">单位 UnitId（全局唯一，技能选目标用）。</param>
    public void Bind(CardConfigSO config, string cardId, BattleSide side = BattleSide.P1, string unitId = "")
    {
        _side = side;
        _boundCardId = string.IsNullOrWhiteSpace(cardId) ? string.Empty : cardId.Trim();
        _boundUnitId = string.IsNullOrWhiteSpace(unitId) ? string.Empty : unitId.Trim();
        _suppressStatsRefresh = true;
        try
        {
            HP = config?.HP ?? 0;
            ApplyDisplayFromConfig(config);
        }
        finally
        {
            _suppressStatsRefresh = false;
        }

        RefreshAllUi();
    }

    /// <summary>
    /// 是否显示「打开操作菜单」按钮（敌方槽位应关闭）。
    /// </summary>
    /// <param name="visible">是否显示并可点。</param>
    public void SetActionMenuAvailable(bool visible)
    {
        if (openActionMenuBtn == null)
        {
            return;
        }

        openActionMenuBtn.gameObject.SetActive(visible && _side == BattleSide.P1);
        ApplyTurnInteractionState();
    }

    /// <summary>
    /// 仅更新生命值（不改动名称与头像）。
    /// </summary>
    /// <param name="hp">生命值。</param>
    public void SetHp(int hp)
    {
        _suppressStatsRefresh = true;
        try
        {
            HP = hp;
        }
        finally
        {
            _suppressStatsRefresh = false;
        }

        RefreshStatsUi();
    }

    /// <summary>
    /// 强制刷新全部 UI（名称、头像、数值）。
    /// </summary>
    public void RefreshAllUi()
    {
        RefreshStatsUi();
    }

    /// <summary>
    /// 根据回合系统刷新交互状态：对方回合时本槽位 Scale 缩小（DOTween 动画），己方回合时恢复。
    /// 同时控制 CanvasGroup 的交互性（玩家槽位可操作 / 不可操作）。
    /// </summary>
    public void ApplyTurnInteractionState()
    {
        if (canvasGroup == null)
        {
            return;
        }

        var turns = BattleContext.Current.Turns;
        var isPlayerPhase = turns.IsPlayerActionPhase;
        var acted = !string.IsNullOrEmpty(_boundUnitId) && turns.HasActedThisRound(_boundUnitId);

        // ── P1 槽位（玩家方可操作） ────────────────────────────────
        if (_side == BattleSide.P1)
        {
            var canOpen = turns.CanOpenActionMenu(_boundUnitId);

            // 选目标模式或可打开菜单时均可交互
            canvasGroup.interactable = canOpen || BattleUiSession.IsAwaitingSkillTarget;
            canvasGroup.blocksRaycasts = canOpen || BattleUiSession.IsAwaitingSkillTarget;

            if (openActionMenuBtn != null)
            {
                openActionMenuBtn.interactable = canOpen;
            }

            // 敌方回合 → 缩小；已行动或非玩家阶段 → 复位选中缩放
            if (!isPlayerPhase)
            {
                AnimateScale(opponentTurnScale, scaleDownDuration, scaleDownDelay);
            }
            else
            {
                AnimateScale(1f, scaleRestoreDuration, 0f);
            }

            if (acted)
            {
                ResetActionMenuScale(immediate: true);
            }

            canvasGroup.alpha = acted ? 0.5f : 1f;
        }
        // ── P2 槽位（对手方） ──────────────────────────────────────
        else
        {
            // 选目标模式下允许交互（点击作为技能目标），平时不可交互
            canvasGroup.interactable = BattleUiSession.IsAwaitingSkillTarget;
            canvasGroup.blocksRaycasts = BattleUiSession.IsAwaitingSkillTarget;

            // 己方回合 → 缩小（敌方不可操作）；敌方回合 → 恢复
            if (isPlayerPhase)
            {
                AnimateScale(opponentTurnScale, scaleDownDuration, scaleDownDelay);
            }
            else
            {
                AnimateScale(1f, scaleRestoreDuration, 0f);
            }

            canvasGroup.alpha = acted ? 0.5f : 1f;
        }
    }

    /// <summary>
    /// 用 DOTween 将 <see cref="scaleRoot"/> 缩放到目标值，避免重复触发相同目标。
    /// </summary>
    /// <param name="targetScale">目标缩放值。</param>
    /// <param name="duration">动画时长。</param>
    /// <param name="delay">开始延迟。</param>
    private void AnimateScale(float targetScale, float duration, float delay)
    {
        if (scaleRoot == null)
        {
            return;
        }

        // 与当前目标相同则跳过，避免打断进行中的动画
        if (Mathf.Abs(_lastAppliedScale - targetScale) < 0.001f)
        {
            return;
        }

        _lastAppliedScale = targetScale;
        _scaleTween?.Kill();

        if (duration <= 0f && delay <= 0f)
        {
            scaleRoot.localScale = Vector3.one * targetScale;
            return;
        }

        _scaleTween = scaleRoot
            .DOScale(Vector3.one * targetScale, duration)
            .SetDelay(delay)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    /// <summary>
    /// 打开操作菜单时的选中放大动画。
    /// </summary>
    public void PlayActionMenuOpenScale()
    {
        if (scaleRoot == null)
        {
            return;
        }

        _scaleTween?.Kill();
        _lastAppliedScale = selectedScale;
        scaleRoot.localScale = Vector3.one;
        _scaleTween = scaleRoot
            .DOScale(Vector3.one * selectedScale, 0.25f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    /// <summary>
    /// 关闭菜单或行动完成后恢复缩放。
    /// </summary>
    /// <param name="immediate">是否立即复位（不播放动画）。</param>
    public void ResetActionMenuScale(bool immediate = false)
    {
        if (scaleRoot == null)
        {
            return;
        }

        _scaleTween?.Kill();
        _lastAppliedScale = 1f;
        if (immediate)
        {
            scaleRoot.localScale = Vector3.one;
            return;
        }

        _scaleTween = scaleRoot
            .DOScale(Vector3.one, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    /// <summary>
    /// 执行动作时的强调动画（敌方行动时播放，让玩家看到 AI 在操作）。
    /// </summary>
    public void PlayActionEmphasis()
    {
        if (scaleRoot == null)
        {
            return;
        }

        _scaleTween?.Kill();
        _scaleTween = scaleRoot
            .DOPunchScale(Vector3.one * 0.15f, 0.35f, 2, 0.5f)
            .SetUpdate(true);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 单字段变更时刷新数值区（批量赋值期间跳过）。
    /// </summary>
    private void OnStatFieldChanged()
    {
        if (_suppressStatsRefresh)
        {
            return;
        }

        RefreshStatsUi();
    }

    /// <summary>
    /// 将名称与头像写入缓存并刷新对应控件。
    /// </summary>
    private void ApplyDisplayFromConfig(CardConfigSO config)
    {
        if (displayNameText != null)
        {
            displayNameText.text = config != null && !string.IsNullOrEmpty(config.DisplayName)
                ? config.DisplayName
                : string.Empty;
        }

        if (portraitImage != null)
        {
            var englishName = config != null ? config.EnglishName : null;
            GameResourceLoader.ApplyShipgirlIconToImage(portraitImage, englishName, logOnMissing: false);
        }
    }

    /// <summary>
    /// 刷新生命值文本。
    /// </summary>
    private void RefreshStatsUi()
    {
        if (hpText != null)
        {
            hpText.text = _hp.ToString();
        }
    }

    /// <summary>
    /// 点击打开舰娘操作小面板。
    /// </summary>
    private void OnClickOpenActionMenu()
    {
        if (string.IsNullOrEmpty(_boundUnitId))
        {
            return;
        }

        BattleUiFlow.OpenSlotActionMenu(_boundUnitId);
    }

    /// <summary>
    /// 绑定技能选目标按钮（头像区域点击）。
    /// </summary>
    private void WireSkillTargetPickButton()
    {
        if (skillTargetPickBtn == null && portraitImage != null)
        {
            skillTargetPickBtn = portraitImage.GetComponent<Button>();
            if (skillTargetPickBtn == null)
            {
                skillTargetPickBtn = portraitImage.gameObject.AddComponent<Button>();
                skillTargetPickBtn.transition = Selectable.Transition.None;
            }
        }

        if (skillTargetPickBtn != null)
        {
            skillTargetPickBtn.onClick.AddListener(OnClickSkillTargetPick);
        }
    }

    /// <summary>
    /// 待选技能目标时，点击槽位头像完成释放。
    /// </summary>
    private void OnClickSkillTargetPick()
    {
        if (!BattleUiSession.IsAwaitingSkillTarget || string.IsNullOrEmpty(_boundCardId))
        {
            return;
        }

        if (!BattleContext.Current.Turns.IsPlayerActionPhase)
        {
            return;
        }

        BattleFacade.TryCompleteSkillOnTarget(_boundUnitId);
    }

    #endregion
}
