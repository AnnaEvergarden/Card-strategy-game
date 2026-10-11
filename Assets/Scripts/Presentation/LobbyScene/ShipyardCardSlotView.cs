using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 船坞面板卡牌槽位视图：显示单张卡牌的基础信息（Icon, Name, HP, Attack, Defense），点击打开详情面板。
/// </summary>
public sealed class ShipyardCardSlotView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _clickButton;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _attackText;
    [SerializeField] private TextMeshProUGUI _defenseText;

    public Action<CardConfig> OnSlotClicked { get; set; }

    private CardConfig _config;

    /// <summary>
    /// 设置槽位数据。异步加载图标，填充文本信息。
    /// </summary>
    public async UniTask SetData(CardConfig config, IResourceService resourceService)
    {
        if (config == null) return;
        _config = config;

        if (_nameText != null)
            _nameText.text = config.DisplayName;
        if (_hpText != null)
            _hpText.text = config.BaseHP.ToString();
        if (_attackText != null)
            _attackText.text = config.BaseAttack.ToString();
        if (_defenseText != null)
            _defenseText.text = config.BaseDefense.ToString();

        if (_iconImage != null && config.IconResourceId > 0)
        {
            var sprite = await resourceService.LoadAsync<Sprite>((ResourceId)config.IconResourceId);
            // await 后必须重新校验：视图可能已被销毁或已回收重绑（首次打开时异步返回常晚于销毁）。
            if (sprite != null && _iconImage != null && _config == config)
                _iconImage.sprite = sprite;
        }
    }

    private void Awake()
    {
        if (_clickButton != null)
            _clickButton.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (_clickButton != null)
            _clickButton.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        OnSlotClicked?.Invoke(_config);
    }

    private void OnValidate()
    {
        if (_iconImage == null) Debug.LogWarning("[ShipyardCardSlotView] _iconImage 未赋值", this);
        if (_clickButton == null) Debug.LogWarning("[ShipyardCardSlotView] _clickButton 未赋值", this);
        if (_nameText == null) Debug.LogWarning("[ShipyardCardSlotView] _nameText 未赋值", this);
        if (_hpText == null) Debug.LogWarning("[ShipyardCardSlotView] _hpText 未赋值", this);
        if (_attackText == null) Debug.LogWarning("[ShipyardCardSlotView] _attackText 未赋值", this);
        if (_defenseText == null) Debug.LogWarning("[ShipyardCardSlotView] _defenseText 未赋值", this);
    }
}
