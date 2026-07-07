using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoKQuestCompletePopupUI : MonoBehaviour
{
    [Header("ROOT")]
    public Canvas targetCanvas;
    public TMP_FontAsset vietnameseFont;

    [Header("STYLE")]
    public Color overlayColor = new Color32(0, 0, 0, 145);
    public Color panelColor = new Color32(58, 36, 21, 255);
    public Color headerColor = new Color32(107, 63, 31, 255);
    public Color cardColor = new Color32(184, 117, 50, 255);
    public Color borderColor = new Color32(224, 166, 74, 255);
    public Color titleColor = new Color32(255, 241, 194, 255);
    public Color bodyColor = new Color32(232, 212, 162, 255);
    public Color buttonColor = new Color32(199, 106, 27, 255);
    public Color buttonHighlightColor = new Color32(240, 167, 58, 255);

    GameObject root;
    TMP_Text titleText;
    TMP_Text bodyText;
    Action onOpenQuest;

    void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        BuildUI();

        if (root != null)
            root.SetActive(false);
    }

    public void Show(string title, string body, Action onOpenQuestClick)
    {
        if (root == null)
            BuildUI();

        onOpenQuest = onOpenQuestClick;

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        root.SetActive(true);
        root.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    void OpenQuestPanel()
    {
        Hide();
        onOpenQuest?.Invoke();
    }

    void BuildUI()
    {
        if (targetCanvas == null)
            return;

        root = new GameObject("RoKQuestCompletePopup", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(targetCanvas.transform, false);

        Canvas c = root.GetComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 9000;

        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image overlay = root.GetComponent<Image>();
        overlay.color = overlayColor;

        RectTransform window = CreatePanel(root.transform, "PopupWindow", new Vector2(680, 330), Vector2.zero, panelColor, true);

        RectTransform header = CreatePanel(window, "Header", new Vector2(680, 80), new Vector2(0, 125), headerColor, true);

        titleText = CreateText(
            header,
            "TitleText",
            "Nhiệm vụ hoàn thành",
            Vector2.zero,
            new Vector2(620, 70),
            38,
            titleColor,
            TextAlignmentOptions.Center,
            true
        );

        RectTransform bodyCard = CreatePanel(window, "BodyCard", new Vector2(600, 135), new Vector2(0, 20), cardColor, true);

        bodyText = CreateText(
            bodyCard,
            "BodyText",
            "",
            Vector2.zero,
            new Vector2(540, 100),
            24,
            bodyColor,
            TextAlignmentOptions.Center,
            false
        );

        Button openButton = CreateButton(
            window,
            "OpenQuestButton",
            "Mở bảng nhiệm vụ",
            new Vector2(0, -115),
            new Vector2(260, 58),
            buttonColor
        );

        openButton.onClick.AddListener(OpenQuestPanel);

        Button closeButton = CreateButton(
            window,
            "CloseButton",
            "X",
            new Vector2(315, 125),
            new Vector2(54, 54),
            new Color32(170, 0, 0, 255)
        );

        closeButton.onClick.AddListener(Hide);
    }

    RectTransform CreatePanel(Transform parent, string name, Vector2 size, Vector2 pos, Color color, bool outline)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        if (outline)
        {
            Outline o = go.AddComponent<Outline>();
            o.effectColor = borderColor;
            o.effectDistance = new Vector2(2, -2);
            o.useGraphicAlpha = false;
        }

        return rt;
    }

    TMP_Text CreateText(Transform parent, string name, string value, Vector2 pos, Vector2 size, int fontSize, Color color, TextAlignmentOptions alignment, bool bold)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;

        if (vietnameseFont != null)
            text.font = vietnameseFont;

        text.outlineColor = new Color32(43, 26, 16, 255);
        text.outlineWidth = 0.14f;

        return text;
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();
        img.color = color;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(2, -2);
        outline.useGraphicAlpha = false;

        Button btn = go.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = buttonHighlightColor;
        cb.pressedColor = new Color32(145, 70, 16, 255);
        cb.selectedColor = buttonHighlightColor;
        cb.colorMultiplier = 1;
        btn.colors = cb;

        CreateText(go.transform, "Text", label, Vector2.zero, size, 24, titleColor, TextAlignmentOptions.Center, true);

        return btn;
    }
}