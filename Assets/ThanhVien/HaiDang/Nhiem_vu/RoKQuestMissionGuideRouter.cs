using System.Collections;
using UnityEngine;

public class RoKQuestMissionGuideRouter : MonoBehaviour
{
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
    public NPCDialogue npc;
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

    [Header("OPTIONS")]
    public bool closeQuestPanelWhenGuideStart = true;
    public bool openQuestPanelWhenComplete = true;
    public bool debugLog = true;

    GameObject arrowInstance;
    bool guideRunning;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (questPanel != null)
        {
            questPanel.onGoPressed.RemoveListener(OnQuestGoPressed);
            questPanel.onGoPressed.AddListener(OnQuestGoPressed);
        }

        SyncInitialProgress();
    }

    void OnDestroy()
    {
        if (questPanel != null)
            questPanel.onGoPressed.RemoveListener(OnQuestGoPressed);
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

        FinishQuest(QUEST_GATHER_WOOD, "Tốt. Gỗ đã được tích trữ.");
    }

    // =====================================================
    // 9. NGƯỜI GIỮ KHO
    // =====================================================

    IEnumerator GuideUpgradeStorage()
    {
        yield return Say("Nhiệm vụ: Nâng cấp Kho chứa lên cấp 2.");

        yield return FocusWorld(storagePoint);

        if (storageUpgradeButton != null && highlight != null)
            highlight.Highlight(storageUpgradeButton);

        yield return Say("Hãy nâng cấp Kho chứa để bảo vệ tài nguyên.");

        yield return new WaitUntil(() => storageUpgraded);

        FinishQuest(QUEST_UPGRADE_STORAGE, "Tốt. Kho chứa đã được nâng cấp.");
    }

    // =====================================================
    // COMPLETE / PROGRESS API
    // GỌI TỪ GAME SYSTEM CỦA BẠN
    // =====================================================

    public void AddTrainedArcher(int amount)
    {
        trainedArcherCount += amount;
        SetQuestProgress(QUEST_TRAIN_ARCHER, trainedArcherCount);
    }

    public void OnWatchTowerBuilt()
    {
        watchTowerBuilt = true;
        SetQuestProgress(QUEST_WATCH_TOWER, 1);
    }

    public void OnCannonUnlocked()
    {
        cannonUnlocked = true;
        SetQuestProgress(QUEST_UNLOCK_CANNON, 1);
    }

    public void OnFirstRaidDefeated()
    {
        firstRaidDefeated = true;
        SetQuestProgress(QUEST_FIRST_RAID, 1);
    }

    public void SetFoodProduction(int value)
    {
        foodProduction = value;
        SetQuestProgress(QUEST_LANDLORD, foodProduction);
    }

    public void AddFoodProduction(int amount)
    {
        SetFoodProduction(foodProduction + amount);
    }

    public void AddBarbarianDefeated(int amount)
    {
        barbarianDefeated += amount;
        SetQuestProgress(QUEST_CIVILIZATION, barbarianDefeated);
    }

    public void OnPlayerNameSet()
    {
        playerNameSet = true;
        SetQuestProgress(QUEST_MY_NAME, 1);
    }

    public void AddGatheredWood(int amount)
    {
        gatheredWood += amount;
        SetQuestProgress(QUEST_GATHER_WOOD, gatheredWood);
    }

    public void OnStorageUpgraded()
    {
        storageUpgraded = true;
        SetQuestProgress(QUEST_UPGRADE_STORAGE, 1);
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
        npc.Hide();
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