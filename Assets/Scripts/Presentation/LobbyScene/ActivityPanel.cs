using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 活动面板：展示活动内容，提供返回和主界面按钮。
/// </summary>
public sealed class ActivityPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;

    protected override void OnEnable() { base.OnEnable(); Subscribe(); }
    protected override void OnDisable() { Unsubscribe(); base.OnDisable(); }
    public void OnClickBack()  { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome()  { UIService?.GoHomeAsync().Forget(); }
    private void Subscribe() {
        if (_backBtn) _backBtn.onClick.AddListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.AddListener(OnClickHome);
    }
    private void Unsubscribe() {
        if (_backBtn) _backBtn.onClick.RemoveListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.RemoveListener(OnClickHome);
    }
}
