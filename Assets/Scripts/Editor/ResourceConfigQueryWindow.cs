using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Resource 配置查询工具：读取 ResourceConfigTable.json，按 ResourceId 排序显示。
/// 支持搜索过滤、表头排序。
/// Tools/Resource/配置查询
/// </summary>
public class ResourceConfigQueryWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private string _searchText = "";
    private List<ResourceConfig> _allEntries = new List<ResourceConfig>();
    private List<ResourceConfig> _filtered = new List<ResourceConfig>();
    private string _statusText = "就绪";
    private bool _loaded;

    // 排序状态
    private string _sortField = "ResourceId"; // 默认按 ID 排序
    private bool _sortAsc = true;

    // 搜索列开关
    private bool _searchId = true;
    private bool _searchAddress = true;
    private bool _searchType = true;
    private bool _searchGroup = true;

    private const string DefaultJsonPath = "Assets/Config/Runtime/ResourceConfigTable.json";

    [MenuItem("Tools/Resource/配置查询")]
    private static void Open()
    {
        GetWindow<ResourceConfigQueryWindow>("Resource 配置查询").minSize = new Vector2(780, 400);
    }

    private void OnEnable() { LoadData(); }

    private void LoadData()
    {
        _allEntries.Clear();
        string jsonPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", DefaultJsonPath));

        if (!File.Exists(jsonPath))
        {
            _statusText = $"未找到配置文件: {DefaultJsonPath}，请先生成配置";
            _loaded = true;
            ApplyFilter();
            return;
        }

        string json = File.ReadAllText(jsonPath);
        var table = JsonUtility.FromJson<ResourceConfigTable>(json);
        if (table?.Items != null)
            _allEntries = table.Items;
        _loaded = true;
        ApplyFilter();
        _statusText = $"已加载 {_allEntries.Count} 条";
    }

    private void OnGUI()
    {
        // ── 工具栏 ──
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            LoadData();
        EditorGUILayout.LabelField(_statusText, GUILayout.Width(200));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
        string newSearch = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(160));
        if (newSearch != _searchText) { _searchText = newSearch; ApplyFilter(); }
        if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
        { _searchText = ""; ApplyFilter(); GUI.FocusControl(null); }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("搜索列:", GUILayout.Width(50));
        _searchId = GUILayout.Toggle(_searchId, "ID", GUILayout.Width(32));
        _searchAddress = GUILayout.Toggle(_searchAddress, "地址", GUILayout.Width(42));
        _searchType = GUILayout.Toggle(_searchType, "类型", GUILayout.Width(42));
        _searchGroup = GUILayout.Toggle(_searchGroup, "分组", GUILayout.Width(42));
        EditorGUILayout.EndHorizontal();

        if (!_loaded) return;

        // ── 表头（带排序） ──
        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } };
        var bgColor = new Color(0.25f, 0.25f, 0.25f, 1f);

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        DrawSortableHeader("ID", "ResourceId", 60, headerStyle, bgColor);
        DrawSortableHeader("Address", "Address", 0, headerStyle, bgColor);
        DrawSortableHeader("类型", "AssetType", 100, headerStyle, bgColor);
        DrawSortableHeader("分组", "Group", 130, headerStyle, bgColor);

        EditorGUILayout.EndHorizontal();

        // ── 数据行 ──
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        var rowStyle = new GUIStyle(EditorStyles.label) { fixedHeight = 20 };

        foreach (var entry in _filtered)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(entry.ResourceId.ToString(), GUILayout.Width(60));
            EditorGUILayout.LabelField(entry.Address, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(entry.AssetType, GUILayout.Width(100));
            EditorGUILayout.LabelField(entry.Group, GUILayout.Width(130));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // ── 底部统计 ──
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"共 {_allEntries.Count} 条");
        if (_filtered.Count != _allEntries.Count)
            EditorGUILayout.LabelField($"（筛选后 {_filtered.Count} 条）");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSortableHeader(string label, string field, float width, GUIStyle style, Color bg)
    {
        var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width > 0 ? width : 10), GUILayout.ExpandWidth(width <= 0));
        EditorGUI.DrawRect(rect, bg);

        string display = label;
        if (_sortField == field)
            display += _sortAsc ? " ▲" : " ▼";

        if (GUI.Button(rect, display, style))
        {
            if (_sortField == field)
                _sortAsc = !_sortAsc;
            else
            { _sortField = field; _sortAsc = true; }
            ApplyFilter();
        }

        // 占位让剩余空间留给 Address
        if (width <= 0)
            GUILayout.Label("", GUILayout.ExpandWidth(true));
    }

    private void ApplyFilter()
    {
        var query = _allEntries.AsEnumerable();

        // 搜索过滤
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            string kw = _searchText.Trim().ToLower();
            query = query.Where(e =>
                (_searchId && e.ResourceId.ToString().Contains(kw)) ||
                (_searchAddress && e.Address?.ToLower().Contains(kw) == true) ||
                (_searchType && e.AssetType?.ToLower().Contains(kw) == true) ||
                (_searchGroup && e.Group?.ToLower().Contains(kw) == true)
            );
        }

        // 排序
        query = _sortField switch
        {
            "Address" => _sortAsc ? query.OrderBy(e => e.Address) : query.OrderByDescending(e => e.Address),
            "AssetType" => _sortAsc ? query.OrderBy(e => e.AssetType) : query.OrderByDescending(e => e.AssetType),
            "Group" => _sortAsc ? query.OrderBy(e => e.Group) : query.OrderByDescending(e => e.Group),
            _ => _sortAsc ? query.OrderBy(e => e.ResourceId) : query.OrderByDescending(e => e.ResourceId),
        };

        _filtered = query.ToList();
        Repaint();
    }
}
