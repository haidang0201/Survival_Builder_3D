using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class RoKQuestMissionGuideRouter : MonoBehaviour
{
    public static RoKQuestMissionGuideRouter Instance { get; private set; }

    // ID phải trùng với quest id trong RoKQuestPanelUI
    const string QUEST_TRAIN_ARCHER = "train_archer";
    const string QUEST_WATCH_TOWER = "build_watchtower";
    const string QUEST_UNLOCK_CANNON = "unlock_cannon";
    const string QUEST_FIRST_RAID = "first_raid";
    const string QUEST_LANDLORD = "landlord";
    const string QUEST_CIVILIZATION = "civilization_land";
    const string QUEST_MY_NAME = "my_name";
    const string QUEST_GATHER_WOOD = "gather_wood";
    const string QUEST_UPGRADE_STORAGE = "upgrade_storage";


    [Header("CORE")]
    public RoKQuestPanelUI questPanel;
    public RoKNpcMissionDialogUI npc;
    public UIHighlightSystem highlight;

    [Header("CAMERA")]
    public Camera mainCamera;
    public Vector3 cameraOffset = new Vector3(0, 10, -10);
    public float cameraMoveTime = 1.5f;
    public bool rotateCameraToTarget = false;

    [Header("WORLD ARROW")]
    public GameObject arrowPrefab;
    public float arrowHeight = 2.5f;

    [Header("1. HUẤN LUYỆN CUNG THỦ")]
    public Transform barracksPoint;
    public RectTransform trainArcherButton;
    public int trainedArcherCount = 0;
    public int trainArcherTarget = 20;

    [Header("2. XÂY THÁP CANH")]
    public GameObject buildPanel;
    public RectTransform watchTowerBuildButton;
    public Transform watchTowerPoint;
    public bool watchTowerBuilt;

    [Header("3. MỞ KHÓA PHÁO THỦ")]
    public RectTransform cannonBuildButton;
    public Transform cannonPoint;
    public bool cannonUnlocked;

    [Header("4. ĐÁNH BẠI ĐỢT CƯỚP ĐẦU TIÊN")]
    public Transform raidEnemyPoint;
    public RectTransform attackButton;
    public bool firstRaidDefeated;

    [Header("5. ĐẠI ĐỊA CHỦ")]
    public RectTransform foodIcon;
    public Transform foodPoint;
    public int foodProduction = 451;
    public int foodTarget = 500;

    [Header("6. XỨ SỞ CỦA NỀN VĂN MINH")]
    public Transform barbarianPoint;
    public RectTransform barbarianAttackButton;
    public int barbarianDefeated = 1;
    public int barbarianTarget = 2;

    [Header("7. BẰNG TÊN TÔI")]
    public RectTransform profileButton;
    public RectTransform renameButton;
    public bool playerNameSet;

    [Header("8. NGƯỜI GOM GÓP")]
    public RectTransform woodIcon;
    public Transform woodPoint;
    public int gatheredWood = 120;
    public int gatherWoodTarget = 300;

    [Header("9. NGƯỜI GIỮ KHO")]
    public RectTransform storageUpgradeButton;
    public Transform storagePoint;
    public bool storageUpgraded;

    [Header("9. STORAGE FLOW")]
    [Tooltip("Bật nếu muốn tutorial chờ người chơi nhấn vào nhà kho trước khi highlight nút nâng cấp.")]
    public bool waitForStorageHouseClick = true;
    public int requiredStorageLevel = 2;

    [Header("9. STORAGE LEVEL DETECTION")]
    [Tooltip("Kéo GameObject nhà kho / nhà Wood vào đây. Router sẽ tự đọc level từ component trên object này.")]
    public GameObject storageLevelSource;

    [Tooltip("Nếu biết chính xác script giữ level, kéo component đó vào đây. Có thể để trống nếu đã gán Storage Level Source.")]
    public MonoBehaviour storageLevelComponent;

    [Tooltip("Tự quét các field/property phổ biến như currentLevel, level, buildingLevel...")]
    public bool autoDetectStorageLevel = true;

    [Tooltip("Khoảng thời gian kiểm tra level khi nhiệm vụ Người giữ kho đang chạy.")]
    public float storageLevelCheckInterval = 0.15f;

    [Tooltip("Nếu ON, bấm nút Upgrade sẽ hoàn thành quest ngay. Chỉ bật nếu nút này chắc chắn nâng Kho lên cấp 2.")]
    public bool completeStorageQuestWhenUpgradeButtonClicked = false;

    [Tooltip("Tự bắt sự kiện click của Storage Upgrade Button để khởi động kiểm tra level.")]
    public bool autoBindStorageUpgradeButton = true;

    public string[] storageLevelMemberNames =
    {
        "currentLevel", "CurrentLevel",
        "level", "Level",
        "buildingLevel", "BuildingLevel",
        "storageLevel", "StorageLevel",
        "warehouseLevel", "WarehouseLevel",
        "currentBuildingLevel", "CurrentBuildingLevel"
    };

    bool storageHouseClicked;
    bool storageQuestCompleted;
    bool storageUpgradeButtonClicked;
    Coroutine storageLevelWatchRoutine;

    [Header("OPTIONS")]
    public bool closeQuestPanelWhenGuideStart = true;
    public bool openQuestPanelWhenComplete = true;
    public bool debugLog = true;

    GameObject arrowInstance;
    bool guideRunning;
    [Header("EXTERNAL COMPLETE")]
    public bool openPanelWhenExternalComplete = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (questPanel != null)
        {
            questPanel.onGoPressed.RemoveListener(OnQuestGoPressed);
            questPanel.onGoPressed.AddListener(OnQuestGoPressed);
        }

        BindStorageUpgradeButton();
        SyncInitialProgress();
    }

    void OnDestroy()
    {
        if (questPanel != null)
            questPanel.onGoPressed.RemoveListener(OnQuestGoPressed);

        UnbindStorageUpgradeButton();

        if (Instance == this)
            Instance = null;
    }

    // =====================================================
    // EVENT TỪ NÚT "ĐI"
    // =====================================================

    public void OnQuestGoPressed(string questId)
    {
        if (debugLog)
            Debug.Log("[RoKQuestMissionGuideRouter] Bấm Đi questId = " + questId);

        if (guideRunning)
            return;

        StartCoroutine(RouteQuest(questId));
    }

    IEnumerator RouteQuest(string questId)
    {
        guideRunning = true;

        if (closeQuestPanelWhenGuideStart && questPanel != null)
            questPanel.ClosePanel();

        switch (questId)
        {
            case QUEST_TRAIN_ARCHER:
                yield return GuideTrainArcher();
                break;

            case QUEST_WATCH_TOWER:
                yield return GuideBuildWatchTower();
                break;

            case QUEST_UNLOCK_CANNON:
                yield return GuideUnlockCannon();
                break;

            case QUEST_FIRST_RAID:
                yield return GuideFirstRaid();
                break;

            case QUEST_LANDLORD:
                yield return GuideLandlord();
                break;

            case QUEST_CIVILIZATION:
                yield return GuideCivilization();
                break;

            case QUEST_MY_NAME:
                yield return GuideMyName();
                break;

            case QUEST_GATHER_WOOD:
                yield return GuideGatherWood();
                break;

            case QUEST_UPGRADE_STORAGE:
                yield return GuideUpgradeStorage();
                break;

            default:
                yield return Say("Nhiệm vụ này chưa có hướng dẫn.");
                Debug.LogWarning("[RoKQuestMissionGuideRouter] Chưa có guide cho questId: " + questId);
                break;
        }

        guideRunning = false;
    }

    // =====================================================
    // 1. HUẤN LUYỆN CUNG THỦ
    // =====================================================

    IEnumerator GuideTrainArcher()
    {
        yield return Say("Nhiệm vụ: Huấn luyện 20 cung thủ.");

        yield return FocusWorld(barracksPoint);

        if (trainArcherButton != null && highlight != null)
            highlight.Highlight(trainArcherButton);

        yield return Say("Hãy bấm nút huấn luyện cung thủ.");

        yield return new WaitUntil(() => trainedArcherCount >= trainArcherTarget);

        FinishQuest(QUEST_TRAIN_ARCHER, "Tốt. Cung thủ đã sẵn sàng.");
    }

    // =====================================================
    // 2. XÂY THÁP CANH
    // =====================================================

    IEnumerator GuideBuildWatchTower()
    {
        yield return Say("Nhiệm vụ: Xây Tháp Canh để phát hiện kẻ địch.");

        if (buildPanel != null)
            buildPanel.SetActive(true);

        yield return FocusWorld(watchTowerPoint);

        if (watchTowerBuildButton != null && highlight != null)
            highlight.Highlight(watchTowerBuildButton);

        yield return Say("Hãy chọn Tháp Canh và đặt vào vị trí được chỉ dẫn.");

        yield return new WaitUntil(() => watchTowerBuilt);

        FinishQuest(QUEST_WATCH_TOWER, "Tốt. Tháp Canh đã được xây dựng.");
    }

    // =====================================================
    // 3. MỞ KHÓA PHÁO THỦ
    // =====================================================

    IEnumerator GuideUnlockCannon()
    {
        yield return Say("Nhiệm vụ: Mở khóa Pháo Thủ để tăng sức mạnh phòng thủ.");

        if (buildPanel != null)
            buildPanel.SetActive(true);

        yield return FocusWorld(cannonPoint);

        if (cannonBuildButton != null && highlight != null)
            highlight.Highlight(cannonBuildButton);

        yield return Say("Hãy mở khóa hoặc chọn công trình Pháo Thủ.");

        yield return new WaitUntil(() => cannonUnlocked);

        FinishQuest(QUEST_UNLOCK_CANNON, "Tốt. Pháo Thủ đã được mở khóa.");
    }

    // =====================================================
    // 4. ĐÁNH BẠI ĐỢT CƯỚP ĐẦU TIÊN
    // =====================================================

    IEnumerator GuideFirstRaid()
    {
        yield return Say("Nhiệm vụ: Đẩy lùi nhóm cướp đầu tiên.");

        yield return FocusWorld(raidEnemyPoint);

        if (attackButton != null && highlight != null)
            highlight.Highlight(attackButton);

        yield return Say("Hãy ra lệnh tấn công kẻ địch.");

        yield return new WaitUntil(() => firstRaidDefeated);

        FinishQuest(QUEST_FIRST_RAID, "Tốt. Đợt cướp đầu tiên đã bị đẩy lùi.");
    }

    // =====================================================
    // 5. ĐẠI ĐỊA CHỦ
    // =====================================================

    IEnumerator GuideLandlord()
    {
        yield return Say("Nhiệm vụ: Đại địa chủ.");

        yield return Say("Hãy đạt 500 sản lượng Lúa.");

        yield return FocusWorld(foodPoint);

        if (foodIcon != null && highlight != null)
            highlight.Highlight(foodIcon);

        SetQuestProgress(QUEST_LANDLORD, foodProduction);

        yield return new WaitUntil(() => foodProduction >= foodTarget);

        FinishQuest(QUEST_LANDLORD, "Tốt. Sản lượng Lúa đã đạt 500.");
    }

    // =====================================================
    // 6. XỨ SỞ CỦA NỀN VĂN MINH
    // =====================================================

    IEnumerator GuideCivilization()
    {
        yield return Say("Nhiệm vụ: Đánh bại 2 đội quân Man Di trên bản đồ.");

        yield return FocusWorld(barbarianPoint);

        if (barbarianAttackButton != null && highlight != null)
            highlight.Highlight(barbarianAttackButton);

        SetQuestProgress(QUEST_CIVILIZATION, barbarianDefeated);

        yield return Say("Hãy tấn công đội quân Man Di.");

        yield return new WaitUntil(() => barbarianDefeated >= barbarianTarget);

        FinishQuest(QUEST_CIVILIZATION, "Tốt. Khu vực xung quanh đã an toàn hơn.");
    }

    // =====================================================
    // 7. BẰNG TÊN TÔI
    // =====================================================

    IEnumerator GuideMyName()
    {
        yield return Say("Nhiệm vụ: Đặt biệt danh của bạn.");

        if (profileButton != null && highlight != null)
            highlight.Highlight(profileButton);

        yield return Say("Mở hồ sơ của bạn.");

        if (renameButton != null && highlight != null)
            highlight.Highlight(renameButton);

        yield return Say("Hãy đặt biệt danh trong hồ sơ.");

        yield return new WaitUntil(() => playerNameSet);

        FinishQuest(QUEST_MY_NAME, "Tốt. Tên của bạn đã được ghi nhận.");
    }

    // =====================================================
    // 8. NGƯỜI GOM GÓP
    // =====================================================

    IEnumerator GuideGatherWood()
    {
        yield return Say("Nhiệm vụ: Thu thập 300 Gỗ ngoài bản đồ.");

        yield return FocusWorld(woodPoint);

        if (woodIcon != null && highlight != null)
            highlight.Highlight(woodIcon);

        SetQuestProgress(QUEST_GATHER_WOOD, gatheredWood);

        yield return Say("Hãy cho dân thu thập Gỗ.");

        yield return new WaitUntil(() => gatheredWood >= gatherWoodTarget);

        if (!gatherWoodQuestCompleted)
        {
            gatherWoodQuestCompleted = true;
            FinishQuest(QUEST_GATHER_WOOD, "Tốt. Gỗ đã được tích trữ.");
        }
        else
        {
            yield return Say("Tốt. Gỗ đã được tích trữ.");
        }
    }

    // =====================================================
    // 9. NGƯỜI GIỮ KHO
    // =====================================================

    IEnumerator GuideUpgradeStorage()
    {
        storageHouseClicked = false;
        storageUpgraded = false;
        storageUpgradeButtonClicked = false;

        StartStorageLevelWatcher();

        yield return Say("Nhiệm vụ: Nâng cấp Kho chứa lên cấp 2.");

        // 1. Lia camera tới nhà kho / ngôi nhà đã có sẵn.
        yield return FocusWorld(storagePoint);

        // Nếu nhà kho đã level 2 sẵn thì hoàn thành luôn.
        if (IsStorageLevelReached())
            yield break;

        // 2. Nói người chơi nhấn vào nhà.
        ShowObjective("Hãy nhấn vào ngôi nhà, rồi nâng cấp Kho chứa lên cấp 2.");

        // 3. Chờ người chơi click nhà kho hoặc hệ thống báo đã lên level.
        if (waitForStorageHouseClick)
            yield return new WaitUntil(() =>
                storageHouseClicked ||
                storageUpgraded ||
                IsStorageLevelReached() ||
                IsUIVisible(storageUpgradeButton)
            );

        if (storageQuestCompleted)
            yield break;

        // 4. Highlight nút nâng cấp khi panel nâng cấp đã mở.
        if (storageUpgradeButton != null && highlight != null)
            highlight.Highlight(storageUpgradeButton);

        ShowObjective("Bấm nút nâng cấp để đưa Kho chứa lên cấp 2.");

        // 5. Chờ nhà kho thật sự lên level 2.
        yield return new WaitUntil(() => storageUpgraded || IsStorageLevelReached());

        if (!storageQuestCompleted)
            CompleteStorageQuestForClaim();
    }

    // =====================================================
    // COMPLETE / PROGRESS API
    // GỌI TỪ GAME SYSTEM CỦA BẠN
    // =====================================================

    public void AddTrainedArcher(int amount)
    {
        trainedArcherCount += amount;
        SetQuestProgress(QUEST_TRAIN_ARCHER, trainedArcherCount);

        if (trainedArcherCount >= trainArcherTarget)
            CompleteQuestForClaim(QUEST_TRAIN_ARCHER, openPanelWhenExternalComplete);
    }

    public void OnWatchTowerBuilt()
    {
        ExternalCompleteWatchTowerFromStartup(openPanelWhenExternalComplete);
    }

    public void ExternalCompleteWatchTowerFromStartup(bool openPanel = true)
    {
        watchTowerBuilt = true;
        CompleteQuestForClaim(QUEST_WATCH_TOWER, openPanel);
    }

    public void OnCannonUnlocked()
    {
        cannonUnlocked = true;
        CompleteQuestForClaim(QUEST_UNLOCK_CANNON, openPanelWhenExternalComplete);
    }

    public void OnFirstRaidDefeated()
    {
        firstRaidDefeated = true;
        CompleteQuestForClaim(QUEST_FIRST_RAID, openPanelWhenExternalComplete);
    }

    public void SetFoodProduction(int value)
    {
        foodProduction = value;
        SetQuestProgress(QUEST_LANDLORD, foodProduction);

        if (foodProduction >= foodTarget)
            CompleteQuestForClaim(QUEST_LANDLORD, openPanelWhenExternalComplete);
    }

    public void AddFoodProduction(int amount)
    {
        SetFoodProduction(foodProduction + amount);
    }

    public void AddBarbarianDefeated(int amount)
    {
        barbarianDefeated += amount;
        SetQuestProgress(QUEST_CIVILIZATION, barbarianDefeated);

        if (barbarianDefeated >= barbarianTarget)
            CompleteQuestForClaim(QUEST_CIVILIZATION, openPanelWhenExternalComplete);
    }

    public void OnPlayerNameSet()
    {
        playerNameSet = true;
        CompleteQuestForClaim(QUEST_MY_NAME, openPanelWhenExternalComplete);
    }


    bool gatherWoodQuestCompleted;

    public void RegisterWorkerWoodGathered(int amount)
    {
        AddGatheredWood(amount);
    }

    public void AddGatheredWood(int amount)
    {
        if (amount <= 0)
            return;

        if (gatherWoodQuestCompleted)
            return;

        gatheredWood = Mathf.Clamp(gatheredWood + amount, 0, gatherWoodTarget);

        SetQuestProgress(QUEST_GATHER_WOOD, gatheredWood);

        Debug.Log("[GatherWoodQuest] Worker + " + amount + " wood. Progress = " + gatheredWood + "/" + gatherWoodTarget);

        if (gatheredWood >= gatherWoodTarget)
        {
            gatherWoodQuestCompleted = true;
            CompleteQuestForClaim(QUEST_GATHER_WOOD, openPanelWhenExternalComplete);
        }
    }

    // Gọi khi người chơi nhấn/chọn vào nhà kho trên map.
    public void NotifyStorageHouseClicked()
    {
        storageHouseClicked = true;
        StartStorageLevelWatcher();

        if (debugLog)
            Debug.Log("[StorageQuest] Đã nhấn vào nhà kho.");
    }

    // Alias cho dễ gọi từ OnClick của nhà.
    public void NotifyStorageSelected()
    {
        NotifyStorageHouseClicked();
    }

    // Gọi khi người chơi bấm nút nâng cấp kho.
    // Hàm này KHÔNG tự hoàn thành quest, trừ khi bật completeStorageQuestWhenUpgradeButtonClicked.
    public void NotifyStorageUpgradeButtonClicked()
    {
        storageUpgradeButtonClicked = true;
        StartStorageLevelWatcher();

        if (debugLog)
            Debug.Log("[StorageQuest] Đã bấm nút nâng cấp kho. Đang chờ level >= " + requiredStorageLevel);

        if (completeStorageQuestWhenUpgradeButtonClicked)
            NotifyStorageLevelChanged(requiredStorageLevel);
    }

    // Gọi khi hệ thống nâng cấp nhà kho báo level hiện tại.
    // Chỉ khi level >= requiredStorageLevel thì nhiệm vụ mới thành 1/1.
    public void NotifyStorageLevelChanged(int storageLevel)
    {
        if (debugLog)
            Debug.Log("[StorageQuest] Nhận level kho = " + storageLevel + " | required = " + requiredStorageLevel);

        if (storageLevel < requiredStorageLevel)
            return;

        if (storageQuestCompleted)
            return;

        storageUpgraded = true;
        CompleteStorageQuestForClaim();
    }

    // Alias dễ gọi từ code nhà kho.
    public void NotifyStorageUpgradedToLevel(int storageLevel)
    {
        NotifyStorageLevelChanged(storageLevel);
    }

    public void NotifyStorageUpgradedToLevel2()
    {
        NotifyStorageLevelChanged(requiredStorageLevel);
    }

    // Hàm cũ giữ lại để các code cũ gọi vẫn chạy.
    public void OnStorageUpgraded()
    {
        NotifyStorageLevelChanged(requiredStorageLevel);
    }

    void BindStorageUpgradeButton()
    {
        if (!autoBindStorageUpgradeButton || storageUpgradeButton == null)
            return;

        Button btn = storageUpgradeButton.GetComponent<Button>();
        if (btn == null)
            return;

        btn.onClick.RemoveListener(NotifyStorageUpgradeButtonClicked);
        btn.onClick.AddListener(NotifyStorageUpgradeButtonClicked);
    }

    void UnbindStorageUpgradeButton()
    {
        if (storageUpgradeButton == null)
            return;

        Button btn = storageUpgradeButton.GetComponent<Button>();
        if (btn == null)
            return;

        btn.onClick.RemoveListener(NotifyStorageUpgradeButtonClicked);
    }

    void StartStorageLevelWatcher()
    {
        if (!autoDetectStorageLevel)
            return;

        if (storageLevelWatchRoutine != null)
            return;

        storageLevelWatchRoutine = StartCoroutine(StorageLevelWatcher());
    }

    IEnumerator StorageLevelWatcher()
    {
        while (!storageQuestCompleted)
        {
            IsStorageLevelReached();

            if (storageQuestCompleted)
                break;

            yield return new WaitForSeconds(storageLevelCheckInterval);
        }

        storageLevelWatchRoutine = null;
    }

    bool IsStorageLevelReached()
    {
        int level = DetectStorageLevel();

        if (level >= requiredStorageLevel)
        {
            NotifyStorageLevelChanged(level);
            return true;
        }

        return false;
    }

    bool IsUIVisible(RectTransform target)
    {
        return target != null && target.gameObject.activeInHierarchy;
    }

    int DetectStorageLevel()
    {
        int level;

        if (TryReadLevelFromComponent(storageLevelComponent, out level))
            return level;

        if (storageLevelSource != null)
        {
            MonoBehaviour[] behaviours = storageLevelSource.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (TryReadLevelFromComponent(behaviour, out level))
                    return level;
            }
        }

        if (storagePoint != null)
        {
            MonoBehaviour[] behaviours = storagePoint.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (TryReadLevelFromComponent(behaviour, out level))
                    return level;
            }
        }

        return -1;
    }

    bool TryReadLevelFromComponent(MonoBehaviour component, out int level)
    {
        level = -1;

        if (component == null)
            return false;

        System.Type type = component.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (storageLevelMemberNames != null)
        {
            foreach (string memberName in storageLevelMemberNames)
            {
                if (string.IsNullOrEmpty(memberName))
                    continue;

                FieldInfo field = type.GetField(memberName, flags);
                if (TryGetIntValue(field != null ? field.GetValue(component) : null, out level))
                    return true;

                PropertyInfo prop = type.GetProperty(memberName, flags);
                if (prop != null && prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        if (TryGetIntValue(prop.GetValue(component, null), out level))
                            return true;
                    }
                    catch { }
                }
            }
        }

        // Fallback: tự tìm field/property có chữ level, nhưng bỏ qua max/target/required.
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!IsLikelyCurrentLevelName(field.Name))
                continue;

            if (TryGetIntValue(field.GetValue(component), out level))
                return true;
        }

        foreach (PropertyInfo prop in type.GetProperties(flags))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0 || !IsLikelyCurrentLevelName(prop.Name))
                continue;

            try
            {
                if (TryGetIntValue(prop.GetValue(component, null), out level))
                    return true;
            }
            catch { }
        }

        return false;
    }

    bool IsLikelyCurrentLevelName(string memberName)
    {
        string n = memberName.ToLowerInvariant();

        if (!n.Contains("level") && !n.Contains("lvl"))
            return false;

        if (n.Contains("max") || n.Contains("target") || n.Contains("required") || n.Contains("capacity"))
            return false;

        return true;
    }

    bool TryGetIntValue(object value, out int result)
    {
        result = -1;

        if (value is int intValue)
        {
            result = intValue;
            return true;
        }

        if (value is float floatValue)
        {
            result = Mathf.RoundToInt(floatValue);
            return true;
        }

        if (value is double doubleValue)
        {
            result = Mathf.RoundToInt((float)doubleValue);
            return true;
        }

        return false;
    }

    void CompleteStorageQuestForClaim()
    {
        if (storageQuestCompleted)
            return;

        storageQuestCompleted = true;
        storageUpgraded = true;

        if (storageLevelWatchRoutine != null)
        {
            StopCoroutine(storageLevelWatchRoutine);
            storageLevelWatchRoutine = null;
        }

        if (npc != null)
            npc.Hide();

        SetQuestProgress(QUEST_UPGRADE_STORAGE, 1);
        CompleteQuestForClaim(QUEST_UPGRADE_STORAGE, openPanelWhenExternalComplete);

        guideRunning = false;

        if (debugLog)
            Debug.Log("[StorageQuest] Kho chứa đã lên cấp " + requiredStorageLevel + " → nhiệm vụ Người giữ kho = 1/1.");
    }

    // Test nhanh
    public void TestTrainArcherDone() => AddTrainedArcher(20);
    public void TestWatchTowerDone() => OnWatchTowerBuilt();
    public void TestCannonDone() => OnCannonUnlocked();
    public void TestRaidDone() => OnFirstRaidDefeated();
    public void TestLandlordDone() => SetFoodProduction(500);
    public void TestCivilizationDone() => AddBarbarianDefeated(2);
    public void TestNameDone() => OnPlayerNameSet();
    public void TestGatherWoodDone() => AddGatheredWood(300);
    public void TestStorageDone() => OnStorageUpgraded();

    // =====================================================
    // HELPERS
    // =====================================================

    void FinishQuest(string questId, string doneMessage)
    {
        ClearGuide();

        if (questPanel != null)
        {
            questPanel.CompleteQuest(questId);

            if (openQuestPanelWhenComplete)
                questPanel.OpenPanel();
        }

        StartCoroutine(Say(doneMessage));
    }
    void CompleteQuestForClaim(string questId, bool openPanel)
    {
        ClearGuide();

        if (npc != null)
            npc.Hide();

        if (questPanel == null)
            return;

        questPanel.CompleteQuest(questId);

        if (openPanel)
            questPanel.OpenPanel();

        if (debugLog)
            Debug.Log("[RoKQuestMissionGuideRouter] Quest hoàn thành, chờ nhận thưởng: " + questId);
    }

    void SetQuestProgress(string questId, int value)
    {
        if (questPanel != null)
            questPanel.SetProgress(questId, value);
    }

    void SyncInitialProgress()
    {
        SetQuestProgress(QUEST_TRAIN_ARCHER, trainedArcherCount);
        SetQuestProgress(QUEST_LANDLORD, foodProduction);
        SetQuestProgress(QUEST_CIVILIZATION, barbarianDefeated);
        SetQuestProgress(QUEST_GATHER_WOOD, gatheredWood);
    }

    IEnumerator Say(string text)
    {
        if (npc == null)
            yield break;

        yield return npc.ShowAndWait(text);
    }

    void ShowObjective(string text)
    {
        if (npc == null)
            return;

        npc.ShowObjective(text);
    }

    IEnumerator FocusWorld(Transform target)
    {
        if (target == null)
            yield break;

        yield return MoveCameraTo(target);
        SpawnArrow(target);
    }

    IEnumerator MoveCameraTo(Transform target)
    {
        if (mainCamera == null || target == null)
            yield break;

        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = target.position + cameraOffset;

        Quaternion startRot = mainCamera.transform.rotation;
        Quaternion endRot = startRot;

        if (rotateCameraToTarget)
        {
            Vector3 dir = target.position - endPos;
            if (dir.sqrMagnitude > 0.01f)
                endRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / cameraMoveTime;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, smooth);

            if (rotateCameraToTarget)
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, smooth);

            yield return null;
        }
    }

    void SpawnArrow(Transform target)
    {
        ClearArrow();

        if (arrowPrefab == null || target == null)
            return;

        arrowInstance = Instantiate(arrowPrefab);
        arrowInstance.transform.position = target.position + Vector3.up * arrowHeight;
        arrowInstance.transform.LookAt(target.position);
    }

    void ClearArrow()
    {
        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
            arrowInstance = null;
        }
    }

    void ClearGuide()
    {
        ClearArrow();

        if (highlight != null)
            highlight.ClearAll();
    }
}