using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 区域选择面板：读取配置并按常驻/活动分类展示区域卡片，点击卡片进入关卡列表面板。
/// </summary>
public sealed class LevelAreaSelectPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;
    [SerializeField] private Button _prevAreaBtn;
    [SerializeField] private Button _nextAreaBtn;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _pagesRoot;
    [SerializeField] private TMP_Text _categoryTitleText;

    [Header("Area Cards")]
    [SerializeField] private GameObject _areaCardPrefab;

    [Header("面板 ID")]
    [SerializeField] private string _stageSelectPanelId = "LevelStageSelect";

    private LevelAreaCategory _currentCategory;

    protected override void OnEnable() { base.OnEnable(); Subscribe(); }
    protected override void OnDisable() { Unsubscribe(); base.OnDisable(); }

    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);

        if (param is LevelAreaCategory category)
        {
            _currentCategory = category;
            await PopulateAreasAsync();
        }
    }

    private UniTask PopulateAreasAsync()
    {
        // 清空旧卡片
        for (int i = _pagesRoot.childCount - 1; i >= 0; i--)
            Destroy(_pagesRoot.GetChild(i).gameObject);

        // 分类标题
        if (_categoryTitleText != null)
        {
            _categoryTitleText.text = _currentCategory == LevelAreaCategory.Permanent
                ? "常驻关卡" : "活动关卡";
        }

        // 从导出的配置表中读取该分类下的区域
        var configService = ServiceContainer?.TryResolve<IConfigService>();
        var allAreas = configService?.GetAll<LevelAreaConfig>();
        if (allAreas == null || allAreas.Count == 0)
        {
            Debug.Log("[LevelAreaSelectPanel] no level area configs loaded");
            return UniTask.CompletedTask;
        }

        // 按分类筛选
        var filtered = new List<LevelAreaConfig>(allAreas.Count);
        for (int i = 0; i < allAreas.Count; i++)
        {
            var area = allAreas[i];
            if (area != null && area.Category == _currentCategory)
                filtered.Add(area);
        }

        if (filtered.Count == 0)
        {
            Debug.Log("[LevelAreaSelectPanel] no areas found for category " + _currentCategory);
            return UniTask.CompletedTask;
        }

        if (_areaCardPrefab == null)
        {
            Debug.LogWarning("[LevelAreaSelectPanel] _areaCardPrefab not assigned", this);
            return UniTask.CompletedTask;
        }

        for (int i = 0; i < filtered.Count; i++)
        {
            var config = filtered[i];
            var cardGo = Instantiate(_areaCardPrefab, _pagesRoot, false);

            var cardView = cardGo.GetComponent<LevelAreaCardView>();
            if (cardView != null)
            {
                cardView.SetData(config, () => OnSelectArea(config));
            }
        }

        return UniTask.CompletedTask;
    }

    private void OnSelectArea(LevelAreaConfig area)
    {
        if (UIService != null && !string.IsNullOrWhiteSpace(_stageSelectPanelId))
            UIService.OpenAsync<PanelBase>(_stageSelectPanelId, area).Forget();
    }

    public void OnClickBack() { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome() { UIService?.GoHomeAsync().Forget(); }
    public void OnClickPrevArea() { Debug.Log("[AreaSelect] 上一区域"); }
    public void OnClickNextArea() { Debug.Log("[AreaSelect] 下一区域"); }

    private void Subscribe()
    {
        if (_backBtn) _backBtn.onClick.AddListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.AddListener(OnClickHome);
        if (_prevAreaBtn) _prevAreaBtn.onClick.AddListener(OnClickPrevArea);
        if (_nextAreaBtn) _nextAreaBtn.onClick.AddListener(OnClickNextArea);
    }

    private void Unsubscribe()
    {
        if (_backBtn) _backBtn.onClick.RemoveListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.RemoveListener(OnClickHome);
        if (_prevAreaBtn) _prevAreaBtn.onClick.RemoveListener(OnClickPrevArea);
        if (_nextAreaBtn) _nextAreaBtn.onClick.RemoveListener(OnClickNextArea);
    }
}