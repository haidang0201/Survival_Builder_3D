using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TutorialStage
{
    None,
    Stage1_TownHall,         
    Stage2_CivilBuildings,   
    Stage3_UpgradeWood,      
    Stage4_BuildWatchTower,  
    Stage5_EnemyWave,        
    Stage6_Complete          
}

public class CampaignTutorialManager : MonoBehaviour
{
    public static CampaignTutorialManager Ins { get; private set; }

    [Header("=== TRẠNG THÁI HIỆN TẠI ===")]
    public TutorialStage currentStage = TutorialStage.None;

    [Header("=== UI HIGHLIGHT & WARNING ===")]
    [SerializeField] private GameObject overlayDim;         
    [SerializeField] private GameObject handPointer;        
    [SerializeField] private RectTransform highlightRing;   
    [SerializeField] private Vector2 pointerOffset = new Vector2(30f, -30f);
    [SerializeField] private Canvas tutorialCanvas;         
    [SerializeField] private Canvas buildShopCanvas;        
    [SerializeField] private TMP_Text hintText;             
    [SerializeField] private TMP_Text warningText;          

    [Header("=== TÙY CHỈNH ANIMATION BÀN TAY ===")]
    [SerializeField] private float pointerMoveSpeed = 12f;  
    [SerializeField] private float bobbingSpeed = 8f;       
    [SerializeField] private float bobbingAmount = 12f;     

    [Header("=== THỜI DIỂM THOẠI MARCUS ===")]
    [SerializeField] private DialogueData[] stage1Dialogues;
    [SerializeField] private DialogueData[] stage2Dialogues;
    [SerializeField] private DialogueData[] stage3Dialogues;
    [SerializeField] private DialogueData[] stage4Dialogues;
    [SerializeField] private DialogueData[] stage5WarningDialogues;
    [SerializeField] private DialogueData[] stage6CompleteDialogues;

    [Header("=== NÚT BẤM CẦN KHỐNG CHẾ ===")]
    [SerializeField] private Button buildMenuButton;
    [SerializeField] private Button civilianTabButton;
    [SerializeField] private Button villaTabButton;         
    [SerializeField] private Button militaryTabButton;      
    [SerializeField] private Button buildWoodCutterButton;
    [SerializeField] private Button buildStoneStorageButton;
    [SerializeField] private Button buildWatchTowerButton;
    [SerializeField] private Button upgradeBuildingButton;

    [Header("=== SCENE REFERENCES ===")]
    [SerializeField] private Transform townHallTransform;   
    [SerializeField] private EnemySpawn enemySpawner;       

    [Header("=== CẤU HÌNH WAVE TUTORIAL ===")]
    [SerializeField] private int tutorialEnemyCount = 2;    
    private int enemiesRemaining = 0;

    private bool isPlacingBuilding = false;          
    private bool isWaitingForConstruction = false;   
    private bool hasBuiltWoodCutter = false;
    private bool hasBuiltStoneStorage = false;
    private bool hasBuiltWatchTower = false;
    private bool hasOpenedBuildMenu = false;
    private bool hasOpenedTab = false;

    private RectTransform pointerRect;
    private Vector2 targetScreenPosition;
    private Coroutine cameraFocusCoroutine;
    private RectTransform currentTargetUI;

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);

        if (handPointer != null)
        {
            pointerRect = handPointer.GetComponent<RectTransform>();
            handPointer.SetActive(false); 
        }

        if (highlightRing != null)
        {
            highlightRing.gameObject.SetActive(false); 
        }

        if (tutorialCanvas == null && handPointer != null)
            tutorialCanvas = handPointer.GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        Time.timeScale = 1f;

        HidePointer();

        if (buildMenuButton != null) buildMenuButton.onClick.AddListener(OnBuildMenuButtonClicked);
        if (civilianTabButton != null) civilianTabButton.onClick.AddListener(OnTabClicked);
        if (villaTabButton != null) villaTabButton.onClick.AddListener(OnTabClicked);
        if (militaryTabButton != null) militaryTabButton.onClick.AddListener(OnTabClicked);

        if (buildWoodCutterButton != null) buildWoodCutterButton.onClick.AddListener(OnStartPlacement);
        if (buildStoneStorageButton != null) buildStoneStorageButton.onClick.AddListener(OnStartPlacement);
        if (buildWatchTowerButton != null) buildWatchTowerButton.onClick.AddListener(OnStartPlacement);
        if (upgradeBuildingButton != null) upgradeBuildingButton.onClick.AddListener(OnActionButtonClicked);

        StartStage1();
    }

    private void Update()
    {
        UpdateHandPointerAnimation();
        UpdateHighlightRingAnimation();
        CheckUIFallbackState(); 
    }

    private void OnActionButtonClicked()
    {
        HidePointer();
    }

    private void CheckUIFallbackState()
    {
        if (currentStage == TutorialStage.None || currentStage == TutorialStage.Stage6_Complete) return;
        if (NPCDialogueUI.Ins != null && NPCDialogueUI.Ins.IsDialogueActive) return; 
        if (isPlacingBuilding || isWaitingForConstruction) return;

        if (hasOpenedBuildMenu && buildShopCanvas != null && !buildShopCanvas.gameObject.activeInHierarchy)
        {
            if (currentStage == TutorialStage.Stage2_CivilBuildings)
            {
                ResetStage2Menu();
            }
            else if (currentStage == TutorialStage.Stage4_BuildWatchTower)
            {
                ResetStage4Menu();
            }
        }

        if (currentTargetUI != null && !currentTargetUI.gameObject.activeInHierarchy && handPointer.activeSelf)
        {
            if (buildMenuButton != null)
            {
                PointHandAtUI(buildMenuButton.transform as RectTransform);
            }
        }
    }

    // ====================================================================
    // GIAI ĐOẠN 1
    // ====================================================================
    public void StartStage1()
    {
        currentStage = TutorialStage.Stage1_TownHall;
        LockAllInputs();
        HidePointer();

        RunDialogueSequence(stage1Dialogues, () =>
        {
            if (townHallTransform != null)
            {
                FocusCameraOn(townHallTransform.position, 1.2f);
                PointHandAt(townHallTransform.position);
            }
            UpdateHint("📍 Bước 1: Nhấn vào **Nhà Chính** để mở khóa mục tiêu đầu tiên.");
        });
    }

    public void OnClickTownHall()
    {
        if (currentStage != TutorialStage.Stage1_TownHall) return;

        if (townHallTransform != null)
        {
            var hp = townHallTransform.GetComponent<HPTower>();
            if (hp != null) hp.gameObject.SetActive(true);
        }

        HidePointer();
        StartStage2();
    }

    // ====================================================================
    // GIAI ĐOẠN 2: CÔNG TRÌNH DÂN SỰ (ĐÃ SỬA LỖI THOẠI & LỆCH VỊ TRÍ HAND)
    // ====================================================================
    private void StartStage2()
    {
        currentStage = TutorialStage.Stage2_CivilBuildings;
        HidePointer();

        // 🛠️ SỬA LỖI 1: Bật chuỗi thoại Stage 2 trước khi hiển thị trỏ tay
        RunDialogueSequence(stage2Dialogues, () =>
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

        if (!hasBuiltWoodCutter)
            UpdateHint("📍 Bước 2: Nhấn **Cửa Hàng Xây Dựng** để chọn xây Khai Thác Gỗ.");
        else
            UpdateHint("📍 Bước 2: Nhấn **Cửa Hàng Xây Dựng** để tiếp tục chọn xây Kho Đá.");
    }

    private void OnBuildMenuButtonClicked()
    {
        if (currentStage != TutorialStage.Stage2_CivilBuildings && currentStage != TutorialStage.Stage4_BuildWatchTower) return;

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
            UpdateHint("📍 Bước 2: Chọn tab **Dân Sự** để mở các công trình.");
        }
        else if (currentStage == TutorialStage.Stage4_BuildWatchTower)
        {
            SetButtonInteractable(militaryTabButton, true);
            PointHandAtUI(militaryTabButton.transform as RectTransform);
            UpdateHint("📍 Bước 4: Chọn tab **Quân Sự** để xem công trình phòng thủ.");
        }
    }

    private void OnTabClicked()
    {
        if (hasOpenedTab) return;

        hasOpenedTab = true;

        if (currentStage == TutorialStage.Stage2_CivilBuildings)
        {
            SetButtonInteractable(buildWoodCutterButton, !hasBuiltWoodCutter);
            SetButtonInteractable(buildStoneStorageButton, !hasBuiltStoneStorage);

            if (!hasBuiltWoodCutter)
            {
                if (buildWoodCutterButton != null) PointHandAtUI(buildWoodCutterButton.transform as RectTransform);
                UpdateHint("📍 Bước 2: Bấm chọn **Khai Thác Gỗ** để đặt xây.");
            }
            else if (!hasBuiltStoneStorage)
            {
                if (buildStoneStorageButton != null) PointHandAtUI(buildStoneStorageButton.transform as RectTransform);
                UpdateHint("📍 Bước 2: Bấm chọn **Kho Đá** để đặt xây.");
            }
        }
        else if (currentStage == TutorialStage.Stage4_BuildWatchTower)
        {
            SetButtonInteractable(buildWatchTowerButton, true);
            if (buildWatchTowerButton != null) PointHandAtUI(buildWatchTowerButton.transform as RectTransform);
            UpdateHint("📍 Bước 4: Chọn **Tháp Canh** để tăng cường bảo vệ căn cứ.");
        }
    }

    public void OnCivilBuildingPlaced(BuildingType buildingType, Transform placedBuildingTransform = null)
    {
        if (currentStage != TutorialStage.Stage2_CivilBuildings) return;

        isPlacingBuilding = false;

        if (buildingType == BuildingType.WoodCutter) hasBuiltWoodCutter = true;
        if (buildingType == BuildingType.StoneStorage) hasBuiltStoneStorage = true;

        if (placedBuildingTransform != null)
        {
            isWaitingForConstruction = true;
            // 🛠️ SỬA LỖI 3: Ẩn ngón tay đi khi bắt đầu tiến trình xây dựng
            HidePointer(); 
            UpdateHint("⏳ Công trình đang được xây dựng... Vui lòng đợi.");
        }
        else
        {
            CheckStage2Progress();
        }
    }

    public void OnBuildingConstructionFinished(BuildingType buildingType)
    {
        isWaitingForConstruction = false;

        if (currentStage == TutorialStage.Stage2_CivilBuildings)
        {
            CheckStage2Progress();
        }
        else if (currentStage == TutorialStage.Stage4_BuildWatchTower)
        {
            HidePointer();
            UpdateHint("");
            StartCoroutine(StartStage5Routine());
        }
    }

    private void CheckStage2Progress()
    {
        if (!hasBuiltWoodCutter || !hasBuiltStoneStorage)
        {
            ResetStage2Menu();
        }
        else
        {
            HidePointer();
            UpdateHint("");
            StartStage3();
        }
    }

    // ====================================================================
    // GIAI ĐOẠN 3: NÂNG CẤP
    // ====================================================================
    private void StartStage3()
    {
        currentStage = TutorialStage.Stage3_UpgradeWood;

        UpgradeableBuilding woodCutter = null;
        if (TutorialSceneScanner.Ins != null)
        {
            woodCutter = TutorialSceneScanner.Ins.FindPlacedBuilding(BuildingType.WoodCutter);
        }

        RunDialogueSequence(stage3Dialogues, () =>
        {
            if (woodCutter != null && TutorialSceneScanner.Ins != null)
            {
                FocusCameraOn(woodCutter.transform.position, 1.0f);

                bool isUIOpen = TutorialSceneScanner.Ins.IsBuildingUIOpen(woodCutter);
                RectTransform upgradeBtnRect = TutorialSceneScanner.Ins.GetUpgradeButtonTransform(woodCutter);

                if (isUIOpen && upgradeBtnRect != null)
                {
                    PointHandAtUI(upgradeBtnRect);
                }
                else
                {
                    PointHandAt(woodCutter.transform.position);
                }
            }
            
            SetButtonInteractable(upgradeBuildingButton, true);
            UpdateHint("📍 Bước 3: Nhấn vào **Nhà Khai Thác Gỗ** và chọn **Nâng Cấp**.");
        });
    }

    public void OnBuildingUpgraded(UpgradeableBuilding building)
    {
        if (currentStage != TutorialStage.Stage3_UpgradeWood) return;

        if (building != null && building.buildingType == BuildingType.WoodCutter)
        {
            HidePointer();
            StartStage4();
        }
    }

    // ====================================================================
    // GIAI ĐOẠN 4 & 5 & 6
    // ====================================================================
    private void StartStage4()
    {
        currentStage = TutorialStage.Stage4_BuildWatchTower;

        Transform enemyCamp = null;
        if (TutorialSceneScanner.Ins != null)
        {
            enemyCamp = TutorialSceneScanner.Ins.GetEnemyCampTransform();
        }

        RunDialogueSequence(stage4Dialogues, () =>
        {
            if (enemyCamp != null)
            {
                FocusCameraOn(enemyCamp.position, 1.5f);
                PointHandAt(enemyCamp.position);
                UpdateHint("⚠️ Phát hiện căn cứ kẻ thù lân cận! Hãy chuẩn bị tháp canh phòng thủ.");
                Invoke(nameof(ResetStage4Menu), 2.5f);
            }
            else
            {
                ResetStage4Menu();
            }
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
        UpdateHint("📍 Bước 4: Mở **Cửa Hàng Xây Dựng** để chuẩn bị phòng thủ.");
    }

    public void OnDefenseBuildingPlaced(BuildingType buildingType, Transform placedBuildingTransform = null)
    {
        if (currentStage != TutorialStage.Stage4_BuildWatchTower) return;

        if (buildingType == BuildingType.WatchTower)
        {
            hasBuiltWatchTower = true;
            isPlacingBuilding = false;

            if (placedBuildingTransform != null)
            {
                isWaitingForConstruction = true;
                // 🛠️ SỬA LỖI 3: Ẩn ngón tay đi khi bắt đầu xây dựng tháp
                HidePointer(); 
                UpdateHint("⏳ Tháp canh đang được xây dựng...");
            }
            else
            {
                HidePointer();
                UpdateHint("");
                StartCoroutine(StartStage5Routine());
            }
        }
    }

    private IEnumerator StartStage5Routine()
    {
        currentStage = TutorialStage.Stage5_EnemyWave;

        if (townHallTransform != null)
        {
            FocusCameraOn(townHallTransform.position, 1.0f);
        }

        bool dialogueDone = false;
        RunDialogueSequence(stage5WarningDialogues, () => { dialogueDone = true; });
        while (!dialogueDone) yield return null;

        enemiesRemaining = tutorialEnemyCount;
        
        if (enemySpawner != null)
        {
            for (int i = 0; i < tutorialEnemyCount; i++)
            {
                enemySpawner.SpawnEnemy(); 
            }

            EnemyAI[] activeEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (var ai in activeEnemies)
            {
                if (ai != null && townHallTransform != null)
                {
                    ai.villageCenter = townHallTransform;
                    ai.attackMainDirectly = true;
                }
            }
        }

        UnlockAllInputs();
    }

    public void OnEnemyKilled()
    {
        if (currentStage != TutorialStage.Stage5_EnemyWave) return;

        enemiesRemaining--;
        if (enemiesRemaining <= 0)
        {
            StartStage6();
        }
    }

    private void StartStage6()
    {
        currentStage = TutorialStage.Stage6_Complete;

        RunDialogueSequence(stage6CompleteDialogues, () =>
        {
            if (JsonDataManager.Ins != null)
            {
                JsonDataManager.Ins.AddWood(500);
                JsonDataManager.Ins.AddStone(500);
                JsonDataManager.Ins.BroadcastAllResources();
            }

            HidePointer();
            UpdateHint("🎉 Bạn đã hoàn thành Tutorial! Nhận **500 Gỗ & 500 Đá** tân thủ.");
            UnlockAllInputs();
        });
    }

    // ====================================================================
    // CÁC HÀM Bổ Trợ Quản Lý Bàn Tay & UI
    // ====================================================================
    public void OnStartPlacement()
    {
        isPlacingBuilding = true;
        HidePointer();
        UpdateHint("📍 Hãy chọn vị trí thích hợp trên bản đồ để **Đặt Công Trình**.");
    }

    public void OnCancelPlacement()
    {
        isPlacingBuilding = false;

        if (currentStage == TutorialStage.Stage2_CivilBuildings)
            ResetStage2Menu();
        else if (currentStage == TutorialStage.Stage4_BuildWatchTower)
            ResetStage4Menu();
    }

    private void UpdateHandPointerAnimation()
    {
        if (handPointer == null || !handPointer.activeSelf || pointerRect == null) return;

        Vector2 currentPos = pointerRect.position;
        Vector2 smoothedPos = Vector2.Lerp(currentPos, targetScreenPosition, Time.unscaledDeltaTime * pointerMoveSpeed);

        float bobbingOffset = Mathf.Sin(Time.unscaledTime * bobbingSpeed) * bobbingAmount;
        Vector2 finalPos = smoothedPos + new Vector2(bobbingOffset * 0.5f, bobbingOffset);

        pointerRect.position = finalPos;
    }

    private void UpdateHighlightRingAnimation()
    {
        if (highlightRing == null || !highlightRing.gameObject.activeSelf) return;

        highlightRing.Rotate(Vector3.forward, -90f * Time.unscaledDeltaTime);
        float pulseScale = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.08f;
        highlightRing.localScale = Vector3.one * pulseScale;
    }

    private void PointHandAt(Vector3 worldPos)
    {
        if (handPointer == null) return;
        currentTargetUI = null;
        handPointer.SetActive(true);

        Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : Vector3.zero;
        targetScreenPosition = (Vector2)screenPos + pointerOffset;

        if (highlightRing != null) highlightRing.gameObject.SetActive(false);
    }

    private void PointHandAtUI(RectTransform uiRect)
    {
        if (handPointer == null || uiRect == null) return;

        // 🛠️ SỬA LỖI 2: Ép Canvas cập nhật ngay lập tức Layout trước khi tính tọa độ
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(uiRect);

        currentTargetUI = uiRect;
        handPointer.SetActive(true);

        Canvas parentCanvas = uiRect.GetComponentInParent<Canvas>();
        Vector2 screenPoint;

        if (parentCanvas != null && (parentCanvas.renderMode == RenderMode.WorldSpace || parentCanvas.renderMode == RenderMode.ScreenSpaceCamera))
        {
            Camera cam = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
            screenPoint = RectTransformUtility.WorldToScreenPoint(cam, uiRect.position);
        }
        else
        {
            screenPoint = uiRect.position;
        }

        targetScreenPosition = screenPoint + pointerOffset;

        if (highlightRing != null)
        {
            highlightRing.gameObject.SetActive(true);
            highlightRing.position = screenPoint;
        }
    }

    private void HidePointer()
    {
        currentTargetUI = null;
        if (handPointer != null) handPointer.SetActive(false);
        if (highlightRing != null) highlightRing.gameObject.SetActive(false);
    }

    private void RunDialogueSequence(DialogueData[] dialogues, System.Action onComplete = null)
    {
        if (NPCDialogueUI.Ins != null)
            NPCDialogueUI.Ins.ShowDialogueSequence(dialogues, onComplete);
        else
            onComplete?.Invoke();
    }

    private void FocusCameraOn(Vector3 targetWorldPos, float duration)
    {
        if (Camera.main == null) return;

        if (cameraFocusCoroutine != null) StopCoroutine(cameraFocusCoroutine);
        cameraFocusCoroutine = StartCoroutine(AnimateCameraFocus(targetWorldPos, duration));
    }

    private IEnumerator AnimateCameraFocus(Vector3 targetPos, float duration)
    {
        Transform camTrans = Camera.main.transform;
        Vector3 startPos = camTrans.position;
        Vector3 endPos = new Vector3(targetPos.x, camTrans.position.y, targetPos.z - 8f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            camTrans.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
    }

    private IEnumerator AnimateTextPop(RectTransform textRect)
    {
        if (textRect == null) yield break;

        textRect.localScale = Vector3.zero;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.15f;
            textRect.localScale = Vector3.one * scale;
            yield return null;
        }
        textRect.localScale = Vector3.one;
    }

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
            if (hintText.gameObject.activeSelf)
            {
                StartCoroutine(AnimateTextPop(hintText.rectTransform));
            }
        }
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
}