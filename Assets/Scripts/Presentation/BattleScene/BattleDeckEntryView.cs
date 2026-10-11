using System;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编队条目视图（对应 BattleDeckEntryView.prefab）：
/// Name_Text 仅显示编队名称；成员卡牌用 ShipIcon 槽位显示卡牌图标（按 IconResourceId 异步加载），
/// 点击回调携带编队序号。预制体子物体按名解析（Name_Text / ShipIcon* / IconRoot / Select_Button）。
/// </summary>
public sealed class BattleDeckEntryView : MonoBehaviour
{
    /// <summary>成员图标槽位名（预制体中 ShipIcon 及其副本命名）。</summary>
    private static readonly string[] IconSlotNames =
    {
        "ShipIcon", "ShipIcon (1)", "ShipIcon (2)", "ShipIcon (3)", "ShipIcon (4)", "ShipIcon (5)",
    };

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject selectMark;

    private readonly List<Image> _iconSlots = new List<Image>();

    /// <summary>绑定的编队序号。</summary>
    public int BoundGroupIndex { get; private set; } = -1;

    /// <summary>点击回调（携带编队序号）。</summary>
    public event Action<int> Clicked;

    private void Awake()
    {
        ResolveChildren();
        ResolveIconSlots();
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// 绑定编队名称（Name_Text 仅显示编队名，不再拼接成员文字）。
    /// </summary>
    /// <param name="groupIndex">编队序号</param>
    /// <param name="groupName">编队名称</param>
    public void BindName(int groupIndex, string groupName)
    {
        BoundGroupIndex = groupIndex;

        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(groupName) ? $"编队 {groupIndex + 1}" : groupName;

        SetSelected(false);
    }

    /// <summary>
    /// 绑定成员图标：按 IconResourceId 加载 Sprite 填入 ShipIcon 槽位（无成员的槽位隐藏）。
    /// </summary>
    /// <param name="memberIconResourceIds">成员头像资源 ID 列表（按编队顺序）</param>
    /// <param name="resourceService">资源服务</param>
    public async UniTaskVoid BindIconsAsync(IReadOnlyList<int> memberIconResourceIds, IResourceService resourceService)
    {
        if (_iconSlots.Count == 0)
            ResolveIconSlots();

        for (var i = 0; i < _iconSlots.Count; i++)
        {
            var slot = _iconSlots[i];
            if (slot == null)
                continue;

            var hasMember = memberIconResourceIds != null && i < memberIconResourceIds.Count
                            && memberIconResourceIds[i] > 0;
            slot.gameObject.SetActive(hasMember);
            if (!hasMember || resourceService == null)
                continue;

            var sprite = await resourceService.LoadAsync<Sprite>((ResourceId)memberIconResourceIds[i]);

            // await 后重新校验：视图可能已随列表重建被销毁。
            if (sprite != null && slot != null)
                slot.sprite = sprite;
        }
    }

    /// <summary>解析成员图标槽位：优先 IconRoot 的子物体，其次按 ShipIcon 命名查找。</summary>
    private void ResolveIconSlots()
    {
        _iconSlots.Clear();

        var iconRoot = transform.Find("IconRoot");
        if (iconRoot != null && iconRoot.childCount > 0)
        {
            for (var i = 0; i < iconRoot.childCount; i++)
            {
                var image = iconRoot.GetChild(i).GetComponent<Image>();
                if (image != null)
                    _iconSlots.Add(image);
            }
        }

        if (_iconSlots.Count > 0)
            return;

        for (var i = 0; i < IconSlotNames.Length; i++)
        {
            var node = transform.Find(IconSlotNames[i]);
            if (node == null)
                continue;

            var image = node.GetComponent<Image>();
            if (image != null)
                _iconSlots.Add(image);
        }
    }

    /// <summary>设置选中标记（可选，预制体含 Select_Button 时用于高亮）。</summary>
    public void SetSelected(bool selected)
    {
        if (selectMark != null)
            selectMark.SetActive(selected);
    }

    private void ResolveChildren()
    {
        var root = transform;
        if (nameText == null) nameText = FindText(root, "Name_Text");

        if (selectButton == null)
            selectButton = GetComponent<Button>();
        if (selectButton == null)
        {
            var selectNode = root.Find("Select_Button");
            if (selectNode != null) selectButton = selectNode.GetComponent<Button>();
        }
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
        if (BoundGroupIndex >= 0)
            Clicked?.Invoke(BoundGroupIndex);
    }

    private static TMP_Text FindText(Transform root, string childName)
    {
        var child = root.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }
}
