using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌展示项组件：挂在 Card Item Prefab 上，通过序列化引用绑定 UI 控件。
/// </summary>
public sealed class CardItemView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _attackText;
    [SerializeField] private TMP_Text _defenseText;

    public async UniTask SetData(CardConfig card, IResourceService resourceService)
    {
        if (card == null) return;

        if (_nameText != null) _nameText.text = card.DisplayName;
        if (_hpText != null) _hpText.text = card.BaseHP.ToString();
        if (_attackText != null) _attackText.text = card.BaseAttack.ToString();
        if (_defenseText != null) _defenseText.text = card.BaseDefense.ToString();

        if (_iconImage != null && card.IconResourceId > 0)
        {
            var sprite = await resourceService.LoadAsync<Sprite>((ResourceId)card.IconResourceId);
            // await 后重新校验：视图可能已随列表重建/面板关闭被销毁。
            if (sprite != null && _iconImage != null)
                _iconImage.sprite = sprite;
        }
    }
}