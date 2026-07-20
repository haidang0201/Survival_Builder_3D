using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI cảnh báo "Kẻ địch xuất hiện!" hiện ở đầu màn hình khi Tháp Canh
/// (WatchTowerAI) quét thấy quái/kẻ địch trong tầm. Tự dựng UI bằng code,
/// KHÔNG cần kéo prefab — chỉ cần add script này vào 1 GameObject bất kỳ
/// trong scene (vd cùng chỗ với Canvas chính) là dùng được ngay.
///
/// CÁCH DÙNG:
/// 1. Add script này vào 1 GameObject trong scene (chỉ cần 1 cái duy nhất).
/// 2. Trong WatchTowerAI, không cần gán gì thêm — nó sẽ tự tìm qua
///    RoKEnemyAlertUI.Instance và gọi ShowAlert()/HideAlert() khi
///    phát hiện/mất dấu kẻ địch.
/// 3. Muốn đổi icon cảnh báo: gán field "warningIconSprite" trong Inspector,
///    để trống thì dùng icon tam giác "!" dựng sẵn bằng code.
///
/// CHỈNH TRONG LÚC PLAY (MỚI):
/// - Bấm Play, chọn GameObject đang gắn script này trong Hierarchy.
/// - Trong Inspector, kéo/sửa panelSize, anchoredPositionFromCenter,
///   panelColor, borderColor, textColor... -> panel sẽ cập nhật NGAY LẬP
///   TỨC trên màn hình game, kể cả khi banner đang hiện hay đang ẩn.
/// - Chuột phải (hoặc bấm icon 3 chấm) vào tên component "Rok Enemy Alert UI"
///   trên đầu Inspector -> chọn "TEST: Hiện cảnh báo" / "TEST: Ẩn cảnh báo"
///   để bật/tắt banner ngay mà không cần WatchTowerAI kích hoạt thật.
/// </summary>
public class RoKEnemyAlertUI : MonoBehaviour
{
    public static RoKEnemyAlertUI Instance { get; private set; }

    [Header("CANVAS")]
    public Canvas targetCanvas;
    public int sortingOrder = 9500;

    [Header("FONT & ICON")]
    public TMP_FontAsset vietnameseFont;
    [Tooltip("Icon cảnh báo (tam giác/chấm than...). Để trống -> dùng icon dựng sẵn bằng code.")]
    public Sprite warningIconSprite;

    [Header("NỘI DUNG MẶC ĐỊNH")]
    public string defaultMessage = "⚠ Kẻ địch xuất hiện! Chuẩn bị tấn công!";

    [Header("STYLE")]
    public Color panelColor = new Color32(120, 25, 20, 235);
    public Color borderColor = new Color32(255, 120, 90, 255);
    public Color textColor = new Color32(255, 235, 220, 255);
    public Color iconColor = new Color32(255, 210, 60, 255);

    [Header("HIỆU ỨNG NHẤP NHÁY ICON")]
    [Tooltip("Icon cảnh báo sẽ nhấp nháy (mờ - rõ) liên tục khi banner đang hiện.")]
    public float blinkMinAlpha = 0.25f;
    public float blinkMaxAlpha = 1f;
    public float blinkSpeed = 2.5f;

    [Header("VỊ TRÍ")]
    public Vector2 anchoredPositionFromCenter = new Vector2(0, 0f);
    public Vector2 panelSize = new Vector2(560, 70);

    [Header("TEST TRONG PLAY MODE")]
    [Tooltip("Nội dung dùng khi bấm nút test 'TEST: Hiện cảnh báo' bên dưới. Để trống -> dùng defaultMessage.")]
    public string testMessage = "";

    // ---- Runtime ----
    GameObject root;
    RectTransform rootRT;
    Canvas rootCanvas;

    RectTransform panelRT;
    Image panelImg;
    Outline panelOutline;

    Image iconImage;
    TMP_Text messageText;
    CanvasGroup panelCanvasGroup;

    Coroutine blinkRoutine;
    Coroutine autoHideRoutine;

    // Khi true: MỌI lời gọi ShowAlert() (kể cả từ WatchTowerAI hay bất kỳ script
    // nào khác trong lúc kẻ địch demo đi ngang qua tầm phát hiện thật) đều bị
    // chặn, banner bị ép giữ trạng thái ẩn. Dùng để tutorial cutscene đảm bảo
    // banner tắt hẳn, không bị hệ thống phát hiện thật bật lại đè lên HideAlert().
    bool suppressShow = false;

    const string ROOT_NAME = "RoK_EnemyAlertUI_AutoRoot";

    void Awake()
    {
        // Đơn giản hoá singleton: nếu đã có 1 instance khác thì huỷ cái mới,
        // tránh trường hợp add nhầm script này ở nhiều nơi trong scene.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureCanvas();
        BuildUI();
        HideAlertImmediate();
    }

    // Áp lại style/vị trí/kích thước mỗi frame để bất kỳ thay đổi nào bạn
    // chỉnh trong Inspector lúc đang Play cũng được cập nhật ngay trên panel
    // đang chạy (không cần dừng Play rồi chạy lại). Chi phí rất nhẹ vì chỉ
    // gán vài field, không tạo/huỷ GameObject.
    void Update()
    {
        ApplyLiveStyleAndLayout();
    }

    void ApplyLiveStyleAndLayout()
    {
        if (panelRT == null)
            return;

        panelRT.sizeDelta = panelSize;
        panelRT.anchoredPosition = anchoredPositionFromCenter;

        if (panelImg != null)
            panelImg.color = panelColor;

        if (panelOutline != null)
            panelOutline.effectColor = borderColor;

        if (messageText != null)
        {
            messageText.color = textColor;
            messageText.rectTransform.sizeDelta = new Vector2(panelSize.x - 95, panelSize.y - 16);
        }

        if (iconImage != null)
        {
            if (warningIconSprite != null)
            {
                if (iconImage.sprite != warningIconSprite)
                {
                    iconImage.sprite = warningIconSprite;
                    iconImage.type = Image.Type.Simple;
                    iconImage.preserveAspect = true;
                }

                // Giữ nguyên alpha hiện tại (do BlinkIconRoutine đang điều khiển),
                // chỉ cập nhật phần RGB để không làm gián đoạn hiệu ứng nhấp nháy.
                Color c = Color.white;
                c.a = iconImage.color.a;
                iconImage.color = c;
            }
            else
            {
                if (iconImage.sprite != null)
                {
                    iconImage.sprite = null;
                    iconImage.type = Image.Type.Simple;
                }

                Color c = iconColor;
                c.a = iconImage.color.a;
                iconImage.color = c;
            }
        }
    }

    void EnsureCanvas()
    {
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();
    }

    void BuildUI()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[RoKEnemyAlertUI] Không tìm thấy Canvas nào trong scene để gắn UI cảnh báo.");
            return;
        }

        root = new GameObject(ROOT_NAME, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(targetCanvas.transform, false);

        rootCanvas = root.GetComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = sortingOrder;

        rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false; // banner chỉ để xem, không cần bắt click

        // ---- Panel banner, neo trên-giữa màn hình ----
        GameObject panel = new GameObject("AlertPanel", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
        panel.transform.SetParent(root.transform, false);

        panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = panelSize;
        panelRT.anchoredPosition = anchoredPositionFromCenter;

        panelImg = panel.GetComponent<Image>();
        panelImg.color = panelColor;

        panelOutline = panel.GetComponent<Outline>();
        panelOutline.effectColor = borderColor;
        panelOutline.effectDistance = new Vector2(3f, -3f);
        panelOutline.useGraphicAlpha = false;

        panelCanvasGroup = panel.GetComponent<CanvasGroup>();

        // ---- Icon cảnh báo (bên trái banner) ----
        GameObject iconGO = new GameObject("WarningIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(panel.transform, false);

        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0f, 0.5f);
        iconRT.anchoredPosition = new Vector2(16, 0);
        iconRT.sizeDelta = new Vector2(46, 46);

        iconImage = iconGO.GetComponent<Image>();

        if (warningIconSprite != null)
        {
            iconImage.sprite = warningIconSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
        }
        else
        {
            // Không có sprite riêng -> dựng icon "!" bằng chữ cho nhanh, không cần
            // ảnh ngoài. Dùng nền tam giác/tròn màu vàng cảnh báo + chữ than đậm.
            iconImage.color = iconColor;

            TMP_Text bang = CreateText(iconGO.transform, "WarningIconText", "!", new Vector2(0, 0), new Vector2(46, 46), 30, Color.black, TextAlignmentOptions.Center, true);
            bang.rectTransform.anchorMin = Vector2.zero;
            bang.rectTransform.anchorMax = Vector2.one;
            bang.rectTransform.offsetMin = Vector2.zero;
            bang.rectTransform.offsetMax = Vector2.zero;
        }

        // ---- Text nội dung cảnh báo ----
        messageText = CreateText(panel.transform, "AlertMessageText", defaultMessage,
            new Vector2(78, 0), new Vector2(panelSize.x - 95, panelSize.y - 16),
            26, textColor, TextAlignmentOptions.Left, true);
        messageText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        messageText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        messageText.rectTransform.pivot = new Vector2(0f, 0.5f);
        messageText.enableWordWrapping = true;

        root.SetActive(true);
    }

    TMP_Text CreateText(Transform parent, string name, string value, Vector2 pos, Vector2 size,
        int fontSize, Color color, TextAlignmentOptions align, bool bold)
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
        text.alignment = align;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;

        if (vietnameseFont != null)
            text.font = vietnameseFont;

        return text;
    }

    // =====================================================
    // API CÔNG KHAI - gọi từ WatchTowerAI (hoặc bất kỳ script nào khác)
    // =====================================================

    /// <summary>
    /// Hiện banner cảnh báo + bật icon nhấp nháy. Gọi lại nhiều lần khi đang
    /// hiện sẽ chỉ cập nhật nội dung, không bị giật/hiện lại từ đầu.
    /// </summary>
    public void ShowAlert(string message = null)
    {
        if (root == null)
            return;

        // Đang bị khoá (ví dụ tutorial đang chạy cutscene camera) -> bỏ qua,
        // không cho bất kỳ ai (kể cả WatchTowerAI thật) bật banner lên lúc này.
        if (suppressShow)
            return;

        if (messageText != null)
            messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;

        // Huỷ lịch tự ẩn cũ (nếu HideAlert() từng được hẹn giờ trước đó).
        if (autoHideRoutine != null)
        {
            StopCoroutine(autoHideRoutine);
            autoHideRoutine = null;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.gameObject.SetActive(true);
        }

        if (blinkRoutine == null)
            blinkRoutine = StartCoroutine(BlinkIconRoutine());
    }

    /// <summary>
    /// Ẩn banner cảnh báo ngay lập tức + dừng icon nhấp nháy.
    /// </summary>
    public void HideAlert()
    {
        if (root == null)
            return;

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        if (panelCanvasGroup != null)
            panelCanvasGroup.gameObject.SetActive(false);
    }

    /// <summary>
    /// Khoá/mở khoá banner. Khi suppressed = true: banner bị ẩn ngay lập tức và
    /// mọi lời gọi ShowAlert() từ bất kỳ nơi nào (kể cả hệ thống phát hiện kẻ
    /// địch thật) đều bị bỏ qua cho tới khi gọi SetSuppressed(false).
    /// Dùng trong các cutscene (vd StartupTwoMissionTutorial) để đảm bảo banner
    /// tắt hẳn, không bị bật lại ngoài ý muốn trong lúc camera đang lia/giữ.
    /// </summary>
    public void SetSuppressed(bool suppressed)
    {
        suppressShow = suppressed;

        if (suppressed)
            HideAlert();
    }

    void HideAlertImmediate()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        if (panelCanvasGroup != null)
            panelCanvasGroup.gameObject.SetActive(false);
    }

    IEnumerator BlinkIconRoutine()
    {
        // Nhấp nháy icon theo dạng sóng sin cho mượt (mờ dần - rõ dần liên tục),
        // dùng blinkMinAlpha/blinkMaxAlpha/blinkSpeed để bạn chỉnh tốc độ + độ mờ.
        while (true)
        {
            float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f; // 0..1
            float alpha = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, t);

            if (iconImage != null)
            {
                Color c = iconImage.color;
                c.a = alpha;
                iconImage.color = c;
            }

            yield return null;
        }
    }

    // =====================================================
    // TEST TRONG PLAY MODE - chuột phải vào tên component trong Inspector
    // (hoặc bấm icon 3 chấm ở góc phải component) để thấy 2 lệnh này.
    // =====================================================

    [ContextMenu("TEST: Hiện cảnh báo")]
    void ContextMenu_TestShowAlert()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[RoKEnemyAlertUI] Hãy bấm Play trước rồi mới dùng lệnh test này.");
            return;
        }

        ShowAlert(string.IsNullOrEmpty(testMessage) ? null : testMessage);
    }

    [ContextMenu("TEST: Ẩn cảnh báo")]
    void ContextMenu_TestHideAlert()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[RoKEnemyAlertUI] Hãy bấm Play trước rồi mới dùng lệnh test này.");
            return;
        }

        HideAlert();
    }
}