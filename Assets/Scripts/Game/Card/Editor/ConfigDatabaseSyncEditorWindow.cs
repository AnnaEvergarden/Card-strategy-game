#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 菜单工具：一键将项目内 CardConfigSO / SkillConfigSO 合并进配置数据库。
/// </summary>
public sealed class ConfigDatabaseSyncEditorWindow : EditorWindow
{
    #region Fields

    /// <summary>
    /// 卡牌库列表。
    /// </summary>
    private readonly List<CardConfigDatabaseSO> _cardDatabases = new();

    /// <summary>
    /// 是否自动收集全部卡牌库。
    /// </summary>
    [SerializeField] private bool autoCollectCardDatabases = true;

    /// <summary>
    /// 技能库列表。
    /// </summary>
    private readonly List<SkillConfigDatabaseSO> _skillDatabases = new();

    /// <summary>
    /// 是否自动收集全部技能库。
    /// </summary>
    [SerializeField] private bool autoCollectSkillDatabases = true;

    /// <summary>
    /// 滚动位置。
    /// </summary>
    private Vector2 _scroll;

    /// <summary>
    /// 上次日志。
    /// </summary>
    private string _lastLog = string.Empty;

    #endregion

    #region Public API

    /// <summary>
    /// 打开窗口。
    /// </summary>
    [MenuItem("Game/Card/配置表一键同步工具")]
    public static void Open()
    {
        var win = GetWindow<ConfigDatabaseSyncEditorWindow>(false, "Config DB Sync", true);
        win.minSize = new Vector2(440f, 320f);
        win.RefreshTargets();
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 绘制 UI。
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField("配置数据库一键同步", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "扫描项目中全部 CardConfigSO / SkillConfigSO，将未收录条目加入数据库。\n" +
            "卡牌 / 技能按 ShipFaction 写入 DatabaseFaction 一致的数据库；无匹配时尝试 Other 库。",
            MessageType.Info);

        if (GUILayout.Button("刷新目标数据库", GUILayout.Height(22f)))
        {
            RefreshTargets();
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("卡牌库", EditorStyles.boldLabel);
        autoCollectCardDatabases = EditorGUILayout.Toggle("自动收集全部 CardConfigDatabase", autoCollectCardDatabases);

        if (!autoCollectCardDatabases)
        {
            DrawCardDatabaseList();
        }
        else if (_cardDatabases.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到 CardConfigDatabaseSO，请创建或取消自动收集后手动指定。", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField($"已找到 {_cardDatabases.Count} 个卡牌库", EditorStyles.miniLabel);
            for (var i = 0; i < _cardDatabases.Count; i++)
            {
                var db = _cardDatabases[i];
                if (db == null)
                {
                    continue;
                }

                EditorGUILayout.LabelField($"· {db.name}（{db.DatabaseFaction}）", EditorStyles.miniLabel);
            }
        }

        if (GUILayout.Button("一键：全部 CardConfigSO → 各卡牌库", GUILayout.Height(28f)))
        {
            RunCardSync();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("技能库", EditorStyles.boldLabel);
        autoCollectSkillDatabases = EditorGUILayout.Toggle("自动收集全部 SkillConfigDatabase", autoCollectSkillDatabases);

        if (!autoCollectSkillDatabases)
        {
            DrawSkillDatabaseList();
        }
        else if (_skillDatabases.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到 SkillConfigDatabaseSO，请创建或取消自动收集后手动指定。", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField($"已找到 {_skillDatabases.Count} 个技能库", EditorStyles.miniLabel);
            for (var i = 0; i < _skillDatabases.Count; i++)
            {
                var db = _skillDatabases[i];
                if (db == null)
                {
                    continue;
                }

                EditorGUILayout.LabelField($"· {db.name}（{db.DatabaseFaction}）", EditorStyles.miniLabel);
            }
        }

        if (GUILayout.Button("一键：全部 SkillConfigSO → 各技能库", GUILayout.Height(28f)))
        {
            RunSkillSync();
        }

        if (GUILayout.Button("一键：卡牌 + 技能 全部同步", GUILayout.Height(32f)))
        {
            RunCardSync();
            RunSkillSync();
        }

        EditorGUILayout.Space(6f);
        if (!string.IsNullOrEmpty(_lastLog))
        {
            EditorGUILayout.LabelField("上次结果", EditorStyles.miniBoldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(120f));
            EditorGUILayout.TextArea(_lastLog);
            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 启用时刷新默认目标。
    /// </summary>
    private void OnEnable()
    {
        RefreshTargets();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 刷新卡牌库与技能库引用。
    /// </summary>
    private void RefreshTargets()
    {
        if (autoCollectCardDatabases)
        {
            ConfigDatabaseSyncUtility.CollectCardDatabases(_cardDatabases);
        }

        if (autoCollectSkillDatabases)
        {
            ConfigDatabaseSyncUtility.CollectSkillDatabases(_skillDatabases);
        }
    }

    /// <summary>
    /// 手动卡牌库列表（预留扩展）。
    /// </summary>
    private void DrawCardDatabaseList()
    {
        EditorGUILayout.LabelField("手动指定卡牌库（当前版本请使用自动收集）", EditorStyles.miniLabel);
    }

    /// <summary>
    /// 手动技能库列表（预留扩展）。
    /// </summary>
    private void DrawSkillDatabaseList()
    {
        EditorGUILayout.LabelField("手动指定技能库（当前版本请使用自动收集）", EditorStyles.miniLabel);
    }

    /// <summary>
    /// 执行卡牌同步。
    /// </summary>
    private void RunCardSync()
    {
        if (autoCollectCardDatabases)
        {
            ConfigDatabaseSyncUtility.CollectCardDatabases(_cardDatabases);
        }

        if (_cardDatabases.Count == 0)
        {
            _lastLog = "未找到 CardConfigDatabaseSO。";
            EditorUtility.DisplayDialog("配置表同步", _lastLog, "确定");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Sync cards into all databases");
        var report = ConfigDatabaseSyncUtility.SyncCardsIntoAllDatabases(_cardDatabases);
        AssetDatabase.SaveAssets();
        LogReport(report);
    }

    /// <summary>
    /// 执行技能同步。
    /// </summary>
    private void RunSkillSync()
    {
        if (autoCollectSkillDatabases)
        {
            ConfigDatabaseSyncUtility.CollectSkillDatabases(_skillDatabases);
        }

        if (_skillDatabases.Count == 0)
        {
            _lastLog = "未找到 SkillConfigDatabaseSO。";
            EditorUtility.DisplayDialog("配置表同步", _lastLog, "确定");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Sync skills into all databases");
        var report = ConfigDatabaseSyncUtility.SyncSkillsIntoAllDatabases(_skillDatabases);
        AssetDatabase.SaveAssets();
        LogReport(report);
    }

    /// <summary>
    /// 输出报告到 Console 与窗口。
    /// </summary>
    private void LogReport(ConfigDatabaseSyncUtility.SyncReport report)
    {
        _lastLog = string.Empty;
        for (var i = 0; i < report.Messages.Count; i++)
        {
            var line = report.Messages[i];
            _lastLog += line + "\n";
            if (i == 0)
            {
                Debug.Log($"[ConfigDatabaseSync] {line}");
            }
            else
            {
                Debug.Log($"[ConfigDatabaseSync] {line}");
            }
        }

        Repaint();
    }

    #endregion
}
#endif
