using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    Stone
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

    [Header("✨ ANIMATION SETTINGS (BẬT/TẮT TỤY CHỈNH)")]
    [Tooltip("Công tắc tổng cho tất cả hiệu ứng")]
    [SerializeField] private bool enableAnimations = true;
    [Tooltip("Khung Panel chính của bảng Quest (để trống sẽ tự lấy Transform của script)")]
    [SerializeField] private Transform panelContainer;
    [Tooltip("Bật/tắt hiệu ứng Mở/Đóng cửa sổ (Pop-up)")]
    [SerializeField] private bool useWindowPopupAnim = true;
    [SerializeField] private float windowAnimDuration = 0.25f;
    [SerializeField] private Ease windowOpenEase = Ease.OutBack;
    [SerializeField] private Ease windowCloseEase = Ease.InBack;

    [Tooltip("Bật/tắt hiệu ứng Thẻ Quest xuất hiện lần lượt (Staggered Entry)")]
    [SerializeField] private bool useStaggeredCardAnim = true;
    [SerializeField] private float cardStaggerDelay = 0.05f;
    [SerializeField] private float cardAnimDuration = 0.25f;

    [Tooltip("Bật/tắt hiệu ứng Chấm Đỏ Nhịp Thở (Notification Pulse)")]
    [SerializeField] private bool useNotificationPulse = true;
    [SerializeField] private float pulseScaleMultiplier = 1.18f;
    [SerializeField] private float pulseDuration = 0.6f;

    [Tooltip("Bật/tắt hiệu ứng Phóng To Tab Đang Chọn")]
    [SerializeField] private bool useTabHighlightAnim = true;

    [Header("🛠️ DEBUG / TEST SETTINGS")]
    [SerializeField] private bool enableDebugHotkeys = true;
    [SerializeField] private KeyCode addProgressHotkey = KeyCode.T;
    [SerializeField] private KeyCode completeAllHotkey = KeyCode.Y;
    [SerializeField] private KeyCode resetAllHotkey = KeyCode.R;

    private QuestType currentTab = QuestType.MainQuest;

    private void Awake()
    {
        if (panelContainer == null) panelContainer = transform;

        if (questList == null || questList.Count == 0)
        {
            InitDefaultQuests();
        }
    }

    private void Reset()
    {
        InitDefaultQuests();
    }

    [ContextMenu("🛠️ Add Default Quests")]
    public void InitDefaultQuests()
    {
        questList = new List<QuestDataDemo>
        {
            new QuestDataDemo
            {
                questID = "main_01",
                questType = QuestType.MainQuest,
                title = "Khởi Đầu Hành Trình",
                description = "Trò chuyện với Trưởng Làng để tiếp nhận nhiệm vụ hướng dẫn khai hoang ban đầu.",
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
                questID = "main_02",
                questType = QuestType.MainQuest,
                title = "Khai Thác Gỗ Xây Dựng",
                description = "Chặt các rặng cây xung quanh làng để thu thập đủ 100 Gỗ làm vật liệu.",
                currentProgress = 60,
                maxProgress = 100,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 300 },
                    new QuestReward { rewardType = RewardType.Wood, amount = 100 },
                    new QuestReward { rewardType = RewardType.Exp, amount = 250 }
                }
            },
            new QuestDataDemo
            {
                questID = "main_03",
                questType = QuestType.MainQuest,
                title = "Xây Dựng Nơi Cư Trú",
                description = "Dựng 1 Căn Nhà Gỗ đầu tiên để tạo chỗ ở và thu hút dân làng mới.",
                currentProgress = 0,
                maxProgress = 1,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 800 },
                    new QuestReward { rewardType = RewardType.Wood, amount = 100 },
                    new QuestReward { rewardType = RewardType.Exp, amount = 400 }
                }
            },
            new QuestDataDemo
            {
                questID = "main_04",
                questType = QuestType.MainQuest,
                title = "Khai Thác Đá Mỏ",
                description = "Khai phá mỏ thạch anh xung quanh căn cứ để tích lũy đủ 80 Đá xây dựng.",
                currentProgress = 35,
                maxProgress = 80,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 500 },
                    new QuestReward { rewardType = RewardType.Stone, amount = 80 },
                    new QuestReward { rewardType = RewardType.Exp, amount = 300 }
                }
            },
            new QuestDataDemo
            {
                questID = "main_05",
                questType = QuestType.MainQuest,
                title = "Tích Lương Trồng Trọt",
                description = "Thu hoạch 120 Lúa lương thực đảm bảo nguồn thức ăn dự trữ cho làng.",
                currentProgress = 120,
                maxProgress = 120,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 1000 },
                    new QuestReward { rewardType = RewardType.Wood, amount = 80 },
                    new QuestReward { rewardType = RewardType.Exp, amount = 350 }
                }
            },
            new QuestDataDemo
            {
                questID = "main_06",
                questType = QuestType.MainQuest,
                title = "Mộ Quân Dân Làng",
                description = "Tuyển dụng 3 Người Dân Làng để gia tăng năng suất khai thác tài nguyên.",
                currentProgress = 1,
                maxProgress = 3,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 1200 },
                    new QuestReward { rewardType = RewardType.Stone, amount = 60 },
                    new QuestReward { rewardType = RewardType.Exp, amount = 500 }
                }
            },
            new QuestDataDemo
            {
                questID = "main_07",
                questType = QuestType.MainQuest,
                title = "Mở Rộng Lãnh Thổ",
                description = "Chinh phục và khai phá thêm 1 Ô Đất Mới để mở rộng quy mô căn cứ.",
                currentProgress = 0,
                maxProgress = 1,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 1500 },
                    new QuestReward { rewardType = RewardType.Wood, amount = 150 },
                    new QuestReward { rewardType = RewardType.Stone, amount = 100 }
                }
            },
            new QuestDataDemo
            {
                questID = "main_08",
                questType = QuestType.MainQuest,
                title = "Gia Cố Hàng Rào Căn Cứ",
                description = "Xây dựng hệ thống rào chắn chắc chắn để chống lại các đợt tấn công ban đêm.",
                currentProgress = 0,
                maxProgress = 1,
                isClaimed = false,
                rewards = new List<QuestReward>
                {
                    new QuestReward { rewardType = RewardType.Gold, amount = 2000 },
                    new QuestReward { rewardType = RewardType.Wood, amount = 200 },
                    new QuestReward { rewardType = RewardType.Exp, amount = 800 }
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
        AnimateTabButtons();
        RefreshQuestList(animateStagger: true);
    }

    private void AnimateTabButtons()
    {
        if (!enableAnimations || !useTabHighlightAnim) return;

        AnimateSingleTab(tabQuestBtn, currentTab == QuestType.MainQuest);
        AnimateSingleTab(tabCombatBtn, currentTab == QuestType.Combat);
        AnimateSingleTab(tabAchievementBtn, currentTab == QuestType.Achievement);
        AnimateSingleTab(tabSettingsBtn, currentTab == QuestType.Settings);
    }

    private void AnimateSingleTab(Button btn, bool isActive)
    {
        if (btn == null) return;
        DOTween.Kill(btn.transform);
        Vector3 targetScale = isActive ? Vector3.one * 1.1f : Vector3.one;
        btn.transform.DOScale(targetScale, 0.15f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void RefreshQuestList(bool animateStagger = false)
    {
        if (contentArea == null || questItemPrefab == null) return;

        // Xóa các thẻ Quest cũ trên Content Area
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        int cardIndex = 0;

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
                    () => OnClaimReward(quest, itemUI)
                );
            }

            // Hiệu ứng thẻ xuất hiện lần lượt (Staggered Entry - chỉ chạy khi chuyển Tab hoặc Mở Bảng)
            if (animateStagger && enableAnimations && useStaggeredCardAnim)
            {
                cardObj.transform.localScale = Vector3.zero;
                float delay = cardIndex * cardStaggerDelay;
                cardObj.transform.DOScale(Vector3.one, cardAnimDuration)
                    .SetDelay(delay)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
            else
            {
                cardObj.transform.localScale = Vector3.one;
            }

            cardIndex++;
        }

        CheckNotification();
    }

    private void OnClaimReward(QuestDataDemo quest, QuestItemUI itemUI)
    {
        if (quest == null || quest.isClaimed) return;

        Debug.Log($"[QuestSystem] Đã nhận thưởng nhiệm vụ: {quest.title}");

        if (itemUI != null)
        {
            itemUI.PlayClaimFX(() => ProcessClaimReward(quest));
        }
        else
        {
            ProcessClaimReward(quest);
        }
    }

    private void ProcessClaimReward(QuestDataDemo quest)
    {
        if (quest.rewards != null)
        {
            foreach (var reward in quest.rewards)
            {
                GiveReward(reward);
            }
        }

        quest.isClaimed = true;
        RefreshQuestList(animateStagger: false);
    }

    private void GiveReward(QuestReward reward)
    {
        if (reward == null || reward.amount <= 0) return;

        if (JsonDataManager.Ins != null)
        {
            switch (reward.rewardType)
            {
                case RewardType.Gold:
                    JsonDataManager.Ins.AddGold(reward.amount);
                    break;
                case RewardType.Wood:
                    JsonDataManager.Ins.AddWood(reward.amount);
                    break;
                case RewardType.Stone:
                    JsonDataManager.Ins.AddStone(reward.amount);
                    break;
                case RewardType.Exp:
                    Debug.Log($"[QuestSystem] Nhận {reward.amount} Exp");
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[QuestSystem] JsonDataManager.Ins chưa khởi tạo! Không thể cộng quà {reward.rewardType} (+{reward.amount})");
        }
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

        if (hasClaimableQuest && enableAnimations && useNotificationPulse)
        {
            DOTween.Kill(notificationIcon.transform);
            notificationIcon.transform.localScale = Vector3.one;
            notificationIcon.transform.DOScale(Vector3.one * pulseScaleMultiplier, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }
        else if (notificationIcon != null)
        {
            DOTween.Kill(notificationIcon.transform);
            notificationIcon.transform.localScale = Vector3.one;
        }
    }

    public void OpenWindow()
    {
        gameObject.SetActive(true);
        RefreshQuestList(animateStagger: true);
        AnimateTabButtons();

        if (enableAnimations && useWindowPopupAnim && panelContainer != null)
        {
            DOTween.Kill(panelContainer);
            panelContainer.localScale = Vector3.zero;
            panelContainer.DOScale(Vector3.one, windowAnimDuration)
                .SetEase(windowOpenEase)
                .SetUpdate(true);
        }
        else if (panelContainer != null)
        {
            panelContainer.localScale = Vector3.one;
        }
    }

    public void CloseWindow()
    {
        if (enableAnimations && useWindowPopupAnim && panelContainer != null)
        {
            DOTween.Kill(panelContainer);
            panelContainer.DOScale(Vector3.zero, windowAnimDuration * 0.8f)
                .SetEase(windowCloseEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    CheckNotification();
                });
        }
        else
        {
            gameObject.SetActive(false);
            CheckNotification();
        }
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