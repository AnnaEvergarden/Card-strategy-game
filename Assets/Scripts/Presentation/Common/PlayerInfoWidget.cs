using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 大厅玩家信息挂件：显示头像、等级与名称；点击头像触发回调（打开玩家信息面板）。
/// 支持 [SerializeField] 绑定（便于后续替换正式美术）；为空时由 <see cref="Create"/> 代码构建。
/// </summary>
public sealed class PlayerInfoWidget : MonoBehaviour
{
    [SerializeField] private Button _avatarButton;
    [SerializeField] private Image _avatarImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _levelText;

    /// <summary>点击头像回调（由大厅面板订阅）。</summary>
    public event Action AvatarClicked;

    private void Awake()
    {
        if (_avatarButton == null)
            _avatarButton = GetComponentInChildren<Button>(true);
        if (_avatarButton != null)
        {
            _avatarButton.onClick.RemoveListener(OnClickAvatar);
            _avatarButton.onClick.AddListener(OnClickAvatar);
        }
    }

    private void OnDestroy()
    {
        if (_avatarButton != null)
            _avatarButton.onClick.RemoveListener(OnClickAvatar);
    }

    private void OnClickAvatar() => AvatarClicked?.Invoke();

    /// <summary>
    /// 代码构建挂件（头像按钮 + 名称 + 等级），置于父节点左上角。
    /// </summary>
    public static PlayerInfoWidget Create(RectTransform parent)
    {
        var rootObject = new GameObject("PlayerInfoWidget", typeof(RectTransform), typeof(PlayerInfoWidget));
        var root = (RectTransform)rootObject.transform;
        root.SetParent(parent, false);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(24f, -20f);
        root.sizeDelta = new Vector2(320f, 84f);

        var widget = rootObject.GetComponent<PlayerInfoWidget>();

        // 头像（可点击）
        var avatarObject = new GameObject("Avatar", typeof(RectTransform), typeof(Image), typeof(Button));
        var avatarRect = (RectTransform)avatarObject.transform;
        avatarRect.SetParent(root, false);
        avatarRect.anchorMin = new Vector2(0f, 1f);
        avatarRect.anchorMax = new Vector2(0f, 1f);
        avatarRect.pivot = new Vector2(0f, 1f);
        avatarRect.anchoredPosition = Vector2.zero;
        avatarRect.sizeDelta = new Vector2(76f, 76f);

        widget._avatarImage = avatarObject.GetComponent<Image>();
        widget._avatarImage.color = new Color(0.25f, 0.3f, 0.4f, 0.95f);
        widget._avatarButton = avatarObject.GetComponent<Button>();
        widget._avatarButton.targetGraphic = widget._avatarImage;

        // 名称
        widget._nameText = PanelWidgetUtil.CreateLabel(root, "Name_Text", "指挥官", 18f,
            new Vector2(88f, 0f), new Vector2(220f, 26f));
        var nameRect = (RectTransform)widget._nameText.transform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(0f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(88f, 0f);
        widget._nameText.alignment = TextAlignmentOptions.Left;

        // 等级
        widget._levelText = PanelWidgetUtil.CreateLabel(root, "Level_Text", "Lv.1", 16f,
            new Vector2(88f, -30f), new Vector2(220f, 24f));
        var levelRect = (RectTransform)widget._levelText.transform;
        levelRect.anchorMin = new Vector2(0f, 1f);
        levelRect.anchorMax = new Vector2(0f, 1f);
        levelRect.pivot = new Vector2(0f, 1f);
        levelRect.anchoredPosition = new Vector2(88f, -30f);
        widget._levelText.alignment = TextAlignmentOptions.Left;
        widget._levelText.color = new Color(1f, 0.9f, 0.6f);

        return widget;
    }

    /// <summary>
    /// 按玩家资料刷新显示（名称 + 等级）。
    /// </summary>
    public async UniTaskVoid RefreshAsync(IPlayerProfileService profileService)
    {
        if (profileService == null)
            return;

        var profile = await profileService.GetProfileAsync();

        // await 后重新校验：挂件可能已随面板销毁。
        if (profile == null || this == null)
            return;

        if (_nameText != null)
            _nameText.text = profile.PlayerName;
        if (_levelText != null)
            _levelText.text = $"Lv.{profile.Level}";
    }
}
