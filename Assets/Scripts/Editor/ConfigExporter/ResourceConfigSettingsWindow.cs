using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

/// <summary>
/// Resource 资源配置表导出设置：管理 Addressables Group 容量和 ID 范围。
/// 管理 Addressables Group 容量和 ID 范围，支持从下拉列表添加/删除分组、调整容量。
/// Tools/Resource/资源配置表导出
/// </summary>
public class ResourceConfigSettingsWindow : EditorWindow
{
    private const string SettingsPath = "Assets/Config/Editor/ResourceGroupRanges.asset";

    private ResourceGroupRangeSO _ranges;
    private string _statusText = "就绪";
    private Vector2 _scrollPos;

    // 添加新分组的输入缓存
    private string[] _availableGroupNames = new string[0];
    private int _selectedGroupIndex = -1;
    private int _newGroupCapacity = 500;

    [MenuItem("Tools/Resource/资源配置表导出")]
    private static void Open()
    {
        GetWindow<ResourceConfigSettingsWindow>("Resource 资源配置表").minSize = new Vector2(600, 420);
    }

    private void OnEnable()
    {
        LoadOrCreate();
        RefreshAvailableGroups();
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Resource 资源配置表导出", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        // ── 操作栏 ──
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("生成配置", GUILayout.Height(28)))
        {
            ResourceConfigGenerator.Generate();
            _statusText = "生成完成";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        if (_ranges == null)
        {
            EditorGUILayout.HelpBox("无法加载 ResourceGroupRangeSO", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        // ── 分组容量表 ──
        var headerStyle = EditorStyles.boldLabel;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("分组名称", headerStyle, GUILayout.Width(120));
        EditorGUILayout.LabelField("容量", headerStyle, GUILayout.Width(60));
        EditorGUILayout.LabelField("起始 ID", headerStyle, GUILayout.Width(60));
        EditorGUILayout.LabelField("终止 ID", headerStyle, GUILayout.Width(60));
        EditorGUILayout.LabelField("用途", headerStyle);
        EditorGUILayout.LabelField("操作", headerStyle, GUILayout.Width(45));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _ranges.Groups.Count; i++)
        {
            var g = _ranges.Groups[i];
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(g.DisplayName, GUILayout.Width(120));

            EditorGUI.BeginChangeCheck();
            int newCap = EditorGUILayout.IntField(g.Capacity, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck() && newCap > 0)
            {
                g.Capacity = newCap;
                MarkDirty();
            }

            EditorGUILayout.LabelField(_ranges.GetMinId(g).ToString(), GUILayout.Width(60));
            EditorGUILayout.LabelField(_ranges.GetMaxId(g).ToString(), GUILayout.Width(60));

            EditorGUI.BeginChangeCheck();
            string newDesc = EditorGUILayout.TextField(g.Description ?? "");
            if (EditorGUI.EndChangeCheck())
            {
                g.Description = newDesc;
                MarkDirty();
            }

            if (GUILayout.Button("删除", GUILayout.Width(45)))
            {
                if (EditorUtility.DisplayDialog("删除分组",
                    $"确定删除分组 \"{g.DisplayName}\"？\n（不影响 Addressables 中的分组，仅删除这里的容量配置）",
                    "确定", "取消"))
                {
                    _ranges.Groups.RemoveAt(i);
                    MarkDirty();
                    RefreshAvailableGroups();
                    _statusText = $"已删除分组: {g.DisplayName}";
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8);

        // ── 添加新分组（从 Addressables 未添加的分组中选择） ──
        EditorGUILayout.LabelField("添加新分组", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        // 下拉选择关联主题
        if (_availableGroupNames.Length == 0)
        {
            EditorGUILayout.LabelField("（没有可添加的 Addressables 分组）", GUILayout.Width(230));
        }
        else
        {
            // 确保索引有效
            if (_selectedGroupIndex < 0) _selectedGroupIndex = 0;
            if (_selectedGroupIndex >= _availableGroupNames.Length) _selectedGroupIndex = _availableGroupNames.Length - 1;

            _selectedGroupIndex = EditorGUILayout.Popup(_selectedGroupIndex, _availableGroupNames, GUILayout.Width(160));
        }

        EditorGUILayout.LabelField("容量", GUILayout.Width(30));
        _newGroupCapacity = EditorGUILayout.IntField(_newGroupCapacity, GUILayout.Width(60));

        GUI.enabled = _selectedGroupIndex >= 0 && _selectedGroupIndex < _availableGroupNames.Length;
        if (GUILayout.Button("添加", GUILayout.Height(20)))
        {
            var name = _availableGroupNames[_selectedGroupIndex];
            _ranges.Groups.Add(new ResourceGroupCapacityEntry
            {
                GroupName = name,
                DisplayName = name,
                Capacity = _newGroupCapacity > 0 ? _newGroupCapacity : 500,
            });
            MarkDirty();
            RefreshAvailableGroups();
            _statusText = $"已添加分组: {name}";
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // ── 操作按钮 ──
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存设置", GUILayout.Height(26)))
        {
            Save();
            _statusText = "设置已保存";
        }
        if (GUILayout.Button("恢复到上次保存", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog("恢复到上次保存", "放弃所有未保存的修改，恢复到上次保存的状态？", "确定", "取消"))
            {
                _ranges = AssetDatabase.LoadAssetAtPath<ResourceGroupRangeSO>(SettingsPath);
                RefreshAvailableGroups();
                _statusText = "已恢复到上次保存的状态";
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("状态: " + _statusText, EditorStyles.helpBox);

        EditorGUILayout.EndScrollView();
    }

    /// <summary>从 Addressables 设置中读取未添加的分组名称列表。</summary>
    private void RefreshAvailableGroups()
    {
        _selectedGroupIndex = -1;

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null || _ranges == null)
        {
            _availableGroupNames = new string[0];
            return;
        }

        var existing = new System.Collections.Generic.HashSet<string>(
            _ranges.Groups
                .Select(g => g.GroupName)
                .Where(n => !string.IsNullOrEmpty(n)));

        _availableGroupNames = settings.groups
            .Where(g => g != null && !existing.Contains(g.Name))
            .Select(g => g.Name)
            .ToArray();

        if (_availableGroupNames.Length > 0)
            _selectedGroupIndex = 0;
    }

    private void LoadOrCreate()
    {
        _ranges = AssetDatabase.LoadAssetAtPath<ResourceGroupRangeSO>(SettingsPath);
        if (_ranges != null) return;

        string dir = Path.GetDirectoryName(
            Path.GetFullPath(Path.Combine(Application.dataPath, "../", SettingsPath)));
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        _ranges = ResourceGroupRangeSO.CreateWithDefault();
        AssetDatabase.CreateAsset(_ranges, SettingsPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _statusText = "已创建默认分组";
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(_ranges);
    }

    private void Save()
    {
        if (_ranges == null) return;
        EditorUtility.SetDirty(_ranges);
        AssetDatabase.SaveAssets();
    }
}
