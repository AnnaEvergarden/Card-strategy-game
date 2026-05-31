using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 战斗技能单槽视图：技能图标、剩余次数与按钮；点击时抛出释放请求。
/// 由 <see cref="BattleSkillSelectPanel"/> 按舰娘技能数量实例化多个。
/// </summary>
public sealed class BattleSkillBarView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 技能图标。
    /// </summary>
    [SerializeField] private Image iconImage;

    /// <summary>
    /// 技能按钮。
    /// </summary>
    [SerializeField] private Button skillButton;

    /// <summary>
    /// 剩余可用次数文本（可选；无限制时可隐藏或显示「∞」）。
    /// </summary>
    [SerializeField] private TMP_Text usesCountText;

    /// <summary>
    /// 所属舰娘 UnitId。
    /// </summary>
    private string _boundUnitId = string.Empty;

    /// <summary>
    /// 所属舰娘 cardId（仅技能配置读取用）。
    /// </summary>
    private string _boundCardId = string.Empty;

    /// <summary>
    /// 在舰娘技能列表中的下标（0～2）。
    /// </summary>
    private int _skillIndex = -1;

    /// <summary>
    /// 当前绑定的技能 id。
    /// </summary>
    private string _skillId = string.Empty;

    /// <summary>
    /// 当前绑定的技能配置（刷新次数显示与释放校验）。
    /// </summary>
    private SkillConfigSO _boundSkill;

    /// <summary>
    /// 请求释放技能：参数为 (cardId, skillIndex, skillId)。
    /// </summary>
    [SerializeField] private SkillCastUnityEvent onSkillCastRequested = new SkillCastUnityEvent();

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 绑定按钮点击。
    /// </summary>
    private void Awake()
    {
        if (skillButton != null)
        {
            skillButton.onClick.AddListener(OnSkillButtonClicked);
        }
    }

    /// <summary>
    /// 移除监听。
    /// </summary>
    private void OnDestroy()
    {
        if (skillButton != null)
        {
            skillButton.onClick.RemoveListener(OnSkillButtonClicked);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 绑定单个技能槽：图标、剩余次数与按钮可用状态。
    /// </summary>
    /// <param name="unitId">舰娘 UnitId。</param>
    /// <param name="cardId">舰娘 cardId（剩余次数查询用）。</param>
    /// <param name="skillIndex">技能下标。</param>
    /// <param name="skill">技能配置；为 null 时按钮不可用。</param>
    public void Bind(string unitId, string cardId, int skillIndex, SkillConfigSO skill)
    {
        _boundUnitId = string.IsNullOrWhiteSpace(unitId) ? string.Empty : unitId.Trim();
        _boundCardId = string.IsNullOrWhiteSpace(cardId) ? string.Empty : cardId.Trim();
        _skillIndex = skillIndex;
        _boundSkill = skill;
        _skillId = skill != null && !string.IsNullOrWhiteSpace(skill.SkillId) ? skill.SkillId.Trim() : string.Empty;

        ApplySkillIcon(skill);
        RefreshUsesAndInteractable();

        if (skillButton != null && iconImage != null && skillButton.targetGraphic == null)
        {
            skillButton.targetGraphic = iconImage;
        }
    }

    /// <summary>
    /// 根据 <see cref="BattleFieldState"/> 刷新剩余次数与按钮状态（释放技能后调用）。
    /// </summary>
    public void RefreshUsesAndInteractable()
    {
        if (skillButton != null && !string.IsNullOrEmpty(_boundUnitId))
        {
            skillButton.interactable = BattleContext.Current.Field.CanUseSkill(_boundUnitId, _boundSkill);
        }

        ApplyUsesCountText();
    }

    /// <summary>
    /// 当前绑定的技能配置 id。
    /// </summary>
    public string BoundSkillId => _skillId;

    /// <summary>
    /// 当前绑定的 cardId。
    /// </summary>
    public string BoundCardId => _boundCardId;

    /// <summary>
    /// 当前技能在舰娘技能列表中的下标。
    /// </summary>
    public int SkillIndex => _skillIndex;

    #endregion

    #region Private Methods

    /// <summary>
    /// 按 <see cref="SkillConfigSO.ResolveSkillIconResourcePath"/> 加载技能图标；缺失时使用占位图。
    /// </summary>
    private void ApplySkillIcon(SkillConfigSO skill)
    {
        if (iconImage == null)
        {
            return;
        }

        GameResourceLoader.ApplySkillIconToImage(iconImage, skill, logOnMissing: false);
    }

    /// <summary>
    /// 刷新剩余次数文案：有限次数显示数字，无限制显示「∞」或隐藏。
    /// </summary>
    private void ApplyUsesCountText()
    {
        if (usesCountText == null)
        {
            return;
        }

        if (_boundSkill == null || string.IsNullOrEmpty(_skillId))
        {
            usesCountText.text = string.Empty;
            usesCountText.gameObject.SetActive(false);
            return;
        }

        if (_boundSkill.IsUnlimitedUses)
        {
            usesCountText.text = "∞";
            usesCountText.gameObject.SetActive(true);
            return;
        }

        var unit = BattleContext.Current.Field.GetUnit(_boundUnitId);
        var remaining = unit?.GetSkillRemainingUses(_skillId) ?? 0;
        usesCountText.text = remaining.ToString();
        usesCountText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 点击技能槽：交由 <see cref="BattleFacade"/> 编排（选目标、执行、扣次数与冷却）。
    /// </summary>
    private void OnSkillButtonClicked()
    {
        if (string.IsNullOrEmpty(_boundUnitId) || string.IsNullOrEmpty(_skillId) || _skillIndex < 0)
        {
            return;
        }

        BattleFacade.TryCastSkill(_boundUnitId, _skillIndex, _skillId);
        RefreshUsesAndInteractable();
        onSkillCastRequested?.Invoke(_boundUnitId, _skillIndex, _skillId);
    }

    #endregion

    /// <summary>
    /// 技能释放请求事件（Inspector 可绑定）。
    /// </summary>
    [System.Serializable]
    public sealed class SkillCastUnityEvent : UnityEvent<string, int, string>
    {
    }
}
