using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 替补槽位视图（对应 BattleCardSwitchReserveSlotView.prefab）：
/// 显示替补舰娘名称与生命，支持选中高亮与禁用遮罩，点击回调给换牌面板。
/// 预制体子物体按名解析（Name_Text / HP_Text / HighLight / DisableMask）。
/// </summary>
public sealed class BattleCardSwitchReserveSlotView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private GameObject highlightMark;
    [SerializeField] private GameObject disableMask;
    [SerializeField] private Button selectButton;

    /// <summary>绑定的替补单位 UnitId。</summary>
    public string BoundUnitId { get; private set; } = string.Empty;

    /// <summary>点击回调（携带 UnitId）。</summary>
    public event Action<string> Clicked;

    private void Awake()
    {
        ResolveChildren();
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// 绑定替补数据。
    /// </summary>
    public void Bind(string unitId, string displayName, int hp, int maxHp)
    {
        BoundUnitId = unitId ?? string.Empty;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(displayName) ? unitId : displayName;
        if (hpText != null)
            hpText.text = $"HP {hp}/{maxHp}";

        SetSelected(false);
        SetDisabled(false);
    }

    /// <summary>设置选中高亮（点击选择时）。</summary>
    public void SetSelected(bool selected)
    {
        if (highlightMark != null)
            highlightMark.SetActive(selected);
    }

    /// <summary>设置禁用遮罩（不可选择时）。</summary>
    public void SetDisabled(bool disabled)
    {
        if (disableMask != null)
            disableMask.SetActive(disabled);
        if (selectButton != null)
            selectButton.interactable = !disabled;
    }

    private void ResolveChildren()
    {
        var root = transform;
        if (nameText == null) nameText = FindText(root, "Name_Text");
        if (hpText == null) hpText = FindText(root, "HP_Text");

        if (highlightMark == null)
        {
            var highlight = root.Find("HighLight");
            if (highlight != null) highlightMark = highlight.gameObject;
        }

        if (disableMask == null)
        {
            var mask = root.Find("DisableMask");
            if (mask != null) disableMask = mask.gameObject;
        }

        if (selectButton == null)
            selectButton = GetComponent<Button>();
        if (selectButton == null)
            selectButton = GetComponentInChildren<Button>(true);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnClick);
            selectButton.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        Clicked?.Invoke(BoundUnitId);
    }

    private static TMP_Text FindText(Transform root, string childName)
    {
        var child = root.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }
}
