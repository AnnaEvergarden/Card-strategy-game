using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 船坞舰娘槽位视图：绑定展示名、头像与稀有度等；头像路径由 <see cref="GameResourcePaths"/> 统一构建后加载。
/// </summary>
public sealed class ShipyardCardSlotView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 卡图。
    /// </summary>
    [SerializeField] private Image icon;

    /// <summary>
    /// 叠在卡图之上的装饰/背景（与 Icon 须为同一父节点下的兄弟）。未指定则不在运行时改层级。
    /// </summary>
    [SerializeField] private RectTransform iconOverlayBackground;

    /// <summary>
    /// 是否在运行时根据 iconOverlayBackground 自动调整兄弟顺序（使 Icon 先于其绘制、位于下层）。
    /// </summary>
    [SerializeField] private bool applyIconUnderOverlayAtAwake = true;

    /// <summary>
    /// 名称文本。
    /// </summary>
    [SerializeField] private TMP_Text nameText;

    /// <summary>
    /// 稀有度文本（可选）。
    /// </summary>
    [SerializeField] private TMP_Text rarityText;

    /// <summary>
    /// 输出绑定与图标 Resources 路径等调试信息。
    /// </summary>
    [SerializeField] private bool debugLog = false;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 将 Icon 排到遮挡背景之前绘制，使背景叠在卡图上（需 Prefab 中二者为兄弟节点）。
    /// </summary>
    private void Awake()
    {
        if (applyIconUnderOverlayAtAwake)
        {
            EnsureIconDrawnBelowOverlay();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 运行时绑定子节点引用（代码生成槽位时调用；Prefab 可在 Inspector 中直接赋值）。
    /// </summary>
    /// <param name="iconImage">卡图 Image。</param>
    /// <param name="name">展示名称的 TMP 文本。</param>
    /// <param name="hp">预留：生命值文本（当前未写入）。</param>
    /// <param name="atk">预留：攻击力文本（当前未写入）。</param>
    /// <param name="def">预留：防御文本（当前未写入）。</param>
    /// <param name="rarity">稀有度展示文本（可选）。</param>
    public void Wire(
        Image iconImage,
        TMP_Text name,
        TMP_Text hp = null,
        TMP_Text atk = null,
        TMP_Text def = null,
        TMP_Text rarity = null)
    {
        icon = iconImage;
        nameText = name;
        rarityText = rarity;
        if (applyIconUnderOverlayAtAwake)
        {
            EnsureIconDrawnBelowOverlay();
        }
    }

    /// <summary>
    /// 绑定一条卡牌数据；图标在 Bind 时按配置路径加载并显示数量后缀（多张时）。
    /// </summary>
    /// <param name="entry">收藏条目（含 cardId、数量等）。</param>
    /// <param name="config">卡牌配置；为 null 时仅尽量展示 id，并清空部分可选 UI。</param>
    public void Bind(CardCollectionStore.CardEntry entry, CardConfigSO config)
    {
        var id = entry != null ? entry.cardId : string.Empty;
        var display = config != null && !string.IsNullOrEmpty(config.DisplayName)
            ? config.DisplayName
            : id;

        if (nameText != null)
        {
            var count = entry != null ? entry.count : 0;
            nameText.text = count > 1 ? $"{display} x{count}" : display;
        }

        if (icon != null)
        {
            icon.preserveAspect = true;
            icon.sprite = LoadShipgirlIcon(config, id);
            icon.enabled = icon.sprite != null;
            if (debugLog)
            {
                Debug.Log($"[ShipyardSlot] Bind cardId={id} display={display} hasSprite={(icon.sprite != null)}");
            }
        }

        if (config != null)
        {
            if (rarityText != null)
            {
                rarityText.text = FormatRarity(config.Rarity);
            }
        }
        else
        {
            ClearOptionalStats();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 保证 Icon 的 sibling 索引小于遮挡背景：同父节点下先绘制 Icon、后绘制背景，实现背景盖住卡图边缘。
    /// </summary>
    private void EnsureIconDrawnBelowOverlay()
    {
        if (icon == null || iconOverlayBackground == null)
        {
            return;
        }

        var iconT = icon.transform;
        var overlayT = iconOverlayBackground;
        if (iconT.parent != overlayT.parent)
        {
            return;
        }

        var overlayIndex = overlayT.GetSiblingIndex();
        var iconIndex = iconT.GetSiblingIndex();
        if (iconIndex >= overlayIndex)
        {
            iconT.SetSiblingIndex(overlayIndex);
        }
    }

    /// <summary>
    /// 无配置时清空可选统计文本。
    /// </summary>
    private void ClearOptionalStats()
    {
        if (rarityText != null)
        {
            rarityText.text = string.Empty;
        }
    }

    /// <summary>
    /// 稀有度转为简短中文标签。
    /// </summary>
    /// <param name="rarity">卡牌稀有度枚举。</param>
    /// <returns>用于 UI 展示的两字或四字中文标签。</returns>
    private static string FormatRarity(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Normal:
                return "普通";
            case CardRarity.Rare:
                return "稀有";
            case CardRarity.Elite:
                return "精锐";
            case CardRarity.SuperRare:
                return "超稀有";
            case CardRarity.SeaLegend:
                return "海上传奇";
            case CardRarity.Activity:
                return "活动";
            default:
                return "普通";
        }
    }

    /// <summary>
    /// 舰娘头像：通过统一资源加载门面按英文名加载。
    /// </summary>
    /// <param name="config">卡牌配置（英文名、展示名）。</param>
    /// <param name="cardIdFallback">调试日志回退 id（英文名为空时仅用于输出日志）。</param>
    /// <returns>加载到的 Sprite；失败或未配置时为 null。</returns>
    private Sprite LoadShipgirlIcon(CardConfigSO config, string cardIdFallback)
    {
        var englishName = config != null ? config.EnglishName : string.Empty;
        if (string.IsNullOrWhiteSpace(englishName))
        {
            if (debugLog)
            {
                Debug.LogWarning($"[ShipyardSlot] LoadIcon 跳过：englishName 为空（cardId={cardIdFallback}）。");
            }

            return null;
        }

        var path = GameResourcePaths.BuildShipgirlIconPath(englishName);
        var sprite = GameResourceLoader.LoadShipgirlIcon(englishName);
        if (debugLog)
        {
            Debug.Log(sprite != null
                ? $"[ShipyardSlot] Icon 加载成功 path={path}"
                : $"[ShipyardSlot] Icon 加载失败（无 Sprite 或路径错误）path={path}");
        }

        return sprite;
    }

    #endregion
}
