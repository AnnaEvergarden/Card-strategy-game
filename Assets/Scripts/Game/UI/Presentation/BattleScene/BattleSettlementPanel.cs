using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗结算面板：展示胜负/放弃标题、奖励占位，并提供返回关卡选择与再次挑战。
/// </summary>
public sealed class BattleSettlementPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 结算标题（胜利 / 失败 / 放弃战斗）。
    /// </summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>
    /// 奖励信息文本。
    /// </summary>
    [SerializeField] private TMP_Text rewardText;

    /// <summary>
    /// 返回关卡选择（GameScene 内区域关卡列表）。
    /// </summary>
    [SerializeField] private Button returnToStageSelectBtn;

    /// <summary>
    /// 再次挑战（重新从卡组选择开始本关）。
    /// </summary>
    [SerializeField] private Button retryStageBtn;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景实例。
    /// </summary>
    public static BattleSettlementPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<BattleSettlementPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.BattleSettlementPanel;
            }

            return existing;
        }

        Debug.LogWarning("BattleSettlementPanel: 场景中未找到该面板，请在 BattleScene UI 下挂载。");
        return null;
    }

    /// <summary>
    /// 按结算类型刷新标题与奖励占位文案。
    /// </summary>
    /// <param name="kind">结算类型。</param>
    public void ApplySettlement(BattleSettlementKind kind)
    {
        if (titleText != null)
        {
            titleText.text = kind switch
            {
                BattleSettlementKind.Victory => "胜利",
                BattleSettlementKind.Defeat => "失败",
                BattleSettlementKind.Surrender => "放弃战斗",
                _ => "战斗结束"
            };
        }

        if (rewardText != null)
        {
            rewardText.text = kind switch
            {
                BattleSettlementKind.Victory => "奖励：待接入掉落与资源结算",
                BattleSettlementKind.Defeat => "奖励：无",
                BattleSettlementKind.Surrender => "奖励：无",
                _ => "奖励：—"
            };
        }
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 补全面板名。
    /// </summary>
    protected override void Awake()
    {
        if (string.IsNullOrWhiteSpace(PanelName))
        {
            PanelName = PanelNames.BattleSettlementPanel;
        }

        base.Awake();
    }

    /// <summary>
    /// 订阅按钮。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        var kind = BattleStartContext.LastSettlement;
        if (kind != BattleSettlementKind.Victory && kind != BattleSettlementKind.Defeat &&
            kind != BattleSettlementKind.Surrender)
        {
            ApplySettlement(BattleSettlementKind.Surrender);
        }
        else
        {
            ApplySettlement(kind);
        }
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    protected override void OnDisable()
    {
        UnsubscribeButtons();
        base.OnDisable();
    }

    #endregion

    #region Private Methods

    private void SubscribeButtons()
    {
        if (returnToStageSelectBtn != null)
        {
            returnToStageSelectBtn.onClick.AddListener(OnClickReturnToStageSelect);
        }

        if (retryStageBtn != null)
        {
            retryStageBtn.onClick.AddListener(OnClickRetryStage);
        }
    }

    private void UnsubscribeButtons()
    {
        if (returnToStageSelectBtn != null)
        {
            returnToStageSelectBtn.onClick.RemoveListener(OnClickReturnToStageSelect);
        }

        if (retryStageBtn != null)
        {
            retryStageBtn.onClick.RemoveListener(OnClickRetryStage);
        }
    }

    /// <summary>
    /// 返回 GameScene 并打开进入战斗前的区域关卡选择界面。
    /// </summary>
    private void OnClickReturnToStageSelect()
    {
        BattleStartContext.RequestReturnToLevelStageSelect();
    }

    /// <summary>
    /// 保留当前关卡，重新从卡组选择流程开始。
    /// </summary>
    private void OnClickRetryStage()
    {
        BattleStartContext.RetryCurrentStageBattlePreparation();
    }

    #endregion
}
