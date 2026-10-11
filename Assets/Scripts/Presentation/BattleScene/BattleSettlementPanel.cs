using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗结算面板：展示真实战斗结果（胜利/失败/撤退 + 回合数 + 原因），
/// 确认后经 BattleEntryService 返回主场景。
/// 表现层只读：结果数据经 IBattleService.GetOutcome() 查询。
/// </summary>
public sealed class BattleSettlementPanel : PanelBase
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _rewardText;
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _retryButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.SetAsLastSibling(); // 场景实例打开时置顶，保证层级覆盖
        ResolveWidgets();
        Subscribe();
        RefreshSettlement();
    }

    protected override void OnDisable()
    {
        Unsubscribe();
        base.OnDisable();
    }

    /// <summary>
    /// 面板打开：读取战斗结果并展示。
    /// </summary>
    public override UniTask OnOpenAsync(object param = null)
    {
        base.OnOpenAsync(param);
        ResolveWidgets();
        RefreshSettlement();
        return UniTask.CompletedTask;
    }

    /// <summary>按子物体名解析场景控件（Title_Text / Reward_Text / Back_Button / Retry_Button）。</summary>
    private void ResolveWidgets()
    {
        if (_titleText == null)
            _titleText = PanelWidgetUtil.FindChild<TMP_Text>(transform, "Title_Text");
        if (_rewardText == null)
            _rewardText = PanelWidgetUtil.FindChild<TMP_Text>(transform, "Reward_Text");
        if (_backButton == null)
            _backButton = PanelWidgetUtil.FindChild<Button>(transform, "Back_Button");
        if (_retryButton == null)
            _retryButton = PanelWidgetUtil.FindChild<Button>(transform, "Retry_Button");
    }

    /// <summary>返回关卡选择（对应历史"返回关卡选择"）。</summary>
    private async void OnClickReturnToStageSelect()
    {
        var entry = ServiceContainer?.TryResolve<BattleEntryService>();
        if (entry != null)
        {
            await entry.ReturnToStageSelectAsync();
            return;
        }

        UIService?.GoBackAsync().Forget();
    }

    /// <summary>再次挑战（对应历史"再次挑战"：同关重开并重新选择编队与首发）。</summary>
    private async void OnClickRetryStage()
    {
        var entry = ServiceContainer?.TryResolve<BattleEntryService>();
        if (entry != null)
            await entry.RetryCurrentStageAsync();
    }

    /// <summary>按结果类型渲染文案。</summary>
    private void RefreshSettlement()
    {
        var outcome = ServiceContainer?.TryResolve<IBattleService>()?.GetOutcome();

        var title = "战斗结束";
        var detail = string.Empty;
        var titleColor = Color.white;

        if (outcome != null)
        {
            switch (outcome.Result)
            {
                case BattleResult.P1Victory:
                    title = "胜利！";
                    detail = $"第 {outcome.RoundNumber} 回合击破敌方全员";
                    titleColor = new Color(1f, 0.85f, 0.3f);
                    break;
                case BattleResult.P2Victory:
                    title = "战斗失败";
                    detail = $"我方舰队于第 {outcome.RoundNumber} 回合全灭";
                    titleColor = new Color(1f, 0.4f, 0.4f);
                    break;
                case BattleResult.P1Retreat:
                    title = "放弃战斗";
                    detail = $"第 {outcome.RoundNumber} 回合放弃战斗";
                    titleColor = new Color(0.7f, 0.7f, 0.7f);
                    break;
                case BattleResult.Draw:
                    title = "平局";
                    detail = $"第 {outcome.RoundNumber} 回合不分胜负";
                    break;
            }
        }

        if (_titleText != null)
        {
            _titleText.text = title;
            _titleText.color = titleColor;
        }

        if (_rewardText != null)
        {
            // 历史逻辑：奖励占位（胜利）/ 无奖励（失败、放弃）+ 本轮结算明细
            var reward = outcome != null && outcome.Result == BattleResult.P1Victory
                ? "奖励：待接入掉落与资源结算"
                : "奖励：无";
            var reason = string.IsNullOrEmpty(outcome?.Reason) ? string.Empty : $"（{outcome.Reason}）";
            _rewardText.text = $"{reward}\n{detail}{reason}";
        }
    }

    private void Subscribe()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(OnClickReturnToStageSelect);
        if (_retryButton != null)
            _retryButton.onClick.AddListener(OnClickRetryStage);
    }

    private void Unsubscribe()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(OnClickReturnToStageSelect);
        if (_retryButton != null)
            _retryButton.onClick.RemoveListener(OnClickRetryStage);
    }

}
