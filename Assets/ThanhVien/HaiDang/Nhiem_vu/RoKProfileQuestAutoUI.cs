using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoKProfileQuestAutoUI : MonoBehaviour
{
    [Header("LINK")]
    public Canvas targetCanvas;
    public RoKQuestPanelUI questPanel;
    public Button profileButton;

    [Header("SPRITES")]
    public Sprite avatarSprite;
    public Sprite avatarFrameSprite;

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

    [Header("STYLE")]
    public Color rootColor = new Color32(0, 0, 0, 130);
    public Color panelColor = new Color32(58, 36, 21, 255);
    public Color headerColor = new Color32(107, 63, 31, 255);
    public Color cardColor = new Color32(184, 117, 50, 255);
    public Color darkCardColor = new Color32(43, 26, 16, 235);
    public Color borderColor = new Color32(224, 166, 74, 255);
    public Color titleColor = new Color32(255, 241, 194, 255);
    public Color bodyColor = new Color32(232, 212, 162, 255);
    public Color valueColor = new Color32(255, 224, 138, 255);
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
        RefreshUI();

        if (root != null)
            root.SetActive(false);
    }

    void OnDestroy()
    {
        if (questPanel != null)
            questPanel.onGoPressed.RemoveListener(OnQuestGoPressed);
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

        RectTransform window = CreatePanel(root.transform, "ProfileWindow", new Vector2(1150, 720), Vector2.zero, panelColor, true);

        // Header
        RectTransform header = CreatePanel(window, "Header", new Vector2(1150, 90), new Vector2(0, 315), headerColor, true);
        CreateText(header, "HeaderTitleText", "Hồ sơ thống đốc", new Vector2(0, 0), new Vector2(900, 70), 44, titleColor, TextAlignmentOptions.Center, true);

        Button closeButton = CreateButton(header, "CloseProfileButton", "X", new Vector2(520, 0), new Vector2(60, 60), new Color32(170, 0, 0, 255));
        closeButton.onClick.AddListener(CloseProfile);

        // Left panel
        RectTransform left = CreatePanel(window, "LeftAvatarPanel", new Vector2(330, 500), new Vector2(-385, -35), headerColor, true);

        Image avatarFrame = CreateImage(left, "AvatarFrameImage", avatarFrameSprite, new Vector2(0, 150), new Vector2(190, 190), Color.white);
        avatarFrame.preserveAspect = true;

        Image avatar = CreateImage(left, "AvatarImage", avatarSprite, new Vector2(0, 150), new Vector2(150, 150), Color.white);
        avatar.preserveAspect = true;

        CreateText(left, "LevelBadgeText", governorLevel.ToString(), new Vector2(0, 45), new Vector2(80, 45), 28, titleColor, TextAlignmentOptions.Center, true);

        allianceText = CreateInfoBlock(left, "Liên minh", allianceName, new Vector2(27, -82));
        civilizationText = CreateInfoBlock(left, "Văn minh", civilizationName, new Vector2(27, -151));

        CreateText(left, "OnlineStatusText", "● Đang online", new Vector2(0, -10), new Vector2(240, 35), 22, new Color32(90, 255, 80, 255), TextAlignmentOptions.Center, true);

        // Info panel
        RectTransform info = CreatePanel(window, "InfoPanel", new Vector2(730, 250), new Vector2(185, 140), cardColor, true);

        CreateLabelValue(info, "Tên thống đốc", out governorNameText, new Vector2(-244, 75), currentName);
        CreateLabelValue(info, "ID", out governorIdText, new Vector2(-244, 25), governorId.ToString());
        CreateLabelValue(info, "Cấp thống đốc", out governorLevelText, new Vector2(-244, -25), governorLevel.ToString());
        CreateLabelValue(info, "Sức mạnh", out powerText, new Vector2(-244, -75), FormatNumber(power));

        CreateLabelValue(info, "Danh hiệu", out governorTitleText, new Vector2(60, -25), governorTitle);
        CreateLabelValue(info, "Ngày đăng nhập", out loginDayText, new Vector2(60, -75), loginDays.ToString());

        // Stats panel
        RectTransform stats = CreatePanel(window, "StatsPanel", new Vector2(730, 210), new Vector2(185, -130), darkCardColor, true);

        CreateTopStat(stats, "Worker", out workerText, new Vector2(-280, 55), workerCurrent + "/" + workerMax);
        CreateTopStat(stats, "Quân đội", out armyText, new Vector2(-140, 55), armyCount.ToString());
        CreateTopStat(stats, "Công trình", out buildingText, new Vector2(0, 55), buildingCount.ToString());
        CreateTopStat(stats, "Tháp canh", out watchTowerText, new Vector2(140, 55), watchTowerCount.ToString());
        CreateTopStat(stats, "Pháo thủ", out cannonText, new Vector2(280, 55), cannonCount.ToString());

        CreateText(stats, "ResourceCollectedLabel", "Tài nguyên thu thập", new Vector2(-180, -45), new Vector2(280, 30), 21, bodyColor, TextAlignmentOptions.Center, false);
        resourceCollectedText = CreateText(stats, "ResourceCollectedText", FormatNumber(resourceCollected), new Vector2(-180, -85), new Vector2(280, 45), 34, valueColor, TextAlignmentOptions.Center, true);

        CreateText(stats, "EnemyDefeatedLabel", "Kẻ địch đánh bại", new Vector2(190, -45), new Vector2(280, 30), 21, bodyColor, TextAlignmentOptions.Center, false);
        enemyDefeatedText = CreateText(stats, "EnemyDefeatedText", enemyDefeated.ToString(), new Vector2(190, -85), new Vector2(280, 45), 34, valueColor, TextAlignmentOptions.Center, true);

        // Bottom buttons
        Button renameButton = CreateButton(window, "RenameButton", "Đổi tên", new Vector2(-300, -315), new Vector2(250, 60), buttonColor);
        renameButton.onClick.AddListener(OpenRenamePanel);

        Button achievementButton = CreateButton(window, "AchievementButton", "Thành tích", new Vector2(0, -315), new Vector2(250, 60), buttonColor);
        achievementButton.onClick.AddListener(OpenAchievementPanel);

        Button detailButton = CreateButton(window, "DetailButton", "Hồ sơ chi tiết", new Vector2(300, -315), new Vector2(250, 60), buttonColor);
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

        RectTransform renameWindow = CreatePanel(renameRoot.transform, "RenameWindow", new Vector2(500, 300), Vector2.zero, headerColor, true);

        CreateText(renameWindow, "RenameTitleText", "Đổi tên", new Vector2(0, 105), new Vector2(450, 45), 34, titleColor, TextAlignmentOptions.Center, true);

        nameInput = CreateInputField(renameWindow, "NameInputField", new Vector2(0, 45), new Vector2(360, 55));

        warningText = CreateText(renameWindow, "WarningText", "", new Vector2(0, -15), new Vector2(400, 30), 20, valueColor, TextAlignmentOptions.Center, false);

        Button confirm = CreateButton(renameWindow, "ConfirmRenameButton", "Xác nhận", new Vector2(-90, -95), new Vector2(150, 50), buttonColor);
        confirm.onClick.AddListener(ConfirmRename);

        Button cancel = CreateButton(renameWindow, "CancelRenameButton", "Hủy", new Vector2(90, -95), new Vector2(150, 50), new Color32(122, 74, 36, 255));
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

        RectTransform window = CreatePanel(achievementRoot.transform, "AchievementWindow", new Vector2(780, 540), Vector2.zero, headerColor, true);

        CreateText(window, "AchievementTitleText", "Thành tích", new Vector2(0, 230), new Vector2(650, 50), 36, titleColor, TextAlignmentOptions.Center, true);

        Button close = CreateButton(window, "CloseAchievementButton", "X", new Vector2(340, 230), new Vector2(52, 52), new Color32(170, 0, 0, 255));
        close.onClick.AddListener(CloseAchievementPanel);

        CreateAchievementRow(window, "Bài học đầu", "Xây công trình đầu tiên.", "Hoàn thành", new Vector2(0, 155));
        CreateAchievementRow(window, "Người chỉ huy", "Huấn luyện đội quân đầu tiên.", armyCount > 0 ? "Hoàn thành" : "Chưa xong", new Vector2(0, 90));
        CreateAchievementRow(window, "Lá chắn làng", "Xây Tháp Canh để bảo vệ dân làng.", watchTowerCount > 0 ? "Hoàn thành" : "Chưa xong", new Vector2(0, 25));
        CreateAchievementRow(window, "Hỏa lực phòng thủ", "Mở khóa Pháo Thủ.", cannonCount > 0 ? "Hoàn thành" : "Chưa xong", new Vector2(0, -40));
        CreateAchievementRow(window, "Nhà khai thác", "Thu thập 5.000 tài nguyên.", resourceCollected >= 5000 ? "Hoàn thành" : FormatNumber(resourceCollected) + "/5.000", new Vector2(0, -105));
        CreateAchievementRow(window, "Dẹp loạn", "Đánh bại 10 kẻ địch.", enemyDefeated >= 10 ? "Hoàn thành" : enemyDefeated + "/10", new Vector2(0, -170));

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

        RectTransform window = CreatePanel(detailRoot.transform, "DetailWindow", new Vector2(820, 560), Vector2.zero, headerColor, true);

        CreateText(window, "DetailTitleText", "Hồ sơ chi tiết", new Vector2(0, 240), new Vector2(680, 50), 36, titleColor, TextAlignmentOptions.Center, true);

        Button close = CreateButton(window, "CloseDetailButton", "X", new Vector2(360, 240), new Vector2(52, 52), new Color32(170, 0, 0, 255));
        close.onClick.AddListener(CloseDetailPanel);

        CreateDetailRow(window, "Tên thống đốc", out detailNameText, currentName, new Vector2(-207, 165));
        CreateDetailRow(window, "ID", out detailIdText, governorId.ToString(), new Vector2(-207, 110));
        CreateDetailRow(window, "Cấp thống đốc", out detailLevelText, governorLevel.ToString(), new Vector2(-207, 55));
        CreateDetailRow(window, "Sức mạnh", out detailPowerText, FormatNumber(power), new Vector2(-207, 0));
        CreateDetailRow(window, "Danh hiệu", out detailTitleText, governorTitle, new Vector2(-207, -55));
        CreateDetailRow(window, "Ngày đăng nhập", out detailLoginDayText, loginDays.ToString(), new Vector2(-207, -110));

        CreateDetailRow(window, "Liên minh", out detailAllianceText, allianceName, new Vector2(213, 165));
        CreateDetailRow(window, "Văn minh", out detailCivilizationText, civilizationName, new Vector2(213, 110));
        CreateDetailRow(window, "Worker", out detailWorkerText, workerCurrent + "/" + workerMax, new Vector2(213, 55));
        CreateDetailRow(window, "Quân đội", out detailArmyText, armyCount.ToString(), new Vector2(213, 0));
        CreateDetailRow(window, "Công trình", out detailBuildingText, buildingCount.ToString(), new Vector2(213, -55));
        CreateDetailRow(window, "Tháp canh", out detailWatchTowerText, watchTowerCount.ToString(), new Vector2(213, -110));
        CreateDetailRow(window, "Pháo thủ", out detailCannonText, cannonCount.ToString(), new Vector2(213, -165));

        CreateDetailRow(window, "Tài nguyên thu thập", out detailResourceText, FormatNumber(resourceCollected), new Vector2(-207, -165));
        CreateDetailRow(window, "Kẻ địch đánh bại", out detailEnemyText, enemyDefeated.ToString(), new Vector2(-207, -220));

        detailRoot.SetActive(false);
    }

    // =====================================================
    // ACTIONS
    // =====================================================

    public void OpenProfile()
    {
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
        if (jsonDataManager == null)
            return;


        // Tổng tài nguyên hiện có trên HUD
        resourceCollected =
            jsonDataManager.food +
            jsonDataManager.wood +
            jsonDataManager.stone;


        if (resourceCollectedText != null)
        {
            resourceCollectedText.text =
                resourceCollected.ToString("#,0");
        }
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

        if (workerText != null) workerText.text = workerCurrent + "/" + workerMax;
        if (uiLinh != null)
        {
            armyCount = uiLinh.GetSoldierCount();
        }


        armyText.text = armyCount.ToString();
        if (uiThapCanh != null)
        {
            watchTowerCount =
                uiThapCanh.GetWatchTowerCount();
        }

        watchTowerText.text =
            watchTowerCount.ToString();
        if (buildingText != null) buildingText.text = buildingCount.ToString();
        if (watchTowerText != null) watchTowerText.text = watchTowerCount.ToString();
        if (cannonText != null) cannonText.text = cannonCount.ToString();
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

        if (outline)
        {
            Outline o = go.AddComponent<Outline>();
            o.effectColor = borderColor;
            o.effectDistance = new Vector2(2f, -2f);
            o.useGraphicAlpha = false;
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
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        Button btn = go.GetComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = buttonHighlightColor;
        cb.pressedColor = new Color32(145, 70, 16, 255);
        cb.selectedColor = buttonHighlightColor;
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        CreateText(go.transform, "Text", label, Vector2.zero, size, 28, titleColor, TextAlignmentOptions.Center, true);

        return btn;
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
        img.color = darkCardColor;

        TMP_InputField input = go.GetComponent<TMP_InputField>();
        input.characterLimit = maxNameLength;

        GameObject viewportGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(go.transform, false);

        RectTransform viewport = viewportGO.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12, 5);
        viewport.offsetMax = new Vector2(-12, -5);

        TMP_Text inputText = CreateText(viewportGO.transform, "Text", "", Vector2.zero, size, 24, titleColor, TextAlignmentOptions.MidlineLeft, false);
        TMP_Text placeholder = CreateText(viewportGO.transform, "Placeholder", "Nhập tên mới...", Vector2.zero, size, 22, bodyColor, TextAlignmentOptions.MidlineLeft, false);

        input.textViewport = viewport;
        input.textComponent = inputText;
        input.placeholder = placeholder;

        return input;
    }

    void CreateAchievementRow(Transform parent, string title, string description, string status, Vector2 pos)
    {
        RectTransform row = CreatePanel(parent, "Achievement_" + title, new Vector2(670, 54), pos, darkCardColor, true);

        CreateText(row, title + "Title", title, new Vector2(-230, 10), new Vector2(210, 28), 22, titleColor, TextAlignmentOptions.Left, true);
        CreateText(row, title + "Desc", description, new Vector2(20, 10), new Vector2(330, 28), 18, bodyColor, TextAlignmentOptions.Left, false);
        CreateText(row, title + "Status", status, new Vector2(275, -8), new Vector2(140, 24), 18, valueColor, TextAlignmentOptions.Center, true);
    }

    void CreateDetailRow(Transform parent, string label, out TMP_Text valueText, string value, Vector2 pos)
    {
        RectTransform row = CreatePanel(parent, "Detail_" + label, new Vector2(360, 42), pos, darkCardColor, true);

        CreateText(row, label + "Label", label, new Vector2(-95, 0), new Vector2(150, 30), 20, bodyColor, TextAlignmentOptions.Left, false);
        valueText = CreateText(row, label + "Value", value, new Vector2(105, 0), new Vector2(175, 30), 22, valueColor, TextAlignmentOptions.Left, true);
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
        CreateText(parent, label + "Title", label, pos, new Vector2(250, 30), 22, bodyColor, TextAlignmentOptions.Left, false);
        return CreateText(parent, label + "Text", value, pos + new Vector2(0, -35), new Vector2(250, 40), 26, valueColor, TextAlignmentOptions.Left, true);
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
            bodyColor,
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
            valueColor,
            TextAlignmentOptions.Left,
            true
        );
    }

    void CreateTopStat(Transform parent, string label, out TMP_Text valueText, Vector2 pos, string value)
    {
        CreateText(parent, label + "Label", label, pos, new Vector2(120, 25), 19, bodyColor, TextAlignmentOptions.Center, false);
        valueText = CreateText(parent, label + "Value", value, pos + new Vector2(0, -35), new Vector2(120, 40), 28, valueColor, TextAlignmentOptions.Center, true);
    }

    string FormatNumber(int value)
    {
        return value.ToString("#,0").Replace(",", ".");
    }
}