using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 区域关卡选择面板：在带 <see cref="VerticalLayoutGroup"/> 的 ScrollRect Content 上实例化关卡按钮预制体，支持纵向滚动。
/// </summary>
public sealed class LevelStageSelectPanel : BasePanel
{
    #region Fields

    /// <summary>
    /// 返回按钮（弹栈返回区域选择）。
    /// </summary>
    [SerializeField] private Button backBtn;

    /// <summary>
    /// 主界面按钮（清栈回 MainPanel）。
    /// </summary>
    [SerializeField] private Button homeBtn;

    /// <summary>
    /// 面板标题文本（模式 + 区域名）。
    /// </summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>
    /// 关卡列表的 Content（<see cref="RectTransform"/>）；建议挂 <see cref="VerticalLayoutGroup"/>，必要时配合 <see cref="ContentSizeFitter"/>。
    /// </summary>
    [SerializeField] private RectTransform stagesRoot;

    /// <summary>
    /// 包裹上述 Content 的纵向 <see cref="ScrollRect"/>（用于刷新后滚回顶部）；可为空则仅依赖布局。
    /// </summary>
    [SerializeField] private ScrollRect stageScrollRect;

    /// <summary>
    /// 单个关卡条目的预制体（根或子物体上需有 <see cref="Button"/>；子层级中需有至少一个 <see cref="TMP_Text"/> 用于显示标题，运行时用 <c>GetComponentInChildren&lt;TMP_Text&gt;(true)</c> 取第一个）。
    /// </summary>
    [SerializeField] private GameObject stageEntryPrefab;

    /// <summary>
    /// 已生成列表项缓存，便于刷新时销毁。
    /// </summary>
    private readonly List<GameObject> _spawnedStageButtons = new();

    /// <summary>
    /// 当前模式。
    /// </summary>
    private LevelMode _currentMode = LevelMode.Permanent;

    /// <summary>
    /// 当前区域配置（SO）。
    /// </summary>
    private LevelAreaConfigSO _currentArea;

    #endregion

    #region Public API

    /// <summary>
    /// 查找场景中已放置的区域关卡面板；若不存在则打调试警告并返回 null（不运行时创建默认物体）。
    /// </summary>
    public static LevelStageSelectPanel EnsureInstance()
    {
        var existing = Object.FindObjectOfType<LevelStageSelectPanel>(true);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.PanelName))
            {
                existing.PanelName = PanelNames.LevelStageSelectPanel;
            }

            return existing;
        }

        Debug.LogWarning(
            "LevelStageSelectPanel: 场景中未找到 LevelStageSelectPanel，请在场景或 UI 预制体中挂载该组件；已取消运行时自动搭建界面。");
        return null;
    }

    /// <summary>
    /// 设置并刷新关卡上下文。
    /// </summary>
    /// <param name="mode">来源模式（常驻/活动）。</param>
    /// <param name="area">目标区域配置（ScriptableObject）。</param>
    public void SetContext(LevelMode mode, LevelAreaConfigSO area)
    {
        _currentMode = mode;
        _currentArea = area;
        RefreshStageButtons();
    }

    /// <summary>
    /// 点击返回：弹栈回区域选择。
    /// </summary>
    public void OnClickBack()
    {
        UIPanelRegistry.TryPop();
    }

    /// <summary>
    /// 点击主界面：清栈回 MainPanel。
    /// </summary>
    public void OnClickHome()
    {
        UIPanelRegistry.ClearAndPush(PanelNames.MainPanel);
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 启用时订阅按钮并刷新。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeButtons();
        RefreshStageButtons();
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
    /// 依据当前区域配置重建关卡列表（预制体 + 布局 + 滚动复位）。
    /// </summary>
    private void RefreshStageButtons()
    {
        ClearStageButtons();

        var modeLabel = _currentMode == LevelMode.Activity ? "活动" : "常驻";
        var areaName = _currentArea != null && !string.IsNullOrWhiteSpace(_currentArea.DisplayName)
            ? _currentArea.DisplayName
            : "未选择区域";
        if (titleText != null)
        {
            titleText.text = $"{modeLabel}关卡 - {areaName}";
        }

        if (_currentArea == null || stagesRoot == null)
        {
            return;
        }

        var stages = _currentArea.Stages;
        if (stages == null || stages.Count == 0)
        {
            CreateHint("该区域暂未配置关卡。");
            return;
        }

        if (stageEntryPrefab == null)
        {
            Debug.LogWarning(
                "LevelStageSelectPanel: 未配置 stageEntryPrefab，无法实例化关卡按钮；请在 Inspector 拖入预制体。");
            CreateHint("未配置关卡按钮预制体。");
            return;
        }

        for (var i = 0; i < stages.Count; i++)
        {
            var cfg = stages[i];
            if (cfg != null)
            {
                CreateStageEntry(cfg, i);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(stagesRoot);
        if (stageScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            // 1 = 列表顶部（Unity 纵向归一化坐标）。
            stageScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// 在 Content 下实例化一条关卡预制体并绑定数据与点击。
    /// </summary>
    private void CreateStageEntry(LevelStageConfigSO stage, int index)
    {
        if (stagesRoot == null || stageEntryPrefab == null)
        {
            return;
        }

        var instance = Instantiate(stageEntryPrefab, stagesRoot, false);
        instance.name = $"StageEntry_{index + 1}";

        var unlocked = stage != null && stage.IsUnlocked;
        var button = instance.GetComponent<Button>() ?? instance.GetComponentInChildren<Button>(true);
        if (button != null)
        {
            button.interactable = unlocked;
            var captured = stage;
            button.onClick.AddListener(() => OnClickStage(captured));
        }
        else
        {
            Debug.LogWarning($"LevelStageSelectPanel: 预制体 {stageEntryPrefab.name} 上未找到 Button，关卡 {index + 1} 将无法点击。");
        }

        var titleTmp = instance.GetComponentInChildren<TMP_Text>(true);
        if (titleTmp != null)
        {
            var display = stage != null && !string.IsNullOrWhiteSpace(stage.DisplayName)
                ? stage.DisplayName
                : $"关卡 {stage.StageIndexInAreaCode}";
            titleTmp.text = unlocked ? display : $"{display}（未解锁）";
        }

        _spawnedStageButtons.Add(instance);
    }

    /// <summary>
    /// 点击关卡按钮：后续可在此接战斗加载逻辑。
    /// </summary>
    private void OnClickStage(LevelStageConfigSO stage)
    {
        if (stage == null)
        {
            return;
        }

        Debug.Log(
            $"[LevelStageSelectPanel] 选择关卡 stageId={stage.StageId} areaId={stage.AreaId} indexInArea={stage.StageIndexInAreaCode} display={stage.DisplayName}");
    }

    /// <summary>
    /// 显示提示文案（带 <see cref="LayoutElement"/> 以便 Vertical Layout Group 排版）。
    /// </summary>
    private void CreateHint(string text)
    {
        if (stagesRoot == null)
        {
            return;
        }

        var go = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(stagesRoot, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 48f;
        le.preferredHeight = 72f;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, le.preferredHeight);
        _spawnedStageButtons.Add(go);
    }

    /// <summary>
    /// 清理当前已生成列表项。
    /// </summary>
    private void ClearStageButtons()
    {
        for (var i = 0; i < _spawnedStageButtons.Count; i++)
        {
            var go = _spawnedStageButtons[i];
            if (go != null)
            {
                Destroy(go);
            }
        }

        _spawnedStageButtons.Clear();
    }

    private void SubscribeButtons()
    {
        if (backBtn != null) backBtn.onClick.AddListener(OnClickBack);
        if (homeBtn != null) homeBtn.onClick.AddListener(OnClickHome);
    }

    private void UnsubscribeButtons()
    {
        if (backBtn != null) backBtn.onClick.RemoveListener(OnClickBack);
        if (homeBtn != null) homeBtn.onClick.RemoveListener(OnClickHome);
    }

    #endregion
}
