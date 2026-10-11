using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗表情面板（复用场景 UI：Close_Button）。
/// 当前仅提供开关与关闭；表情内容后续接入表情资源时在 EmojisRoot 下动态生成。
/// </summary>
public sealed class BattleEmojiPanel : BattleOverlayPanelBase
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private RectTransform _emojisRoot;

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.SetAsLastSibling();
        if (_closeButton == null) _closeButton = FindButton(transform, "Close_Button");
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        EnsureEmojisRoot();
    }

    protected override void OnDisable()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        base.OnDisable();
    }

    /// <summary>
    /// 打开面板：表情按钮在 EmojisRoot 下为动态内容预留（当前无表情数据源）。
    /// </summary>
    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);
        transform.SetAsLastSibling();
    }

    private void OnCloseClicked() { CloseSelf(); }

    /// <summary>表情容器：序列化引用指向面板自身（或为空）时改用专属子容器。</summary>
    private void EnsureEmojisRoot()
    {
        if (_emojisRoot != null && _emojisRoot != (RectTransform)transform)
            return;

        _emojisRoot = FindOrCreateRoot(transform, "EmojisRoot", true);
    }
}
