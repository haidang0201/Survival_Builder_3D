using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoKProfileQuestAutoUI : MonoBehaviour
{
    [Header("LINK")]
    public Canvas targetCanvas;
    public RoKQuestPanelUI questPanel;
    public Button profileButton;
    [Header("Achievement Text Style")]
    public Color achievementTitleColor = new Color32(245, 232, 200, 255);
    public Color achievementDescColor = new Color32(186, 158, 112, 255);
    public Color achievementStatusColor = new Color32(232, 190, 82, 255);

    [Header("Detail Row Text Style")]
    // Nhãn: màu sáng kem, nổi rõ trên nền hàng gỗ tối (darkCardColor / detailRowBgSprite).
    public Color detailLabelColor = new Color32(226, 210, 178, 255);
    // Giá trị: vàng sáng, tương phản mạnh, dễ đọc và nổi bật hơn nhãn.
    public Color detailValueColor = new Color32(255, 199, 84, 255);

    public Color textOutlineColor = new Color32(45, 25, 10, 200);
    public Vector2 textShadowDistance = new Vector2(1.2f, -1.2f);

    [Header("SPRITES")]
    public Sprite avatarSprite;
    public Sprite avatarFrameSprite;

    // =====================================================
    // BACKGROUND SPRITES - dùng để đồng bộ style khung gỗ (ảnh mẫu)
    // Nếu để trống (None) thì các ô sẽ tự động fallback về tô màu phẳng
    // như code cũ (không thay đổi gì nếu bạn không gán sprite).
    // =====================================================
    [Header("BACKGROUND SPRITES (đồng bộ style khung gỗ)")]
    public Sprite mainWindowBgSprite;
    public Sprite headerBgSprite;
    public Sprite leftPanelBgSprite;
    public Sprite infoPanelBgSprite;
    public Sprite statsPanelBgSprite;

    public Sprite renameWindowBgSprite;
    public Sprite achievementWindowBgSprite;
    public Sprite detailWindowBgSprite;

    public Sprite achievementRowBgSprite;
    public Sprite detailRowBgSprite;

    public Sprite actionButtonBgSprite;     // giữ lại để không ảnh hưởng dữ liệu cũ

    [Header("CUSTOM BOTTOM BUTTON SPRITES")]
    public Sprite renameButtonSprite;        // ảnh nút Đổi tên đã có sẵn chữ
    public Sprite achievementButtonSprite;   // ảnh nút Thành tích đã có sẵn chữ
    public Sprite detailButtonSprite;        // ảnh nút Hồ sơ chi tiết đã có sẵn chữ

    public Sprite closeButtonBgSprite;      // nút X đóng panel
    public Sprite confirmButtonBgSprite;    // nút "Xác nhận"
    public Sprite cancelButtonBgSprite;     // nút "Hủy"
    public Sprite inputFieldBgSprite;       // khung nhập tên

    [Header("FONT")]
    public TMP_FontAsset vietnameseFont;

    [Header("QUEST")]
    public string renameQuestId = "my_name";

    [Header("PLAYER DATA")]
    public string defaultName = "Thống đốc";
    public int governorLevel = 1;
    public int power = 0;
    public string governorTitle = "Lãnh chúa mới";
    public int loginDays = 1;
    public string allianceName = "Chưa gia nhập";
    public string civilizationName = "Khởi nguyên";

    [Header("STATS")]
    public int workerCurrent = 3;
    public int workerMax = 4;
    public int armyCount = 0;
    public int buildingCount = 0;
    public int watchTowerCount = 0;
    public int cannonCount = 0;
    public int resourceCollected = 0;
    public int enemyDefeated = 0;


    [Header("GAME LINK")]
    public UILinh uiLinh;
    public JsonDataManager jsonDataManager;
    public UIThapCanh uiThapCanh;
    public UIPhaoThu uiPhaoThu;
    public UIWorkerCount uiWorkerCount;
    public UIBuildingCount uiBuildingCount;

    [Header("JSON AUTO RECONNECT")]
    [Tooltip("Tự tìm lại JsonDataManager nếu nó được tạo sau UI hoặc bị thay instance khi Play.")]
    public bool autoReconnectJson = true;

    [Tooltip("Khoảng thời gian kiểm tra lại liên kết JsonDataManager.")]
    [Min(0.1f)]
    public float jsonReconnectInterval = 0.5f;

    JsonDataManager subscribedJsonDataManager;
    Coroutine jsonReconnectCoroutine;

    [Header("STYLE")]
    public Color rootColor = new Color32(0, 0, 0, 130);
    public Color panelColor = new Color32(58, 36, 21, 255);
    public Color headerColor = new Color32(107, 63, 31, 255);
    public Color cardColor = new Color32(184, 117, 50, 255);
    public Color darkCardColor = new Color32(43, 26, 16, 235);
    public Color borderColor = new Color32(224, 166, 74, 255);
    public UIThemeManager theme;
    public Color buttonColor = new Color32(199, 106, 27, 255);
    public Color buttonHighlightColor = new Color32(240, 167, 58, 255);

    [Header("SETTINGS")]
    public string playerPrefsNameKey = "PLAYER_NAME";
    public string playerPrefsIdKey = "PLAYER_ID";
    public int minNameLength = 2;
    public int maxNameLength = 16;
    public int sortingOrder = 8000;

    GameObject root;
    GameObject renameRoot;
    GameObject achievementRoot;
    GameObject detailRoot;

    TMP_Text governorNameText;
    TMP_Text governorIdText;
    TMP_Text governorLevelText;
    TMP_Text powerText;
    TMP_Text governorTitleText;
    TMP_Text loginDayText;
    TMP_Text allianceText;
    TMP_Text civilizationText;

    TMP_Text workerText;
    TMP_Text armyText;
    TMP_Text buildingText;
    TMP_Text watchTowerText;
    TMP_Text cannonText;
    TMP_Text resourceCollectedText;
    TMP_Text enemyDefeatedText;

    TMP_Text detailNameText;
    TMP_Text detailIdText;
    TMP_Text detailLevelText;
    TMP_Text detailPowerText;
    TMP_Text detailTitleText;
    TMP_Text detailLoginDayText;
    TMP_Text detailAllianceText;
    TMP_Text detailCivilizationText;
    TMP_Text detailWorkerText;
    TMP_Text detailArmyText;
    TMP_Text detailBuildingText;
    TMP_Text detailWatchTowerText;
    TMP_Text detailCannonText;
    TMP_Text detailResourceText;
    TMP_Text detailEnemyText;

    TMP_InputField nameInput;
    TMP_Text warningText;

    string currentName;
    int governorId;

    void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        LoadData();
        BuildUI();
        BindEvents();

        // Không phụ thuộc Script Execution Order.
        // Nếu JsonDataManager chưa Awake xong, coroutine sẽ tự nối lại sau.
        TryReconnectJsonDataManager(true);

        RefreshUI();

        if (root != null)
            root.SetActive(false);
    }
    Color TitleColor
    {
        get
        {
            if (theme != null)
                return theme.title;

            if (UIThemeManager.Instance != null)
                return UIThemeManager.Instance.title;

            return new Color32(255, 238, 190, 255);
        }
    }


    Color BodyColor
    {
        get
        {
            if (theme != null)
                return theme.description;

            if (UIThemeManager.Instance != null)
                return UIThemeManager.Instance.description;

            return new Color32(120, 88, 55, 255);
        }
    }


    Color ValueColor
    {
        get
        {
            if (theme != null)
                return theme.value;

            if (UIThemeManager.Instance != null)
                return UIThemeManager.Instance.value;

            return new Color32(190, 140, 45, 255);
        }
    }

    void OnEnable()
    {
        TryReconnectJsonDataManager(false);

        if (autoReconnectJson && jsonReconnectCoroutine == null)
            jsonReconnectCoroutine = StartCoroutine(JsonReconnectLoop());
    }
    void ApplyReadableTextStyle(TMP_Text txt, Color faceColor, float outlineWidth = 0.18f)
    {
        if (txt == null) return;

        txt.color = faceColor;
        txt.fontStyle = FontStyles.Bold;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Ellipsis;

        txt.outlineWidth = outlineWidth;
        txt.outlineColor = textOutlineColor;

        var shadow = txt.GetComponent<Shadow>();
        if (shadow == null) shadow = txt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = textShadowDistance;
    }

    void OnDisable()
    {
        if (jsonReconnectCoroutine != null)
        {
            StopCoroutine(jsonReconnectCoroutine);
            jsonReconnectCoroutine = null;
        }
    }

    void OnDestroy()
    {
        if (questPanel != null)
            questPanel.onGoPressed.RemoveListener(OnQuestGoPressed);

        UnsubscribeJsonEvents();
    }

    void BindEvents()
    {
        if (profileButton != null)
        {
            profileButton.onClick.RemoveListener(OpenProfile);
            profileButton.onClick.AddListener(OpenProfile);
        }

        if (questPanel != null)
        {
            questPanel.onGoPressed.RemoveListener(OnQuestGoPressed);
            questPanel.onGoPressed.AddListener(OnQuestGoPressed);
        }

        // JsonDataManager được nối riêng bằng TryReconnectJsonDataManager().
        // Cách này xử lý cả trường hợp manager được tạo sau UI hoặc đổi instance khi Play.
        TryReconnectJsonDataManager(false);
    }

    IEnumerator JsonReconnectLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.1f, jsonReconnectInterval));

        while (enabled)
        {
            TryReconnectJsonDataManager(false);
            yield return wait;
        }

        jsonReconnectCoroutine = null;
    }

    void TryReconnectJsonDataManager(bool logIfMissing)
    {
        JsonDataManager candidate = null;

        // Ưu tiên singleton hiện tại.
        candidate = JsonDataManager.Ins;

        // Nếu singleton chưa sẵn sàng, giữ liên kết Inspector nếu object còn sống.
        if (candidate == null && jsonDataManager != null)
            candidate = jsonDataManager;

        // Tìm object active trong scene.
        if (candidate == null)
            candidate = FindObjectOfType<JsonDataManager>();

        // Tìm cả object inactive trong scene, tránh mất link do thứ tự bật GameObject.
        if (candidate == null)
        {
            JsonDataManager[] allManagers = Resources.FindObjectsOfTypeAll<JsonDataManager>();

            for (int i = 0; i < allManagers.Length; i++)
            {
                JsonDataManager manager = allManagers[i];

                if (manager != null &&
                    manager.gameObject.scene.IsValid() &&
                    manager.gameObject.scene.isLoaded)
                {
                    candidate = manager;
                    break;
                }
            }
        }

        // Không đổi gì nếu đang nối đúng instance.
        if (candidate != null && candidate == subscribedJsonDataManager)
        {
            jsonDataManager = candidate;
            return;
        }

        // Instance cũ bị destroy/thay mới: tháo event cũ trước.
        UnsubscribeJsonEvents();

        jsonDataManager = candidate;

        if (jsonDataManager == null)
        {
            if (logIfMissing)
                Debug.LogWarning("[RoKProfileQuestAutoUI] JsonDataManager chưa tồn tại. UI sẽ tiếp tục tự tìm và nối lại.");

            return;
        }

        SubscribeJsonEvents(jsonDataManager);
        UpdateResourceCollected();

        Debug.Log("[RoKProfileQuestAutoUI] Đã liên kết JsonDataManager: " + jsonDataManager.name);
    }

    void SubscribeJsonEvents(JsonDataManager manager)
    {
        if (manager == null)
            return;

        subscribedJsonDataManager = manager;

        // Trừ trước để bảo đảm không bị đăng ký trùng listener.
        manager.OnWoodChanged -= OnResourceChanged;
        manager.OnStoneChanged -= OnResourceChanged;
        manager.OnFoodChanged -= OnResourceChanged;

        manager.OnWoodChanged += OnResourceChanged;
        manager.OnStoneChanged += OnResourceChanged;
        manager.OnFoodChanged += OnResourceChanged;
    }

    void UnsubscribeJsonEvents()
    {
        if (subscribedJsonDataManager == null)
            return;

        subscribedJsonDataManager.OnWoodChanged -= OnResourceChanged;
        subscribedJsonDataManager.OnStoneChanged -= OnResourceChanged;
        subscribedJsonDataManager.OnFoodChanged -= OnResourceChanged;
        subscribedJsonDataManager = null;
    }

    // Handler chung cho mọi event tài nguyên (nhận giá trị mới nhưng chỉ dùng để trigger refresh)
    void OnResourceChanged(int _)
    {
        UpdateResourceCollected();
    }

    void OnQuestGoPressed(string questId)
    {
        if (questId != renameQuestId)
            return;

        if (questPanel != null)
            questPanel.ClosePanel();

        OpenProfile();
        OpenRenamePanel();
    }

    void LoadData()
    {
        currentName = PlayerPrefs.GetString(playerPrefsNameKey, defaultName);

        if (PlayerPrefs.HasKey(playerPrefsIdKey))
        {
            governorId = PlayerPrefs.GetInt(playerPrefsIdKey);
        }
        else
        {
            governorId = Random.Range(100000, 999999);
            PlayerPrefs.SetInt(playerPrefsIdKey, governorId);
            PlayerPrefs.Save();
        }
    }

    // =====================================================
    // BUILD UI
    // =====================================================

    void BuildUI()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[RoKProfileQuestAutoUI] Không có Canvas.");
            return;
        }

        root = new GameObject("Auto_ProfilePanelRoot", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(targetCanvas.transform, false);

        Canvas c = root.GetComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = sortingOrder;

        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        root.GetComponent<Image>().color = rootColor;

        RectTransform window = CreatePanel(root.transform, "ProfileWindow", new Vector2(1150, 720), Vector2.zero, panelColor, true, mainWindowBgSprite);

        // Header
        RectTransform header = CreatePanel(window, "Header", new Vector2(1150, 90), new Vector2(0, 315), headerColor, true, headerBgSprite);
        CreateText(header, "HeaderTitleText", "Hồ sơ thống đốc", new Vector2(0, -10), new Vector2(900, 70), 44, TitleColor, TextAlignmentOptions.Center, true);

        Button closeButton = CreateButton(header, "CloseProfileButton", "", new Vector2(520, 0), new Vector2(60, 60), new Color32(170, 0, 0, 255), closeButtonBgSprite);
        closeButton.onClick.AddListener(CloseProfile);

        // Left panel
        RectTransform left = CreatePanel(window, "LeftAvatarPanel", new Vector2(330, 522), new Vector2(-385, -20), headerColor, true, leftPanelBgSprite);

        Image avatarFrame = CreateImage(left, "AvatarFrameImage", avatarFrameSprite, new Vector2(0, 150), new Vector2(190, 190), Color.white);
        avatarFrame.preserveAspect = true;

        Image avatar = CreateImage(left, "AvatarImage", avatarSprite, new Vector2(0, 150), new Vector2(150, 150), Color.white);
        avatar.preserveAspect = true;

        CreateText(left, "LevelBadgeText", governorLevel.ToString(), new Vector2(0, 45), new Vector2(80, 45), 28, TitleColor, TextAlignmentOptions.Center, true);

        allianceText = CreateInfoBlock(left, "Liên minh", allianceName, new Vector2(27, -82));
        civilizationText = CreateInfoBlock(left, "Văn minh", civilizationName, new Vector2(27, -151));

        CreateText(left, "OnlineStatusText", "● Đang online", new Vector2(0, -10), new Vector2(240, 35), 22, new Color32(90, 255, 80, 255), TextAlignmentOptions.Center, true);

        // Info panel
        RectTransform info = CreatePanel(window, "InfoPanel", new Vector2(730, 250), new Vector2(185, 140), cardColor, true, infoPanelBgSprite);

        CreateLabelValue(info, "Tên thống đốc", out governorNameText, new Vector2(-244, 75), currentName);
        CreateLabelValue(info, "ID", out governorIdText, new Vector2(-244, 25), governorId.ToString());
        CreateLabelValue(info, "Cấp thống đốc", out governorLevelText, new Vector2(-244, -25), governorLevel.ToString());
        CreateLabelValue(info, "Sức mạnh", out powerText, new Vector2(-244, -75), FormatNumber(power));

        CreateLabelValue(info, "Danh hiệu", out governorTitleText, new Vector2(60, -25), governorTitle);
        CreateLabelValue(info, "Ngày đăng nhập", out loginDayText, new Vector2(60, -75), loginDays.ToString());

        // Stats panel
        RectTransform stats = CreatePanel(window, "StatsPanel", new Vector2(730, 210), new Vector2(185, -130), darkCardColor, true, statsPanelBgSprite);

        CreateTopStat(stats, "Worker", out workerText, new Vector2(-280, 55), workerCurrent + "/" + workerMax);
        CreateTopStat(stats, "Quân đội", out armyText, new Vector2(-140, 55), armyCount.ToString());
        CreateTopStat(stats, "Công trình", out buildingText, new Vector2(0, 55), buildingCount.ToString());
        CreateTopStat(stats, "Tháp canh", out watchTowerText, new Vector2(140, 55), watchTowerCount.ToString());
        CreateTopStat(stats, "Pháo thủ", out cannonText, new Vector2(280, 55), cannonCount.ToString());

        CreateText(stats, "ResourceCollectedLabel", "Tài nguyên thu thập", new Vector2(-180, -28), new Vector2(280, 30), 21, BodyColor, TextAlignmentOptions.Center, false);
        resourceCollectedText = CreateText(stats, "ResourceCollectedText", FormatNumber(resourceCollected), new Vector2(-180, -58), new Vector2(280, 45), 34, ValueColor, TextAlignmentOptions.Center, true);

        CreateText(stats, "EnemyDefeatedLabel", "Kẻ địch đánh bại", new Vector2(180, -28), new Vector2(280, 30), 21, BodyColor, TextAlignmentOptions.Center, false);
        enemyDefeatedText = CreateText(stats, "EnemyDefeatedText", enemyDefeated.ToString(), new Vector2(180, -58), new Vector2(280, 45), 34, ValueColor, TextAlignmentOptions.Center, true);

        // Bottom buttons: dùng trực tiếp ảnh đã vẽ, không tạo text và không có hiệu ứng hover/press
        Button renameButton = CreateStaticImageButton(
            window, "RenameButton", renameButtonSprite,
            new Vector2(-300, -315), new Vector2(250, 60));
        renameButton.onClick.AddListener(OpenRenamePanel);

        Button achievementButton = CreateStaticImageButton(
            window, "AchievementButton", achievementButtonSprite,
            new Vector2(0, -315), new Vector2(250, 60));
        achievementButton.onClick.AddListener(OpenAchievementPanel);

        Button detailButton = CreateStaticImageButton(
            window, "DetailButton", detailButtonSprite,
            new Vector2(300, -315), new Vector2(250, 60));
        detailButton.onClick.AddListener(OpenDetailPanel);

        BuildRenamePanel(window);
        BuildAchievementPanel(window);
        BuildDetailPanel(window);
    }

    void BuildRenamePanel(RectTransform parent)
    {
        renameRoot = new GameObject("RenamePanelRoot", typeof(RectTransform), typeof(Image));
        renameRoot.transform.SetParent(parent, false);

        RectTransform rt = renameRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        renameRoot.GetComponent<Image>().color = new Color32(0, 0, 0, 150);

        RectTransform renameWindow = CreatePanel(renameRoot.transform, "RenameWindow", new Vector2(500, 300), Vector2.zero, headerColor, true, renameWindowBgSprite);

        CreateText(renameWindow, "RenameTitleText", "", new Vector2(0, 105), new Vector2(450, 45), 34, TitleColor, TextAlignmentOptions.Center, true);

        nameInput = CreateInputField(renameWindow, "NameInputField", new Vector2(0, 45), new Vector2(360, 55));

        warningText = CreateText(renameWindow, "WarningText", "", new Vector2(0, -15), new Vector2(400, 30), 20, ValueColor, TextAlignmentOptions.Center, false);

        Button confirm = CreateButton(renameWindow, "ConfirmRenameButton", "", new Vector2(-90, -95), new Vector2(150, 50), buttonColor, confirmButtonBgSprite);
        confirm.onClick.AddListener(ConfirmRename);

        Button cancel = CreateButton(renameWindow, "CancelRenameButton", "", new Vector2(90, -95), new Vector2(150, 50), new Color32(122, 74, 36, 255), cancelButtonBgSprite);
        cancel.onClick.AddListener(CloseRenamePanel);

        renameRoot.SetActive(false);
    }

    void BuildAchievementPanel(RectTransform parent)
    {
        achievementRoot = new GameObject("AchievementPanelRoot", typeof(RectTransform), typeof(Image));
        achievementRoot.transform.SetParent(parent, false);

        RectTransform rt = achievementRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        achievementRoot.GetComponent<Image>().color = new Color32(0, 0, 0, 150);

        // Tăng chiều cao cửa sổ (540 -> 620) để chứa các hàng đã giãn cách rộng hơn, tránh tràn/chồng chữ.
        RectTransform window = CreatePanel(achievementRoot.transform, "AchievementWindow", new Vector2(780, 620), Vector2.zero, headerColor, true, achievementWindowBgSprite);

        CreateText(window, "AchievementTitleText", "", new Vector2(0, 260), new Vector2(650, 50), 36, TitleColor, TextAlignmentOptions.Center, true);

        Button close = CreateButton(window, "CloseAchievementButton", "", new Vector2(340, 260), new Vector2(52, 52), new Color32(170, 0, 0, 255), closeButtonBgSprite);
        close.onClick.AddListener(CloseAchievementPanel);

        // Giãn khoảng cách các hàng (65 -> 76) cho khớp với chiều cao hàng mới (70), tránh hàng nọ đè hàng kia.
        CreateAchievementRow(window, "Bài học đầu", "Xây công trình đầu tiên.", "Hoàn thành", new Vector2(0, 175));
        CreateAchievementRow(window, "Người chỉ huy", "Huấn luyện đội quân đầu tiên.", armyCount > 0 ? "Hoàn thành" : "Chưa xong", new Vector2(0, 99));
        CreateAchievementRow(window, "Lá chắn làng", "Xây Tháp Canh để bảo vệ dân làng.", watchTowerCount > 0 ? "Hoàn thành" : "Chưa xong", new Vector2(0, 23));
        CreateAchievementRow(window, "Hỏa lực phòng thủ", "Mở khóa Pháo Thủ.", cannonCount > 0 ? "Hoàn thành" : "Chưa xong", new Vector2(0, -53));
        CreateAchievementRow(window, "Nhà khai thác", "Thu thập 5.000 tài nguyên.", resourceCollected >= 5000 ? "Hoàn thành" : FormatNumber(resourceCollected) + "/5.000", new Vector2(0, -129));
        CreateAchievementRow(window, "Dẹp loạn", "Đánh bại 10 kẻ địch.", enemyDefeated >= 10 ? "Hoàn thành" : enemyDefeated + "/10", new Vector2(0, -205));

        achievementRoot.SetActive(false);
    }

    void BuildDetailPanel(RectTransform parent)
    {
        detailRoot = new GameObject("DetailPanelRoot", typeof(RectTransform), typeof(Image));
        detailRoot.transform.SetParent(parent, false);

        RectTransform rt = detailRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        detailRoot.GetComponent<Image>().color = new Color32(0, 0, 0, 150);

        RectTransform window = CreatePanel(detailRoot.transform, "DetailWindow", new Vector2(820, 560), Vector2.zero, headerColor, true, detailWindowBgSprite);

        CreateText(window, "DetailTitleText", "", new Vector2(0, 240), new Vector2(680, 50), 36, TitleColor, TextAlignmentOptions.Center, true);

        Button close = CreateButton(window, "CloseDetailButton", "", new Vector2(360, 240), new Vector2(52, 52), new Color32(170, 0, 0, 255), closeButtonBgSprite);
        close.onClick.AddListener(CloseDetailPanel);

        CreateDetailRow(window, "Tên", out detailNameText, currentName, new Vector2(-207, 165));
        CreateDetailRow(window, "ID", out detailIdText, governorId.ToString(), new Vector2(-207, 110));
        CreateDetailRow(window, "Cấp", out detailLevelText, governorLevel.ToString(), new Vector2(-207, 55));
        CreateDetailRow(window, "Sức mạnh", out detailPowerText, FormatNumber(power), new Vector2(-207, 0));
        CreateDetailRow(window, "Danh hiệu", out detailTitleText, governorTitle, new Vector2(-207, -55));
        CreateDetailRow(window, "Ngày ĐN", out detailLoginDayText, loginDays.ToString(), new Vector2(-207, -110));

        CreateDetailRow(window, "Liên minh", out detailAllianceText, allianceName, new Vector2(213, 165));
        CreateDetailRow(window, "Văn minh", out detailCivilizationText, civilizationName, new Vector2(213, 110));
        CreateDetailRow(window, "Worker", out detailWorkerText, workerCurrent + "/" + workerMax, new Vector2(213, 55));
        CreateDetailRow(window, "Quân đội", out detailArmyText, armyCount.ToString(), new Vector2(213, 0));
        CreateDetailRow(window, "Công trình", out detailBuildingText, buildingCount.ToString(), new Vector2(213, -55));
        CreateDetailRow(window, "Tháp canh", out detailWatchTowerText, watchTowerCount.ToString(), new Vector2(213, -110));
        CreateDetailRow(window, "Pháo thủ", out detailCannonText, cannonCount.ToString(), new Vector2(213, -165));

        CreateDetailRow(window, "Tài nguyên", out detailResourceText, FormatNumber(resourceCollected), new Vector2(-207, -165));
        CreateDetailRow(window, "Địch bại", out detailEnemyText, enemyDefeated.ToString(), new Vector2(-207, -220));

        detailRoot.SetActive(false);
    }

    // =====================================================
    // ACTIONS
    // =====================================================

    public void OpenProfile()
    {
        TryReconnectJsonDataManager(false);

        if (root == null)
            BuildUI();

        root.SetActive(true);
        root.transform.SetAsLastSibling();
        RefreshUI();
    }

    public void CloseProfile()
    {
        CloseAllSubPanels();

        if (root != null)
            root.SetActive(false);
    }

    public void OpenRenamePanel()
    {
        if (root != null)
            root.SetActive(true);

        if (achievementRoot != null)
            achievementRoot.SetActive(false);

        if (detailRoot != null)
            detailRoot.SetActive(false);

        if (renameRoot != null)
            renameRoot.SetActive(true);

        if (warningText != null)
            warningText.text = "";

        if (nameInput != null)
        {
            nameInput.text = currentName;
            nameInput.Select();
            nameInput.ActivateInputField();
        }
    }

    public void CloseRenamePanel()
    {
        if (renameRoot != null)
            renameRoot.SetActive(false);
    }

    public void OpenAchievementPanel()
    {
        if (root != null)
            root.SetActive(true);

        if (renameRoot != null)
            renameRoot.SetActive(false);

        if (detailRoot != null)
            detailRoot.SetActive(false);

        RefreshAchievementPanel();

        if (achievementRoot != null)
            achievementRoot.SetActive(true);
    }

    public void CloseAchievementPanel()
    {
        if (achievementRoot != null)
            achievementRoot.SetActive(false);
    }

    public void OpenDetailPanel()
    {
        if (root != null)
            root.SetActive(true);

        if (renameRoot != null)
            renameRoot.SetActive(false);

        if (achievementRoot != null)
            achievementRoot.SetActive(false);

        RefreshUI();

        if (detailRoot != null)
            detailRoot.SetActive(true);
    }

    public void CloseDetailPanel()
    {
        if (detailRoot != null)
            detailRoot.SetActive(false);
    }

    void CloseAllSubPanels()
    {
        if (renameRoot != null) renameRoot.SetActive(false);
        if (achievementRoot != null) achievementRoot.SetActive(false);
        if (detailRoot != null) detailRoot.SetActive(false);
    }

    public void ConfirmRename()
    {
        if (nameInput == null)
            return;

        string newName = nameInput.text.Trim();

        if (newName.Length < 2)
        {
            warningText.text = "Tên quá ngắn.";
            return;
        }

        if (newName.Length > 16)
        {
            warningText.text = "Tên quá dài.";
            return;
        }

        currentName = newName;
        PlayerPrefs.SetString(playerPrefsNameKey, currentName);
        PlayerPrefs.Save();

        RefreshUI();
        CloseRenamePanel();

        if (RoKQuestMissionGuideRouter.Instance != null)
        {
            RoKQuestMissionGuideRouter.Instance.OnPlayerNameSet();
        }
        else
        {
            // fallback nếu Router chưa tồn tại
            if (questPanel != null)
                questPanel.CompleteQuest(renameQuestId);
        }


        Debug.Log("[RoKProfileQuestAutoUI] Đã đổi tên: " + currentName);
    }

    void UpdateResourceCollected()
    {
        if (jsonDataManager == null || jsonDataManager != subscribedJsonDataManager)
            TryReconnectJsonDataManager(false);

        if (jsonDataManager == null)
            return;

        // Tổng tài nguyên hiện có trên HUD (Wood + Stone + Food)
        resourceCollected =
            jsonDataManager.food +
            jsonDataManager.wood +
            jsonDataManager.stone;

        // Cập nhật cả panel Stats chính lẫn panel Hồ sơ chi tiết,
        // dùng chung FormatNumber để giữ đúng định dạng "1.234"
        if (resourceCollectedText != null)
            resourceCollectedText.text = FormatNumber(resourceCollected);

        if (detailResourceText != null)
            detailResourceText.text = FormatNumber(resourceCollected);
    }

    void RefreshUI()
    {
        UpdateResourceCollected();
        if (governorNameText != null) governorNameText.text = currentName;
        if (governorIdText != null) governorIdText.text = governorId.ToString();
        if (governorLevelText != null) governorLevelText.text = governorLevel.ToString();
        if (powerText != null) powerText.text = FormatNumber(power);
        if (governorTitleText != null) governorTitleText.text = governorTitle;
        if (loginDayText != null) loginDayText.text = loginDays.ToString();
        if (allianceText != null) allianceText.text = allianceName;
        if (civilizationText != null) civilizationText.text = civilizationName;

        if (uiWorkerCount != null)
        {
            workerCurrent = uiWorkerCount.GetWorkerCount();
        }


        if (workerText != null)
        {
            workerText.text =
                workerCurrent + "/" + workerMax;
        }
        if (uiLinh != null)
        {
            armyCount = uiLinh.GetSoldierCount();
        }


        if (armyText != null) armyText.text = armyCount.ToString();
        if (uiThapCanh != null)
        {
            watchTowerCount =
                uiThapCanh.GetWatchTowerCount();
        }

        if (watchTowerText != null) watchTowerText.text = watchTowerCount.ToString();

        if (uiPhaoThu != null)
        {
            cannonCount =
                uiPhaoThu.GetCannonCount();
        }

        if (cannonText != null) cannonText.text = cannonCount.ToString();

        if (uiBuildingCount != null)
        {
            buildingCount =
                uiBuildingCount.GetBuildingCount();
        }


        if (buildingText != null)
        {
            buildingText.text =
                buildingCount.ToString();
        }
        if (resourceCollectedText != null) resourceCollectedText.text = FormatNumber(resourceCollected);
        if (enemyDefeatedText != null) enemyDefeatedText.text = enemyDefeated.ToString();

        if (detailNameText != null) detailNameText.text = currentName;
        if (detailIdText != null) detailIdText.text = governorId.ToString();
        if (detailLevelText != null) detailLevelText.text = governorLevel.ToString();
        if (detailPowerText != null) detailPowerText.text = FormatNumber(power);
        if (detailTitleText != null) detailTitleText.text = governorTitle;
        if (detailLoginDayText != null) detailLoginDayText.text = loginDays.ToString();
        if (detailAllianceText != null) detailAllianceText.text = allianceName;
        if (detailCivilizationText != null) detailCivilizationText.text = civilizationName;
        if (detailWorkerText != null) detailWorkerText.text = workerCurrent + "/" + workerMax;
        if (detailArmyText != null) detailArmyText.text = armyCount.ToString();
        if (detailBuildingText != null) detailBuildingText.text = buildingCount.ToString();
        if (detailWatchTowerText != null) detailWatchTowerText.text = watchTowerCount.ToString();
        if (detailCannonText != null) detailCannonText.text = cannonCount.ToString();
        if (detailResourceText != null) detailResourceText.text = FormatNumber(resourceCollected);
        if (detailEnemyText != null) detailEnemyText.text = enemyDefeated.ToString();
    }

    // =====================================================
    // CREATE HELPERS
    // =====================================================

    // Đã thêm tham số "bgSprite" (mặc định null) để hỗ trợ nền dạng ảnh (khung gỗ) đồng bộ style.
    // Nếu bgSprite == null -> hành vi giữ nguyên y hệt bản gốc (tô màu phẳng + Outline).
    // Nếu bgSprite != null -> dùng ảnh khung làm nền (Image.Type.Sliced), không cộng thêm Outline
    // vì viền hoa văn đã có sẵn trong ảnh.
    RectTransform CreatePanel(Transform parent, string name, Vector2 size, Vector2 pos, Color color, bool outline, Sprite bgSprite = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();

        if (bgSprite != null)
        {
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = color;

            if (outline)
            {
                Outline o = go.AddComponent<Outline>();
                o.effectColor = borderColor;
                o.effectDistance = new Vector2(2f, -2f);
                o.useGraphicAlpha = false;
            }
        }

        return rt;
    }

    Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;

        return img;
    }

    TMP_Text CreateText(Transform parent, string name, string value, Vector2 pos, Vector2 size, int fontSize, Color color, TextAlignmentOptions align, bool bold)
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

        if (theme != null)
        {
            theme.Apply(
                text,
                DetectTextType(name)
            );
        }
        text.alignment = align;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        if (vietnameseFont != null)
            text.font = vietnameseFont;

        text.outlineColor = new Color32(43, 26, 16, 255);
        text.outlineWidth = 0.15f;

        return text;
    }
    UI_TEXT_TYPE DetectTextType(string name)
    {

        if (name.Contains("Title"))
            return UI_TEXT_TYPE.Title;


        if (name.Contains("Label"))
            return UI_TEXT_TYPE.Label;


        if (name.Contains("Value"))
            return UI_TEXT_TYPE.Value;


        if (name.Contains("Reward"))
            return UI_TEXT_TYPE.Reward;


        return UI_TEXT_TYPE.Description;

    }

    // Đã thêm tham số "bgSprite" (mặc định null) để hỗ trợ nền dạng ảnh cho nút bấm.
    // Nếu bgSprite == null -> hành vi giữ nguyên y hệt bản gốc.
    Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, Sprite bgSprite = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();

        Color normal = color;

        if (bgSprite != null)
        {
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            normal = Color.white;
        }
        else
        {
            img.color = color;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
        }

        Button btn = go.GetComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = normal;
        cb.highlightedColor = bgSprite != null ? new Color(0.9f, 0.9f, 0.9f, 1f) : buttonHighlightColor;
        cb.pressedColor = bgSprite != null ? new Color(0.75f, 0.75f, 0.75f, 1f) : new Color32(145, 70, 16, 255);
        cb.selectedColor = cb.highlightedColor;
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        CreateText(go.transform, "Text", label, Vector2.zero, size, 28, TitleColor, TextAlignmentOptions.Center, true);

        return btn;
    }

    // Nút dùng nguyên ảnh đã vẽ:
    // - Không tạo TMP text
    // - Không đổi màu khi hover
    // - Không có trạng thái pressed/selected
    Button CreateStaticImageButton(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 pos,
        Vector2 size)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        return button;
    }

    TMP_InputField CreateInputField(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.GetComponent<Image>();

        if (inputFieldBgSprite != null)
        {
            img.sprite = inputFieldBgSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = darkCardColor;
        }

        TMP_InputField input = go.GetComponent<TMP_InputField>();
        input.characterLimit = maxNameLength;

        GameObject viewportGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(go.transform, false);

        RectTransform viewport = viewportGO.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12, 5);
        viewport.offsetMax = new Vector2(-12, -5);

        TMP_Text inputText = CreateText(viewportGO.transform, "Text", "", Vector2.zero, size, 24, TitleColor, TextAlignmentOptions.MidlineLeft, false);
        TMP_Text placeholder = CreateText(viewportGO.transform, "Placeholder", "Nhập tên mới...", Vector2.zero, size, 22, BodyColor, TextAlignmentOptions.MidlineLeft, false);

        input.textViewport = viewport;
        input.textComponent = inputText;
        input.placeholder = placeholder;

        return input;
    }

    void CreateAchievementRow(Transform parent, string title, string description, string status, Vector2 pos)
    {
        // Tăng chiều cao hàng (54 -> 70) và tách "Tiêu đề + Trạng thái" (dòng trên)
        // ra khỏi "Mô tả" (dòng dưới) thay vì để chung 1 hàng ngang như trước.
        // Đây là nguyên nhân khiến tiêu đề dài (VD "Hòa lực phòng thủ") tràn đè lên mô tả.
        RectTransform row = CreatePanel(parent, "Achievement_" + title, new Vector2(670, 70), pos, darkCardColor, true, achievementRowBgSprite);

        // Dòng trên - trái: Tiêu đề. Dùng chung màu detailLabelColor với bảng Hồ sơ chi tiết để đồng bộ tông màu.
        TMP_Text titleText = CreateText(row, title + "Title", title, new Vector2(-125, 15), new Vector2(380, 26), 22, detailLabelColor, TextAlignmentOptions.Left, true);
        titleText.color = detailLabelColor;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        // Dòng trên - phải: Trạng thái. Dùng chung màu detailValueColor (vàng sáng) với giá trị bên bảng chi tiết.
        TMP_Text statusText = CreateText(row, title + "Status", status, new Vector2(235, 15), new Vector2(160, 26), 19, detailValueColor, TextAlignmentOptions.Right, true);
        statusText.color = detailValueColor;
        statusText.overflowMode = TextOverflowModes.Ellipsis;

        // Dòng dưới: Mô tả cũng dùng detailLabelColor cho đồng bộ với phần nhãn/mô tả của bảng chi tiết.
        TMP_Text descText = CreateText(row, title + "Desc", description, new Vector2(0, -16), new Vector2(630, 26), 18, detailLabelColor, TextAlignmentOptions.Left, false);
        descText.color = detailLabelColor;
        descText.overflowMode = TextOverflowModes.Ellipsis;
    }

    void CreateDetailRow(Transform parent, string label, out TMP_Text valueText, string value, Vector2 pos)
    {
        RectTransform row = CreatePanel(parent, "Detail_" + label, new Vector2(360, 42), pos, darkCardColor, true, detailRowBgSprite);

        // Thu hẹp box nhãn + lùi vào 15px so với viền trái, giảm cỡ chữ (20 -> 18)
        // và bật Ellipsis để nhãn dài (VD "Tài nguyên", "Ngày ĐN") không còn tràn đè vào giá trị.
        TMP_Text labelText = CreateText(row, label + "Label", label, new Vector2(-100, 0), new Vector2(130, 30), 18, detailLabelColor, TextAlignmentOptions.Left, false);
        labelText.color = detailLabelColor;
        labelText.overflowMode = TextOverflowModes.Ellipsis;

        // Giá trị lùi vào trong viền phải (thay vì tràn ra ngoài như trước), màu vàng sáng để nổi
        // rõ trên nền hàng gỗ tối, không còn phụ thuộc theme (tránh bị ghi đè về màu nhạt).
        valueText = CreateText(row, label + "Value", value, new Vector2(92, 0), new Vector2(150, 30), 20, detailValueColor, TextAlignmentOptions.Left, true);
        valueText.color = detailValueColor;
        valueText.overflowMode = TextOverflowModes.Ellipsis;
    }

    void RefreshAchievementPanel()
    {
        if (achievementRoot == null)
            return;

        // Để dữ liệu trạng thái được làm mới đơn giản nhất, dựng lại panel thành tích mỗi lần mở.
        RectTransform parent = achievementRoot.transform.parent as RectTransform;

        if (parent == null)
            return;

        Destroy(achievementRoot);
        BuildAchievementPanel(parent);
    }

    TMP_Text CreateInfoBlock(Transform parent, string label, string value, Vector2 pos)
    {
        CreateText(parent, label + "Title", label, pos, new Vector2(250, 30), 22, BodyColor, TextAlignmentOptions.Left, false);
        return CreateText(parent, label + "Text", value, pos + new Vector2(0, -35), new Vector2(250, 40), 26, ValueColor, TextAlignmentOptions.Left, true);
    }

    void CreateLabelValue(Transform parent, string label, out TMP_Text valueText, Vector2 pos, string value)
    {
        CreateText(
            parent,
            label + "Label",
            label,
            pos,
            new Vector2(180, 30),
            22,
            BodyColor,
            TextAlignmentOptions.Left,
            false
        );

        valueText = CreateText(
            parent,
            label + "Value",
            value,
            pos + new Vector2(220, 0),
            new Vector2(250, 35),
            26,
            ValueColor,
            TextAlignmentOptions.Left,
            true
        );
    }

    void CreateTopStat(Transform parent, string label, out TMP_Text valueText, Vector2 pos, string value)
    {
        CreateText(parent, label + "Label", label, pos, new Vector2(120, 25), 19, BodyColor, TextAlignmentOptions.Center, false);
        valueText = CreateText(parent, label + "Value", value, pos + new Vector2(0, -35), new Vector2(120, 40), 28, ValueColor, TextAlignmentOptions.Center, true);
    }

    string FormatNumber(int value)
    {
        return value.ToString("#,0").Replace(",", ".");
    }
}