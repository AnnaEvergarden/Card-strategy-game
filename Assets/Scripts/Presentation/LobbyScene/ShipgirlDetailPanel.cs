using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 舰娘详情面板：显示卡牌大图、基础属性与技能图标。
/// 通过 UIService.OpenAsync 传入 CardConfig 作为参数打开。
/// </summary>
public sealed class ShipgirlDetailPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;

    [Header("Ship Info")]
    [SerializeField] private Image _shipImage;
    [SerializeField] private TextMeshProUGUI _displayNameText;
    [SerializeField] private TextMeshProUGUI _factionText;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _attackText;
    [SerializeField] private TextMeshProUGUI _defenseText;

    [Header("Skills")]
    [SerializeField] private Transform _skillContainer;
    [SerializeField] private GameObject _skillIconPrefab;

    private CardConfig _currentConfig;

    protected override void OnEnable() { base.OnEnable(); Subscribe(); }
    protected override void OnDisable() { Unsubscribe(); base.OnDisable(); }

    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);

        if (param is CardConfig config)
        {
            _currentConfig = config;
            await PopulateAsync();
        }
    }

    private async UniTask PopulateAsync()
    {
        if (_currentConfig == null) return;

        var config = _currentConfig;

        // 基础信息文本
        if (_displayNameText != null)
            _displayNameText.text = config.DisplayName;
        if (_factionText != null)
            _factionText.text = config.Faction.GetLocalizedName();
        if (_hpText != null)
            _hpText.text = config.BaseHP.ToString();
        if (_attackText != null)
            _attackText.text = config.BaseAttack.ToString();
        if (_defenseText != null)
            _defenseText.text = config.BaseDefense.ToString();

        // 舰娘大图（Portrait）
        if (_shipImage != null && config.PortraitResourceId > 0)
        {
            var sprite = await ResourceService.LoadAsync<Sprite>((ResourceId)config.PortraitResourceId);
            // await 后重新校验：面板可能已关闭/销毁。
            if (sprite != null && _shipImage != null)
                _shipImage.sprite = sprite;
        }

        // 技能图标 —— 根据 Card SkillIds 数量生成对应个数的 Prefab 实例，并绑定技能数据
        if (_skillContainer != null && _skillIconPrefab != null &&
            config.SkillIds != null && config.SkillIds.Count > 0)
        {
            // 清空旧图标
            for (int i = _skillContainer.childCount - 1; i >= 0; i--)
                Destroy(_skillContainer.GetChild(i).gameObject);

            var configService = ServiceContainer?.TryResolve<IConfigService>();
            var loadTasks = new List<UniTask>(config.SkillIds.Count);

            foreach (var skillId in config.SkillIds)
            {
                if (string.IsNullOrWhiteSpace(skillId)) continue;

                var skillConfig = configService?.Get<SkillConfig>(skillId);
                if (skillConfig == null) continue;

                // 生成一个 Prefab 实例
                var iconGo = Instantiate(_skillIconPrefab, _skillContainer, false);

                // 绑定技能配置数据（供后续悬浮框功能使用）
                var iconView = iconGo.GetComponent<SkillIconView>();
                if (iconView != null)
                    iconView.SkillConfig = skillConfig;

                // 加载技能图标 Sprite
                var iconImage = iconGo.GetComponentInChildren<Image>();
                if (iconImage != null && skillConfig.IconResourceId > 0)
                {
                    var resourceId = skillConfig.IconResourceId;
                    loadTasks.Add(LoadIconAsync(iconImage, resourceId));
                }
            }

            if (loadTasks.Count > 0)
                await UniTask.WhenAll(loadTasks);
        }
    }

    private async UniTask LoadIconAsync(Image image, int resourceId)
    {
        var sprite = await ResourceService.LoadAsync<Sprite>((ResourceId)resourceId);
        // await 后重新校验：图标可能已随技能列表重建被销毁。
        if (sprite != null && image != null)
            image.sprite = sprite;
    }

    public void OnClickBack() { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome() { UIService?.GoHomeAsync().Forget(); }

    private void Subscribe()
    {
        if (_backBtn) _backBtn.onClick.AddListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.AddListener(OnClickHome);
    }

    private void Unsubscribe()
    {
        if (_backBtn) _backBtn.onClick.RemoveListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.RemoveListener(OnClickHome);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_shipImage == null) Debug.LogWarning("[ShipgirlDetailPanel] _shipImage 未赋值", this);
        if (_displayNameText == null) Debug.LogWarning("[ShipgirlDetailPanel] _displayNameText 未赋值", this);
        if (_factionText == null) Debug.LogWarning("[ShipgirlDetailPanel] _factionText 未赋值", this);
        if (_hpText == null) Debug.LogWarning("[ShipgirlDetailPanel] _hpText 未赋值", this);
        if (_attackText == null) Debug.LogWarning("[ShipgirlDetailPanel] _attackText 未赋值", this);
        if (_defenseText == null) Debug.LogWarning("[ShipgirlDetailPanel] _defenseText 未赋值", this);
        if (_skillContainer == null) Debug.LogWarning("[ShipgirlDetailPanel] _skillContainer 未赋值", this);
        if (_skillIconPrefab == null) Debug.LogWarning("[ShipgirlDetailPanel] _skillIconPrefab 未赋值", this);
    }
#endif
}
