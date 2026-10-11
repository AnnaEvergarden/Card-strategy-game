using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 舰娘操作菜单面板（复用场景 UI：CastSkill_Button / SwitchCard_Button / Emoji_Button / Close_Button）。
/// 打开参数：操作目标舰娘的 UnitId（由 BattleMainPanel 在点击我方槽位时传入）。
/// 放技能 → 技能选择面板；换牌 → 换牌面板；表情 → 表情面板；关闭 → 关闭自身。
/// </summary>
public sealed class BattleSlotActionMenuPanel : BattleOverlayPanelBase
{
    [SerializeField] private Button _castSkillButton;
    [SerializeField] private Button _switchCardButton;
    [SerializeField] private Button _emojiButton;
    [SerializeField] private Button _closeButton;

    [Header("面板 ID")]
    [SerializeField]
    private string _skillSelectPanelId = "BattleSkillSelect";
    [SerializeField]
    private string _cardSwitchPanelId = "BattleCardSwitch";
    [SerializeField]
    private string _emojiPanelId = "BattleEmoji";

    private string _targetUnitId;

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.SetAsLastSibling();
        ResolveWidgets();
        Subscribe();
    }

    protected override void OnDisable()
    {
        Unsubscribe();
        base.OnDisable();
    }

    /// <summary>
    /// 打开菜单：记录目标舰娘并刷新按钮可用性。
    /// </summary>
    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);
        transform.SetAsLastSibling();

        if (param is string unitId)
            _targetUnitId = unitId;

        RefreshButtonState();
    }

    /// <summary>按子物体名解析场景控件（[SerializeField] 已赋值时跳过）。</summary>
    private void ResolveWidgets()
    {
        var root = transform;
        if (_castSkillButton == null) _castSkillButton = FindButton(root, "CastSkill_Button");
        if (_switchCardButton == null) _switchCardButton = FindButton(root, "SwitchCard_Button");
        if (_emojiButton == null) _emojiButton = FindButton(root, "Emoji_Button");
        if (_closeButton == null) _closeButton = FindButton(root, "Close_Button");
    }

    /// <summary>无可用技能或无替补时置灰对应按钮，避免无效点击。</summary>
    private void RefreshButtonState()
    {
        var battleService = ServiceContainer?.TryResolve<IBattleService>();

        if (_castSkillButton != null)
        {
            var usable = false;
            var entries = battleService?.GetSkillMenuEntries(_targetUnitId);
            if (entries != null)
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    if (entries[i].IsUsable) { usable = true; break; }
                }
            }
            _castSkillButton.interactable = usable;
        }

        if (_switchCardButton != null)
            _switchCardButton.interactable = battleService?.GetBattleView()?.P1Reserve.Count > 0;
    }

    private void Subscribe()
    {
        if (_castSkillButton != null) _castSkillButton.onClick.AddListener(OnCastSkillClicked);
        if (_switchCardButton != null) _switchCardButton.onClick.AddListener(OnSwitchCardClicked);
        if (_emojiButton != null) _emojiButton.onClick.AddListener(OnEmojiClicked);
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void Unsubscribe()
    {
        if (_castSkillButton != null) _castSkillButton.onClick.RemoveListener(OnCastSkillClicked);
        if (_switchCardButton != null) _switchCardButton.onClick.RemoveListener(OnSwitchCardClicked);
        if (_emojiButton != null) _emojiButton.onClick.RemoveListener(OnEmojiClicked);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
    }

    private void OnCastSkillClicked() { CloseSelfAndOpen(_skillSelectPanelId); }

    private void OnSwitchCardClicked() { CloseSelfAndOpen(_cardSwitchPanelId); }

    private void OnEmojiClicked() { CloseSelfAndOpen(_emojiPanelId); }

    private void OnCloseClicked() { CloseSelf(); }

    /// <summary>先关闭自身再打开子面板（携带目标 UnitId）。</summary>
    private async void CloseSelfAndOpen(string panelId)
    {
        var unitId = _targetUnitId;
        CloseSelf();

        if (UIService != null && !string.IsNullOrEmpty(panelId))
            await UIService.OpenAsync<PanelBase>(panelId, unitId);
    }
}
