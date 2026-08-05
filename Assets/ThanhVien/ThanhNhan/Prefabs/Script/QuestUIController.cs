using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum QuestType
{
    MainQuest,      // Tab_Quest
    Combat,         // Tab_Combat
    Achievement,    // Tab_Achievement
    Settings        // Tab_Settings
}

public enum RewardType
{
    Gold,
    Exp,
    Wood,
    Stone,
    Gem
}

[System.Serializable]
public class QuestReward
{
    public RewardType rewardType;
    public Sprite customIcon; // Để None nếu muốn dùng Icon mặc định
    public int amount;
}

[System.Serializable]
public class QuestDataDemo
{
    public string questID;
    public QuestType questType;
    public Sprite icon;
    public string title;
    public string description;
    public int currentProgress;
    public int maxProgress;
    public bool isClaimed;

    // Danh sách phần thưởng linh hoạt (thích để 1, 2, 3 hay 4 item tùy ý)
    public List<QuestReward> rewards = new List<QuestReward>();
}

public class QuestUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentArea;
    [SerializeField] private GameObject questItemPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundOverlayButton;
    [SerializeField] private Button openQuestButton;

    [Header("Notification")]
    [SerializeField] private GameObject notificationIcon;

    [Header("HotKey Config")]
    [SerializeField] private KeyCode toggleHotkey = KeyCode.Q;

    [Header("Tab Buttons")]
    [SerializeField] private Button tabQuestBtn;
    [SerializeField] private Button tabCombatBtn;
    [SerializeField] private Button tabAchievementBtn;
    [SerializeField] private Button tabSettingsBtn;

    [Header("Data List")]
    [SerializeField] private List<QuestDataDemo> questList = new List<QuestDataDemo>();

    [Header("🛠️ DEBUG / TEST SETTINGS")]
    [SerializeField] private bool enableDebugHotkeys = true;
    [SerializeField] private KeyCode addProgressHotkey = KeyCode.T;
    [SerializeField] private KeyCode completeAllHotkey = KeyCode.Y;
    [SerializeField] private KeyCode resetAllHotkey = KeyCode.R;

    private QuestType currentTab = QuestType.MainQuest;

    private void Awake()
    {
        if (questList == null || questList.Count == 0)
        {
            InitDefaultQuests();
        }
    }

    private void Reset()
    {
        InitDefaultQuests();
    }

    [ContextMenu("🛠️ Add Default 4 Quests")]
    public void InitDefaultQuests()
    {
        questList = new List<QuestDataDemo>
        {
            new QuestDataDemo
            {
                questID = "main_01",
                questType = QuestType.MainQuest,
                title = "Khởi Đầu Hành Trình",
                description = "Trò chuyện với Trưởng Làng để tiếp nhận nhiệm vụ hướng dẫn ban đầu.",
                currentProgress = 1,
                maxProgress = 1,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 500 },
                    new QuestReward { rewardType = RewardType.Exp, amount = 200 },
                    new QuestReward { rewardType = RewardType.Wood, amount = 50 }
                }
            },
            new QuestDataDemo
            {
                questID = "combat_01",
                questType = QuestType.Combat,
                title = "Dọn Dẹp Quái Vật",
                description = "Hạ gục 10 Quái Thạch Quỷ đang hoành hành xung quanh căn cứ.",
                currentProgress = 4,
                maxProgress = 10,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Exp, amount = 500 },
                    new QuestReward { rewardType = RewardType.Gold, amount = 1000 },
                    new QuestReward { rewardType = RewardType.Stone, amount = 30 }
                }
            },
            new QuestDataDemo
            {
                questID = "achieve_01",
                questType = QuestType.Achievement,
                title = "Nhà Thu Hoạch Tài Ba",
                description = "Khai thác và tích lũy đủ 100 Gỗ và 50 Đá từ tài nguyên ngoài bản đồ.",
                currentProgress = 65,
                maxProgress = 100,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Wood, amount = 100 },
                    new QuestReward { rewardType = RewardType.Stone, amount = 50 },
                    new QuestReward { rewardType = RewardType.Gold, amount = 1500 }
                }
            },
            new QuestDataDemo
            {
                questID = "settings_01",
                questType = QuestType.Settings,
                title = "Thiết Lập & Bảo Mật",
                description = "Tùy chỉnh cấu hình âm thanh, đồ họa và liên kết tài khoản để nhận quà.",
                currentProgress = 1,
                maxProgress = 1,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 2000 },
                    new QuestReward { rewardType = RewardType.Exp, amount = 300 }
                }
            }
        };
    }

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseWindow);
        if (backgroundOverlayButton != null) backgroundOverlayButton.onClick.AddListener(CloseWindow);
        if (openQuestButton != null) openQuestButton.onClick.AddListener(ToggleWindow);

        if (tabQuestBtn != null) tabQuestBtn.onClick.AddListener(() => SwitchTab(QuestType.MainQuest));
        if (tabCombatBtn != null) tabCombatBtn.onClick.AddListener(() => SwitchTab(QuestType.Combat));
        if (tabAchievementBtn != null) tabAchievementBtn.onClick.AddListener(() => SwitchTab(QuestType.Achievement));
        if (tabSettingsBtn != null) tabSettingsBtn.onClick.AddListener(() => SwitchTab(QuestType.Settings));

        SwitchTab(QuestType.MainQuest);
        CheckNotification();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleHotkey)) ToggleWindow();

        if (enableDebugHotkeys)
        {
            if (Input.GetKeyDown(addProgressHotkey)) Test_AddProgress();
            if (Input.GetKeyDown(completeAllHotkey)) Test_CompleteAll();
            if (Input.GetKeyDown(resetAllHotkey)) Test_ResetAll();
        }
    }

    public void ToggleWindow()
    {
        if (gameObject.activeSelf) CloseWindow();
        else OpenWindow();
    }

    public void SwitchTab(QuestType newTab)
    {
        currentTab = newTab;
        RefreshQuestList();
    }

    public void RefreshQuestList()
    {
        if (contentArea == null || questItemPrefab == null) return;

        // Xóa các thẻ Quest cũ trên Content Area
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        // Lọc và Instantiate các Quest thuộc Tab hiện tại
        foreach (var quest in questList)
        {
            if (quest.isClaimed || quest.questType != currentTab) continue;

            GameObject cardObj = Instantiate(questItemPrefab, contentArea);
            QuestItemUI itemUI = cardObj.GetComponent<QuestItemUI>();

            if (itemUI != null)
            {
                itemUI.SetupQuest(
                    quest.icon,
                    quest.title,
                    quest.description,
                    quest.currentProgress,
                    quest.maxProgress,
                    quest.rewards,
                    quest.isClaimed,
                    () => OnClaimReward(quest)
                );
            }
        }

        CheckNotification();
    }

    private void OnClaimReward(QuestDataDemo quest)
    {
        Debug.Log($"[QuestSystem] Đã nhận thưởng nhiệm vụ: {quest.title}");
        quest.isClaimed = true;
        RefreshQuestList();
    }

    public void CheckNotification()
    {
        if (notificationIcon == null) return;

        bool hasClaimableQuest = false;
        foreach (var quest in questList)
        {
            if (!quest.isClaimed && quest.currentProgress >= quest.maxProgress)
            {
                hasClaimableQuest = true;
                break;
            }
        }

        notificationIcon.SetActive(hasClaimableQuest);
    }

    public void OpenWindow()
    {
        gameObject.SetActive(true);
        RefreshQuestList();
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);
        CheckNotification();
    }

    // ========================================================================
    // 🛠️ DEBUG METHODS (Phím T / Y / R)
    // ========================================================================

    [ContextMenu("TEST: +1 Progress First Quest")]
    public void Test_AddProgress()
    {
        if (questList.Count > 0)
        {
            var q = questList[0];
            if (q.currentProgress < q.maxProgress) q.currentProgress++;
            RefreshQuestList();
        }
    }

    [ContextMenu("TEST: Complete All Quests")]
    public void Test_CompleteAll()
    {
        foreach (var q in questList) q.currentProgress = q.maxProgress;
        RefreshQuestList();
    }

    [ContextMenu("TEST: Reset All Quests")]
    public void Test_ResetAll()
    {
        foreach (var q in questList)
        {
            q.currentProgress = 0;
            q.isClaimed = false;
        }
        RefreshQuestList();
    }
}