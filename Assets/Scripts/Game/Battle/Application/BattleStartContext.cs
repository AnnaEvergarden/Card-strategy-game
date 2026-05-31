using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从选关进入战斗至本局开战前的跨场景上下文：关卡配置、所选卡组与出战舰娘列表；不写入编队存档。
/// </summary>
public static class BattleStartContext
{
    #region Fields

    /// <summary>
    /// 当前关卡配置（由关卡选择界面写入）。
    /// </summary>
    private static LevelStageConfigSO _currentStage;

    /// <summary>
    /// 进入本关时所在的关卡模式（常驻/活动）。
    /// </summary>
    private static LevelMode _entryLevelMode = LevelMode.Permanent;

    /// <summary>
    /// 最近一次结算类型。
    /// </summary>
    private static BattleSettlementKind _lastSettlement = BattleSettlementKind.Surrender;

    /// <summary>
    /// 加载 GameScene 后是否应打开区域关卡选择面板。
    /// </summary>
    private static bool _pendingReturnToLevelStageSelect;

    /// <summary>
    /// 玩家选择的卡组下标（<see cref="FleetStore.FleetData.groups"/>）。
    /// </summary>
    private static int _selectedFleetGroupIndex = -1;

    /// <summary>
    /// 所选卡组中的舰娘 cardId（顺序与编队一致，长度 1～<see cref="FleetStore.MaxCardsPerFleet"/>）。
    /// </summary>
    private static readonly List<string> _selectedDeckCardIds = new(FleetStore.MaxCardsPerFleet);

    /// <summary>
    /// 本局出战舰娘 cardId（顺序即上场顺序，长度 <see cref="FleetStore.MinActivesPerBattleFleet"/>～<see cref="FleetStore.MaxActivesPerBattleFleet"/>）。
    /// </summary>
    private static readonly List<string> _playerActiveCardIds = new(FleetStore.MaxActivesPerBattleFleet);

    #endregion

    #region Public API

    /// <summary>
    /// 是否已有关卡配置（从关卡列表进入战斗时应为 true）。
    /// </summary>
    public static bool HasStage => _currentStage != null;

    /// <summary>
    /// 当前关卡配置；无则为 null。
    /// </summary>
    public static LevelStageConfigSO CurrentStage => _currentStage;

    /// <summary>
    /// 已选卡组下标；-1 表示未选。
    /// </summary>
    public static int SelectedFleetGroupIndex => _selectedFleetGroupIndex;

    /// <summary>
    /// 本局出战舰娘（只读，至少 1 条）。
    /// </summary>
    public static IReadOnlyList<string> PlayerActiveCardIds => _playerActiveCardIds;

    /// <summary>
    /// 最近一次结算类型（打开结算面板时写入）。
    /// </summary>
    public static BattleSettlementKind LastSettlement => _lastSettlement;

    /// <summary>
    /// 进入本关时的关卡模式。
    /// </summary>
    public static LevelMode EntryLevelMode => _entryLevelMode;

    /// <summary>
    /// 从区域关卡界面进入战斗：记录关卡、模式并清空卡组/出战选择。
    /// </summary>
    /// <param name="stage">关卡 ScriptableObject。</param>
    /// <param name="mode">来源模式（常驻/活动）。</param>
    public static void BeginFromLevelSelect(LevelStageConfigSO stage, LevelMode mode)
    {
        _currentStage = stage;
        _entryLevelMode = mode;
        _selectedFleetGroupIndex = -1;
        _selectedDeckCardIds.Clear();
        _playerActiveCardIds.Clear();
        _pendingReturnToLevelStageSelect = false;
    }

    /// <summary>
    /// 记录结算类型（由 <see cref="BattleUiFlow.OpenSettlement"/> 调用）。
    /// </summary>
    /// <param name="kind">结算类型。</param>
    public static void SetLastSettlement(BattleSettlementKind kind)
    {
        _lastSettlement = kind;
    }

    /// <summary>
    /// 在战斗准备中选中一套卡组后调用：记下标并复制非空 cardId（至多 <see cref="FleetStore.MaxCardsPerFleet"/> 条）。
    /// </summary>
    /// <param name="groupIndex">卡组下标。</param>
    /// <param name="deckCardIds">已校验的 id 列表（非空、去重、长度在允许范围内）。</param>
    public static void SetSelectedDeck(int groupIndex, IReadOnlyList<string> deckCardIds)
    {
        _selectedFleetGroupIndex = groupIndex;
        _selectedDeckCardIds.Clear();
        if (deckCardIds == null)
        {
            return;
        }

        for (var i = 0; i < deckCardIds.Count && _selectedDeckCardIds.Count < FleetStore.MaxCardsPerFleet; i++)
        {
            var id = deckCardIds[i];
            if (!string.IsNullOrWhiteSpace(id))
            {
                _selectedDeckCardIds.Add(id.Trim());
            }
        }
    }

    /// <summary>
    /// 复制当前所选卡组 id 到目标列表（用于上阵选择 UI）。
    /// </summary>
    /// <param name="destination">输出列表（会先 Clear）。</param>
    public static void CopySelectedDeckCardIds(List<string> destination)
    {
        destination?.Clear();
        if (destination == null)
        {
            return;
        }

        for (var i = 0; i < _selectedDeckCardIds.Count; i++)
        {
            destination.Add(_selectedDeckCardIds[i]);
        }
    }

    /// <summary>
    /// 设置本局出战舰娘（确认后调用；顺序与列表一致，至多 <see cref="FleetStore.MaxActivesPerBattleFleet"/> 条）。
    /// </summary>
    /// <param name="actives">出战 cardId 列表。</param>
    public static void SetPlayerActives(IReadOnlyList<string> actives)
    {
        _playerActiveCardIds.Clear();
        if (actives == null)
        {
            return;
        }

        for (var i = 0; i < actives.Count && _playerActiveCardIds.Count < FleetStore.MaxActivesPerBattleFleet; i++)
        {
            var id = actives[i];
            if (!string.IsNullOrWhiteSpace(id))
            {
                _playerActiveCardIds.Add(id.Trim());
            }
        }
    }

    /// <summary>
    /// 离开战斗或放弃本局时清空上下文。
    /// </summary>
    public static void Clear()
    {
        _currentStage = null;
        _entryLevelMode = LevelMode.Permanent;
        _selectedFleetGroupIndex = -1;
        _selectedDeckCardIds.Clear();
        _playerActiveCardIds.Clear();
        _pendingReturnToLevelStageSelect = false;
        BattleContext.Current.ResetBattle();
    }

    /// <summary>
    /// 请求返回 GameScene 的区域关卡选择界面（保留当前关卡配置以便再次挑战）。
    /// </summary>
    public static void RequestReturnToLevelStageSelect()
    {
        if (!HasStage)
        {
            Debug.LogWarning("BattleStartContext: 无当前关卡，无法返回关卡选择。");
            UIPanelRegistry.LoadScene(SceneNames.GameScene);
            return;
        }

        _pendingReturnToLevelStageSelect = true;
        ResetBattlePreparationSelections();
        UIPanelRegistry.LoadScene(SceneNames.GameScene);
    }

    /// <summary>
    /// 再次挑战当前关：清空卡组/出战选择并重新加载战斗场景（从卡组选择开始）。
    /// </summary>
    public static void RetryCurrentStageBattlePreparation()
    {
        if (!HasStage)
        {
            Debug.LogWarning("BattleStartContext: 无当前关卡，无法再次挑战。");
            return;
        }

        _pendingReturnToLevelStageSelect = false;
        ResetBattlePreparationSelections();
        UIPanelRegistry.LoadScene(SceneNames.BattleScene);
    }

    /// <summary>
    /// GameScene 加载后消费「返回关卡选择」标记并解析区域配置。
    /// </summary>
    /// <param name="mode">进入战斗时的模式。</param>
    /// <param name="area">当前关卡所属区域。</param>
    /// <returns>是否应导航到 <see cref="PanelNames.LevelStageSelectPanel"/>。</returns>
    public static bool TryConsumeBattleReturnNav(out LevelMode mode, out LevelAreaConfigSO area)
    {
        mode = _entryLevelMode;
        area = null;
        if (!_pendingReturnToLevelStageSelect)
        {
            return false;
        }

        _pendingReturnToLevelStageSelect = false;
        area = ResolveAreaForCurrentStage();
        return area != null;
    }

    /// <summary>
    /// 清空本局卡组与出战选择（保留 <see cref="CurrentStage"/>）。
    /// </summary>
    private static void ResetBattlePreparationSelections()
    {
        _selectedFleetGroupIndex = -1;
        _selectedDeckCardIds.Clear();
        _playerActiveCardIds.Clear();
        BattleContext.Current.ResetBattle();
    }

    /// <summary>
    /// 按当前关卡的 areaId 从区域数据库查找区域配置。
    /// </summary>
    private static LevelAreaConfigSO ResolveAreaForCurrentStage()
    {
        if (_currentStage == null || string.IsNullOrWhiteSpace(_currentStage.AreaId))
        {
            return null;
        }

        var db = GameResourceLoader.LoadLevelAreaDatabase(logOnMissing: false);
        if (db == null)
        {
            return null;
        }

        var areas = db.GetAreas(_entryLevelMode);
        if (areas == null)
        {
            return null;
        }

        var targetId = _currentStage.AreaId.Trim();
        for (var i = 0; i < areas.Count; i++)
        {
            var a = areas[i];
            if (a != null && !string.IsNullOrWhiteSpace(a.AreaId) && a.AreaId.Trim() == targetId)
            {
                return a;
            }
        }

        return null;
    }

    #endregion
}
