using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能图标视图：挂载在 SkillIcon Prefab 上，绑定技能配置数据供后续悬浮框使用。
/// </summary>
public sealed class SkillIconView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;

    public SkillConfig SkillConfig { get; set; }

    public void SetIcon(Sprite sprite)
    {
        if (_iconImage != null)
            _iconImage.sprite = sprite;
    }
}
