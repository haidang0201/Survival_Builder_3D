using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class RoKRenamePanel : MonoBehaviour
{
    [Header("ROOT")]
    public GameObject profilePanelRoot;
    public GameObject renamePanelRoot;

    [Header("BUTTONS")]
    public Button profileButton;
    public Button closeProfileButton;
    public Button renameButton;
    public Button achievementButton;
    public Button detailButton;

    [Header("RENAME BUTTONS")]
    public Button confirmRenameButton;
    public Button cancelRenameButton;

    [Header("AVATAR")]
    public Image avatarImage;
    public Image avatarFrameImage;
    public TMP_Text levelBadgeText;
    public TMP_Text onlineStatusText;

    [Header("MAIN INFO")]
    public TMP_Text headerTitleText;
    public TMP_Text governorNameText;
    public TMP_Text governorIdText;
    public TMP_Text governorLevelText;
    public TMP_Text powerText;
    public TMP_Text governorTitleText;
    public TMP_Text loginDayText;
    public TMP_Text allianceText;
    public TMP_Text civilizationText;

    [Header("STATS")]
    public TMP_Text workerText;
    public TMP_Text armyText;
    public TMP_Text buildingText;
    public TMP_Text watchTowerText;
    public TMP_Text cannonText;
    public TMP_Text resourceCollectedText;
    public TMP_Text enemyDefeatedText;

    [Header("RENAME PANEL")]
    public TMP_Text renameTitleText;
    public TMP_InputField nameInputField;
    public TMP_Text warningText;

    [Header("QUEST LINK")]
    public RoKQuestPanelUI questPanel;
    public string renameQuestId = "my_name";

    [Header("PLAYER DATA")]
    public string defaultName = "Thống đốc";
    public int governorLevel = 1;
    public int power = 0;
    public string governorTitle = "Lãnh chúa mới";
    public int loginDays = 1;
    public string allianceName = "Chưa gia nhập";
    public string civilizationName = "Khởi nguyên";

    [Header("GAME STATS")]
    public int workerCurrent = 0;
    public int workerMax = 4;
    public int armyCount = 0;
    public int buildingCount = 0;
    public int watchTowerCount = 0;
    public int cannonCount = 0;
    public int resourceCollected = 0;
    public int enemyDefeated = 0;

    [Header("RENAME SETTINGS")]
    public int minNameLength = 2;
    public int maxNameLength = 16;
    public string playerNamePrefsKey = "PLAYER_NAME";
    public string playerIdPrefsKey = "PLAYER_ID";

    [Header("CANVAS LAYER")]
    public bool forceTopCanvas = true;
    public int sortingOrder = 7000;

    [Header("COLORS")]
    public bool applyWoodThemeOnStart = true;
    public Color panelColor = new Color32(58, 36, 21, 255);          // #3A2415
    public Color windowColor = new Color32(107, 63, 31, 255);        // #6B3F1F
    public Color cardColor = new Color32(184, 117, 50, 255);         // #B87532
    public Color darkCardColor = new Color32(43, 26, 16, 235);       // #2B1A10
    public Color titleColor = new Color32(255, 241, 194, 255);       // #FFF1C2
    public Color bodyColor = new Color32(232, 212, 162, 255);        // #E8D4A2
    public Color rewardColor = new Color32(255, 224, 138, 255);      // #FFE08A
    public Color buttonColor = new Color32(199, 106, 27, 255);       // #C76A1B
    public Color buttonHighlightColor = new Color32(240, 167, 58, 255);

    [Header("EVENT")]
    public UnityEvent onRenameConfirmed;
    public UnityEvent<string> onNameConfirmed;

    string currentName;
    int governorId;

    void Awake()
    {
        BindButtons();
        LoadProfileData();
        ApplyCanvasLayer();

        if (applyWoodThemeOnStart)
            ApplyWoodTheme();

        RefreshProfileUI();

        if (profilePanelRoot != null)
            profilePanelRoot.SetActive(false);

        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(false);
    }

    void BindButtons()
    {
        if (profileButton != null)
        {
            profileButton.onClick.RemoveListener(OpenProfile);
            profileButton.onClick.AddListener(OpenProfile);
        }

        if (closeProfileButton != null)
        {
            closeProfileButton.onClick.RemoveListener(CloseProfile);
            closeProfileButton.onClick.AddListener(CloseProfile);
        }

        if (renameButton != null)
        {
            renameButton.onClick.RemoveListener(OpenRenamePanel);
            renameButton.onClick.AddListener(OpenRenamePanel);
        }

        if (confirmRenameButton != null)
        {
            confirmRenameButton.onClick.RemoveListener(ConfirmRename);
            confirmRenameButton.onClick.AddListener(ConfirmRename);
        }

        if (cancelRenameButton != null)
        {
            cancelRenameButton.onClick.RemoveListener(CloseRenamePanel);
            cancelRenameButton.onClick.AddListener(CloseRenamePanel);
        }

        if (achievementButton != null)
        {
            achievementButton.onClick.RemoveAllListeners();
            achievementButton.onClick.AddListener(() =>
            {
                Debug.Log("[Profile] Mở bảng thành tích.");
            });
        }

        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() =>
            {
                Debug.Log("[Profile] Mở hồ sơ chi tiết.");
            });
        }
    }

    void LoadProfileData()
    {
        currentName = PlayerPrefs.GetString(playerNamePrefsKey, defaultName);

        if (PlayerPrefs.HasKey(playerIdPrefsKey))
        {
            governorId = PlayerPrefs.GetInt(playerIdPrefsKey);
        }
        else
        {
            governorId = Random.Range(100000, 999999);
            PlayerPrefs.SetInt(playerIdPrefsKey, governorId);
            PlayerPrefs.Save();
        }
    }

    public void OpenProfile()
    {
        ApplyCanvasLayer();

        if (profilePanelRoot != null)
            profilePanelRoot.SetActive(true);

        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(false);

        RefreshProfileUI();
    }

    public void CloseProfile()
    {
        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(false);

        if (profilePanelRoot != null)
            profilePanelRoot.SetActive(false);
    }

    public void OpenRenamePanel()
    {
        ApplyCanvasLayer();

        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(true);

        if (warningText != null)
            warningText.text = "";

        if (renameTitleText != null)
            renameTitleText.text = "Đổi tên";

        if (nameInputField != null)
        {
            nameInputField.text = currentName;
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }
    }

    public void CloseRenamePanel()
    {
        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(false);
    }

    public void ConfirmRename()
    {
        if (nameInputField == null)
            return;

        string newName = nameInputField.text.Trim();

        if (newName.Length < minNameLength)
        {
            ShowWarning("Tên quá ngắn.");
            return;
        }

        if (newName.Length > maxNameLength)
        {
            ShowWarning("Tên quá dài.");
            return;
        }

        currentName = newName;

        PlayerPrefs.SetString(playerNamePrefsKey, currentName);
        PlayerPrefs.Save();

        RefreshProfileUI();
        CloseRenamePanel();

        // Complete nhiệm vụ "Bằng tên tôi"
        if (questPanel != null)
            questPanel.CompleteQuest(renameQuestId);

        onNameConfirmed?.Invoke(currentName);
        onRenameConfirmed?.Invoke();

        Debug.Log("[RoKGovernorProfileUI] Đổi tên thành: " + currentName);
    }

    void ShowWarning(string msg)
    {
        if (warningText != null)
            warningText.text = msg;
    }

    public void RefreshProfileUI()
    {
        if (headerTitleText != null)
            headerTitleText.text = "Hồ sơ thống đốc";

        if (governorNameText != null)
            governorNameText.text = currentName;

        if (governorIdText != null)
            governorIdText.text = governorId.ToString();

        if (governorLevelText != null)
            governorLevelText.text = governorLevel.ToString();

        if (levelBadgeText != null)
            levelBadgeText.text = governorLevel.ToString();

        if (powerText != null)
            powerText.text = FormatNumber(power);

        if (governorTitleText != null)
            governorTitleText.text = governorTitle;

        if (loginDayText != null)
            loginDayText.text = loginDays.ToString();

        if (allianceText != null)
            allianceText.text = allianceName;

        if (civilizationText != null)
            civilizationText.text = civilizationName;

        if (onlineStatusText != null)
            onlineStatusText.text = "● Đang online";

        if (workerText != null)
            workerText.text = workerCurrent + "/" + workerMax;

        if (armyText != null)
            armyText.text = armyCount.ToString();

        if (buildingText != null)
            buildingText.text = buildingCount.ToString();

        if (watchTowerText != null)
            watchTowerText.text = watchTowerCount.ToString();

        if (cannonText != null)
            cannonText.text = cannonCount.ToString();

        if (resourceCollectedText != null)
            resourceCollectedText.text = FormatNumber(resourceCollected);

        if (enemyDefeatedText != null)
            enemyDefeatedText.text = enemyDefeated.ToString();
    }

    public void SetGovernorName(string newName)
    {
        currentName = newName;
        PlayerPrefs.SetString(playerNamePrefsKey, currentName);
        PlayerPrefs.Save();
        RefreshProfileUI();
    }

    public void SetGovernorLevel(int value)
    {
        governorLevel = Mathf.Max(1, value);
        RefreshProfileUI();
    }

    public void SetPower(int value)
    {
        power = Mathf.Max(0, value);
        RefreshProfileUI();
    }

    public void SetWorker(int current, int max)
    {
        workerCurrent = Mathf.Max(0, current);
        workerMax = Mathf.Max(1, max);
        RefreshProfileUI();
    }

    public void SetArmyCount(int value)
    {
        armyCount = Mathf.Max(0, value);
        RefreshProfileUI();
    }

    public void SetBuildingCount(int value)
    {
        buildingCount = Mathf.Max(0, value);
        RefreshProfileUI();
    }

    public void SetWatchTowerCount(int value)
    {
        watchTowerCount = Mathf.Max(0, value);
        RefreshProfileUI();
    }

    public void SetCannonCount(int value)
    {
        cannonCount = Mathf.Max(0, value);
        RefreshProfileUI();
    }

    public void SetResourceCollected(int value)
    {
        resourceCollected = Mathf.Max(0, value);
        RefreshProfileUI();
    }

    public void AddResourceCollected(int amount)
    {
        resourceCollected += Mathf.Max(0, amount);
        RefreshProfileUI();
    }

    public void SetEnemyDefeated(int value)
    {
        enemyDefeated = Mathf.Max(0, value);
        RefreshProfileUI();
    }

    public void AddEnemyDefeated(int amount)
    {
        enemyDefeated += Mathf.Max(0, amount);
        RefreshProfileUI();
    }

    string FormatNumber(int value)
    {
        return value.ToString("#,0").Replace(",", ".");
    }

    void ApplyCanvasLayer()
    {
        if (!forceTopCanvas)
            return;

        if (profilePanelRoot == null)
            return;

        profilePanelRoot.transform.SetAsLastSibling();

        Canvas c = profilePanelRoot.GetComponent<Canvas>();
        if (c == null)
            c = profilePanelRoot.AddComponent<Canvas>();

        c.overrideSorting = true;
        c.sortingOrder = sortingOrder;

        if (profilePanelRoot.GetComponent<GraphicRaycaster>() == null)
            profilePanelRoot.AddComponent<GraphicRaycaster>();
    }

    [ContextMenu("Apply Wood Profile Theme")]
    public void ApplyWoodTheme()
    {
        ApplyRootImage(profilePanelRoot, panelColor);
        ApplyRootImage(renamePanelRoot, new Color32(0, 0, 0, 120));

        ApplyText(headerTitleText, titleColor, 42, true);
        ApplyText(governorNameText, titleColor, 28, true);
        ApplyText(governorIdText, rewardColor, 24, true);
        ApplyText(governorLevelText, rewardColor, 24, true);
        ApplyText(powerText, rewardColor, 24, true);
        ApplyText(governorTitleText, bodyColor, 22, false);
        ApplyText(loginDayText, bodyColor, 22, false);
        ApplyText(allianceText, rewardColor, 24, true);
        ApplyText(civilizationText, rewardColor, 24, true);
        ApplyText(levelBadgeText, titleColor, 28, true);
        ApplyText(onlineStatusText, new Color32(90, 255, 80, 255), 22, true);

        ApplyText(workerText, rewardColor, 24, true);
        ApplyText(armyText, rewardColor, 24, true);
        ApplyText(buildingText, rewardColor, 24, true);
        ApplyText(watchTowerText, rewardColor, 24, true);
        ApplyText(cannonText, rewardColor, 24, true);
        ApplyText(resourceCollectedText, rewardColor, 28, true);
        ApplyText(enemyDefeatedText, rewardColor, 28, true);

        ApplyText(renameTitleText, titleColor, 34, true);
        ApplyText(warningText, rewardColor, 20, false);

        ApplyButton(renameButton);
        ApplyButton(achievementButton);
        ApplyButton(detailButton);
        ApplyButton(confirmRenameButton);
        ApplyButton(cancelRenameButton);
        ApplyButton(closeProfileButton);
    }

    void ApplyRootImage(GameObject root, Color color)
    {
        if (root == null)
            return;

        Image img = root.GetComponent<Image>();

        if (img != null)
            img.color = color;
    }

    void ApplyText(TMP_Text text, Color color, int size, bool bold)
    {
        if (text == null)
            return;

        text.color = color;
        text.fontSize = size;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;

        text.outlineColor = new Color32(43, 26, 16, 255);
        text.outlineWidth = 0.15f;
    }

    void ApplyButton(Button btn)
    {
        if (btn == null)
            return;

        Image img = btn.GetComponent<Image>();

        if (img != null)
            img.color = buttonColor;

        ColorBlock cb = btn.colors;
        cb.normalColor = buttonColor;
        cb.highlightedColor = buttonHighlightColor;
        cb.pressedColor = new Color32(145, 70, 16, 255);
        cb.selectedColor = buttonHighlightColor;
        cb.disabledColor = new Color32(80, 60, 45, 160);
        cb.colorMultiplier = 1f;
        btn.colors = cb;
    }
}