using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 面板控件工具：按子物体名解析场景控件、动态创建文本与按钮。
/// 供各面板在"场景控件优先、缺失时动态构建"模式下复用（避免各面板重复实现）。
/// </summary>
public static class PanelWidgetUtil
{
    /// <summary>
    /// 按子物体名查找指定类型组件（场景控件解析；找不到返回 null）。
    /// </summary>
    /// <param name="root">面板根节点</param>
    /// <param name="childName">子物体名（如 "DeckRoot"、"Back_Button"）</param>
    public static T FindChild<T>(Transform root, string childName) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        var child = root.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    /// <summary>
    /// 查找或动态创建内容容器（用于场景未提供容器时的兜底）。
    /// </summary>
    public static RectTransform FindOrCreateRoot(Transform root, string childName,
        Vector2 sizeDelta = default)
    {
        if (root == null)
            return null;

        var existing = root.Find(childName);
        if (existing != null)
            return existing as RectTransform;

        var created = new GameObject(childName, typeof(RectTransform));
        var rect = (RectTransform)created.transform;
        rect.SetParent(root, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta == default ? new Vector2(520f, 260f) : sizeDelta;
        return rect;
    }

    /// <summary>
    /// 动态创建文本标签。
    /// </summary>
    public static TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text,
        float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)labelObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>
    /// 动态创建按钮（含居中标签）。
    /// </summary>
    public static Button CreateButton(RectTransform parent, string name, string labelText,
        Vector2 anchoredPosition, Vector2 sizeDelta,
        Color? backgroundColor = null)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)buttonObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor ?? new Color(0.25f, 0.35f, 0.55f, 0.95f);

        var label = CreateLabel(rect, "Label", labelText, 16f, Vector2.zero, Vector2.zero);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return buttonObject.GetComponent<Button>();
    }

    /// <summary>
    /// 设置按钮底色（选中态/常态高亮）。
    /// </summary>
    public static void SetButtonSelected(Button button, bool selected)
    {
        if (button == null)
            return;

        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected
                ? new Color(0.3f, 0.7f, 0.4f, 0.95f)
                : new Color(0.25f, 0.35f, 0.55f, 0.95f);
    }
}
