using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 建造卡池按钮视图：挂载在 Pool Button 预制体上，显示卡池图标并响应选中点击。
/// </summary>
public sealed class BuildPoolButtonView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _selectBtn;

    public BuildPoolConfig PoolConfig { get; private set; }

    public void SetData(BuildPoolConfig config, UnityAction onClick)
    {
        PoolConfig = config;
        if (_selectBtn != null)
        {
            _selectBtn.onClick.RemoveAllListeners();
            _selectBtn.onClick.AddListener(onClick);
        }
    }

    public void SetIcon(Sprite sprite)
    {
        if (_iconImage != null)
            _iconImage.sprite = sprite;
    }
}