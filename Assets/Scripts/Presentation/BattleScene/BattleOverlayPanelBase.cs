using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗内浮层面板基类：技能选择/换牌/表情等叠加在战斗主界面上的轻量面板。
/// 统一处理"关闭自身"与"按子物体名解析控件"（场景 UI 控件经名称解析，
/// 避免依赖序列化引用；[SerializeField] 有值时优先使用）。
/// </summary>
public abstract class BattleOverlayPanelBase : PanelBase
{
    /// <summary>
    /// 关闭自身：经 UIService 统一关闭以保持面板栈一致；服务不可用时直接隐藏。
    /// </summary>
    protected void CloseSelf()
    {
        if (UIService != null && !string.IsNullOrEmpty(PanelId))
        {
            UIService.CloseAsync(PanelId).Forget();
            return;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 按子物体名查找按钮（场景控件名：CastSkill_Button / Close_Button 等）。
    /// </summary>
    /// <param name="root">面板根节点</param>
    /// <param name="childName">子物体名</param>
    /// <returns>按钮组件；不存在返回 null</returns>
    protected static Button FindButton(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        var child = root.Find(childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    /// <summary>
    /// 按名称查找子物体（不存在时可选动态创建，用于内容容器）。
    /// </summary>
    /// <param name="root">面板根节点</param>
    /// <param name="childName">子物体名</param>
    /// <param name="createIfMissing">缺失时是否创建</param>
    protected static RectTransform FindOrCreateRoot(Transform root, string childName, bool createIfMissing)
    {
        if (root == null)
            return null;

        var child = root.Find(childName);
        if (child != null)
            return child as RectTransform;

        if (!createIfMissing)
            return null;

        var created = new GameObject(childName, typeof(RectTransform));
        var rect = (RectTransform)created.transform;
        rect.SetParent(root, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(520f, 260f);
        return rect;
    }

    /// <summary>
    /// 动态创建一个文本标签（挂在指定父节点）。
    /// </summary>
    protected static TMPro.TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text,
        float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        var rect = (RectTransform)labelObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var label = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.text = text;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>
    /// 动态创建一个按钮（挂在指定父节点，含居中标签）。
    /// </summary>
    protected static Button CreateButton(RectTransform parent, string name, string labelText,
        Vector2 anchoredPosition, Vector2 sizeDelta)
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
        image.color = new Color(0.25f, 0.35f, 0.55f, 0.95f);

        var label = CreateLabel(rect, "Label", labelText, 16f, Vector2.zero, Vector2.zero);
        label.raycastTarget = false;
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return buttonObject.GetComponent<Button>();
    }
}
