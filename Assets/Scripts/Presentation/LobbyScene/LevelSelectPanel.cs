using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡选择面板：常驻区域/活动区域入口。
/// </summary>
public sealed class LevelSelectPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;
    [SerializeField] private Button _permanentBtn;
    [SerializeField] private Button _activityBtn;

    [Header("面板 ID")]
    [SerializeField]
    private string _areaSelectPanelId = "LevelAreaSelect";

    protected override void OnEnable() { base.OnEnable(); Subscribe(); }
    protected override void OnDisable() { Unsubscribe(); base.OnDisable(); }
    public void OnClickBack()       { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome()       { UIService?.GoHomeAsync().Forget(); }
    public void OnClickPermanent()  { if (UIService != null) UIService.OpenAsync<PanelBase>(_areaSelectPanelId, LevelAreaCategory.Permanent).Forget(); }
    public void OnClickActivity()   { if (UIService != null) UIService.OpenAsync<PanelBase>(_areaSelectPanelId, LevelAreaCategory.Activity).Forget(); }
    private void Subscribe() {
        if (_backBtn) _backBtn.onClick.AddListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.AddListener(OnClickHome);
        if (_permanentBtn) _permanentBtn.onClick.AddListener(OnClickPermanent);
        if (_activityBtn) _activityBtn.onClick.AddListener(OnClickActivity);
    }
    private void Unsubscribe() {
        if (_backBtn) _backBtn.onClick.RemoveListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.RemoveListener(OnClickHome);
        if (_permanentBtn) _permanentBtn.onClick.RemoveListener(OnClickPermanent);
        if (_activityBtn) _activityBtn.onClick.RemoveListener(OnClickActivity);
    }
}