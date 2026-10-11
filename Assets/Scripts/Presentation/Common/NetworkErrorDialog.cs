using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 运行时网络错误弹窗：不依赖预制体，动态创建 UGUI 弹窗。
/// ShowAsync 返回 true 表示用户选择重试，false 表示跳过/关闭。
/// </summary>
public static class NetworkErrorDialog
{
    private static readonly object Lock = new object();
    private static GameObject _instance;
    private static UniTaskCompletionSource<bool> _pending;

    /// <summary>
    /// 显示网络错误弹窗并等待用户选择。
    /// </summary>
    /// <param name="message">提示内容。</param>
    /// <param name="retryText">重试按钮文字；传 null 时只显示单个确认按钮。</param>
    /// <param name="skipText">跳过按钮文字。</param>
    /// <returns>true=重试；false=跳过/关闭。</returns>
    public static async UniTask<bool> ShowAsync(
        string message,
        string retryText = "重试",
        string skipText = "跳过")
    {
        Close();

        var tcs = new UniTaskCompletionSource<bool>();
        lock (Lock)
        {
            _pending = tcs;
        }

        CreateDialog(message, retryText, skipText, tcs);
        return await tcs.Task;
    }

    private static void CreateDialog(
        string message,
        string retryText,
        string skipText,
        UniTaskCompletionSource<bool> tcs)
    {
        EnsureEventSystem();

        var root = new GameObject("NetworkErrorDialog", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _instance = root;

        CreateImage(root.transform, "Mask", new Color(0f, 0f, 0f, 0.55f),
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var panel = CreateImage(root.transform, "Panel", Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, -110f), new Vector2(260f, 110f));

        CreateText(panel.transform, "Title", "更新失败", 20, new Color(0.15f, 0.15f, 0.15f),
            TextAnchor.UpperCenter, new Vector2(0f, 62f), new Vector2(460f, 32f));

        CreateText(panel.transform, "Message", message, 16, new Color(0.25f, 0.25f, 0.25f),
            TextAnchor.MiddleCenter, new Vector2(0f, 6f), new Vector2(460f, 76f));

        if (!string.IsNullOrEmpty(retryText))
        {
            CreateButton(panel.transform, "RetryButton", retryText,
                () => Finish(tcs, true), new Vector2(-74f, -66f), new Vector2(140f, 44f));
        }

        CreateButton(panel.transform, "SkipButton",
            string.IsNullOrEmpty(skipText) ? "关闭" : skipText,
            () => Finish(tcs, false), new Vector2(74f, -66f), new Vector2(140f, 44f));
    }

    private static void Finish(UniTaskCompletionSource<bool> tcs, bool result)
    {
        lock (Lock)
        {
            _pending = null;
        }

        Close();
        tcs.TrySetResult(result);
    }

    private static void Close()
    {
        if (_instance != null)
        {
            UnityEngine.Object.Destroy(_instance);
            _instance = null;
        }
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(
        Transform parent,
        string name,
        string content,
        int fontSize,
        Color color,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Action onClick,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.17f, 0.34f, 0.60f, 1f);

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(() => onClick());

        var labelGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelGo.GetComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.text = label;
        labelText.fontSize = 16;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;

        return button;
    }
}
