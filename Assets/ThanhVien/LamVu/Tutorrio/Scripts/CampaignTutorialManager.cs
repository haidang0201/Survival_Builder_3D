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

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f; // 👈 Ép thời gian chạy bình thường
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
            PointHandAt(townHallTransform.position);
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

        NPCDialogueUI.Ins.ShowDialogueSequence(stage2Dialogues, () =>
        {
            SetButtonInteractable(buildMenuButton, true);
            SetButtonInteractable(buildWoodCutterButton, true);
            SetButtonInteractable(buildStoneStorageButton, true);
            PointHandAtUI(buildMenuButton.transform as RectTransform);
        });
    }

    public void OnCivilBuildingPlaced(BuildingType buildingType)
    {
        if (currentStage != TutorialStage.Stage2_CivilBuildings) return;

        if (buildingType == BuildingType.WoodCutter) hasBuiltWoodCutter = true;
        if (buildingType == BuildingType.StoneStorage) hasBuiltStoneStorage = true;

        // Khi đã đặt cả Khai thác gỗ và Khai thác đá
        if (hasBuiltWoodCutter && hasBuiltStoneStorage)
        {
            HidePointer();
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

        NPCDialogueUI.Ins.ShowDialogueSequence(stage4Dialogues, () =>
        {
            SetButtonInteractable(buildWatchTowerButton, true);
            PointHandAtUI(buildWatchTowerButton.transform as RectTransform);
        });
    }

    public void OnWatchTowerPlaced()
    {
        if (currentStage != TutorialStage.Stage4_BuildWatchTower) return;

        HidePointer();
        StartCoroutine(StartStage5Routine());
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

            UnlockAllInputs();
            Debug.Log("✅ [TUTORIAL] ĐÃ HOÀN THÀNH TUTORIAL & BÀN GIAO QUYỀN TRỰC TIẾP!");
        });
    }

    // ================= HÀM BỔ TRỢ CHẶN / MỞ TƯƠNG TÁC =================

    private void LockAllInputs()
    {
        if (overlayDim != null) overlayDim.SetActive(true);
        SetButtonInteractable(buildMenuButton, false);
        SetButtonInteractable(buildWoodCutterButton, false);
        SetButtonInteractable(buildStoneStorageButton, false);
        SetButtonInteractable(buildWatchTowerButton, false);
        SetButtonInteractable(upgradeBuildingButton, false);
    }

    private void UnlockAllInputs()
    {
        if (overlayDim != null) overlayDim.SetActive(false);
        SetButtonInteractable(buildMenuButton, true);
        SetButtonInteractable(buildWoodCutterButton, true);
        SetButtonInteractable(buildStoneStorageButton, true);
        SetButtonInteractable(buildWatchTowerButton, true);
        SetButtonInteractable(upgradeBuildingButton, true);
    }

    private void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }

    private void PointHandAt(Vector3 worldPos)
    {
        if (handPointer == null) return;
        handPointer.SetActive(true);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        handPointer.transform.position = screenPos;
    }

    private void PointHandAtUI(RectTransform uiRect)
    {
        if (handPointer == null || uiRect == null) return;
        handPointer.SetActive(true);
        handPointer.transform.position = uiRect.transform.position;
    }

    private void HidePointer()
    {
        if (handPointer != null) handPointer.SetActive(false);
    }
}