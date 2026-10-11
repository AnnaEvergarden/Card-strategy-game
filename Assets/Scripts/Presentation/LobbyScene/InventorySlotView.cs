using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仓库面板道具槽位视图：显示单条道具的图标和数量。
/// </summary>
public sealed class InventorySlotView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _quantityText;

    /// <summary>
    /// 设置槽位数据。
    /// </summary>
    /// <param name="config">道具静态配置</param>
    /// <param name="quantity">持有数量</param>
    /// <param name="resourceService">资源服务，用于异步加载图标</param>
    public async UniTask SetData(ItemConfig config, int quantity, IResourceService resourceService)
    {
        if (config == null) return;

        if (_quantityText != null)
            _quantityText.text = quantity.ToString();

        if (_iconImage != null && config.IconResourceId > 0)
        {
            var sprite = await resourceService.LoadAsync<Sprite>((ResourceId)config.IconResourceId);
            // await 后重新校验：视图可能已随列表重建/面板关闭被销毁。
            if (sprite != null && _iconImage != null)
                _iconImage.sprite = sprite;
        }
    }

    private void OnValidate()
    {
        if (_iconImage == null) Debug.LogWarning("[InventorySlotView] _iconImage 未赋值", this);
        if (_quantityText == null) Debug.LogWarning("[InventorySlotView] _quantityText 未赋值", this);
    }
}