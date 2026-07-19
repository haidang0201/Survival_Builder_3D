using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI huấn luyện Cung Thủ: chọn số lượng -> trừ Gỗ/Vàng -> chờ theo thời gian
/// -> huấn luyện xong tự cộng tiến độ vào nhiệm vụ "train_archer" trong RoKQuestPanelUI.
///
/// GHI CHÚ CHỈNH LAYOUT (bản cập nhật):
///    Toàn bộ khối nội dung giữa header và nút Huấn Luyện (dòng "Cung thủ hiện
///    có", icon cung thủ, bộ chọn số lượng -/+, dòng chi phí gỗ/vàng, dòng cảnh
///    báo) đã được dịch lên trên (~20-30px) so với bản gốc để đỡ dồn sát nhau
///    và chừa thêm khoảng trống phía dưới cho nút Huấn Luyện + thanh tiến trình.
///    Muốn chỉnh thêm, chỉ cần sửa các hằng số trong hàm BuildUI():
///    currentCountY, iconY, quantityRowY, costRowY, statusY — số càng NHỎ thì
///    phần tử càng dịch lên GẦN header hơn (các giá trị này đo từ mép TRÊN
///    panel xuống).
/// </summary>
public class RoKArcherTrainingUI : MonoBehaviour
{
    [Header("CANVAS")]
    public Canvas targetCanvas;
    public int sortingOrder = 9200;

    [Header("LINK QUEST")]
    public RoKQuestPanelUI questPanelUI;
    public string questIdToReport = "train_archer";

    [Header("DATA")]
    public JsonDataManager jsonData;

    [Header("FONT & ICON")]
    public TMP_FontAsset vietnameseFont;
    public Sprite archerIcon;
    public Sprite woodIcon;
    public Sprite goldIcon;

    [Header("HÌNH ẢNH TUỲ CHỌN")]
    public Sprite panelBackgroundSprite;
    public Sprite headerBackgroundSprite;
    public Sprite closeButtonSprite;
    public Sprite minusButtonSprite;
    public Sprite plusButtonSprite;
    public Sprite trainButtonSprite;
    public Sprite progressBarBackgroundSprite;
    public Sprite progressBarFillSprite;

    [Header("COST / TIME PER ARCHER")]
    public int woodCostPerArcher = 20;
    public int goldCostPerArcher = 5;
    public float trainSecondsPerArcher = 3f;

    [Header("QUANTITY")]
    public int minQuantity = 1;
    public int maxQuantity = 20;

    [Header("SINH LÍNH RA BẢN ĐỒ (tuỳ chọn)")]
    public GameObject archerUnitPrefab;
    public Transform archerSpawnPoint;
    public float spawnSpacing = 1.2f;

    [Header("SỐ LÍNH HIỆN CÓ")]
    public string archerCountSaveKey = "RoK_ArcherCount";
    public int currentArcherCount = 0;

    [Header("STYLE - GỖ")]
    public Color panelColor = new Color32(58, 36, 21, 255);
    public Color headerColor = new Color32(107, 63, 31, 255);
    public Color cardColor = new Color32(122, 74, 36, 255);
    public Color borderColor = new Color32(224, 166, 74, 255);
    public Color titleColor = new Color32(255, 241, 194, 255);
    public Color bodyColor = new Color32(232, 212, 162, 255);
    public Color affordableButtonColor = new Color32(42, 145, 66, 255);
    public Color notAffordableButtonColor = new Color32(120, 60, 45, 255);
    public Color quantityButtonColor = new Color32(199, 106, 27, 255);
    public Color progressBarColor = new Color32(240, 167, 58, 255);

    // ---- Runtime ----
    GameObject root;
    RectTransform rootRT;
    Canvas rootCanvas;

    TMP_Text quantityText;
    TMP_Text woodCostText;
    TMP_Text goldCostText;
    TMP_Text statusText;
    TMP_Text currentArcherCountText;

    Button trainButton;
    Image trainButtonImage;
    TMP_Text trainButtonText;

    Button minusButton;
    Button plusButton;

    GameObject progressBarRoot;
    Image progressBarBackgroundImage;
    Image progressBarFill;
    TMP_Text progressBarText;

    int quantity = 1;
    bool isTraining = false;

    const string ROOT_NAME = "RoK_ArcherTrainingUI_AutoRoot";

    const float PANEL_WIDTH = 640f;
    const float PANEL_HEIGHT = 600f;

    void Awake()
    {
        currentArcherCount = PlayerPrefs.GetInt(archerCountSaveKey, currentArcherCount);

        EnsureCanvas();
        BuildUI();
        RefreshAll();
        ClosePanel();

        // Cảnh báo NGAY từ đầu (thay vì đợi tới lúc bấm Huấn Luyện) nếu prefab
        // lính chưa được gán, để bạn luôn biết chắc script còn liên kết đúng
        // với prefab hay không.
        if (archerUnitPrefab == null)
            Debug.LogWarning("[RoKArcherTrainingUI] Chưa gán 'Archer Unit Prefab' trong Inspector — lính sẽ KHÔNG được sinh ra ngoài bản đồ khi huấn luyện xong (số lượng vẫn cộng bình thường).");
    }

    void EnsureCanvas()
    {
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();
    }

    public void OpenPanel()
    {
        if (root == null)
            return;

        root.SetActive(true);
        root.transform.SetAsLastSibling();

        quantity = minQuantity;
        RefreshAll();
    }

    public void ClosePanel()
    {
        if (root != null)
            root.SetActive(false);
    }

    void BuildUI()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[RoKArcherTrainingUI] Chưa có Target Canvas.");
            return;
        }

        root = new GameObject(ROOT_NAME, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(targetCanvas.transform, false);

        rootCanvas = root.GetComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        rootCanvas.sortingOrder = sortingOrder;

        rootRT = root.GetComponent<RectTransform>();
        Stretch(rootRT);

        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dim.transform.SetParent(root.transform, false);
        RectTransform dimRT = dim.GetComponent<RectTransform>();
        Stretch(dimRT);
        Image dimImg = dim.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.55f);
        Button dimBtn = dim.GetComponent<Button>();
        dimBtn.onClick.AddListener(ClosePanel);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImg = panel.GetComponent<Image>();
        ApplySpriteOrColor(panelImg, panelBackgroundSprite, panelColor);

        Outline panelOutline = panel.GetComponent<Outline>();
        panelOutline.effectColor = borderColor;
        panelOutline.effectDistance = new Vector2(4f, -4f);
        panelOutline.useGraphicAlpha = false;

        // ---- Bố cục theo trục dọc, tính từ mép TRÊN của panel xuống (top = 0) ----
        // ĐÃ DỊCH LÊN TRÊN so với bản gốc (currentCountY 105->85, iconY 205->175,
        // quantityRowY 325->300, costRowY 393->365, statusY 441->410).
        const float headerHeight = 74f;
        const float currentCountY = 85f;
        const float iconY = 175f;
        const float iconSize = 140f;
        const float quantityRowY = 300f;
        const float costRowY = 345f; // nhích lên thêm cho thoáng, tránh sát mép dưới
        const float statusY = 410f;
        const float trainButtonBottomOffset = 100f;
        const float progressBarBottomOffset = 25f;

        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(0, headerHeight);
        headerRT.anchoredPosition = Vector2.zero;
        ApplySpriteOrColor(header.GetComponent<Image>(), headerBackgroundSprite, headerColor);

        TMP_Text headerText = CreateText(header.transform, "HeaderText", "Huấn Luyện Cung Thủ",
            Vector2.zero, new Vector2(500, headerHeight), 34, titleColor, TextAlignmentOptions.Center, true);
        StretchInside(headerText.rectTransform);

        GameObject closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.transform.SetParent(header.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 0.5f);
        closeRT.pivot = new Vector2(1f, 0.5f);
        closeRT.anchoredPosition = new Vector2(-15, 0);
        closeRT.sizeDelta = new Vector2(50, 50);
        ApplySpriteOrColor(closeGO.GetComponent<Image>(), closeButtonSprite, new Color32(180, 60, 45, 255));
        closeGO.GetComponent<Button>().onClick.AddListener(ClosePanel);

        TMP_Text closeText = CreateText(closeGO.transform, "", closeButtonSprite != null ? "" : "",
            Vector2.zero, new Vector2(50, 50), 28, Color.white, TextAlignmentOptions.Center, true);
        StretchInside(closeText.rectTransform);

        currentArcherCountText = CreateText(panel.transform, "CurrentArcherCountText",
            BuildCurrentArcherCountLabel(), Vector2.zero, new Vector2(500, 34),
            24, bodyColor, TextAlignmentOptions.Center, false);
        SetAnchorTopCenter(currentArcherCountText.rectTransform);
        currentArcherCountText.rectTransform.anchoredPosition = new Vector2(0, -currentCountY);

        GameObject iconGO = new GameObject("ArcherIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(panel.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 1f);
        iconRT.pivot = new Vector2(0.5f, 1f);
        iconRT.anchoredPosition = new Vector2(0, -iconY);
        iconRT.sizeDelta = new Vector2(iconSize, iconSize);
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = archerIcon;
        iconImg.color = archerIcon != null ? Color.white : new Color32(255, 210, 45, 255);
        iconImg.preserveAspect = true;

        minusButton = CreateQtyButton(panel.transform, "MinusButton", "-", new Vector2(-150, -quantityRowY), minusButtonSprite);
        minusButton.onClick.AddListener(() => ChangeQuantity(-1));

        quantityText = CreateText(panel.transform, "QuantityText", quantity.ToString(),
            Vector2.zero, new Vector2(120, 55), 34, titleColor, TextAlignmentOptions.Center, true);
        SetAnchorTopCenter(quantityText.rectTransform);
        quantityText.rectTransform.anchoredPosition = new Vector2(0, -quantityRowY);

        plusButton = CreateQtyButton(panel.transform, "PlusButton", "+", new Vector2(150, -quantityRowY), plusButtonSprite);
        plusButton.onClick.AddListener(() => ChangeQuantity(1));

        // GHI CHÚ: kéo 2 cụm Gỗ/Vàng lại gần nhau hơn ở giữa (trước đó cách
        // nhau ~78px trông rời rạc). Icon-số trong từng cụm vẫn giữ đệm ~8px
        // để không bao giờ đè chữ; chỉ thu hẹp khoảng cách GIỮA 2 cụm.
        GameObject woodRow = CreateImage(panel.transform, "WoodCostIcon", Vector2.zero, new Vector2(34, 34));
        Image woodRowImg = woodRow.GetComponent<Image>();
        woodRowImg.sprite = woodIcon;
        woodRowImg.color = woodIcon != null ? Color.white : new Color32(150, 98, 50, 255);
        woodRowImg.preserveAspect = true;
        SetAnchorTopCenter(woodRow.GetComponent<RectTransform>());
        woodRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(-95, -costRowY);

        woodCostText = CreateText(panel.transform, "WoodCostText", "0", Vector2.zero,
            new Vector2(60, 34), 24, bodyColor, TextAlignmentOptions.Left, false);
        SetAnchorTopCenter(woodCostText.rectTransform);
        woodCostText.rectTransform.anchoredPosition = new Vector2(-40, -costRowY);

        GameObject goldRow = CreateImage(panel.transform, "GoldCostIcon", Vector2.zero, new Vector2(34, 34));
        Image goldRowImg = goldRow.GetComponent<Image>();
        goldRowImg.sprite = goldIcon;
        goldRowImg.color = goldIcon != null ? Color.white : new Color32(255, 210, 45, 255);
        goldRowImg.preserveAspect = true;
        SetAnchorTopCenter(goldRow.GetComponent<RectTransform>());
        goldRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, -costRowY);

        goldCostText = CreateText(panel.transform, "GoldCostText", "0", Vector2.zero,
            new Vector2(60, 34), 24, bodyColor, TextAlignmentOptions.Left, false);
        SetAnchorTopCenter(goldCostText.rectTransform);
        goldCostText.rectTransform.anchoredPosition = new Vector2(75, -costRowY);

        GameObject trainGO = new GameObject("TrainButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        trainGO.transform.SetParent(panel.transform, false);
        RectTransform trainRT = trainGO.GetComponent<RectTransform>();
        trainRT.anchorMin = trainRT.anchorMax = new Vector2(0.5f, 0f);
        trainRT.pivot = new Vector2(0.5f, 0f);
        trainRT.anchoredPosition = new Vector2(0, trainButtonBottomOffset);

        trainButtonImage = trainGO.GetComponent<Image>();
        if (trainButtonSprite != null)
        {
            // SỬA LỖI BÓP MÉO: trước đây ép cứng sprite vào khung 260x74 (hình
            // chữ nhật dẹt) bằng Image.Type.Sliced — nếu ảnh gốc là hình vuông/
            // hình thoi (như icon kim cương chéo kiếm) thì bị kéo bẹp ngang rất
            // xấu. Nay dùng Image.Type.Simple + preserveAspect=true và TỰ TÍNH
            // kích thước khung theo đúng tỉ lệ Width/Height gốc của sprite (giới
            // hạn trong một khung tối đa) để hình luôn hiển thị tròn trịa, không
            // méo, dù bạn gán bất kỳ ảnh nào (chữ nhật dài hay vuông/thoi).
            trainButtonImage.sprite = trainButtonSprite;
            trainButtonImage.type = Image.Type.Simple;
            trainButtonImage.preserveAspect = true;
            trainButtonImage.color = Color.white;

            const float maxTrainButtonWidth = 220f;
            const float maxTrainButtonHeight = 130f;
            float spriteAspect = trainButtonSprite.rect.width / trainButtonSprite.rect.height;

            float w = maxTrainButtonWidth;
            float h = w / spriteAspect;
            if (h > maxTrainButtonHeight)
            {
                h = maxTrainButtonHeight;
                w = h * spriteAspect;
            }

            trainRT.sizeDelta = new Vector2(w, h);
        }
        else
        {
            // Không có sprite riêng -> dùng khung chữ nhật màu phẳng như cũ,
            // trường hợp này không có nguy cơ bị méo vì không kéo giãn ảnh.
            trainButtonImage.color = affordableButtonColor;
            trainRT.sizeDelta = new Vector2(260, 74);
        }

        Outline trainOutline = trainGO.GetComponent<Outline>();
        trainOutline.effectColor = borderColor;
        trainOutline.effectDistance = new Vector2(3f, -3f);
        trainOutline.useGraphicAlpha = false;

        trainButton = trainGO.GetComponent<Button>();
        trainButton.onClick.AddListener(OnTrainButtonClicked);

        trainButtonText = CreateText(trainGO.transform, "TrainButtonText", "",
            Vector2.zero, trainRT.sizeDelta, 30, Color.white, TextAlignmentOptions.Center, true);
        StretchInside(trainButtonText.rectTransform);

        progressBarRoot = new GameObject("ProgressBarRoot", typeof(RectTransform), typeof(Image));
        progressBarRoot.transform.SetParent(panel.transform, false);
        RectTransform barRootRT = progressBarRoot.GetComponent<RectTransform>();
        barRootRT.anchorMin = barRootRT.anchorMax = new Vector2(0.5f, 0f);
        barRootRT.pivot = new Vector2(0.5f, 0f);
        barRootRT.anchoredPosition = new Vector2(0, progressBarBottomOffset);
        barRootRT.sizeDelta = new Vector2(500, 34);
        progressBarBackgroundImage = progressBarRoot.GetComponent<Image>();
        ApplySpriteOrColor(progressBarBackgroundImage, progressBarBackgroundSprite, new Color32(30, 20, 12, 255));

        // Thanh fill kiểu SLIDER thật: dùng Image.Type.Filled (Horizontal, gốc Trái)
        // thay vì kéo giãn RectTransform. Cách này chạy mượt, không bị vỡ/méo
        // sprite ở hai đầu thanh, và animate y hệt cơ chế của UI Slider chuẩn
        // trong Unity (chỉ cần đổi fillAmount từ 0 -> 1).
        GameObject fillGO = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(progressBarRoot.transform, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        // Stretch kín toàn bộ rãnh thanh tiến trình — phần "chạy" sẽ do fillAmount
        // của Image quyết định, không phải kích thước RectTransform.
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        progressBarFill = fillGO.GetComponent<Image>();
        ApplyFillSpriteOrColor(progressBarFill, progressBarFillSprite, progressBarColor);
        progressBarFill.fillAmount = 0f;

        progressBarText = CreateText(progressBarRoot.transform, "ProgressBarText", "",
            Vector2.zero, new Vector2(500, 34), 20, titleColor, TextAlignmentOptions.Center, true);
        StretchInside(progressBarText.rectTransform);

        progressBarRoot.SetActive(false);

        statusText = CreateText(panel.transform, "StatusText", "", Vector2.zero,
            new Vector2(560, 30), 20, new Color32(230, 120, 100, 255), TextAlignmentOptions.Center, false);
        SetAnchorTopCenter(statusText.rectTransform);
        statusText.rectTransform.anchoredPosition = new Vector2(0, -statusY);
    }

    void ApplySpriteOrColor(Image img, Sprite sprite, Color fallbackColor)
    {
        if (img == null)
            return;

        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.sprite = null;
            img.type = Image.Type.Simple;
            img.color = fallbackColor;
        }
    }

    // Sprite trắng 1x1 dùng làm dự phòng cho thanh fill kiểu Filled khi bạn
    // chưa gán ảnh riêng — Image.Type.Filled cần có sprite hợp lệ mới fill
    // đúng, nếu để sprite = null thì fillAmount có thể không hiển thị đúng.
    static Sprite s_fallbackWhiteSprite;

    static Sprite GetFallbackWhiteSprite()
    {
        if (s_fallbackWhiteSprite == null)
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            s_fallbackWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        return s_fallbackWhiteSprite;
    }

    /// <summary>
    /// Gán sprite + kiểu Filled (Horizontal, gốc Trái) cho thanh fill kiểu
    /// SLIDER thật. Nếu có sprite riêng thì dùng màu trắng để không ám màu
    /// ảnh gốc; nếu không có thì dùng sprite trắng dự phòng + màu tuỳ chỉnh.
    /// Sau khi gọi hàm này, chỉ cần đổi img.fillAmount (0..1) để chạy thanh,
    /// không cần đụng tới RectTransform nữa.
    /// </summary>
    void ApplyFillSpriteOrColor(Image img, Sprite sprite, Color fallbackColor)
    {
        if (img == null)
            return;

        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
        else
        {
            img.sprite = GetFallbackWhiteSprite();
            img.color = fallbackColor;
        }

        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillClockwise = true;
        img.raycastTarget = false;
    }

    Button CreateQtyButton(Transform parent, string name, string label, Vector2 pos, Sprite overrideSprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        SetAnchorTopCenter(rt);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(64, 58);

        ApplySpriteOrColor(go.GetComponent<Image>(), overrideSprite, quantityButtonColor);

        TMP_Text text = CreateText(go.transform, name + "Text", overrideSprite != null ? "" : label,
            Vector2.zero, new Vector2(64, 58), 32, Color.white, TextAlignmentOptions.Center, true);
        StretchInside(text.rectTransform);

        return go.GetComponent<Button>();
    }

    void ChangeQuantity(int delta)
    {
        if (isTraining)
            return;

        quantity = Mathf.Clamp(quantity + delta, minQuantity, maxQuantity);
        RefreshAll();
    }

    void RefreshAll()
    {
        if (quantityText != null)
            quantityText.text = quantity.ToString();

        int totalWood = quantity * woodCostPerArcher;
        int totalGold = quantity * goldCostPerArcher;

        if (woodCostText != null)
            woodCostText.text = totalWood.ToString("#,0").Replace(",", ".");

        if (goldCostText != null)
            goldCostText.text = totalGold.ToString("#,0").Replace(",", ".");

        bool canAfford = CurrentWood() >= totalWood && CurrentGold() >= totalGold;

        if (trainButtonImage != null && trainButtonSprite == null)
            trainButtonImage.color = canAfford ? affordableButtonColor : notAffordableButtonColor;

        if (trainButton != null)
            trainButton.interactable = !isTraining && canAfford;

        if (minusButton != null)
            minusButton.interactable = !isTraining;

        if (plusButton != null)
            plusButton.interactable = !isTraining;

        if (statusText != null)
            statusText.text = canAfford ? "" : "Không đủ tài nguyên để huấn luyện.";

        if (currentArcherCountText != null)
            currentArcherCountText.text = BuildCurrentArcherCountLabel();
    }

    string BuildCurrentArcherCountLabel()
    {
        return "Cung thủ hiện có: " + currentArcherCount.ToString("#,0").Replace(",", ".");
    }

    JsonDataManager GetJsonData()
    {
        if (jsonData == null)
            jsonData = FindObjectOfType<JsonDataManager>();

        if (jsonData == null)
            Debug.LogWarning("[RoKArcherTrainingUI] Không tìm thấy JsonDataManager trong scene.");

        return jsonData;
    }

    int CurrentWood()
    {
        JsonDataManager data = GetJsonData();
        return data != null ? data.wood : 0;
    }

    int CurrentGold()
    {
        JsonDataManager data = GetJsonData();
        return data != null ? data.gold : 0;
    }

    void OnTrainButtonClicked()
    {
        if (isTraining)
            return;

        int totalWood = quantity * woodCostPerArcher;
        int totalGold = quantity * goldCostPerArcher;

        JsonDataManager data = GetJsonData();

        if (data == null)
        {
            statusText.text = "Lỗi: không tìm thấy dữ liệu tài nguyên.";
            return;
        }

        if (CurrentWood() < totalWood || CurrentGold() < totalGold)
        {
            statusText.text = "Không đủ tài nguyên để huấn luyện.";
            return;
        }

        data.AddWood(-totalWood);
        data.AddGold(-totalGold);
        data.BroadcastAllResources();

        StartCoroutine(TrainRoutine(quantity));
    }

    IEnumerator TrainRoutine(int count)
    {
        isTraining = true;
        RefreshAll();

        progressBarRoot.SetActive(true);
        trainButtonText.text = "Đang huấn luyện...";

        float totalTime = count * trainSecondsPerArcher;
        float elapsed = 0f;

        // Reset về 0 mỗi lần bắt đầu mẻ huấn luyện mới.
        progressBarFill.fillAmount = 0f;

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalTime);

            // Dùng SmoothStep thay vì tuyến tính để thanh chạy có "đà" (chậm rãi
            // vào/ra), giống cảm giác một thanh Slider thật đang được kéo, thay
            // vì chạy đều đều máy móc. Vẫn chạm đúng 0 -> 1 sau đúng totalTime.
            progressBarFill.fillAmount = Mathf.SmoothStep(0f, 1f, t);
            progressBarText.text = $"Huấn luyện... {Mathf.CeilToInt(totalTime - elapsed)}s";

            yield return null;
        }

        // Đảm bảo chạm mốc đầy 100% chính xác khi kết thúc, tránh sai số cộng dồn
        // của Time.deltaTime khiến fillAmount dừng ở ~0.98 thay vì 1.0.
        progressBarFill.fillAmount = 1f;

        // Slider vừa chạy đầy 100% (huấn luyện xong) -> LUÔN gọi sinh lính từ
        // prefab (SpawnTrainedArchers) trước, rồi mới cộng số lượng hiện có.
        // Thứ tự này đảm bảo prefab lính luôn gắn liền với kết quả huấn luyện,
        // không bị tách rời hay gọi nhầm chỗ khác trong code.
        SpawnTrainedArchers(count);
        AddCurrentArcherCount(count);

        if (questPanelUI != null && !string.IsNullOrEmpty(questIdToReport))
            questPanelUI.AddProgress(questIdToReport, count);

        progressBarText.text = "Hoàn thành!";
        yield return new WaitForSeconds(0.6f);

        progressBarRoot.SetActive(false);
        trainButtonText.text = "";
        isTraining = false;

        RefreshAll();

        Debug.Log($"[RoKArcherTrainingUI] Đã huấn luyện xong {count} cung thủ. Tổng hiện có: {currentArcherCount}.");
    }

    // GHI CHÚ LIÊN KẾT PREFAB LÍNH: hàm này LUÔN được gọi ngay sau khi thanh
    // tiến trình (slider) chạy đầy 100% trong TrainRoutine(), trước cả bước
    // cộng currentArcherCount — nghĩa là lính CHỈ được sinh ra khi huấn luyện
    // thật sự hoàn tất, không bao giờ bị tách rời khỏi luồng huấn luyện.
    // Nếu bạn thấy lính không hiện ra ngoài bản đồ, kiểm tra Console: script
    // sẽ cảnh báo rõ ràng nếu archerUnitPrefab chưa được kéo vào Inspector,
    // thay vì âm thầm bỏ qua như trước.
    void SpawnTrainedArchers(int count)
    {
        if (archerUnitPrefab == null)
        {
            Debug.LogWarning("[RoKArcherTrainingUI] Chưa gán 'Archer Unit Prefab' trong Inspector nên KHÔNG sinh lính ra bản đồ được (số Cung Thủ hiện có vẫn cộng bình thường). Hãy kéo prefab lính Cung Thủ vào field này để bật tính năng sinh lính.");
            return;
        }

        Vector3 basePos = archerSpawnPoint != null ? archerSpawnPoint.position : transform.position;
        Quaternion baseRot = archerSpawnPoint != null ? archerSpawnPoint.rotation : transform.rotation;

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3((i - (count - 1) / 2f) * spawnSpacing, 0f, 0f);
            Instantiate(archerUnitPrefab, basePos + offset, baseRot);
        }
    }

    void AddCurrentArcherCount(int amount)
    {
        currentArcherCount += amount;
        PlayerPrefs.SetInt(archerCountSaveKey, currentArcherCount);
        PlayerPrefs.Save();
    }

    public int GetCurrentArcherCount()
    {
        return currentArcherCount;
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
        text.enableWordWrapping = true;

        if (vietnameseFont != null)
            text.font = vietnameseFont;

        return text;
    }

    GameObject CreateImage(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        go.GetComponent<Image>().raycastTarget = false;

        return go;
    }

    void SetAnchorCenter(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    void SetAnchorTopCenter(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
    }

    void StretchInside(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}