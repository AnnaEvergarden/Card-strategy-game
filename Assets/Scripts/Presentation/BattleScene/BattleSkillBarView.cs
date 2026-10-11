using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能槽视图（对应 BattleSkillBarView.prefab）：图标按钮 + 可释放状态，点击回调技能 ID。
/// 预制体子物体按名解析（SKill_Button / Icon）；头像/图标资源由面板按需注入。
/// </summary>
public sealed class BattleSkillBarView : MonoBehaviour
{
    [SerializeField] private Button skillButton;
    [SerializeField] private Image iconImage;

    /// <summary>绑定的施法单位 UnitId。</summary>
    public string BoundCasterUnitId { get; private set; } = string.Empty;

    /// <summary>绑定的技能 ID。</summary>
    public string BoundSkillId { get; private set; } = string.Empty;

    /// <summary>点击回调（携带技能 ID）。</summary>
    public event Action<string> SkillClicked;

    private void Awake()
    {
        ResolveChildren();
    }

    private void OnDestroy()
    {
        if (skillButton != null)
            skillButton.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// 绑定技能：记录施法者与技能 ID，并按可用性置灰、设置图标。
    /// </summary>
    /// <param name="casterUnitId">施法单位</param>
    /// <param name="skillId">技能 ID</param>
    /// <param name="usable">是否可释放（冷却/次数通过）</param>
    /// <param name="iconSprite">图标（可选）</param>
    public void Bind(string casterUnitId, string skillId, bool usable, Sprite iconSprite = null)
    {
        BoundCasterUnitId = casterUnitId ?? string.Empty;
        BoundSkillId = skillId ?? string.Empty;

        if (iconImage != null && iconSprite != null)
            iconImage.sprite = iconSprite;

        if (skillButton != null)
            skillButton.interactable = usable;
    }

    /// <summary>设置图标（资源加载完成后回填）。</summary>
    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null && sprite != null)
            iconImage.sprite = sprite;
    }

    private void ResolveChildren()
    {
        var root = transform;
        if (skillButton == null)
        {
            var buttonNode = root.Find("SKill_Button") ?? root.Find("Skill_Button");
            if (buttonNode != null) skillButton = buttonNode.GetComponent<Button>();
        }
        if (skillButton == null)
            skillButton = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);

        if (iconImage == null)
        {
            var iconNode = root.Find("Icon");
            if (iconNode != null) iconImage = iconNode.GetComponent<Image>();
        }
        if (iconImage == null && skillButton != null)
            iconImage = skillButton.GetComponentInChildren<Image>(true);

        if (skillButton != null)
        {
            skillButton.onClick.RemoveListener(OnClick);
            skillButton.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (!string.IsNullOrEmpty(BoundSkillId))
            SkillClicked?.Invoke(BoundSkillId);
    }
}
