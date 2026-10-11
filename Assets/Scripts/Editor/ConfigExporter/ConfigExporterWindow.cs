using UnityEditor;
using UnityEngine;

public class ConfigExporterWindow : EditorWindow
{
    private string _statusText = "就绪";
    private bool _lastExportSuccess;
    private Vector2 _scrollPos;

    private const string HelpText =
        "从 SO Database 资产读取数据，导出为 JSON 供运行时 ConfigService 加载。\n" +
        "全部导出到 Assets/Config/Runtime 目录。";

    [MenuItem("Tools/Config/配置文件表导出")]
    private static void Open()
    {
        GetWindow<ConfigExporterWindow>("配置文件表导出").minSize = new Vector2(520, 200);
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("配置文件表导出", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(HelpText, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);

        GUI.enabled = !EditorApplication.isPlaying;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新 SO Database", GUILayout.Height(28)))
        {
            DatabasePopulator.RefreshAllDatabases();
            _statusText = "Database 已刷新";
        }
        if (GUILayout.Button("全部导出", GUILayout.Height(28)))
        {
            ExportAll();
        }
        if (GUILayout.Button("打开导出目录", GUILayout.Height(28)))
        {
            EditorUtility.RevealInFinder(ConfigExportUtility.ConfigOutputPath);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("状态:", EditorStyles.boldLabel, GUILayout.Width(40));
        EditorGUILayout.LabelField(_statusText);
        if (_lastExportSuccess)
            EditorGUILayout.LabelField("输出目录:", ConfigExportUtility.ConfigOutputAssetPath, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void ExportAll()
    {
        try
        {
            _statusText = "正在从 SO 资产导出...";
            EditorUtility.DisplayProgressBar("配置导出", "正在导出...", 0.5f);
            ConfigExportUtility.ExportFromSOAssets();
            EditorUtility.ClearProgressBar();
            _statusText = "导出完成！请在 Addressables Groups 中刷新。";
            _lastExportSuccess = true;
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            _statusText = "导出失败: " + ex.Message;
            _lastExportSuccess = false;
            Debug.LogError("[ConfigExporterWindow] " + ex.Message);
        }
    }
}
