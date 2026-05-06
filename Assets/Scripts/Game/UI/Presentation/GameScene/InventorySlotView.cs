using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仓库格子视图：负责将单个道具数据绑定到格子 UI。
/// </summary>
public sealed class InventorySlotView : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// 背景图片引用。
    /// </summary>
    [SerializeField] private Image backGround;

    /// <summary>
    /// 图标图片引用。
    /// </summary>
    [SerializeField] private Image icon;

    /// <summary>
    /// 数量文本引用。
    /// </summary>
    [SerializeField] private TMP_Text countText;

    #endregion

    #region Public API

    /// <summary>
    /// 绑定道具数据和配置到格子。
    /// </summary>
    public void Bind(InventoryStore.InventoryItemData data, ItemConfigSO config)
    {
        var hasData = data != null && data.count > 0;

        if (icon != null)
        {
            icon.enabled = hasData;
            icon.sprite = hasData ? LoadIconByConfig(config) : null;
        }

        if (countText != null)
        {
            countText.text = hasData ? data.count.ToString() : string.Empty;
        }

        if (backGround != null)
        {
            backGround.color = hasData ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 根据道具配置中的 Resources 路径读取图标（通过统一加载门面）。
    /// </summary>
    private static Sprite LoadIconByConfig(ItemConfigSO config)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.IconResourcePath))
        {
            return null;
        }

        var path = config.IconResourcePath;
        return GameResourceLoader.LoadSprite(path);
    }

    #endregion
}

