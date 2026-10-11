using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编辑器工具：一键生成"编队侧边栏"与其"行条目"预制体（布局已搭好，替换图片资源即可），
/// 并自动把两个预制体接到 FleetPanel 预制体的 _sidebarPrefab / _fleetRowPrefab 字段上。
/// 菜单：CardGame/工具/生成编队侧边栏预制体
/// </summary>
public static class FleetSidebarPrefabCreator
{
    private const string WidgetDir = "Assets/Addressables/Prefabs/UI/Widget/FleetPanel";
    private const string SidebarPrefabPath = WidgetDir + "/FleetSidebar.prefab";
    private const string RowPrefabPath = WidgetDir + "/FleetSidebarRow.prefab";
    private const string FleetPanelPrefabPath = "Assets/Addressables/Prefabs/UI/Panel/FleetPanel.prefab";

    /// <summary>侧边栏尺寸（与 FleetPanel.SidebarWidth 一致；高度由锚点拉伸决定）。</summary>
    private static readonly Vector2 SidebarSize = new Vector2(360f, 1080f);

    /// <summary>底部"添加编队"预留高度。</summary>
    private const float BottomReserve = 76f;

    /// <summary>行高。</summary>
    private const float RowHeight = 46f;

    /// <summary>
    /// 生成侧边栏与行预制体，并接入 FleetPanel 预制体。
    /// </summary>
    [MenuItem("CardGame/工具/生成编队侧边栏预制体")]
    public static void CreatePrefabs()
    {
        EnsureFolder(WidgetDir);

        var rowAsset = CreateRowPrefab();
        var sidebarAsset = CreateSidebarPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var assigned = TryAssignToFleetPanel(rowAsset);

        Debug.Log("[FleetSidebarPrefabCreator] 已生成：\n" +
                  $"  {SidebarPrefabPath}\n  {RowPrefabPath}\n" +
                  (assigned
                      ? "已自动接入 FleetPanel 预制体的 _sidebarPrefab / _fleetRowPrefab。"
                      : "请手动把这两个预制体拖入 FleetPanel 的 _sidebarPrefab / _fleetRowPrefab。") +
                  "\n接下来替换 ShipIcon/背景等图片资源即可。");
        Selection.activeObject = sidebarAsset;
        EditorGUIUtility.PingObject(sidebarAsset);
    }

    /// <summary>创建行条目预制体：根（Image+Button）+ Name_Text。</summary>
    private static GameObject CreateRowPrefab()
    {
        var root = new GameObject("FleetSidebarRow", typeof(RectTransform), typeof(Image), typeof(Button));
        var rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(340f, RowHeight);

        var background = root.GetComponent<Image>();
        background.color = new Color(0.18f, 0.21f, 0.27f, 0.95f);

        var button = root.GetComponent<Button>();
        button.targetGraphic = background;

        var label = CreateText(rootRect, "Name_Text", "编队 1", 18f, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);

        var asset = PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
        Object.DestroyImmediate(root);
        return asset;
    }

    /// <summary>创建侧边栏预制体：根（Image+ScrollRect）+ Viewport（RectMask2D）+ Rows（VerticalLayoutGroup+ContentSizeFitter）+ AddFleet_Button。</summary>
    private static GameObject CreateSidebarPrefab()
    {
        var root = new GameObject("FleetSidebar", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = SidebarSize;

        var background = root.GetComponent<Image>();
        background.color = new Color(0.07f, 0.08f, 0.11f, 0.96f);

        // 可视区（裁剪）
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        var viewportRect = (RectTransform)viewport.transform;
        viewportRect.SetParent(rootRect, false);
        Stretch(viewportRect);
        viewportRect.offsetMin = new Vector2(0f, BottomReserve);
        viewportRect.offsetMax = Vector2.zero;

        // 行容器：纵向布局 + 内容高度自适应
        var rows = new GameObject("Rows", typeof(RectTransform));
        var rowsRect = (RectTransform)rows.transform;
        rowsRect.SetParent(viewportRect, false);
        rowsRect.anchorMin = new Vector2(0f, 1f);
        rowsRect.anchorMax = new Vector2(1f, 1f);
        rowsRect.pivot = new Vector2(0.5f, 1f);
        rowsRect.anchoredPosition = Vector2.zero;
        rowsRect.sizeDelta = Vector2.zero;

        var layout = rows.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = rows.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = root.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = rowsRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;

        // 底部"添加编队"
        var addButtonObject = new GameObject("AddFleet_Button",
            typeof(RectTransform), typeof(Image), typeof(Button));
        var addRect = (RectTransform)addButtonObject.transform;
        addRect.SetParent(rootRect, false);
        addRect.anchorMin = new Vector2(0.5f, 0f);
        addRect.anchorMax = new Vector2(0.5f, 0f);
        addRect.pivot = new Vector2(0.5f, 0f);
        addRect.anchoredPosition = new Vector2(0f, 18f);
        addRect.sizeDelta = new Vector2(300f, RowHeight);

        var addImage = addButtonObject.GetComponent<Image>();
        addImage.color = new Color(0.25f, 0.35f, 0.55f, 0.95f);
        addButtonObject.GetComponent<Button>().targetGraphic = addImage;

        var addLabel = CreateText(addRect, "Label", "+ 添加编队", 17f, TextAlignmentOptions.Center);
        Stretch(addLabel.rectTransform);

        var asset = PrefabUtility.SaveAsPrefabAsset(root, SidebarPrefabPath);
        Object.DestroyImmediate(root);
        return asset;
    }

    /// <summary>把两个预制体接入 FleetPanel 预制体（存在时）。</summary>
    private static bool TryAssignToFleetPanel(GameObject rowAsset)
    {
        if (!File.Exists(FleetPanelPrefabPath))
            return false;

        var sidebarAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SidebarPrefabPath);
        var panelRoot = PrefabUtility.LoadPrefabContents(FleetPanelPrefabPath);
        try
        {
            var panel = panelRoot.GetComponent<FleetPanel>()
                        ?? panelRoot.GetComponentInChildren<FleetPanel>(true);
            if (panel == null)
                return false;

            var serialized = new SerializedObject(panel);
            var sidebarProp = serialized.FindProperty("_sidebarPrefab");
            var rowProp = serialized.FindProperty("_fleetRowPrefab");
            if (sidebarProp == null || rowProp == null)
                return false;

            sidebarProp.objectReferenceValue = sidebarAsset;
            rowProp.objectReferenceValue = rowAsset;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(panelRoot, FleetPanelPrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(panelRoot);
        }
    }

    /// <summary>创建 TMP 文本子物体。</summary>
    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string text,
        float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)textObject.transform;
        rect.SetParent(parent, false);

        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>铺满父节点。</summary>
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 4f);
        rect.offsetMax = new Vector2(-8f, -4f);
    }

    /// <summary>确保目录存在（逐级创建）。</summary>
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parts = folderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
