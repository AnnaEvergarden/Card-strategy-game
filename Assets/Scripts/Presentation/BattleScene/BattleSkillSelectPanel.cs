using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技能选择面板（复用场景 UI：Close_Button；技能列表动态生成，对应历史 skillsRoot + 技能槽预制体做法）。
/// 打开参数：施法单位 UnitId。
/// 点击技能 → IBattleService.TryBeginCast：
/// - 需手动选目标 → 关闭自身，由 BattleMainPanel 进入选目标模式（订阅 SkillTargetSelectingEvent）；
/// - 自动目标 → 直接释放并关闭自身。释放失败时不提示（与历史一致）。
/// </summary>
public sealed class BattleSkillSelectPanel : BattleOverlayPanelBase
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private RectTransform _listRoot;

    [Tooltip("技能槽预制体（BattleSkillBarView.prefab）；留空则回退为动态按钮")]
    [SerializeField] private GameObject _skillSlotPrefab;

    private readonly List<Button> _skillButtons = new List<Button>();
    private readonly List<BattleSkillBarView> _skillSlotViews = new List<BattleSkillBarView>();
    private string _casterUnitId;

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.SetAsLastSibling();
        if (_closeButton == null) _closeButton = FindButton(transform, "Close_Button");
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
    }

    protected override void OnDisable()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        ClearSkillButtons();
        base.OnDisable();
    }

    /// <summary>
    /// 打开面板：按施法单位重建技能列表。
    /// </summary>
    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);
        transform.SetAsLastSibling();

        if (param is string unitId)
            _casterUnitId = unitId;

        RebuildSkillList();
    }

    /// <summary>重建技能按钮列表（数据来自 IBattleService 只读查询）。</summary>
    private void RebuildSkillList()
    {
        ClearSkillButtons();

        var battleService = ServiceContainer?.TryResolve<IBattleService>();
        var entries = battleService?.GetSkillMenuEntries(_casterUnitId);
        var root = EnsureListRoot();

        if (entries == null || entries.Count == 0)
            return;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var position = new Vector2(0f, 70f - i * 52f);

            // 优先使用原预制体技能槽（BattleSkillBarView.prefab，图标按钮），缺失时回退动态按钮。
            if (_skillSlotPrefab != null)
            {
                var slotObject = Instantiate(_skillSlotPrefab, root, false);
                slotObject.name = $"Skill_{entry.SkillId}";
                if (slotObject.transform is RectTransform slotRect)
                    slotRect.anchoredPosition = position;

                var slotView = slotObject.GetComponent<BattleSkillBarView>()
                               ?? slotObject.GetComponentInChildren<BattleSkillBarView>(true);
                if (slotView != null)
                {
                    slotView.Bind(_casterUnitId, entry.SkillId, entry.IsUsable);
                    slotView.SkillClicked += OnSkillViewClicked;
                    _skillSlotViews.Add(slotView);
                    continue;
                }

                Destroy(slotObject);
            }

            var label = entry.IsUsable
                ? $"{entry.DisplayName}（CD {entry.CooldownRemaining}／次数 {(entry.RemainingUses < 0 ? "∞" : entry.RemainingUses.ToString())}）"
                : $"{entry.DisplayName}（{entry.BlockReason}）";

            var button = CreateButton(root, $"Skill_{entry.SkillId}", label, position, new Vector2(420f, 46f));
            button.interactable = entry.IsUsable;
            var captured = entry;
            button.onClick.AddListener(() => OnSkillClicked(captured));
            _skillButtons.Add(button);
        }
    }

    /// <summary>预制体技能槽点击（携带技能 ID）：走统一的释放逻辑。</summary>
    private void OnSkillViewClicked(string skillId)
    {
        var battleService = ServiceContainer?.TryResolve<IBattleService>();
        if (battleService == null || string.IsNullOrEmpty(skillId))
            return;

        // 释放失败时不提示（与历史一致）；成功则关闭本面板（需手动选目标时交给主界面选目标模式）。
        if (!battleService.TryBeginCast(_casterUnitId, skillId, out _))
            return;

        CloseSelf();
    }

    /// <summary>点击技能：经服务释放（两段式第一段）。</summary>
    private void OnSkillClicked(SkillMenuEntry entry)
    {
        var battleService = ServiceContainer?.TryResolve<IBattleService>();
        if (battleService == null)
            return;

        // 释放失败（冷却/次数/目标非法）时不提示，与历史行为一致。
        if (!battleService.TryBeginCast(_casterUnitId, entry.SkillId, out _))
            return;

        // 需要手动选目标时：关闭本面板，交给 BattleMainPanel 的选目标模式；
        // 自动目标技能已直接释放完成。
        CloseSelf();
    }

    private void OnCloseClicked() { CloseSelf(); }

    /// <summary>清理技能槽（预制体视图 + 动态按钮）。</summary>
    private void ClearSkillButtons()
    {
        for (var i = 0; i < _skillSlotViews.Count; i++)
        {
            var slotView = _skillSlotViews[i];
            if (slotView == null) continue;
            slotView.SkillClicked -= OnSkillViewClicked;
            Destroy(slotView.gameObject);
        }
        _skillSlotViews.Clear();

        for (var i = 0; i < _skillButtons.Count; i++)
        {
            if (_skillButtons[i] != null)
                Destroy(_skillButtons[i].gameObject);
        }
        _skillButtons.Clear();
    }

    /// <summary>技能列表容器（场景缺少时动态创建）。</summary>
    private RectTransform EnsureListRoot()
    {
        // 序列化引用指向面板自身（或为空）时，改用专属子容器，避免技能按钮与关闭按钮重叠。
        if (_listRoot != null && _listRoot != (RectTransform)transform)
            return _listRoot;

        _listRoot = FindOrCreateRoot(transform, "SkillListRoot", true);
        return _listRoot;
    }
}
