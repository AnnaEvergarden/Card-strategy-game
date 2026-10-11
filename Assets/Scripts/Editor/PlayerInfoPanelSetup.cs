using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编辑器工具：一键创建"玩家信息面板"预制体并完成打开链路的全部注册，
/// 同时在大厅预制体中生成顶部区域（Top Area：玩家信息挂件 + 金币 / 钻石文本）。
/// 菜单：CardGame/工具/生成玩家信息面板并接入大厅
///
/// 打开链路（与其它面板一致）：PanelId → PanelConfig.ResourceId → ResourceConfig.Address
/// → Addressables（Prefab_Panel 组）→ 预制体实例化。
/// 团结工程的 .meta guid 为 base64 形态、无法手写引用，因此预制体一律由本工具经
/// PrefabUtility 生成，脚本引用交给引擎写入。
/// </summary>
public static class PlayerInfoPanelSetup
{
    /// <summary>面板预制体目录。</summary>
    private const string PanelDir = "Assets/Addressables/Prefabs/UI/Panel";

    /// <summary>玩家信息面板预制体路径。</summary>
    private const string PanelPrefabPath = PanelDir + "/PlayerInfoPanel.prefab";

    /// <summary>大厅面板预制体路径。</summary>
    private const string LobbyPrefabPath = PanelDir + "/LobbyPanel.prefab";

    /// <summary>面板配置 SO 目录。</summary>
    private const string PanelConfigDir = "Assets/Config/Config_SO/Config_Panel/Panel";

    /// <summary>玩家信息面板配置 SO 路径。</summary>
    private const string PanelConfigAssetPath = PanelConfigDir + "/PanelConfig_PlayerInfoPanel.asset";

    /// <summary>面板所在 Addressables 分组。</summary>
    private const string PanelGroupName = "Prefab_Panel";

    /// <summary>面板 ID（与 PanelBase._panelId、PanelConfig.PanelId、Addressables address 一致）。</summary>
    private const string PanelId = "PlayerInfoPanel";

    /// <summary>顶部区域高度。</summary>
    private const float TopAreaHeight = 120f;

    /// <summary>
    /// 创建玩家信息面板预制体并完成注册，随后改造大厅预制体的顶部区域。
    /// </summary>
    [MenuItem("CardGame/工具/生成玩家信息面板并接入大厅")]
    public static void Setup()
    {
        EnsureFolder(PanelDir);
        EnsureFolder(PanelConfigDir);

        // ① 生成面板预制体
        var panelAsset = CreatePanelPrefab();

        // ② 注册到 Addressables（地址 = 面板 ID）
        if (!RegisterAddressable(panelAsset))
            return;

        // ③ 重新生成资源表，自动为新资源分配 ResourceId
        ResourceConfigGenerator.Generate();
        int resourceId = ReadResourceIdFromTable(PanelId);
        if (resourceId <= 0)
        {
            Debug.LogError($"[PlayerInfoPanelSetup] 未能从 ResourceConfigTable 中解析 \"{PanelId}\" 的 ResourceId，已中止");
            return;
        }

        // ④ 创建 / 更新面板配置 SO，并刷新面板数据库
        UpsertPanelConfig(resourceId);
        DatabasePopulator.RefreshPanelDatabase();

        // ⑤ 导出运行时 JSON（PanelConfigTable.json）
        ConfigExportUtility.ExportPanelConfigsFromSO();

        // ⑥ 大厅预制体：Top Area + 玩家信息挂件 + 金币/钻石文本
        bool patched = PatchLobbyPanel();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PlayerInfoPanelSetup] 完成：\n" +
                  $"  面板预制体: {PanelPrefabPath}\n" +
                  $"  Addressables: {PanelGroupName} / {PanelId}\n" +
                  $"  ResourceId: {resourceId}\n" +
                  $"  面板配置: {PanelConfigAssetPath}\n" +
                  "  运行时配置: Assets/Config/Runtime/PanelConfigTable.json\n" +
                  (patched
                      ? "  大厅预制体: 已新建 Top Area，并接入 Gold_Text / Diamond_Text / PlayerInfoWidget"
                      : "  大厅预制体: 未找到或缺少 LobbyPanel 组件，请手动确认") +
                  "\n若 Addressables 处于 Use Existing Build 模式，请重新 Build Addressables 后运行。");

        Selection.activeObject = panelAsset;
        EditorGUIUtility.PingObject(panelAsset);
    }

    /// <summary>
    /// 生成玩家信息面板预制体：根节点为全屏 Image（底色 + 遮挡点击），
    /// 内部排版调用面板自身的 PlayerInfoPanel.BuildStructure 生成——
    /// 与运行时保底共用同一套布局代码，因此预制体即为可直接编辑的正式 UI。
    /// </summary>
    private static GameObject CreatePanelPrefab()
    {
        var root = new GameObject(PanelId, typeof(RectTransform), typeof(Image), typeof(PlayerInfoPanel));

        var rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        var background = root.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.09f, 0.78f);
        background.raycastTarget = true;

        // 生成正式 UI 排版（顶栏返回按钮 + 左半侧预留区 + 右半侧信息区）
        PlayerInfoPanel.BuildStructure(rect);
        AssignDefaultFonts(root);
        SetLayerRecursively(root, GetUILayer());

        // 写入 PanelId + 分配控件序列化引用（运行时零查找）
        var serialized = new SerializedObject(root.GetComponent<PlayerInfoPanel>());
        SetString(serialized, "_panelId", PanelId);
        AssignPanelReferences(serialized, root.transform);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var asset = PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
        Object.DestroyImmediate(root);
        return asset;
    }

    /// <summary>
    /// 把面板内的控件按序列化字段写入预制体（运行时直接取引用，不做 Find）。
    /// </summary>
    private static void AssignPanelReferences(SerializedObject serialized, Transform root)
    {
        var rightArea = FindComponent<RectTransform>(root, "RightArea");
        var editArea = FindComponent<RectTransform>(rightArea, "EditArea");

        SetRef(serialized, "_rightArea", rightArea);
        SetRef(serialized, "_backButton", FindComponent<Button>(root, "Back_Button"));

        SetRef(serialized, "_nameValueText", FindComponent<TMP_Text>(rightArea, "Name_Value"));
        SetRef(serialized, "_nameEditButton", FindComponent<Button>(rightArea, "Name_Edit_Button"));
        SetRef(serialized, "_birthdayValueText", FindComponent<TMP_Text>(rightArea, "Birthday_Value"));
        SetRef(serialized, "_birthdayEditButton", FindComponent<Button>(rightArea, "Birthday_Edit_Button"));
        SetRef(serialized, "_levelValueText", FindComponent<TMP_Text>(rightArea, "Level_Value"));

        SetRef(serialized, "_editRoot", editArea);
        SetRef(serialized, "_editTitleText", FindComponent<TMP_Text>(editArea, "EditTitle"));
        SetRef(serialized, "_nameInput", FindComponent<TMP_InputField>(editArea, "NameInput"));
        SetRef(serialized, "_yearInput", FindComponent<TMP_InputField>(editArea, "YearInput"));
        SetRef(serialized, "_monthInput", FindComponent<TMP_InputField>(editArea, "MonthInput"));
        SetRef(serialized, "_dayInput", FindComponent<TMP_InputField>(editArea, "DayInput"));
        SetRef(serialized, "_confirmButton", FindComponent<Button>(editArea, "Confirm_Button"));
        SetRef(serialized, "_cancelButton", FindComponent<Button>(editArea, "Cancel_Button"));
        SetRef(serialized, "_errorText", FindComponent<TMP_Text>(editArea, "Error_Text"));
    }

    /// <summary>按子物体名查找指定类型组件（找不到返回 null）。</summary>
    private static T FindComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null)
            return null;

        var child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    /// <summary>给构建出的文本补默认 TMP 字体，保证预制体在编辑器中即可正常预览。</summary>
    private static void AssignDefaultFonts(GameObject root)
    {
        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
            return;

        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
            if (text.font == null)
                text.font = font;
    }

    /// <summary>把面板预制体登记到 Addressables 分组（地址 = 面板 ID，带资源表标签）。</summary>
    private static bool RegisterAddressable(GameObject asset)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[PlayerInfoPanelSetup] 未找到 Addressables 设置，请先在编辑器中打开 Addressables 窗口");
            return false;
        }

        var group = settings.FindGroup(PanelGroupName);
        if (group == null)
        {
            Debug.LogError($"[PlayerInfoPanelSetup] 未找到 Addressables 分组 \"{PanelGroupName}\"");
            return false;
        }

        if (asset == null)
        {
            Debug.LogError($"[PlayerInfoPanelSetup] 面板预制体创建失败: {PanelPrefabPath}");
            return false;
        }

        string guid = AssetDatabase.AssetPathToGUID(PanelPrefabPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"[PlayerInfoPanelSetup] 无法获取预制体 GUID: {PanelPrefabPath}");
            return false;
        }

        var entry = settings.CreateOrMoveEntry(guid, group);
        entry.address = PanelId;
        entry.SetLabel("Essential", true, true);
        entry.SetLabel("NeedResource_Config", true, true);

        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(group);
        AssetDatabase.SaveAssets();
        return true;
    }

    /// <summary>读取 ResourceConfigTable.json 中指定地址的 ResourceId（由 ResourceConfigGenerator 分配）。</summary>
    private static int ReadResourceIdFromTable(string address)
    {
        string path = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Assets/Config/Runtime/ResourceConfigTable.json"));
        if (!File.Exists(path))
        {
            Debug.LogError($"[PlayerInfoPanelSetup] 未找到 ResourceConfigTable.json: {path}");
            return 0;
        }

        var table = JsonUtility.FromJson<ResourceConfigTable>(File.ReadAllText(path));
        if (table?.Items == null)
            return 0;

        foreach (var item in table.Items)
            if (item != null && item.Address == address)
                return item.ResourceId;

        return 0;
    }

    /// <summary>创建或更新玩家信息面板的 PanelConfig SO（层级 1、关闭后缓存）。</summary>
    private static void UpsertPanelConfig(int resourceId)
    {
        var config = AssetDatabase.LoadAssetAtPath<PanelConfigSO>(PanelConfigAssetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<PanelConfigSO>();
            AssetDatabase.CreateAsset(config, PanelConfigAssetPath);
        }

        var serialized = new SerializedObject(config);
        SetString(serialized, "_panelId", PanelId);
        SetInt(serialized, "_resourceId", resourceId);
        SetInt(serialized, "_layer", 1);
        SetBool(serialized, "_cacheOnClose", true);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    /// <summary>改造大厅预制体：新建 Top Area，并在其下放置挂件与货币文本，最后接线。</summary>
    private static bool PatchLobbyPanel()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath) == null)
            return false;

        var lobbyRoot = PrefabUtility.LoadPrefabContents(LobbyPrefabPath);
        try
        {
            var lobbyPanel = lobbyRoot.GetComponent<LobbyPanel>();
            if (lobbyPanel == null)
            {
                Debug.LogError("[PlayerInfoPanelSetup] LobbyPanel 预制体根节点上未找到 LobbyPanel 组件");
                return false;
            }

            var topArea = EnsureTopArea(lobbyRoot);
            var widget = EnsurePlayerInfoWidget(topArea);
            var goldText = EnsureCurrencyText(topArea, "Gold_Text", "金币: 0",
                new Vector2(-120f, -26f), new Color(1f, 0.84f, 0.4f));
            var diamondText = EnsureCurrencyText(topArea, "Diamond_Text", "钻石: 0",
                new Vector2(120f, -26f), new Color(0.65f, 0.92f, 1f));

            var serialized = new SerializedObject(lobbyPanel);
            SetRef(serialized, "_goldText", goldText);
            SetRef(serialized, "_diamondText", diamondText);
            SetRef(serialized, "_playerInfoWidget", widget);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(lobbyRoot, LobbyPrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(lobbyRoot);
        }
    }

    /// <summary>确保大厅根节点下存在顶部区域（横向铺满、贴顶的条带，置于最上层）。</summary>
    private static RectTransform EnsureTopArea(GameObject lobbyRoot)
    {
        var existing = lobbyRoot.transform.Find("Top Area");
        if (existing != null)
            return (RectTransform)existing;

        var topAreaObject = new GameObject("Top Area", typeof(RectTransform));
        topAreaObject.layer = GetUILayer();

        var rect = (RectTransform)topAreaObject.transform;
        rect.SetParent(lobbyRoot.transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, TopAreaHeight);
        rect.SetAsLastSibling();
        return rect;
    }

    /// <summary>确保顶部区域左上角存在玩家信息挂件（复用运行时构建逻辑，保证结构一致）。</summary>
    private static PlayerInfoWidget EnsurePlayerInfoWidget(RectTransform topArea)
    {
        var existing = topArea.GetComponentInChildren<PlayerInfoWidget>(true);
        if (existing != null)
            return existing;

        var widget = PlayerInfoWidget.Create(topArea);
        SetLayerRecursively(widget.gameObject, GetUILayer());
        return widget;
    }

    /// <summary>确保顶部区域中上方存在货币文本（锚点：顶部居中）。</summary>
    private static TMP_Text EnsureCurrencyText(RectTransform topArea, string name, string text,
        Vector2 anchoredPosition, Color color)
    {
        var existing = topArea.Find(name);
        if (existing != null)
        {
            var existingText = existing.GetComponent<TMP_Text>();
            if (existingText != null)
                return existingText;
        }

        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = GetUILayer();

        var rect = (RectTransform)textObject.transform;
        rect.SetParent(topArea, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(240f, 40f);

        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;

        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null)
            label.font = defaultFont;

        return label;
    }

    /// <summary>获取 UI 层索引（缺失时回退为 5）。</summary>
    private static int GetUILayer()
    {
        int layer = LayerMask.NameToLayer("UI");
        return layer >= 0 ? layer : 5;
    }

    /// <summary>递归设置层。</summary>
    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    /// <summary>写入字符串序列化字段（字段缺失时忽略）。</summary>
    private static void SetString(SerializedObject serialized, string field, string value)
    {
        var prop = serialized.FindProperty(field);
        if (prop != null)
            prop.stringValue = value;
    }

    /// <summary>写入整型序列化字段（字段缺失时忽略）。</summary>
    private static void SetInt(SerializedObject serialized, string field, int value)
    {
        var prop = serialized.FindProperty(field);
        if (prop != null)
            prop.intValue = value;
    }

    /// <summary>写入布尔序列化字段（字段缺失时忽略）。</summary>
    private static void SetBool(SerializedObject serialized, string field, bool value)
    {
        var prop = serialized.FindProperty(field);
        if (prop != null)
            prop.boolValue = value;
    }

    /// <summary>写入对象引用序列化字段（字段缺失时忽略）。</summary>
    private static void SetRef(SerializedObject serialized, string field, Object value)
    {
        var prop = serialized.FindProperty(field);
        if (prop != null)
            prop.objectReferenceValue = value;
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
