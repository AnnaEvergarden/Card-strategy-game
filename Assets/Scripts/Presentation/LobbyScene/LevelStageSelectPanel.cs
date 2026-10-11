using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 区域关卡选择面板：读取关卡配置生成可点击的关卡条目，点击后进入战斗场景。
/// </summary>
public sealed class LevelStageSelectPanel : PanelBase
{
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _homeBtn;
    [SerializeField] private RectTransform _stagesRoot;
    [SerializeField] private GameObject _stageEntryPrefab;
    [SerializeField] private TMP_Text _titleText;

    private LevelAreaConfig _currentArea;

    protected override void OnEnable()
    {
        base.OnEnable();
        Subscribe();
    }

    protected override void OnDisable() { Unsubscribe(); base.OnDisable(); }

    public override async UniTask OnOpenAsync(object param = null)
    {
        await base.OnOpenAsync(param);

        if (param is LevelAreaConfig area)
        {
            _currentArea = area;
            RefreshStages();
        }
    }

    public void OnClickBack() { UIService?.GoBackAsync().Forget(); }
    public void OnClickHome() { UIService?.GoHomeAsync().Forget(); }

    /// <summary>点击关卡条目：进入战斗场景。</summary>
    public async void OnClickStage(string stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId)) return;

        var entry = ServiceContainer?.TryResolve<BattleEntryService>();
        if (entry != null)
            await entry.StartBattleAsync(stageId, LevelMode.Permanent);
    }

    private void RefreshStages()
    {
        // 清空旧条目
        for (int i = _stagesRoot.childCount - 1; i >= 0; i--)
            Destroy(_stagesRoot.GetChild(i).gameObject);

        if (_currentArea == null || _currentArea.Stages == null || _currentArea.Stages.Count == 0)
        {
            CreateHintText("该区域暂未配置关卡");
            return;
        }

        if (_titleText != null)
            _titleText.text = _currentArea.DisplayName;

        for (int i = 0; i < _currentArea.Stages.Count; i++)
        {
            var stage = _currentArea.Stages[i];

            if (_stageEntryPrefab != null)
            {
                // 使用 Stage Prefab 生成关卡条目，显示 DisplayName
                var go = Instantiate(_stageEntryPrefab, _stagesRoot, false);
                go.name = $"Stage_{stage.StageIndexInArea}";

                var tmp = go.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                    tmp.text = stage.DisplayName;

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var capturedId = stage.StageId;
                    btn.onClick.AddListener(() => OnClickStage(capturedId));
                    btn.interactable = stage.IsUnlocked;
                }
            }
            else
            {
                // 降级：动态创建
                CreateStageEntryRaw(stage, i);
            }
        }
    }

    private void CreateStageEntryRaw(LevelStageConfig stage, int index)
    {
        var go = new GameObject($"Stage_{index + 1}", typeof(RectTransform), typeof(Button));
        go.transform.SetParent(_stagesRoot, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);

        var btn = go.GetComponent<Button>();
        var capturedId = stage.StageId;
        btn.onClick.AddListener(() => OnClickStage(capturedId));

        // 标题文本
        var label = new GameObject("Label", typeof(RectTransform), typeof(TMP_Text));
        label.transform.SetParent(go.transform, false);
        var tmp = label.GetComponent<TMP_Text>();
        tmp.text = stage.DisplayName;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = stage.IsUnlocked ? Color.white : Color.gray;
        btn.interactable = stage.IsUnlocked;
    }

    private void CreateHintText(string msg)
    {
        var go = new GameObject("Hint", typeof(RectTransform), typeof(TMP_Text));
        go.transform.SetParent(_stagesRoot, false);
        var tmp = go.GetComponent<TMP_Text>();
        tmp.text = msg;
        tmp.fontSize = 20;
        tmp.color = Color.gray;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void Subscribe()
    {
        if (_backBtn) _backBtn.onClick.AddListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.AddListener(OnClickHome);
    }

    private void Unsubscribe()
    {
        if (_backBtn) _backBtn.onClick.RemoveListener(OnClickBack);
        if (_homeBtn) _homeBtn.onClick.RemoveListener(OnClickHome);
    }
}