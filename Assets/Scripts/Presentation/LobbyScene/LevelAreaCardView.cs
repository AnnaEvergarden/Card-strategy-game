using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 区域卡片视图：挂载在 Area Card 预制体上，显示区域信息并响应点击。
/// </summary>
public sealed class LevelAreaCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text _displayNameText;
    [SerializeField] private Button _selectBtn;

    public LevelAreaConfig AreaConfig { get; private set; }

    public void SetData(LevelAreaConfig config, UnityAction onClick)
    {
        AreaConfig = config;
        if (_displayNameText != null)
            _displayNameText.text = config.DisplayName;
        if (_selectBtn != null)
        {
            _selectBtn.onClick.RemoveAllListeners();
            _selectBtn.onClick.AddListener(onClick);
        }
    }
}