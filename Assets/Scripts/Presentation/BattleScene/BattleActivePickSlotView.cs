using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 首发选择槽位视图（对应 BattleActivePickSlotView.prefab）：
/// 显示编队成员的名称与生命，并显示选择先后序号（①②）。点击回调给面板。
/// 预制体子物体按名解析（Name_Text / HP_Text / Order / Order_Text / Border），
/// 缺少按钮时在根节点动态补一个，保证整块槽位可点击。
/// </summary>
public sealed class BattleActivePickSlotView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private RectTransform pickOrderMaskRoot;
    [SerializeField] private TMP_Text pickOrderText;
    [SerializeField] private Button selectButton;

    /// <summary>绑定的卡牌 ID（配置表 ID）。</summary>
    public string BoundCardId { get; private set; } = string.Empty;

    /// <summary>点击回调（面板订阅）。</summary>
    public event Action<BattleActivePickSlotView> Clicked;

    /// <summary>当前选择序号（1 起，0 表示未选中）。</summary>
    public int PickOrder { get; private set; }

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
    /// 绑定卡牌数据（名称/生命；头像资源暂由预制体自身占位图承担）。
    /// </summary>
    /// <param name="cardId">配置表卡牌 ID</param>
    /// <param name="displayName">显示名</param>
    /// <param name="hp">最大生命</param>
    public void Bind(string cardId, string displayName, int hp)
    {
        BoundCardId = cardId ?? string.Empty;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(displayName) ? cardId : displayName;
        if (hpText != null)
            hpText.text = $"HP {hp}";

        SetPickSelectionOrder(0);
    }

    /// <summary>
    /// 设置选择序号：>0 显示序号与遮罩，0 隐藏。
    /// </summary>
    /// <param name="orderOneBased">序号（1 起；0 = 未选中）</param>
    public void SetPickSelectionOrder(int orderOneBased)
    {
        PickOrder = orderOneBased;

        if (pickOrderText != null)
            pickOrderText.text = orderOneBased > 0 ? orderOneBased.ToString() : string.Empty;
        if (pickOrderMaskRoot != null)
            pickOrderMaskRoot.gameObject.SetActive(orderOneBased > 0);
    }

    /// <summary>按子物体名解析预制体控件（已序列化赋值时跳过）。</summary>
    private void ResolveChildren()
    {
        var root = transform;
        if (nameText == null) nameText = FindText(root, "Name_Text");
        if (hpText == null) hpText = FindText(root, "HP_Text");
        if (pickOrderMaskRoot == null) pickOrderMaskRoot = FindRect(root, "Order");
        if (pickOrderText == null) pickOrderText = FindText(root, "Order_Text");
        if (pickOrderMaskRoot == null) pickOrderMaskRoot = pickOrderText != null
            ? pickOrderText.rectTransform
            : null;

        EnsureSelectable();
    }

    /// <summary>预制体没有按钮时，在根节点补一个整块可点击按钮。</summary>
    private void EnsureSelectable()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>();
        if (selectButton == null)
        {
            var image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = image.color.a <= 0f ? new Color(1f, 1f, 1f, 0f) : image.color;
            selectButton = gameObject.AddComponent<Button>();
            selectButton.transition = Selectable.Transition.None;
        }

        selectButton.onClick.RemoveListener(OnClick);
        selectButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Clicked?.Invoke(this);
    }

    private static TMP_Text FindText(Transform root, string childName)
    {
        var child = root.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static RectTransform FindRect(Transform root, string childName)
    {
        var child = root.Find(childName);
        return child as RectTransform;
    }
}
