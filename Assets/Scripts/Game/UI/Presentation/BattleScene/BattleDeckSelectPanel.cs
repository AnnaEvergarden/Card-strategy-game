using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗准备：卡组选择面板；列出玩家编队，校验至少 <see cref="FleetStore.MinCardsPerBattleFleet"/> 艘后可进入出战舰娘选择。
/// 请在挂载物体上将 <see cref="BasePanel.PanelName"/> 设为 <see cref="PanelNames.BattleDeckSelectPanel"/>；
/// 战斗场景加载后由 <see cref="UIPanelRegistry"/> 作为默认栈顶显示（同场景内需存在本面板实例）。
/// </summary>
public sealed class BattleDeckSelectPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 卡组条目父节点（建议挂 VerticalLayoutGroup）。
    /// </summary>
    [SerializeField] private RectTransform decksRoot;

    /// <summary>
    /// 卡组条目预制体（根节点挂 <see cref="BattleDeckEntryView"/> 与 <see cref="Button"/>）。
    /// </summary>
    [SerializeField] private GameObject deckEntryPrefab;

    /// <summary>
    /// 返回游戏场景并放弃本局战斗上下文。
    /// </summary>
    [SerializeField] private Button backToGameBtn;

    /// <summary>
    /// 校验失败提示（可选）。
    /// </summary>
    [SerializeField] private TMP_Text tipText;

    /// <summary>
    /// 已生成的卡组条目实例。
    /// </summary>
    private readonly List<GameObject> _spawnedEntries = new();

    /// <summary>
    /// cardId 到 <see cref="CardConfigSO"/> 的映射缓存（通过 <see cref="GameResourceLoader.GetCardConfigMap"/> 加载）。
    /// <see cref="LoadConfigMap"/> 将替换为全局共享字典引用以避免每面板独立加载的开销；
    /// 因此不声明为 readonly，允许在非构造方法中赋值。
    /// </summary>
    private Dictionary<string, CardConfigSO> _configMap = new();

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 注册前写入默认面板名，避免遗漏 Inspector 配置。
    /// </summary>
    protected override void Awake()
    {
        if (string.IsNullOrWhiteSpace(PanelName))
        {
            PanelName = PanelNames.BattleDeckSelectPanel;
        }

        base.Awake();
    }

    /// <summary>
    /// 启用时订阅按钮并刷新卡组列表。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        ReloadDecks();
        ClearTip();
    }

    /// <summary>
    /// 禁用时取消订阅。
    /// </summary>
    protected override void OnDisable()
    {
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
        if (backToGameBtn != null)
        {
            backToGameBtn.onClick.AddListener(OnClickBackToGame);
        }
    }

    /// <summary>
    /// 取消订阅。
    /// </summary>
    private void UnsubscribeButtons()
    {
        if (backToGameBtn != null)
        {
            backToGameBtn.onClick.RemoveListener(OnClickBackToGame);
        }
    }

    /// <summary>
    /// 返回游戏主场景并清空战斗上下文。
    /// </summary>
    private void OnClickBackToGame()
    {
        BattleStartContext.Clear();
        UIPanelRegistry.LoadScene(SceneNames.GameScene);
    }

    /// <summary>
    /// 从编队数据与配置表重建卡组条目。
    /// </summary>
    private void ReloadDecks()
    {
        ClearSpawned();
        LoadConfigMap();

        if (decksRoot == null || deckEntryPrefab == null)
        {
            Debug.LogWarning(
                "BattleDeckSelectPanel: 未绑定 decksRoot 或 deckEntryPrefab，无法显示卡组列表。");
            return;
        }

        var fleet = FleetStore.Load();
        if (fleet?.groups == null || fleet.groups.Count == 0)
        {
            ShowTip("暂无编队数据");
            return;
        }

        for (var i = 0; i < fleet.groups.Count; i++)
        {
            var group = fleet.groups[i];
            if (group == null)
            {
                continue;
            }

            var go = Instantiate(deckEntryPrefab, decksRoot, false);
            go.name = $"DeckEntry_{i + 1}";
            var view = go.GetComponent<BattleDeckEntryView>();
            if (view != null)
            {
                var idx = i;
                view.Bind(group, idx, _configMap, OnDeckEntryClicked);
            }
            else
            {
                Debug.LogWarning(
                    "BattleDeckSelectPanel: deckEntryPrefab 根节点缺少 BattleDeckEntryView。");
            }

            _spawnedEntries.Add(go);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(decksRoot);
    }

    /// <summary>
    /// 点击某套卡组：满足最少人数则写入 <see cref="BattleStartContext"/> 并打开出战选择。
    /// </summary>
    private void OnDeckEntryClicked(int groupIndex)
    {
        ClearTip();
        var fleet = FleetStore.Load();
        if (fleet?.groups == null || groupIndex < 0 || groupIndex >= fleet.groups.Count)
        {
            ShowTip("卡组数据无效");
            return;
        }

        var group = fleet.groups[groupIndex];
        if (!FleetStore.TryValidateBattleFleetGroup(group, out var failureReason))
        {
            ShowTip(failureReason ?? "编队不满足出战条件");
            return;
        }

        var ids = new List<string>(FleetStore.MaxCardsPerFleet);
        for (var i = 0; i < group.cardIds.Count; i++)
        {
            var raw = group.cardIds[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            ids.Add(raw.Trim());
        }

        BattleStartContext.SetSelectedDeck(groupIndex, ids);
        UIPanelRegistry.Push(PanelNames.BattleActivePickPanel);
    }

    /// <summary>
    /// 加载卡牌配置表映射：将 <see cref="_configMap"/> 替换为 <see cref="GameResourceLoader.GetCardConfigMap"/>
    /// 返回的全局共享字典，避免每面板独立查询配置表产生的额外开销。
    /// </summary>
    private void LoadConfigMap()
    {
        _configMap = GameResourceLoader.GetCardConfigMap(logOnMissing: false);
    }

    /// <summary>
    /// 销毁已生成条目。
    /// </summary>
    private void ClearSpawned()
    {
        for (var i = 0; i < _spawnedEntries.Count; i++)
        {
            var go = _spawnedEntries[i];
            if (go != null)
            {
                Destroy(go);
            }
        }

        _spawnedEntries.Clear();
    }

    /// <summary>
    /// 显示提示。
    /// </summary>
    private void ShowTip(string message)
    {
        if (tipText != null)
        {
            tipText.text = message ?? string.Empty;
            tipText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
        else
        {
            Debug.LogWarning($"[BattleDeckSelectPanel] {message}");
        }
    }

    /// <summary>
    /// 隐藏提示。
    /// </summary>
    private void ClearTip()
    {
        if (tipText != null)
        {
            tipText.text = string.Empty;
            tipText.gameObject.SetActive(false);
        }
    }

    #endregion
}
