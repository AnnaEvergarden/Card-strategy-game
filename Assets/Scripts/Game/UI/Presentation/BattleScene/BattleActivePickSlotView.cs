using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗出战挑选槽位：展示舰娘图标、名称与生命值；支持出战顺序遮罩。
/// </summary>
public sealed class BattleActivePickSlotView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 当前绑定的卡牌 id（刷新出战序号时使用）。
    /// </summary>
    public string BoundCardId { get; private set; } = string.Empty;

    /// <summary>
    /// 舰娘头像。
    /// </summary>
    [SerializeField] private Image iconImage;

    /// <summary>
    /// 舰娘展示名称。
    /// </summary>
    [SerializeField] private TMP_Text nameText;

    /// <summary>
    /// 生命值文本。
    /// </summary>
    [SerializeField] private TMP_Text hpText;

    /// <summary>
    /// 出战序号遮罩根节点（可选；未指定时在首次需要时创建）。
    /// </summary>
    [SerializeField] private RectTransform pickOrderMaskRoot;

    /// <summary>
    /// 出战序号文本（可选）。
    /// </summary>
    [SerializeField] private TMP_Text pickOrderText;

    /// <summary>
    /// 遮罩半透明底色。
    /// </summary>
    [SerializeField] private Color pickOrderMaskColor = new(0f, 0f, 0f, 0.45f);

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 校正遮罩引用，避免 Inspector 误绑到槽位根导致 SetActive 关掉整卡。
    /// </summary>
    private void Awake()
    {
        ResolvePickOrderMaskRoot();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 绑定舰娘展示数据（图标、名称、生命）。
    /// </summary>
    /// <param name="cardId">卡牌配置 id。</param>
    /// <param name="config">卡牌配置；为 null 时仅展示 id 并清空数值与图标。</param>
    public void Bind(string cardId, CardConfigSO config)
    {
        BoundCardId = string.IsNullOrWhiteSpace(cardId) ? string.Empty : cardId.Trim();

        var display = config != null && !string.IsNullOrWhiteSpace(config.DisplayName)
            ? config.DisplayName
            : BoundCardId;

        if (nameText != null)
        {
            nameText.text = display;
        }

        if (hpText != null)
        {
            hpText.text = config != null ? config.HP.ToString() : string.Empty;
        }

        if (iconImage != null)
        {
            var englishName = config != null ? config.EnglishName : null;
            GameResourceLoader.ApplyShipgirlIconToImage(iconImage, englishName, logOnMissing: false);
        }
    }

    /// <summary>
    /// 更新出战挑选序号遮罩（0 表示未选中并隐藏遮罩）。
    /// </summary>
    /// <param name="orderOneBased">出战顺序 1～N；0 为隐藏。</param>
    public void SetPickSelectionOrder(int orderOneBased)
    {
        ResolvePickOrderMaskRoot();

        if (orderOneBased <= 0)
        {
            if (TryGetSafePickOrderMaskRoot(out var maskRoot))
            {
                maskRoot.gameObject.SetActive(false);
            }

            return;
        }

        EnsurePickOrderMarkUi();
        if (TryGetSafePickOrderMaskRoot(out var activeMask))
        {
            activeMask.gameObject.SetActive(true);
        }

        if (pickOrderText != null)
        {
            pickOrderText.text = orderOneBased.ToString();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 若遮罩误绑为槽位根，则改为子节点 <c>Order</c>（与 Prefab 约定一致）。
    /// </summary>
    private void ResolvePickOrderMaskRoot()
    {
        if (pickOrderMaskRoot != null && pickOrderMaskRoot != transform)
        {
            return;
        }

        var order = transform.Find("Order");
        if (order != null)
        {
            pickOrderMaskRoot = order as RectTransform;
        }
    }

    /// <summary>
    /// 仅当遮罩节点不是本槽位根时才允许 SetActive，防止误配置关掉整张卡。
    /// </summary>
    private bool TryGetSafePickOrderMaskRoot(out RectTransform maskRoot)
    {
        maskRoot = pickOrderMaskRoot;
        if (maskRoot == null || maskRoot == transform)
        {
            return false;
        }

        return maskRoot.gameObject != gameObject;
    }

    /// <summary>
    /// 确保出战序号遮罩 UI 存在（铺满槽位根，不拦截点击）。
    /// </summary>
    private void EnsurePickOrderMarkUi()
    {
        var rootRt = transform as RectTransform;
        if (rootRt == null)
        {
            return;
        }

        if (pickOrderMaskRoot == null || pickOrderMaskRoot == transform)
        {
            var go = new GameObject("PickOrderMark", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(rootRt, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = pickOrderMaskColor;
            img.raycastTarget = false;
            pickOrderMaskRoot = rt;
            pickOrderMaskRoot.gameObject.SetActive(false);
        }

        if (pickOrderText == null && pickOrderMaskRoot != null)
        {
            var textGo = new GameObject("OrderText", typeof(RectTransform));
            var tr = textGo.GetComponent<RectTransform>();
            tr.SetParent(pickOrderMaskRoot, false);
            tr.anchorMin = new Vector2(1f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(1f, 1f);
            tr.anchoredPosition = new Vector2(-8f, -8f);
            tr.sizeDelta = new Vector2(56f, 56f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 36f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            pickOrderText = tmp;
        }
    }

    #endregion
}
