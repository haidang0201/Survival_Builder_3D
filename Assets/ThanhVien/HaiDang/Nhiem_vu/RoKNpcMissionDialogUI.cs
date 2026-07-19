using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoKNpcMissionDialogUI : MonoBehaviour
{
    [Header("CANVAS")]
    public Canvas targetCanvas;
    public int sortingOrder = 9100;
    public bool autoBuildOnAwake = true;

    [Tooltip("Bật nếu muốn mỗi lần Play tự reset về style nghèo gỗ. Nếu muốn tự chỉnh size/màu trong Inspector thì để OFF.")]
    public bool applyPoorWoodStyleOnAwake = false;

    [Tooltip("Bật để khi Play, chỉnh Bubble Size / Offset / Font Size trong Inspector là UI đổi ngay.")]
    public bool liveEditWhilePlaying = true;

    [Tooltip("Giữ hộp thoại luôn nằm trên UI khác khi đang hiện.")]
    public bool keepOnTopWhenVisible = true;

    [Header("NPC")]
    public string npcName = "PHÓ LÝ";
    public Sprite portraitSprite;
    public TMP_FontAsset vietnameseFont;

    [Header("SPRITES (tuỳ chọn - dùng ảnh khung gỗ chạm khắc thật giống mẫu)")]
    [Tooltip("Sprite khung thoại chính (nên là ảnh 9-slice có sẵn 4 góc chạm khắc + viền vàng như ảnh mẫu). Nếu để trống sẽ dùng màu phẳng như bản cũ.")]
    public Sprite frameSprite;
    [Tooltip("Sprite ruy băng chứa tên NPC, nhô ra góc trên-trái khung (như 'PHÓ LÝ' trong ảnh mẫu). Nếu để trống sẽ dùng màu phẳng như bản cũ.")]
    public Sprite nameRibbonSprite;
    [Tooltip("Màu tint áp lên frameSprite/nameRibbonSprite. Để trắng nếu muốn giữ nguyên màu gốc của sprite.")]
    public Color frameTint = Color.white;

    [Tooltip("Bật nếu muốn ô tên NPC (PHÓ LÝ) có nền màu/ruy băng phía sau. Tắt = chỉ hiện chữ, không có nền.")]
    public bool nameRibbonHasBackground = false;

    [Header("LAYOUT - EDITABLE IN PLAY")]
    public Vector2 bubbleSize = new Vector2(1000, 380);
    public Vector2 bubbleOffset = new Vector2(-500, 380);
    [Tooltip("Kích thước ảnh NPC, tính theo bên trong khung thoại (parent = khung, không phải toàn màn hình nữa).")]
    public Vector2 portraitSize = new Vector2(260, 340);
    [Tooltip("Vị trí ảnh NPC neo góc dưới-phải BÊN TRONG khung thoại.")]
    public Vector2 portraitOffset = new Vector2(-25, 20);

    [Header("BUBBLE INNER")]
    public Vector2 innerOffsetMin = new Vector2(40, 30);
    public Vector2 innerOffsetMax = new Vector2(-40, -70);

    [Header("NAME RIBBON - EDITABLE IN PLAY")]
    [Tooltip("Vị trí ruy băng tên tính từ góc trên-trái khung, nhô lên trên viền như ảnh mẫu.")]
    public Vector2 ribbonOffset = new Vector2(68, -49);
    public Vector2 ribbonSize = new Vector2(300, 70);

    [Header("TEXT LAYOUT - EDITABLE IN PLAY")]
    public Vector2 namePosition = Vector2.zero;
    public Vector2 nameSize = new Vector2(300, 70);
    public Vector2 messagePosition = new Vector2(20, -110);
    public Vector2 messageSize = new Vector2(560, 130);
    public Vector2 tapHintPosition = new Vector2(0, -20);
    public Vector2 tapHintSize = new Vector2(603, 110);

    [Header("TAIL")]
    public bool showTail = false;
    public Vector2 tailSize = new Vector2(32, 32);
    public Vector2 tailOffset = new Vector2(-8, -18);
    public float tailRotation = 45f;

    [Header("STYLE - POOR WOOD (dùng khi không gắn frameSprite/nameRibbonSprite)")]
    public Color bubbleColor = new Color32(93, 55, 30, 245);
    public Color bubbleInnerColor = new Color32(150, 98, 50, 245);
    public Color bubbleBorderColor = new Color32(205, 139, 54, 255);
    public Color tailColor = new Color32(150, 98, 50, 245);
    public Color ribbonColor = new Color32(122, 45, 30, 255);

    public Color nameColor = new Color32(255, 224, 138, 255);
    public Color messageColor = new Color32(255, 241, 194, 255);
    public Color tapHintColor = new Color32(255, 224, 138, 210);

    [Header("TEXT")]
    public int nameFontSize = 26;
    public int messageFontSize = 26;
    public int hintFontSize = 25;
    public string tapHintMessage = "Nhấn vào màn hình để tiếp tục";
    [Tooltip("Ký tự mũi tên hiển thị 2 bên dòng tap hint, giống ảnh mẫu.")]
    public string tapHintLeftArrow = "\u2190\u2014";
    public string tapHintRightArrow = "\u2014\u2192";
    public bool showTapHint = true;

    [Header("MESSAGE WRAP / SPACING - EDITABLE IN PLAY")]
    [Tooltip("Khoảng cách giữa dòng trên và dòng dưới của nội dung thoại.")]
    public float messageLineSpacing = 10f;

    [Tooltip("Số dòng tối đa của nội dung thoại. Quá số dòng sẽ hiện dấu ...")]
    public int messageMaxLines = 3;

    [Tooltip("Tự tăng chiều cao vùng MessageText theo số dòng để chữ không bị dính nhau.")]
    public bool autoGrowMessageHeight = true;

    [Tooltip("Chiều cao ước lượng mỗi dòng thoại.")]
    public float messageLineHeight = 34f;

    private const string ROOT_NAME = "RoK_NpcMissionDialog_AutoRoot";

    GameObject root;
    RectTransform rootRT;
    Canvas rootCanvas;

    GameObject clickCatcherGO;
    Button clickCatcherButton;

    RectTransform bubbleRT;
    RectTransform bubbleInnerRT;
    RectTransform bubbleTailRT;
    RectTransform portraitRT;
    RectTransform nameRibbonRT;
    RectTransform nameRT;
    RectTransform messageRT;
    RectTransform tapHintRT;

    Image bubbleImage;
    Image bubbleInnerImage;
    Image bubbleTailImage;
    Image portraitImage;
    Image nameRibbonImage;
    Outline bubbleOutline;

    TMP_Text nameText;
    TMP_Text messageText;
    TMP_Text tapHintText;

    bool waitingContinue;

    void Awake()
    {

        if (applyPoorWoodStyleOnAwake)
            SetPoorWoodValues();

        if (autoBuildOnAwake)
            RebuildDialogUI();

        Hide();
    }

    void Update()
    {
        if (Application.isPlaying && liveEditWhilePlaying && root != null)
            ApplyLayoutAndStyle();

        if (Application.isPlaying && keepOnTopWhenVisible && root != null && root.activeSelf)
            root.transform.SetAsLastSibling();
    }

    // =====================================================
    // PUBLIC API - DÙNG CHO STARTUP TUTORIAL / QUEST ROUTER
    // =====================================================

    public IEnumerator ShowAndWait(string message)
    {
        EnsureBuilt();

        root.SetActive(true);
        root.transform.SetAsLastSibling();

        SetMessage(message);
        ApplyLayoutAndStyle();

        waitingContinue = true;
        SetClickCatcher(true);

        if (tapHintText != null)
            tapHintText.gameObject.SetActive(showTapHint);

        yield return new WaitUntil(() => waitingContinue == false);

        Hide();
    }

    public void ShowObjective(string message)
    {
        EnsureBuilt();

        root.SetActive(true);
        root.transform.SetAsLastSibling();

        SetMessage(message);
        ApplyLayoutAndStyle();

        // Quan trọng: Objective KHÔNG bắt click toàn màn hình,
        // để người chơi vẫn bấm Build / Tháp Canh / UI khác được.
        waitingContinue = false;
        SetClickCatcher(false);

        if (tapHintText != null)
            tapHintText.gameObject.SetActive(false);
    }

    public void Show(string message)
    {
        ShowObjective(message);
    }

    public void Hide()
    {
        waitingContinue = false;
        SetClickCatcher(false);

        if (tapHintText != null)
            tapHintText.gameObject.SetActive(false);

        if (root != null)
            root.SetActive(false);
    }

    public void ContinueNow()
    {
        if (!waitingContinue)
            return;

        waitingContinue = false;
        SetClickCatcher(false);
    }

    // Giữ tên này để nếu code cũ / Button cũ còn gọi thì không lỗi.
    public void OnContinueClicked()
    {
        ContinueNow();
    }

    public void OnScreenClicked()
    {
        ContinueNow();
    }

    void SetMessage(string message)
    {
        if (nameText != null)
            nameText.text = npcName;

        if (messageText != null)
            messageText.text = message;

        if (tapHintText != null)
            tapHintText.text = BuildTapHintText();
    }

    string BuildTapHintText()
    {
        return $"{tapHintLeftArrow}   {tapHintMessage}   {tapHintRightArrow}";
    }

    void SetClickCatcher(bool active)
    {
        if (clickCatcherGO != null)
            clickCatcherGO.SetActive(active);
    }

    // =====================================================
    // CONTEXT MENU
    // =====================================================

    [ContextMenu("Apply Poor Wood Style")]
    public void ApplyPoorWoodStyle()
    {
        SetPoorWoodValues();
        ApplyLayoutAndStyle();
    }

    [ContextMenu("Rebuild Dialog UI")]
    public void RebuildDialogUI()
    {
        EnsureCanvas();
        ClearOldRoot();
        BuildUI();
        ApplyLayoutAndStyle();
        Hide();
    }

    [ContextMenu("TEST Show Dialog")]
    public void TestShowDialog()
    {
        EnsureBuilt();

        root.SetActive(true);
        root.transform.SetAsLastSibling();

        SetMessage("Hãy xây Tháp Canh để phát hiện kẻ địch từ xa.");

        waitingContinue = true;
        SetClickCatcher(true);

        if (tapHintText != null)
            tapHintText.gameObject.SetActive(showTapHint);

        ApplyLayoutAndStyle();
    }

    [ContextMenu("TEST Show Objective")]
    public void TestShowObjective()
    {
        ShowObjective("Đặt Tháp Canh vào vị trí mũi tên chỉ dẫn.");
    }

    void SetPoorWoodValues()
    {
        // Màu nghèo / gỗ cũ (dùng khi không gắn sprite khung thật)
        bubbleColor = new Color32(93, 55, 30, 245);
        bubbleInnerColor = new Color32(150, 98, 50, 245);
        bubbleBorderColor = new Color32(205, 139, 54, 255);
        tailColor = bubbleInnerColor;
        ribbonColor = new Color32(122, 45, 30, 255);

        nameColor = new Color32(255, 224, 138, 255);
        messageColor = new Color32(255, 241, 194, 255);
        tapHintColor = new Color32(255, 224, 138, 210);

        // Kích thước & bố cục giống ảnh mẫu: khung rộng, ribbon tên nhô góc trên-trái,
        // portrait to bên phải, tap hint canh giữa phía dưới.
        bubbleSize = new Vector2(825, 380);
        bubbleOffset = new Vector2(-11, 476);
        portraitSize = new Vector2(230, 340);
        portraitOffset = new Vector2(-25, 20);

        innerOffsetMin = new Vector2(40, 30);
        innerOffsetMax = new Vector2(-40, -70);

        ribbonOffset = new Vector2(-10, -72);
        ribbonSize = new Vector2(300, 70);

        namePosition = Vector2.zero;
        nameSize = ribbonSize;
        messagePosition = new Vector2(32, -90);
        messageSize = new Vector2(560, 130);
        tapHintPosition = new Vector2(-94, -10);
        tapHintSize = new Vector2(603, 110);

        nameFontSize = 32;
        messageFontSize = 30;
        hintFontSize = 25;

        messageLineSpacing = 30f;
        messageMaxLines = 3;
        autoGrowMessageHeight = true;
        messageLineHeight = 34f;

        showTail = false;
        tailSize = new Vector2(32, 32);
        tailOffset = new Vector2(-8, -18);
        tailRotation = 45f;
    }

    // =====================================================
    // BUILD UI
    // =====================================================

    void EnsureBuilt()
    {
        if (root == null)
            RebuildDialogUI();
    }

    void EnsureCanvas()
    {
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();
    }

    void ClearOldRoot()
    {
        if (targetCanvas == null)
            return;

        Transform old = targetCanvas.transform.Find(ROOT_NAME);

        if (old == null)
            return;

        if (Application.isPlaying)
            Destroy(old.gameObject);
        else
            DestroyImmediate(old.gameObject);
    }

    void BuildUI()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[RoKNpcMissionDialogUI] Chưa có Target Canvas.");
            return;
        }

        root = new GameObject(ROOT_NAME, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(targetCanvas.transform, false);

        rootCanvas = root.GetComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = sortingOrder;

        rootRT = root.GetComponent<RectTransform>();
        Stretch(rootRT);

        BuildClickCatcher(root.transform);
        BuildBubble(root.transform);
    }

    void BuildClickCatcher(Transform parent)
    {
        clickCatcherGO = new GameObject("ClickAnywhereCatcher", typeof(RectTransform), typeof(Image), typeof(Button));
        clickCatcherGO.transform.SetParent(parent, false);
        clickCatcherGO.transform.SetAsFirstSibling();

        RectTransform rt = clickCatcherGO.GetComponent<RectTransform>();
        Stretch(rt);

        Image img = clickCatcherGO.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.001f);
        img.raycastTarget = true;

        clickCatcherButton = clickCatcherGO.GetComponent<Button>();
        clickCatcherButton.onClick.RemoveListener(OnScreenClicked);
        clickCatcherButton.onClick.AddListener(OnScreenClicked);

        clickCatcherGO.SetActive(false);
    }

    void BuildPortrait(Transform parent)
    {
        GameObject go = new GameObject("NpcPortrait", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        portraitRT = go.GetComponent<RectTransform>();
        // Neo bottom-right theo khung thoại (parent = bubble) để ảnh NPC nằm BÊN TRONG khung,
        // không tràn ra ngoài/xuống dưới như trước.
        SetBottomRight(portraitRT);

        portraitImage = go.GetComponent<Image>();
        portraitImage.sprite = portraitSprite;
        portraitImage.color = Color.white;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
    }

    void BuildBubble(Transform parent)
    {
        GameObject bubble = new GameObject("DialogBubble", typeof(RectTransform), typeof(Image), typeof(Outline));
        bubble.transform.SetParent(parent, false);

        bubbleRT = bubble.GetComponent<RectTransform>();
        SetBottomRight(bubbleRT);

        bubbleImage = bubble.GetComponent<Image>();
        bubbleImage.raycastTarget = false;

        bubbleOutline = bubble.GetComponent<Outline>();
        bubbleOutline.useGraphicAlpha = false;

        BuildBubbleTail(bubble.transform);

        GameObject inner = new GameObject("DialogBubbleInner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(bubble.transform, false);

        bubbleInnerRT = inner.GetComponent<RectTransform>();

        bubbleInnerImage = inner.GetComponent<Image>();
        bubbleInnerImage.raycastTarget = false;

        // Ruy băng tên NPC - nhô lên góc trên-trái khung, giống ảnh mẫu "PHÓ LÝ"
        GameObject ribbon = new GameObject("NameRibbon", typeof(RectTransform), typeof(Image));
        ribbon.transform.SetParent(bubble.transform, false);

        nameRibbonRT = ribbon.GetComponent<RectTransform>();
        nameRibbonRT.anchorMin = nameRibbonRT.anchorMax = new Vector2(0f, 1f);
        nameRibbonRT.pivot = new Vector2(0f, 1f);

        nameRibbonImage = ribbon.GetComponent<Image>();
        nameRibbonImage.raycastTarget = false;

        nameText = CreateText(
            ribbon.transform,
            "NpcNameText",
            npcName,
            namePosition,
            nameSize,
            nameFontSize,
            nameColor,
            TextAlignmentOptions.Center,
            true,
            out nameRT
        );
        // Ribbon là parent riêng, text phủ toàn bộ ribbon và canh giữa.
        nameRT.anchorMin = Vector2.zero;
        nameRT.anchorMax = Vector2.one;
        nameRT.pivot = new Vector2(0.5f, 0.5f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;

        messageText = CreateText(
            inner.transform,
            "MessageText",
            "",
            messagePosition,
            messageSize,
            messageFontSize,
            messageColor,
            TextAlignmentOptions.TopLeft,
            false,
            out messageRT
        );

        ApplyMessageTextSettings();

        tapHintText = CreateText(
            inner.transform,
            "TapHintText",
            BuildTapHintText(),
            tapHintPosition,
            tapHintSize,
            hintFontSize,
            tapHintColor,
            TextAlignmentOptions.Center,
            false,
            out tapHintRT
        );
        // Tap hint neo theo cạnh dưới, canh giữa theo chiều ngang - giống ảnh mẫu.
        tapHintRT.anchorMin = new Vector2(0.5f, 0f);
        tapHintRT.anchorMax = new Vector2(0.5f, 0f);
        tapHintRT.pivot = new Vector2(0.5f, 0f);

        ApplyTapHintTextSettings();
        tapHintText.gameObject.SetActive(false);

        // Ảnh NPC được build SAU cùng, parent = bubble (khung thoại) để nằm BÊN TRONG khung,
        // và vẽ đè lên trên panel gỗ chứ không nằm ngoài/dưới khung.
        BuildPortrait(bubble.transform);
        portraitRT.SetAsLastSibling();
    }

    void BuildBubbleTail(Transform bubble)
    {
        GameObject tail = new GameObject("DialogBubbleTail", typeof(RectTransform), typeof(Image));
        tail.transform.SetParent(bubble, false);
        tail.transform.SetAsFirstSibling();

        bubbleTailRT = tail.GetComponent<RectTransform>();
        bubbleTailRT.anchorMin = bubbleTailRT.anchorMax = new Vector2(1f, 0.5f);
        bubbleTailRT.pivot = new Vector2(0.5f, 0.5f);

        bubbleTailImage = tail.GetComponent<Image>();
        bubbleTailImage.raycastTarget = false;
    }

    TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        Vector2 pos,
        Vector2 size,
        int fontSize,
        Color color,
        TextAlignmentOptions align,
        bool bold,
        out RectTransform rt
    )
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = align;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;

        if (vietnameseFont != null)
            text.font = vietnameseFont;

        text.outlineColor = new Color32(43, 26, 16, 255);
        text.outlineWidth = 0.08f;

        return text;
    }

    void ApplyLayoutAndStyle()
    {
        if (rootCanvas != null)
        {
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = sortingOrder;
        }

        if (rootRT != null)
            Stretch(rootRT);

        if (bubbleRT != null)
        {
            bubbleRT.sizeDelta = bubbleSize;
            bubbleRT.anchoredPosition = bubbleOffset;
        }

        if (portraitRT != null)
        {
            portraitRT.sizeDelta = portraitSize;
            portraitRT.anchoredPosition = portraitOffset;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = portraitSprite;
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;
        }

        // Khung thoại: nếu có frameSprite thì dùng sprite 9-slice (giữ nguyên góc chạm khắc),
        // nếu không thì fallback về màu phẳng + Outline như bản cũ.
        if (bubbleImage != null)
        {
            if (frameSprite != null)
            {
                bubbleImage.sprite = frameSprite;
                bubbleImage.type = Image.Type.Sliced;
                bubbleImage.color = frameTint;
                if (bubbleOutline != null)
                    bubbleOutline.enabled = false;
            }
            else
            {
                bubbleImage.sprite = null;
                bubbleImage.type = Image.Type.Simple;
                bubbleImage.color = bubbleColor;
                if (bubbleOutline != null)
                {
                    bubbleOutline.enabled = true;
                    bubbleOutline.effectColor = bubbleBorderColor;
                    bubbleOutline.effectDistance = new Vector2(3f, -3f);
                    bubbleOutline.useGraphicAlpha = false;
                }
            }
        }

        if (bubbleInnerRT != null)
            Stretch(bubbleInnerRT, innerOffsetMin, innerOffsetMax);

        if (bubbleInnerImage != null)
        {
            // Khi đã có frameSprite thật, phần inner không cần lớp màu phủ riêng nữa.
            bubbleInnerImage.color = frameSprite != null ? Color.clear : bubbleInnerColor;
        }

        if (bubbleTailRT != null)
        {
            bubbleTailRT.gameObject.SetActive(showTail && frameSprite == null);
            bubbleTailRT.sizeDelta = tailSize;
            bubbleTailRT.anchoredPosition = tailOffset;
            bubbleTailRT.localRotation = Quaternion.Euler(0, 0, tailRotation);
        }

        if (bubbleTailImage != null)
            bubbleTailImage.color = tailColor;

        // Ruy băng tên
        if (nameRibbonRT != null)
        {
            nameRibbonRT.sizeDelta = ribbonSize;
            nameRibbonRT.anchoredPosition = ribbonOffset;
        }

        if (nameRibbonImage != null)
        {
            if (!nameRibbonHasBackground)
            {
                // Chỉ hiện chữ tên NPC, không có nền/ruy băng phía sau.
                nameRibbonImage.sprite = null;
                nameRibbonImage.type = Image.Type.Simple;
                nameRibbonImage.color = Color.clear;
            }
            else if (nameRibbonSprite != null)
            {
                nameRibbonImage.sprite = nameRibbonSprite;
                nameRibbonImage.type = Image.Type.Sliced;
                nameRibbonImage.color = frameTint;
            }
            else
            {
                nameRibbonImage.sprite = null;
                nameRibbonImage.type = Image.Type.Simple;
                nameRibbonImage.color = ribbonColor;
            }
        }

        ApplyTextBasic(nameText, npcName, nameFontSize, nameColor);
        ApplyText(messageText, messageRT, null, messagePosition, GetRuntimeMessageSize(), messageFontSize, messageColor);
        ApplyText(tapHintText, tapHintRT, BuildTapHintText(), tapHintPosition, tapHintSize, hintFontSize, tapHintColor);

        ApplyMessageTextSettings();
        ApplyTapHintTextSettings();
    }

    void ApplyTextBasic(TMP_Text text, string value, int fontSize, Color color)
    {
        if (text == null)
            return;

        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;

        if (vietnameseFont != null)
            text.font = vietnameseFont;
    }

    Vector2 GetRuntimeMessageSize()
    {
        if (!autoGrowMessageHeight)
            return messageSize;

        float neededHeight = Mathf.Max(
            messageSize.y,
            messageLineHeight * Mathf.Max(1, messageMaxLines) + messageLineSpacing * Mathf.Max(0, messageMaxLines - 1)
        );

        return new Vector2(messageSize.x, neededHeight);
    }

    void ApplyMessageTextSettings()
    {
        if (messageText == null)
            return;

        messageText.enableWordWrapping = true;
        messageText.overflowMode = TextOverflowModes.Ellipsis;
        messageText.maxVisibleLines = Mathf.Max(1, messageMaxLines);
        messageText.lineSpacing = messageLineSpacing;
        messageText.paragraphSpacing = 0f;
        messageText.margin = Vector4.zero;
        messageText.alignment = TextAlignmentOptions.TopLeft;
    }

    void ApplyTapHintTextSettings()
    {
        if (tapHintText == null)
            return;

        tapHintText.enableWordWrapping = false;
        tapHintText.overflowMode = TextOverflowModes.Ellipsis;
        tapHintText.maxVisibleLines = 1;
        tapHintText.lineSpacing = 0f;
        tapHintText.margin = Vector4.zero;
        tapHintText.alignment = TextAlignmentOptions.Center;
    }

    void ApplyText(TMP_Text text, RectTransform rt, string fixedValue, Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        if (text == null || rt == null)
            return;

        if (fixedValue != null)
            text.text = fixedValue;

        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;

        if (vietnameseFont != null)
            text.font = vietnameseFont;
    }

    void SetBottomRight(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
    }

    void Stretch(RectTransform rt)
    {
        Stretch(rt, Vector2.zero, Vector2.zero);
    }

    void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }
}