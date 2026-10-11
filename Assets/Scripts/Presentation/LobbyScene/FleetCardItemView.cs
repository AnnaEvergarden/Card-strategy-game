using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编队选择面板中的卡牌展示项。
/// </summary>
public sealed class FleetCardItemView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _attackText;
    [SerializeField] private TMP_Text _defenseText;
    [SerializeField] private GameObject _selectionOverlay;
    [SerializeField] private TMP_Text _slotNumberText;
    [SerializeField] private Button _clickBtn;

    public CardInstance CardInstance { get; private set; }
    public CardConfig CardConfig { get; private set; }

    public void SetData(CardInstance instance, CardConfig config, IResourceService resourceService)
    {
        CardInstance = instance;
        CardConfig = config;

        if (_nameText != null) _nameText.text = config.DisplayName;
        if (_hpText != null) _hpText.text = config.BaseHP.ToString();
        if (_attackText != null) _attackText.text = config.BaseAttack.ToString();
        if (_defenseText != null) _defenseText.text = config.BaseDefense.ToString();

        if (_iconImage != null && config.IconResourceId > 0)
            LoadIconAsync(config.IconResourceId, resourceService).Forget();

        SetDeselected();
    }

    private async UniTask LoadIconAsync(int resourceId, IResourceService resourceService)
    {
        var sprite = await resourceService.LoadAsync<Sprite>((ResourceId)resourceId);
        if (sprite != null && _iconImage != null)
            _iconImage.sprite = sprite;
    }

    public void SetSelected(int slotNumber)
    {
        if (_selectionOverlay != null) _selectionOverlay.SetActive(true);
        if (_slotNumberText != null) _slotNumberText.text = slotNumber > 0 ? slotNumber.ToString() : "";
    }

    public void SetDeselected()
    {
        if (_selectionOverlay != null) _selectionOverlay.SetActive(false);
        if (_slotNumberText != null) _slotNumberText.text = "";
    }

    public void SetListener(UnityEngine.Events.UnityAction onClick)
    {
        if (_clickBtn != null)
        {
            _clickBtn.onClick.RemoveAllListeners();
            _clickBtn.onClick.AddListener(onClick);
        }
    }
}