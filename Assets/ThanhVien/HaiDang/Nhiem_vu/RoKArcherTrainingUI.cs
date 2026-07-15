using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI huấn luyện Cung Thủ: chọn số lượng -> trừ Gỗ/Vàng -> chờ theo thời gian
/// -> huấn luyện xong tự cộng tiến độ vào nhiệm vụ "train_archer" trong RoKQuestPanelUI.
///
/// CÁCH DÙNG:
/// 1. Gắn script này vào 1 GameObject trống trong scene.
/// 2. Gán targetCanvas, questPanelUI (kéo GameObject có RoKQuestPanelUI vào).
/// 3. Nếu tên field tài nguyên trong JsonDataManager của bạn khác "wood"/"gold",
///    sửa lại 2 hàm CurrentWood()/CurrentGold() bên dưới cho khớp.
/// 4. Gọi OpenPanel() từ nút "Đi" của quest "train_archer" (hoặc từ nút mở
///    trại huấn luyện trên bản đồ) để hiện UI này ra.
/// </summary>
public class RoKArcherTrainingUI : MonoBehaviour
{
    [Header("CANVAS")]
    public Canvas targetCanvas;
    public int sortingOrder = 9200;

    [Header("LINK QUEST")]
    [Tooltip("Kéo GameObject có RoKQuestPanelUI vào đây để tự động cộng tiến độ nhiệm vụ.")]
    public RoKQuestPanelUI questPanelUI;
    [Tooltip("Id nhiệm vụ sẽ được cộng tiến độ mỗi khi huấn luyện xong 1 lính.")]
    public string questIdToReport = "train_archer";

    [Header("DATA")]
    [Tooltip("Để trống sẽ tự FindObjectOfType lúc Awake.")]
    public JsonDataManager jsonData;

    [Header("FONT & ICON")]
    public TMP_FontAsset vietnameseFont;
    public Sprite archerIcon;
    public Sprite woodIcon;
    public Sprite goldIcon;

    [Header("COST / TIME PER ARCHER")]
    public int woodCostPerArcher = 20;
    public int goldCostPerArcher = 5;
    public float trainSecondsPerArcher = 3f;

    [Header("QUANTITY")]
    public int minQuantity = 1;
    public int maxQuantity = 20;

    [Header("SINH LÍNH RA BẢN ĐỒ (tuỳ chọn)")]
    [Tooltip("Prefab lính Cung Thủ sẽ được Instantiate khi huấn luyện xong. Để trống nếu bạn chưa có prefab, script vẫn cập nhật số lượng bình thường.")]
    public GameObject archerUnitPrefab;
    [Tooltip("Vị trí sinh lính ra (thường là cổng doanh trại). Để trống sẽ dùng vị trí của chính GameObject này.")]
    public Transform archerSpawnPoint;
    [Tooltip("Khoảng cách giữa các lính khi sinh ra hàng loạt, tránh chồng lên nhau.")]
    public float spawnSpacing = 1.2f;

    [Header("SỐ LÍNH HIỆN CÓ")]
    [Tooltip("Khoá lưu PlayerPrefs cho số cung thủ hiện có, đổi khác nếu bạn có nhiều loại lính.")]
    public string archerCountSaveKey = "RoK_ArcherCount";
    [Tooltip("Tổng số Cung Thủ hiện có (đọc/ghi runtime, tự lưu qua PlayerPrefs).")]
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
    Image progressBarFill;
    TMP_Text progressBarText;

    int quantity = 1;
    bool isTraining = false;

    const string ROOT_NAME = "RoK_ArcherTrainingUI_AutoRoot";

    void Awake()
    {
        // Không cần tự tìm jsonData ở đây nữa — GetJsonData() sẽ tự tìm lại
        // mỗi khi cần dùng, kể cả khi JsonDataManager khởi tạo chậm hơn hoặc
        // bị destroy/tạo lại khi đổi scene.

        // Đọc lại số Cung Thủ hiện có đã lưu từ trước (nếu có)
        currentArcherCount = PlayerPrefs.GetInt(archerCountSaveKey, currentArcherCount);

        EnsureCanvas();
        BuildUI();
        RefreshAll();
        ClosePanel();
    }

    void EnsureCanvas()
    {
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();
    }

    // =====================================================
    // PUBLIC API
    // =====================================================

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

    // =====================================================
    // BUILD UI
    // =====================================================

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

        // Nền mờ phía sau, bấm ra ngoài để đóng
        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dim.transform.SetParent(root.transform, false);
        RectTransform dimRT = dim.GetComponent<RectTransform>();
        Stretch(dimRT);
        Image dimImg = dim.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.55f);
        Button dimBtn = dim.GetComponent<Button>();
        dimBtn.onClick.AddListener(ClosePanel);

        // Panel chính
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(620, 460);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImg = panel.GetComponent<Image>();
        panelImg.color = panelColor;

        Outline panelOutline = panel.GetComponent<Outline>();
        panelOutline.effectColor = borderColor;
        panelOutline.effectDistance = new Vector2(4f, -4f);
        panelOutline.useGraphicAlpha = false;

        // Header
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.sizeDelta = new Vector2(0, 70);
        headerRT.anchoredPosition = Vector2.zero;
        header.GetComponent<Image>().color = headerColor;

        TMP_Text headerText = CreateText(header.transform, "HeaderText", "Huấn Luyện Cung Thủ",
            Vector2.zero, new Vector2(500, 70), 34, titleColor, TextAlignmentOptions.Center, true);
        StretchInside(headerText.rectTransform);

        // Nút đóng
        GameObject closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.transform.SetParent(header.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 0.5f);
        closeRT.pivot = new Vector2(1f, 0.5f);
        closeRT.anchoredPosition = new Vector2(-15, 0);
        closeRT.sizeDelta = new Vector2(50, 50);
        closeGO.GetComponent<Image>().color = new Color32(180, 60, 45, 255);
        closeGO.GetComponent<Button>().onClick.AddListener(ClosePanel);

        TMP_Text closeText = CreateText(closeGO.transform, "X", "X", Vector2.zero, new Vector2(50, 50), 28,
            Color.white, TextAlignmentOptions.Center, true);
        StretchInside(closeText.rectTransform);

        // Số Cung Thủ hiện có
        currentArcherCountText = CreateText(panel.transform, "CurrentArcherCountText",
            BuildCurrentArcherCountLabel(), new Vector2(0, -40), new Vector2(400, 34),
            24, bodyColor, TextAlignmentOptions.Center, false);
        SetAnchorTopCenter(currentArcherCountText.rectTransform);
        currentArcherCountText.rectTransform.anchoredPosition = new Vector2(0, -40);

        // Icon cung thủ
        GameObject iconGO = new GameObject("ArcherIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(panel.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 1f);
        iconRT.pivot = new Vector2(0.5f, 1f);
        iconRT.anchoredPosition = new Vector2(0, -90);
        iconRT.sizeDelta = new Vector2(110, 110);
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = archerIcon;
        iconImg.color = archerIcon != null ? Color.white : new Color32(255, 210, 45, 255);
        iconImg.preserveAspect = true;

        // Bộ chọn số lượng: [ - ]  Số lượng  [ + ]
        minusButton = CreateQtyButton(panel.transform, "MinusButton", "-", new Vector2(-140, -230));
        minusButton.onClick.AddListener(() => ChangeQuantity(-1));

        quantityText = CreateText(panel.transform, "QuantityText", quantity.ToString(),
            new Vector2(0, -230), new Vector2(120, 55), 34, titleColor, TextAlignmentOptions.Center, true);
        SetAnchorCenter(quantityText.rectTransform);
        quantityText.rectTransform.anchoredPosition = new Vector2(0, -230);

        plusButton = CreateQtyButton(panel.transform, "PlusButton", "+", new Vector2(140, -230));
        plusButton.onClick.AddListener(() => ChangeQuantity(1));

        // Chi phí: gỗ + vàng
        GameObject woodRow = CreateImage(panel.transform, "WoodCostIcon", new Vector2(-90, -300), new Vector2(30, 30));
        woodRow.GetComponent<Image>().sprite = woodIcon;
        woodRow.GetComponent<Image>().color = woodIcon != null ? Color.white : new Color32(150, 98, 50, 255);

        woodCostText = CreateText(panel.transform, "WoodCostText", "0", new Vector2(-50, -300),
            new Vector2(100, 30), 24, bodyColor, TextAlignmentOptions.Left, false);
        SetAnchorTopCenter(woodCostText.rectTransform);
        woodCostText.rectTransform.anchoredPosition = new Vector2(-50, -300);
        SetAnchorTopCenter(woodRow.GetComponent<RectTransform>());
        woodRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(-90, -300);

        GameObject goldRow = CreateImage(panel.transform, "GoldCostIcon", new Vector2(60, -300), new Vector2(30, 30));
        goldRow.GetComponent<Image>().sprite = goldIcon;
        goldRow.GetComponent<Image>().color = goldIcon != null ? Color.white : new Color32(255, 210, 45, 255);
        SetAnchorTopCenter(goldRow.GetComponent<RectTransform>());
        goldRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(60, -300);

        goldCostText = CreateText(panel.transform, "GoldCostText", "0", new Vector2(100, -300),
            new Vector2(100, 30), 24, bodyColor, TextAlignmentOptions.Left, false);
        SetAnchorTopCenter(goldCostText.rectTransform);
        goldCostText.rectTransform.anchoredPosition = new Vector2(100, -300);

        // Nút Huấn luyện
        GameObject trainGO = new GameObject("TrainButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        trainGO.transform.SetParent(panel.transform, false);
        RectTransform trainRT = trainGO.GetComponent<RectTransform>();
        trainRT.anchorMin = trainRT.anchorMax = new Vector2(0.5f, 0f);
        trainRT.pivot = new Vector2(0.5f, 0f);
        trainRT.anchoredPosition = new Vector2(0, 90);
        trainRT.sizeDelta = new Vector2(260, 70);

        trainButtonImage = trainGO.GetComponent<Image>();
        trainButtonImage.color = affordableButtonColor;

        Outline trainOutline = trainGO.GetComponent<Outline>();
        trainOutline.effectColor = borderColor;
        trainOutline.effectDistance = new Vector2(3f, -3f);
        trainOutline.useGraphicAlpha = false;

        trainButton = trainGO.GetComponent<Button>();
        trainButton.onClick.AddListener(OnTrainButtonClicked);

        trainButtonText = CreateText(trainGO.transform, "TrainButtonText", "Huấn Luyện",
            Vector2.zero, new Vector2(260, 70), 30, Color.white, TextAlignmentOptions.Center, true);
        StretchInside(trainButtonText.rectTransform);

        // Thanh tiến trình (ẩn cho tới khi bắt đầu huấn luyện)
        progressBarRoot = new GameObject("ProgressBarRoot", typeof(RectTransform), typeof(Image));
        progressBarRoot.transform.SetParent(panel.transform, false);
        RectTransform barRootRT = progressBarRoot.GetComponent<RectTransform>();
        barRootRT.anchorMin = barRootRT.anchorMax = new Vector2(0.5f, 0f);
        barRootRT.pivot = new Vector2(0.5f, 0f);
        barRootRT.anchoredPosition = new Vector2(0, 30);
        barRootRT.sizeDelta = new Vector2(480, 34);
        progressBarRoot.GetComponent<Image>().color = new Color32(30, 20, 12, 255);

        GameObject fillGO = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(progressBarRoot.transform, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        fillRT.sizeDelta = new Vector2(0, 0);
        progressBarFill = fillGO.GetComponent<Image>();
        progressBarFill.color = progressBarColor;

        progressBarText = CreateText(progressBarRoot.transform, "ProgressBarText", "",
            Vector2.zero, new Vector2(480, 34), 20, titleColor, TextAlignmentOptions.Center, true);
        StretchInside(progressBarText.rectTransform);

        progressBarRoot.SetActive(false);

        // Dòng trạng thái (báo thiếu tài nguyên, v.v.)
        statusText = CreateText(panel.transform, "StatusText", "", new Vector2(0, -350),
            new Vector2(520, 30), 20, new Color32(230, 120, 100, 255), TextAlignmentOptions.Center, false);
        SetAnchorTopCenter(statusText.rectTransform);
        statusText.rectTransform.anchoredPosition = new Vector2(0, -350);
    }

    Button CreateQtyButton(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        SetAnchorCenter(rt);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(60, 55);

        go.GetComponent<Image>().color = quantityButtonColor;

        TMP_Text text = CreateText(go.transform, name + "Text", label, Vector2.zero, new Vector2(60, 55), 32,
            Color.white, TextAlignmentOptions.Center, true);
        StretchInside(text.rectTransform);

        return go.GetComponent<Button>();
    }

    // =====================================================
    // LOGIC SỐ LƯỢNG / CHI PHÍ
    // =====================================================

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

        if (trainButtonImage != null)
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

    // =====================================================
    // JSON DATA MANAGER — LUÔN TỰ TÌM LẠI NẾU BỊ MẤT LIÊN KẾT
    // =====================================================

    /// <summary>
    /// Luôn dùng hàm này thay vì gọi thẳng field "jsonData", vì nó tự động tìm lại
    /// JsonDataManager nếu bị null (do thứ tự Awake khác nhau, đổi scene, object bị
    /// destroy/tạo lại...). Giúp script không bao giờ "mất liên kết" tài nguyên.
    /// </summary>
    JsonDataManager GetJsonData()
    {
        if (jsonData == null)
            jsonData = FindObjectOfType<JsonDataManager>();

        if (jsonData == null)
            Debug.LogWarning("[RoKArcherTrainingUI] Không tìm thấy JsonDataManager trong scene.");

        return jsonData;
    }

    // =====================================================
    // TÀI NGUYÊN — ĐỔI THEO TÊN FIELD THẬT TRONG JsonDataManager CỦA BẠN
    // =====================================================

    int CurrentWood()
    {
        // TODO: nếu JsonDataManager của bạn dùng tên khác (vd currentWood, Wood...),
        // sửa lại dòng dưới cho khớp.
        JsonDataManager data = GetJsonData();
        return data != null ? data.wood : 0;
    }

    int CurrentGold()
    {
        // TODO: nếu JsonDataManager của bạn dùng tên khác (vd currentGold, Gold...),
        // sửa lại dòng dưới cho khớp.
        JsonDataManager data = GetJsonData();
        return data != null ? data.gold : 0;
    }

    // =====================================================
    // HUẤN LUYỆN
    // =====================================================

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

        // Trừ tài nguyên ngay khi bắt đầu huấn luyện
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

        RectTransform fillRT = progressBarFill.rectTransform;
        RectTransform barRootRT = progressBarRoot.GetComponent<RectTransform>();

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalTime);

            fillRT.sizeDelta = new Vector2(barRootRT.sizeDelta.x * t, 0);
            progressBarText.text = $"Huấn luyện... {Mathf.CeilToInt(totalTime - elapsed)}s";

            yield return null;
        }

        // Huấn luyện xong -> sinh lính ra bản đồ + cộng số lính hiện có
        SpawnTrainedArchers(count);
        AddCurrentArcherCount(count);

        // Báo tiến độ về quest
        if (questPanelUI != null && !string.IsNullOrEmpty(questIdToReport))
            questPanelUI.AddProgress(questIdToReport, count);

        progressBarText.text = "Hoàn thành!";
        yield return new WaitForSeconds(0.6f);

        progressBarRoot.SetActive(false);
        trainButtonText.text = "Huấn Luyện";
        isTraining = false;

        RefreshAll();

        Debug.Log($"[RoKArcherTrainingUI] Đã huấn luyện xong {count} cung thủ. Tổng hiện có: {currentArcherCount}.");
    }

    /// <summary>
    /// Instantiate ra bản đối "count" lính Cung Thủ tại archerSpawnPoint (hoặc vị trí
    /// của chính GameObject này nếu chưa gán). Nếu chưa gán archerUnitPrefab thì bỏ qua
    /// bước sinh lính (chỉ cộng số lượng lính hiện có).
    /// </summary>
    void SpawnTrainedArchers(int count)
    {
        if (archerUnitPrefab == null)
            return;

        Vector3 basePos = archerSpawnPoint != null ? archerSpawnPoint.position : transform.position;
        Quaternion baseRot = archerSpawnPoint != null ? archerSpawnPoint.rotation : transform.rotation;

        for (int i = 0; i < count; i++)
        {
            // Xếp lính thành hàng ngang, cách đều nhau spawnSpacing, tránh chồng lên nhau.
            Vector3 offset = new Vector3((i - (count - 1) / 2f) * spawnSpacing, 0f, 0f);
            Instantiate(archerUnitPrefab, basePos + offset, baseRot);
        }
    }

    /// <summary>
    /// Cộng thêm số Cung Thủ hiện có và lưu lại qua PlayerPrefs để giữ nguyên
    /// sau khi tắt/mở lại game.
    /// </summary>
    void AddCurrentArcherCount(int amount)
    {
        currentArcherCount += amount;
        PlayerPrefs.SetInt(archerCountSaveKey, currentArcherCount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Lấy số Cung Thủ hiện có, dùng cho các script khác (vd hiển thị lên HUD chính,
    /// dùng để tính sức mạnh quân đội...).
    /// </summary>
    public int GetCurrentArcherCount()
    {
        return currentArcherCount;
    }

    // =====================================================
    // HELPERS
    // =====================================================

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