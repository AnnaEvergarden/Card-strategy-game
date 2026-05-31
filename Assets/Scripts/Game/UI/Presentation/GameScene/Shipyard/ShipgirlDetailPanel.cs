using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 舰娘详情面板：立绘、StatsMiniPanel（分项 HP/阵营 TMP）、至多三个技能槽（Prefab + 悬停详情）；数据来自 <see cref="CardConfigSO"/> 与 <see cref="SkillConfigSO"/>。
/// </summary>
public sealed class ShipgirlDetailPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 返回上一级。
    /// </summary>
    [SerializeField] private Button backBtn;

    /// <summary>
    /// 回主界面：清空导航栈并打开 <see cref="PanelNames.MainPanel"/>。
    /// </summary>
    [SerializeField] private Button homeBtn;

    /// <summary>
    /// 舰娘名称（可与立绘旁标题共用）。
    /// </summary>
    [SerializeField] private TMP_Text shipNameText;

    /// <summary>
    /// 舰娘展示用立绘（通常为大图；资源规则与列表头像一致：按 <see cref="CardConfigSO.EnglishName"/> 加载）。
    /// </summary>
    [SerializeField] private Image shipPortraitImage;

    /// <summary>
    /// 属性小面板（StatsMiniPanel）根节点；有舰娘数据时显示，内部排版由预制体/场景自行摆放。
    /// </summary>
    [SerializeField] private GameObject statsMiniPanelRoot;

    /// <summary>
    /// 耐久（HP）数值展示；标签文案可在场景中静态排版，此处仅填数字。
    /// </summary>
    [SerializeField] private TMP_Text statsHpText;

    /// <summary>
    /// 阵营展示（中文简写）。
    /// </summary>
    [SerializeField] private TMP_Text statsFactionText;

    /// <summary>
    /// 技能槽父节点（横向或网格布局由 Prefab 与 LayoutGroup 决定）。
    /// </summary>
    [SerializeField] private Transform skillSlotsContainer;

    /// <summary>
    /// 单个技能槽预制体（根物体挂 <see cref="ShipgirlDetailSkillSlotView"/>）；拖拽 Prefab 资源即可，运行时 Instantiate 会生成实例。
    /// </summary>
    [SerializeField] private GameObject skillSlotPrefab;

    /// <summary>
    /// 技能悬停提示宿主（同一物体或子物体上挂 <see cref="SkillHoverTooltipPresenter"/>，并绑定提示框预制体）。
    /// </summary>
    [SerializeField] private SkillHoverTooltipPresenter skillTooltipPresenter;

    /// <summary>
    /// 解析技能缓冲。
    /// </summary>
    private readonly List<SkillConfigSO> _skillsBuffer = new(CardConfigSO.MaxSkillsPerCard);

    /// <summary>
    /// 运行时生成的技能槽实例（至多 3）。
    /// </summary>
    private readonly List<ShipgirlDetailSkillSlotView> _skillSlotInstances = new(CardConfigSO.MaxSkillsPerCard);

    #endregion

    #region Public API

    /// <summary>
    /// 强制刷新详情面板（即使面板已在栈顶，也可通过此方法立即更新）。
    /// </summary>
    /// <param name="cardId">舰娘 ID；为空则清空展示。</param>
    public void ForceRefresh(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return;
        }

        var newId = cardId.Trim();
        Refresh(newId);
    }

    /// <summary>
    /// 查找场景中的舰娘详情面板。
    /// </summary>
    public static ShipgirlDetailPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<ShipgirlDetailPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.ShipgirlDetailPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "ShipgirlDetailPanel: 场景中未找到面板，请在 GameScene UI 下挂载并设置 PanelName = ShipgirlDetailPanel。");
        return null;
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时订阅按钮并刷新展示。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        var cardId = ShipgirlDetailOpenRequest.ConsumePending();
        Refresh(cardId);
    }

    /// <summary>
    /// 禁用时取消订阅并关闭技能 Tooltip。
    /// </summary>
    protected override void OnDisable()
    {
        skillTooltipPresenter?.Hide();
        UnsubscribeButtons();
        base.OnDisable();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 订阅按钮。
    /// </summary>
    private void SubscribeButtons()
    {
        if (backBtn != null)
        {
            backBtn.onClick.AddListener(OnClickBack);
        }

        if (homeBtn != null)
        {
            homeBtn.onClick.AddListener(OnClickHome);
        }
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (backBtn != null)
        {
            backBtn.onClick.RemoveListener(OnClickBack);
        }

        if (homeBtn != null)
        {
            homeBtn.onClick.RemoveListener(OnClickHome);
        }
    }

    /// <summary>
    /// 关闭详情。
    /// </summary>
    private void OnClickBack()
    {
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 回主界面。
    /// </summary>
    private void OnClickHome()
    {
        skillTooltipPresenter?.Hide();
        UIPanelRegistry.ClearAndPush(PanelNames.MainPanel);
    }

    /// <summary>
    /// 根据 cardId 加载配置与技能并刷新 UI。
    /// </summary>
    /// <param name="cardId">舰娘 ID；为空则清空展示。</param>
    private void Refresh(string cardId)
    {
        skillTooltipPresenter?.Hide();

        if (string.IsNullOrWhiteSpace(cardId))
        {
            ClearPresentation();
            return;
        }

        cardId = cardId.Trim();
        var cfg = FindCardConfig(cardId);
        if (shipNameText != null)
        {
            shipNameText.text = cfg != null && !string.IsNullOrEmpty(cfg.DisplayName)
                ? cfg.DisplayName
                : cardId;
        }

        BindPortrait(cfg);
        BindStats(cfg);

        CardSkillQuery.ResolveSkillsForCard(cardId, cfg?.Faction ?? ShipFaction.Other, _skillsBuffer);
        BindSkillSlots(_skillsBuffer);
    }

    /// <summary>
    /// 空状态：清空文本、隐藏立绘与属性面板、卸空技能槽。
    /// </summary>
    private void ClearPresentation()
    {
        if (shipNameText != null)
        {
            shipNameText.text = string.Empty;
        }

        if (shipPortraitImage != null)
        {
            shipPortraitImage.sprite = null;
            shipPortraitImage.enabled = false;
        }

        if (statsMiniPanelRoot != null)
        {
            statsMiniPanelRoot.SetActive(false);
        }

        ClearStatsTexts();

        BindSkillSlots(null);
    }

    /// <summary>
    /// 绑定立绘（与船坞列表相同 Resources 规则）。
    /// </summary>
    private void BindPortrait(CardConfigSO cfg)
    {
        if (shipPortraitImage == null)
        {
            return;
        }

        if (cfg == null || string.IsNullOrWhiteSpace(cfg.EnglishName))
        {
            shipPortraitImage.sprite = null;
            shipPortraitImage.enabled = false;
            return;
        }

        shipPortraitImage.preserveAspect = true;
        shipPortraitImage.sprite = GameResourceLoader.LoadShipgirlIcon(cfg.EnglishName, logOnMissing: false);
        shipPortraitImage.enabled = shipPortraitImage.sprite != null;
    }

    /// <summary>
    /// 绑定 StatsMiniPanel：根节点显隐与各分项 TMP（数值/阵营由配置写入，标签在场景中排版）。
    /// </summary>
    private void BindStats(CardConfigSO cfg)
    {
        var has = cfg != null;
        if (statsMiniPanelRoot != null)
        {
            statsMiniPanelRoot.SetActive(has);
        }

        if (!has)
        {
            ClearStatsTexts();
            return;
        }

        if (statsHpText != null)
        {
            statsHpText.text = "生命值：" + cfg.HP.ToString();
        }

        if (statsFactionText != null)
        {
            statsFactionText.text = "阵营：" + FormatFaction(cfg.Faction);
        }
    }

    /// <summary>
    /// 清空属性分项文本。
    /// </summary>
    private void ClearStatsTexts()
    {
        if (statsHpText != null)
        {
            statsHpText.text = string.Empty;
        }

        if (statsFactionText != null)
        {
            statsFactionText.text = string.Empty;
        }
    }

    /// <summary>
    /// 从卡牌配置字典查找配置。
    /// </summary>
    private static CardConfigSO FindCardConfig(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return null;
        }

        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        if (map == null)
        {
            return null;
        }

        map.TryGetValue(cardId.Trim(), out var cfg);
        return cfg;
    }

    /// <summary>
    /// 阵营中文简写。
    /// </summary>
    private static string FormatFaction(ShipFaction faction)
    {
        switch (faction)
        {
            case ShipFaction.EagleUnion:
                return "白鹰";
            case ShipFaction.RoyalNavy:
                return "皇家";
            case ShipFaction.SakuraEmpire:
                return "重樱";
            case ShipFaction.IronBlood:
                return "铁血";
            case ShipFaction.DragonEmpery:
                return "东煌";
            default:
                return "其他";
        }
    }

    /// <summary>
    /// 实例化或复用技能槽并绑定数据。
    /// </summary>
    private void BindSkillSlots(List<SkillConfigSO> skills)
    {
        if (skillSlotsContainer == null || skillSlotPrefab == null)
        {
            Debug.LogWarning(
                "ShipgirlDetailPanel: 未绑定 skillSlotsContainer 或 skillSlotPrefab，技能区将不显示。");
            return;
        }

        if (skillTooltipPresenter == null)
        {
            Debug.LogWarning(
                "ShipgirlDetailPanel: 未绑定 skillTooltipPresenter，技能悬停提示不可用。");
        }

        EnsureSkillSlotCount();
        var max = CardConfigSO.MaxSkillsPerCard;
        for (var i = 0; i < max; i++)
        {
            var slot = _skillSlotInstances[i];
            if (slot == null)
            {
                continue;
            }

            slot.gameObject.SetActive(true);
            var sk = skills != null && i < skills.Count ? skills[i] : null;
            slot.Bind(sk, skillTooltipPresenter);
        }
    }

    /// <summary>
    /// 保证技能槽实例数量为 <see cref="CardConfigSO.MaxSkillsPerCard"/>。
    /// </summary>
    private void EnsureSkillSlotCount()
    {
        var max = CardConfigSO.MaxSkillsPerCard;
        while (_skillSlotInstances.Count < max)
        {
            var go = Instantiate(skillSlotPrefab, skillSlotsContainer, false);
            var slot = go.GetComponent<ShipgirlDetailSkillSlotView>();
            if (slot == null)
            {
                Destroy(go);
                Debug.LogError(
                    "ShipgirlDetailPanel: skillSlotPrefab 根节点缺少 ShipgirlDetailSkillSlotView。");
                break;
            }

            _skillSlotInstances.Add(slot);
        }

        for (var i = 0; i < _skillSlotInstances.Count; i++)
        {
            var go = _skillSlotInstances[i] != null ? _skillSlotInstances[i].gameObject : null;
            if (go != null)
            {
                go.SetActive(i < max);
            }
        }
    }

    #endregion
}
