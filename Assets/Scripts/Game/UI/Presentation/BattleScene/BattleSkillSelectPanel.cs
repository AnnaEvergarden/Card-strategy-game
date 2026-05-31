using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能选择面板：按当前焦点舰娘技能数量实例化 <see cref="BattleSkillBarView"/> 单槽预制体并绑定。
/// </summary>
public sealed class BattleSkillSelectPanel : BattleOverlayPanelBase
{
    #region Fields

    /// <summary>
    /// 技能单槽预制体（根节点挂 <see cref="BattleSkillBarView"/>）。
    /// </summary>
    [SerializeField] private GameObject skillSlotPrefab;

    /// <summary>
    /// 技能槽父节点（建议挂 HorizontalLayoutGroup）。
    /// </summary>
    [SerializeField] private RectTransform skillsRoot;

    /// <summary>
    /// 已生成的技能槽实例。
    /// </summary>
    private readonly List<GameObject> _spawnedSlots = new(CardConfigSO.MaxSkillsPerCard);

    /// <summary>
    /// 解析技能缓冲。
    /// </summary>
    private readonly List<SkillConfigSO> _skillsBuffer = new(CardConfigSO.MaxSkillsPerCard);

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景实例。
    /// </summary>
    public static BattleSkillSelectPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<BattleSkillSelectPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.BattleSkillSelectPanel;
            }

            return existing;
        }

        Debug.LogWarning("BattleSkillSelectPanel: 场景中未找到该面板，请在 BattleScene UI 下挂载。");
        return null;
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
            PanelName = PanelNames.BattleSkillSelectPanel;
        }

        base.Awake();
    }

    /// <summary>
    /// 启用时按焦点舰娘生成技能槽。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        RebuildSkillSlots();
    }

    /// <summary>
    /// 禁用时清理动态槽位。
    /// </summary>
    protected override void OnDisable()
    {
        ClearSkillSlots();
        base.OnDisable();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 根据 <see cref="BattleUiSession.FocusUnitId"/> 重建技能单槽列表。
    /// </summary>
    private void RebuildSkillSlots()
    {
        ClearSkillSlots();
        var unitId = BattleUiSession.FocusUnitId;
        if (string.IsNullOrEmpty(unitId) || skillsRoot == null || skillSlotPrefab == null)
        {
            return;
        }

        BattleContext.Current.Field.EnsureInitialized();
        var unit = BattleContext.Current.Field.GetUnit(unitId);
        var cardId = unit?.CardId ?? string.Empty;
        if (string.IsNullOrEmpty(cardId))
        {
            return;
        }

        var map = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
        var faction = map != null && map.TryGetValue(cardId, out var cfg) && cfg != null ? cfg.Faction : ShipFaction.Other;
        CardSkillQuery.ResolveSkillsForCard(cardId, faction, _skillsBuffer);
        for (var i = 0; i < _skillsBuffer.Count; i++)
        {
            var skill = _skillsBuffer[i];
            if (skill == null)
            {
                continue;
            }

            var go = Instantiate(skillSlotPrefab, skillsRoot, false);
            go.name = $"SkillSlot_{i + 1}_{skill.SkillId}";
            var view = go.GetComponent<BattleSkillBarView>()
                       ?? go.GetComponentInChildren<BattleSkillBarView>(true);
            if (view != null)
            {
                view.Bind(unitId, cardId, i, skill);
            }

            _spawnedSlots.Add(go);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(skillsRoot);
    }

    /// <summary>
    /// 销毁已生成的技能槽。
    /// </summary>
    private void ClearSkillSlots()
    {
        for (var i = 0; i < _spawnedSlots.Count; i++)
        {
            var go = _spawnedSlots[i];
            if (go != null)
            {
                Destroy(go);
            }
        }

        _spawnedSlots.Clear();
    }

    #endregion
}
