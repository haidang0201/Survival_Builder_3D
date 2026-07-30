using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TutorialStage
{
    None,
    Stage1_TownHall,         // Thức tỉnh & Chọn Nhà chính
    Stage2_CivilBuildings,   // Xây công trình dân sự (Khai thác gỗ & đá)
    Stage3_UpgradeWood,      // Nâng cấp Kho gỗ
    Stage4_BuildWatchTower,  // Xây Tháp canh phòng thủ
    Stage5_EnemyWave,        // Cảnh báo & Đòn tấn công khống chế từ kẻ thù
    Stage6_Complete          // Hoàn thành & Trao quyền (Không cần VictoryPanel)
}

public class CampaignTutorialManager : MonoBehaviour
{
    public static CampaignTutorialManager Ins { get; private set; }

    [Header("=== TRẠNG THÁI HIỆN TẠI ===")]
    public TutorialStage currentStage = TutorialStage.None;

    [Header("=== UI HIGHLIGHT & WARNING ===")]
    [SerializeField] private GameObject overlayDim;         // Màn che tối chặn tương tác (Không cần Pause Time)
    [SerializeField] private GameObject handPointer;        // Bàn tay chỉ dẫn
    [SerializeField] private Vector2 pointerOffset = new Vector2(30f, 30f);
    [SerializeField] private Canvas tutorialCanvas;         // Canvas dùng để đặt bàn tay tutorial
    [SerializeField] private Canvas buildShopCanvas;        // Canvas của build shop để định vị nút chính xác
    [SerializeField] private TMP_Text hintText;             // Text hướng dẫn hiện tại
    [SerializeField] private TMP_Text warningText;          // Text cảnh báo lính địch sắp tấn công

    [Header("=== THỜI DIỂM THOẠI MARCUS TỪNG GIAI ĐOẠN ===")]
    [SerializeField] private DialogueData[] stage1Dialogues;
    [SerializeField] private DialogueData[] stage2Dialogues;
    [SerializeField] private DialogueData[] stage3Dialogues;
    [SerializeField] private DialogueData[] stage4Dialogues;
    [SerializeField] private DialogueData[] stage5WarningDialogues;
    [SerializeField] private DialogueData[] stage6CompleteDialogues;

    [Header("=== CÁC NÚT BẤM / ĐỐI TƯỢNG CẦN KHỐNG CHẾ ===")]
    [SerializeField] private Button buildMenuButton;
    [SerializeField] private Button civilianTabButton;
    [SerializeField] private Button villaTabButton;         // Tab cho Kho Gỗ
    [SerializeField] private Button militaryTabButton;      // Tab cho Tháp Canh
    [SerializeField] private Button buildWoodCutterButton;
    [SerializeField] private Button buildStoneStorageButton;
    [SerializeField] private Button buildWatchTowerButton;
    [SerializeField] private Button upgradeBuildingButton;

    [Header("=== THAM CHIẾU CÁC GAME OBJECT NGOÀI SCENE ===")]
    [SerializeField] private Transform townHallTransform;   // Transform Nhà Chính
    [SerializeField] private EnemySpawn enemySpawner;       // Script EnemySpawn điều khiển spawn quái

    [Header("=== CẤU HÌNH WAVE TUTORIAL ===")]
    [SerializeField] private int tutorialEnemyCount = 2;    // Khống chế chính xác số lượng quái spawn (Ví dụ: 2 con)
    private int enemiesRemaining = 0;

    private bool hasBuiltWoodCutter = false;
    private bool hasBuiltStoneStorage = false;
    private bool hasBuiltWatchTower = false;
    private bool hasOpenedBuildMenu = false;
    private bool hasOpenedTab = false;
    private RectTransform pointerRect;

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);

        if (handPointer != null)
            pointerRect = handPointer.GetComponent<RectTransform>();

        if (tutorialCanvas == null && handPointer != null)
            tutorialCanvas = handPointer.GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        Time.timeScale = 1f; // 👈 Ép thời gian chạy bình thường

        if (buildMenuButton != null)
            buildMenuButton.onClick.AddListener(OnBuildMenuButtonClicked);

        if (civilianTabButton != null)
            civilianTabButton.onClick.AddListener(OnTabClicked);

        if (villaTabButton != null)
            villaTabButton.onClick.AddListener(OnTabClicked);

        if (militaryTabButton != null)
            militaryTabButton.onClick.AddListener(OnTabClicked);

        StartStage1();
    }



    // ====================================================================
    // GIAI ĐOẠN 1: THỨC TỈNH & GIỚI THIỆU MỤC TIÊU CỐT LÕI
    // ====================================================================
    public void StartStage1()
    {
        currentStage = TutorialStage.Stage1_TownHall;
        LockAllInputs();

        NPCDialogueUI.Ins.ShowDialogueSequence(stage1Dialogues, () =>
        {
            if (townHallTransform != null)
            {
                PointHandAt(townHallTransform.position);
            }
            UpdateHint("📍 Bước 1: Nhấn vào Nhà Chính để bắt đầu tutorial và mở khóa mục tiêu đầu tiên.");
        });
    }

    public void OnClickTownHall()
    {
        if (currentStage != TutorialStage.Stage1_TownHall) return;

        var hp = townHallTransform.GetComponent<HPTower>();
        if (hp != null) hp.gameObject.SetActive(true);

        HidePointer();
        StartStage2();
    }

    // ====================================================================
    // GIAI ĐOẠN 2: XÂY DỰNG CÔNG TRÌNH DÂN SỰ (KHAI THÁC GỖ & ĐÁ)
    // ====================================================================
    private void StartStage2()
    {
        currentStage = TutorialStage.Stage2_CivilBuildings;
        hasOpenedBuildMenu = false;
        hasOpenedTab = false;

        NPCDialogueUI.Ins.ShowDialogueSequence(stage2Dialogues, () =>
        {
            ResetStage2Menu();
        });
    }

    private void ResetStage2Menu()
    {
        hasOpenedBuildMenu = false;
        hasOpenedTab = false;
        SetButtonInteractable(buildMenuButton, true);
        SetButtonInteractable(civilianTabButton, false);
        SetButtonInteractable(villaTabButton, false);
        SetButtonInteractable(buildWoodCutterButton, false);
        SetButtonInteractable(buildStoneStorageButton, false);
        PointHandAtUI(buildMenuButton.transform as RectTransform);
        UpdateHint("📍 Bước 2: Nhấn vào nút Mở Cửa Hàng Xây Dựng để mở danh sách công trình.");
    }

    private void OnBuildMenuButtonClicked()
    {
        if (currentStage != TutorialStage.Stage2_CivilBuildings && currentStage != TutorialStage.Stage4_BuildWatchTower) return;
        if (hasOpenedBuildMenu) return;

        hasOpenedBuildMenu = true;
        hasOpenedTab = false;

        if (currentStage == TutorialStage.Stage2_CivilBuildings)
        {
            SetBuildTabButtonsInteractable(true);
            Button buildTabButton = GetSharedBuildTabButton();
            if (buildTabButton != null)
            {
                PointHandAtUI(buildTabButton.transform as RectTransform);
            }
            UpdateHint("📍 Bước 2: Chọn tab Dân Sự để xem các công trình cần xây dựng.");
        }
        else if (currentStage == TutorialStage.Stage4_BuildWatchTower)
        {
            SetButtonInteractable(militaryTabButton, true);
            PointHandAtUI(militaryTabButton.transform as RectTransform);
            UpdateHint("📍 Bước 4: Chọn tab Quân Sự để xem các công trình phòng thủ.");
        }
    }

    private void OnTabClicked()
    {
        if (hasOpenedTab) return;

        hasOpenedTab = true;

        if (currentStage == TutorialStage.Stage2_CivilBuildings)
        {
            SetButtonInteractable(buildWoodCutterButton, true);
            SetButtonInteractable(buildStoneStorageButton, true);
            if (buildWoodCutterButton != null)
            {
                PointHandAtUI(buildWoodCutterButton.transform as RectTransform);
            }
            UpdateHint("📍 Bước 2: Chọn công trình Khai thác Gỗ hoặc Kho Đá để bắt đầu xây dựng.");
        }
        else if (currentStage == TutorialStage.Stage4_BuildWatchTower)
        {
            SetButtonInteractable(buildWatchTowerButton, true);
            if (buildWatchTowerButton != null)
            {
                PointHandAtUI(buildWatchTowerButton.transform as RectTransform);
            }
            UpdateHint("📍 Bước 4: Chọn Tháp Canh để tăng cường phòng thủ cho căn cứ.");
        }
    }

    public void OnCivilBuildingPlaced(BuildingType buildingType)
    {
        if (currentStage == TutorialStage.Stage2_CivilBuildings)
        {
            if (buildingType == BuildingType.WoodCutter) hasBuiltWoodCutter = true;
            if (buildingType == BuildingType.StoneStorage) hasBuiltStoneStorage = true;

            // Nếu chưa xây cả 2 công trình, quay lại bước đầu
            if (!hasBuiltWoodCutter || !hasBuiltStoneStorage)
            {
                ResetStage2Menu();
                return;
            }

            // Đã xây đủ cả 2 công trình
            HidePointer();
            UpdateHint("");
            StartStage3();
        }
    }

    // ====================================================================
    // GIAI ĐOẠN 3: NÂNG CẤP KHO GỖ
    // ====================================================================
    private void StartStage3()
    {
        currentStage = TutorialStage.Stage3_UpgradeWood;

        NPCDialogueUI.Ins.ShowDialogueSequence(stage3Dialogues, () =>
        {
            SetButtonInteractable(upgradeBuildingButton, true);
            if (upgradeBuildingButton != null)
            {
                PointHandAtUI(upgradeBuildingButton.transform as RectTransform);
            }
            UpdateHint("📍 Bước 3: Nhấn nút Nâng Cấp để cải thiện Kho Gỗ và tăng hiệu quả sản xuất.");
        });
    }

    public void OnBuildingUpgraded(UpgradeableBuilding building)
    {
        if (currentStage != TutorialStage.Stage3_UpgradeWood) return;

        // Bắt chính xác sự kiện khi Kho Gỗ / Khai thác gỗ được nâng cấp
        if (building != null && building.buildingType == BuildingType.WoodCutter)
        {
            HidePointer();
            StartStage4();
        }
    }

    // ====================================================================
    // GIAI ĐOẠN 4: HỆ THỐNG PHÒNG THỦ - THÁP CANH
    // ====================================================================
    private void StartStage4()
    {
        currentStage = TutorialStage.Stage4_BuildWatchTower;
        hasOpenedBuildMenu = false;
        hasOpenedTab = false;

        NPCDialogueUI.Ins.ShowDialogueSequence(stage4Dialogues, () =>
        {
            ResetStage4Menu();
        });
    }

    private void ResetStage4Menu()
    {
        hasOpenedBuildMenu = false;
        hasOpenedTab = false;
        SetButtonInteractable(buildMenuButton, true);
        SetButtonInteractable(militaryTabButton, false);
        SetButtonInteractable(buildWatchTowerButton, false);
        PointHandAtUI(buildMenuButton.transform as RectTransform);
        UpdateHint("📍 Bước 4: Mở Cửa Hàng Xây Dựng để tiếp tục xây phòng thủ cho căn cứ.");
    }

    public void OnDefenseBuildingPlaced(BuildingType buildingType)
    {
        if (currentStage != TutorialStage.Stage4_BuildWatchTower) return;

        if (buildingType == BuildingType.WatchTower)
        {
            hasBuiltWatchTower = true;
            HidePointer();
            UpdateHint("");
            StartCoroutine(StartStage5Routine());
        }
    }

    // ====================================================================
    // GIAI ĐOẠN 5: CẢNH BÁO & ĐỢT SÓNG KẺ THÙ (KHỐNG CHẾ SPAWN)
    // ====================================================================
    private IEnumerator StartStage5Routine()
    {
        currentStage = TutorialStage.Stage5_EnemyWave;

        // 1. NPC cảnh báo
        bool dialogueDone = false;
        NPCDialogueUI.Ins.ShowDialogueSequence(stage5WarningDialogues, () =>
        {
            dialogueDone = true;
        });

        while (!dialogueDone) yield return null;

        // 2. Hiển thị UI Cảnh Báo lính địch sắp tấn công
        if (warningText != null)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = "⚠️ CẢNH BÁO: KẺ THÙ ĐANG TIẾP CẬN CĂN CỨ!";
            if (warningText.rectTransform != null)
            {
                PointHandAtUI(warningText.rectTransform);
            }
            UpdateHint("📍 Bước 5: Cảnh báo đã xuất hiện, hãy giữ phòng thủ và tiêu diệt kẻ địch để hoàn thành thử thách.");
            yield return new WaitForSeconds(2.5f);
            warningText.gameObject.SetActive(false);
        }

        // 3. Khống chế số lượng quái spawn chính xác
        enemiesRemaining = tutorialEnemyCount;
        if (enemySpawner != null)
        {
            for (int i = 0; i < tutorialEnemyCount; i++)
            {
                enemySpawner.SpawnEnemy();
            }
        }

        UnlockAllInputs(); // Cho phép tương tác bình thường trong lúc thủ tháp
    }

    // Gọi hàm này từ EnemyHealth.cs khi quái chết
    public void OnEnemyKilled()
    {
        if (currentStage != TutorialStage.Stage5_EnemyWave) return;

        enemiesRemaining--;
        if (enemiesRemaining <= 0)
        {
            StartStage6();
        }
    }

    // ====================================================================
    // GIAI ĐOẠN 6: HOÀN THÀNH & TRAO QUYỀN (KHÔNG DÙNG VICTORY PANEL)
    // ====================================================================
    private void StartStage6()
    {
        currentStage = TutorialStage.Stage6_Complete;

        // NPC thông báo phòng thủ thành công và bàn giao quyền trực tiếp
        NPCDialogueUI.Ins.ShowDialogueSequence(stage6CompleteDialogues, () =>
        {
            // Cộng phần thưởng Tân Thủ
            if (JsonDataManager.Ins != null)
            {
                JsonDataManager.Ins.AddWood(500);
                JsonDataManager.Ins.AddStone(500);
                JsonDataManager.Ins.BroadcastAllResources();
            }

            HidePointer();
            UpdateHint("🎉 Bước 6: Tutorial hoàn tất! Bạn đã nhận thưởng và có thể tiếp tục khám phá thế giới.");
            UnlockAllInputs();
            Debug.Log("✅ [TUTORIAL] ĐÃ HOÀN THÀNH TUTORIAL & BÀN GIAO QUYỀN TRỰC TIẾP!");
        });
    }

    // ================= HÀM BỔ TRỢ CHẶN / MỞ TƯƠNG TÁC =================

    private void LockAllInputs()
    {
        if (overlayDim != null) overlayDim.SetActive(true);
        SetButtonInteractable(buildMenuButton, false);
        SetButtonInteractable(civilianTabButton, false);
        SetButtonInteractable(villaTabButton, false);
        SetButtonInteractable(militaryTabButton, false);
        SetButtonInteractable(buildWoodCutterButton, false);
        SetButtonInteractable(buildStoneStorageButton, false);
        SetButtonInteractable(buildWatchTowerButton, false);
        SetButtonInteractable(upgradeBuildingButton, false);
    }

    private void UnlockAllInputs()
    {
        if (overlayDim != null) overlayDim.SetActive(false);
        SetButtonInteractable(buildMenuButton, true);
        SetButtonInteractable(civilianTabButton, true);
        SetButtonInteractable(villaTabButton, true);
        SetButtonInteractable(militaryTabButton, true);
        SetButtonInteractable(buildWoodCutterButton, true);
        SetButtonInteractable(buildStoneStorageButton, true);
        SetButtonInteractable(buildWatchTowerButton, true);
        SetButtonInteractable(upgradeBuildingButton, true);
    }

    private void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }

    private void UpdateHint(string hintMessage)
    {
        if (hintText != null)
        {
            hintText.text = hintMessage;
            hintText.gameObject.SetActive(!string.IsNullOrEmpty(hintMessage));
        }
    }

    private void PointHandAt(Vector3 worldPos)
    {
        if (handPointer == null) return;
        handPointer.SetActive(true);

        Vector3 screenPos = Camera.main != null
            ? Camera.main.WorldToScreenPoint(worldPos)
            : Vector3.zero;

        PositionPointerAtScreenPoint(screenPos);
    }

    private void PointHandAtUI(RectTransform uiRect)
    {
        if (handPointer == null || uiRect == null) return;
        handPointer.SetActive(true);

        Canvas uiCanvas = ResolveTutorialCanvas();
        if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            PositionPointerAtScreenPoint(uiRect.position);
            return;
        }

        Camera cam = uiCanvas != null ? uiCanvas.worldCamera : Camera.main;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, uiRect.position);
        PositionPointerAtScreenPoint(screenPoint);
    }

    private void PositionPointerAtScreenPoint(Vector2 screenPoint)
    {
        if (pointerRect == null) return;

        Canvas uiCanvas = ResolveTutorialCanvas();
        if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            pointerRect.position = screenPoint + pointerOffset;
            return;
        }

        RectTransform parentRect = pointerRect.parent as RectTransform;
        if (parentRect == null)
        {
            pointerRect.position = screenPoint + pointerOffset;
            return;
        }

        Camera cam = uiCanvas != null ? uiCanvas.worldCamera : Camera.main;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out Vector2 localPoint);
        pointerRect.localPosition = localPoint + pointerOffset;
    }

    private Canvas ResolveTutorialCanvas()
    {
        if (tutorialCanvas != null) return tutorialCanvas;
        return buildShopCanvas != null ? buildShopCanvas : null;
    }

    private Button GetSharedBuildTabButton()
    {
        if (civilianTabButton != null) return civilianTabButton;
        return villaTabButton;
    }

    private void SetBuildTabButtonsInteractable(bool interactable)
    {
        SetButtonInteractable(civilianTabButton, interactable);
        SetButtonInteractable(villaTabButton, interactable);
    }

    private void HidePointer()
    {
        if (handPointer != null) handPointer.SetActive(false);
    }
}