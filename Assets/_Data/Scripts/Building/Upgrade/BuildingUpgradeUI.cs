using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
/*
 * BuildingUpgradeUI.cs
 * Dự án: KHẨN HOANG (PENTA DEV)
 * BẢN FIX TOÀN DIỆN:
 * 1. FIX LỖI MỞ NHIỀU UI: Dùng static CurrentlyOpenUI để tự động đóng UI công trình cũ khi chọn công trình mới.
 * 2. FIX LỖI ĐÈ CHỮ: Tự động tính toán khoảng cách Y giữa Tên nhà, Level và Chi phí.
 * 3. FIX LỖI SCALE & XOAY: Giữ nguyên khả năng triệt tiêu Scale cha và xếp nút 360 độ.
 */

public class BuildingUpgradeUI : MonoBehaviour
{   
    // QUẢN LÝ CHỈ MỞ 1 UI DUY NHẤT TRÊN TOÀN SCENE
    public static BuildingUpgradeUI CurrentlyOpenUI { get; private set; }

    public bool IsOpen => localUpgradePanel != null && localUpgradePanel.activeSelf;
    
    [Header("UI Canvas & Panel")]
    [SerializeField] private GameObject localUpgradePanel;
    [SerializeField] private RectTransform panelRectTransform;

    [Header("CHỐNG XOAY & GIỮ KÍCH THƯỚC (FIX SCALE LỆCH)")]
    [SerializeField] private bool lockUIRotation = true;
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool keepConstantSize = true;
    [SerializeField] private float baseDistance = 15f;
    [SerializeField] private float baseOrthographicSize = 5f;
    [Tooltip("Scale chuẩn của nút UI trong không gian World")]
    [SerializeField] private float globalUIScaleMultiplier = 1.0f;

    [Header("TỰ ĐỘNG XẾP NÚT HÌNH TRÒN 360 ĐỘ")]
    [SerializeField] private bool autoArrangeButtons = true;
    [SerializeField] private float ringRadius = 120f;       // Bán kính xòe nút (pixel)
    [SerializeField] private float startAngle = 90f;        // Góc xòe nút đầu tiên

    [Header("CẤU HÌNH TÍCH MỞ CHIÊU MỘ")]
    [Tooltip("Tích vào đây nếu muốn công trình này có Nút Chiêu Mộ Dân Làng")]
    public bool enableWorkerSpawn = false; 
    [SerializeField] private Button spawnWorkerButton;

    [Header("HIỆU ỨNG POP ANIMATION")]
    [SerializeField] private float animDuration = 0.18f;

    [Header("Information Elements")]
    [SerializeField] private TMP_Text buildingNameText;
    [SerializeField] private TMP_Text levelText;

    [Header("Buttons")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private Button repairButton;
    [SerializeField] private Button moveButton;

    [Header("Cost Texts")]
    [SerializeField] private TMP_Text woodCostText;
    [SerializeField] private TMP_Text stoneCostText;
    [SerializeField] private TMP_Text foodCostText;

    private UpgradeableBuilding building;
    private int upgradeClickCount = 0;
    private bool isOpeningFrame = false;
    private Coroutine animCoroutine;
    private float currentAnimScale = 1f;

    public Button UpgradeButton => upgradeButton;
    public GameObject LocalUpgradePanel => localUpgradePanel;

    private void Awake()
    {
        building = GetComponentInParent<UpgradeableBuilding>();
        if (building == null) building = GetComponent<UpgradeableBuilding>();

        if (panelRectTransform == null && localUpgradePanel != null)
        {
            panelRectTransform = localUpgradePanel.GetComponent<RectTransform>();
        }

        // Đăng ký sự kiện nút
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnClickUpgrade);
        if (repairButton != null) repairButton.onClick.AddListener(OnClickRepair);
        if (moveButton != null) moveButton.onClick.AddListener(OnClickMove);
        if (spawnWorkerButton != null) spawnWorkerButton.onClick.AddListener(OnClickToggleSpawnPanel);

        // Tự động phân chia vị trí chữ tránh đè lên nhau
        AutoFixTextLayout();

        CloseUI();
    }

   private void Update()
    {
        if (localUpgradePanel == null || !localUpgradePanel.activeSelf) return;

        UpdateUIRotation();
        UpdateUIScale();

        if (isOpeningFrame)
        {
            isOpeningFrame = false;
            return;
        }

        // CLICK RA NGOÀI ĐỂ ĐÓNG UI
        if (Input.GetMouseButtonDown(0) && !IsPointerOverPanel())
        {
            // BỔ SUNG CHECK NÀY: Nếu con chuột đang bấm vào BẤT KỲ UI NÀO KHÁC (như Shop UI)
            // thì KHÔNG ĐÓNG Upgrade Panel!
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            CloseUI();
        }
    }

    private void UpdateUIRotation()
    {
        if (!lockUIRotation || panelRectTransform == null) return;

        if (faceCamera && Camera.main != null)
        {
            panelRectTransform.rotation = Camera.main.transform.rotation;
        }
        else
        {
            panelRectTransform.rotation = Quaternion.identity;
        }
    }

    private void UpdateUIScale()
    {
        if (!keepConstantSize || Camera.main == null || panelRectTransform == null) return;

        Camera cam = Camera.main;
        float distanceFactor = 1f;

        if (cam.orthographic)
        {
            distanceFactor = cam.orthographicSize / baseOrthographicSize;
        }
        else
        {
            float distance = Vector3.Distance(cam.transform.position, panelRectTransform.position);
            distanceFactor = distance / baseDistance;
        }

        float targetScale = distanceFactor * currentAnimScale * globalUIScaleMultiplier;

        // Triệt tiêu Transform Scale của nhà cha (Lossy Scale)
        Vector3 parentLossyScale = transform.lossyScale;
        float parentX = (parentLossyScale.x > 0.0001f) ? parentLossyScale.x : 1f;
        float parentY = (parentLossyScale.y > 0.0001f) ? parentLossyScale.y : 1f;
        float parentZ = (parentLossyScale.z > 0.0001f) ? parentLossyScale.z : 1f;

        panelRectTransform.localScale = new Vector3(
            targetScale / parentX,
            targetScale / parentY,
            targetScale / parentZ
        );
    }

    /// <summary>
    /// Tự động căn chỉnh vị trí các dòng chữ từ trên xuống dưới để KHÔNG BAO GIỜ BỊ ĐÈ CHỮ
    /// </summary>
    private void AutoFixTextLayout()
    {
        // 1. Căn Tên Nhà lên trên cùng
        if (buildingNameText != null)
        {
            RectTransform rect = buildingNameText.rectTransform;
            rect.anchoredPosition = new Vector2(0, 30f);
            buildingNameText.alignment = TextAlignmentOptions.Center;
        }

        // 2. Căn Level ở giữa
        if (levelText != null)
        {
            RectTransform rect = levelText.rectTransform;
            rect.anchoredPosition = new Vector2(0, 5f);
            levelText.alignment = TextAlignmentOptions.Center;
        }

        // 3. Căn Chi phí ở dưới cùng
        if (woodCostText != null && woodCostText.transform.parent != null)
        {
            RectTransform costParentRect = woodCostText.transform.parent.GetComponent<RectTransform>();
            if (costParentRect != null && costParentRect != panelRectTransform)
            {
                costParentRect.anchoredPosition = new Vector2(0, -20f);
            }
        }
    }

    public void OpenUI()
    {
        // FIX LỖI MỞ NHIỀU UI: Tắt UI của công trình khác trước khi mở UI mới
        if (CurrentlyOpenUI != null && CurrentlyOpenUI != this)
        {
            CurrentlyOpenUI.CloseUI();
        }
        CurrentlyOpenUI = this;

        if (UIManager.Ins != null)
        {
            UIManager.Ins.CloseAllPopups();
        }

        if (localUpgradePanel != null)
        {
            localUpgradePanel.SetActive(true);
            isOpeningFrame = true;
            upgradeClickCount = 0;

            RefreshUI();

            if (autoArrangeButtons)
            {
                ArrangeButtonsInFullCircle();
            }

            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateOpenUI());
        }
    }

    public void CloseUI()
    {
        if (CurrentlyOpenUI == this)
        {
            CurrentlyOpenUI = null;
        }

        if (localUpgradePanel != null)
        {
            localUpgradePanel.SetActive(false);
        }
        upgradeClickCount = 0;
    }

    private void ArrangeButtonsInFullCircle()
    {
        List<RectTransform> activeButtons = new List<RectTransform>();

        if (upgradeButton != null && upgradeButton.gameObject.activeSelf)
            activeButtons.Add(upgradeButton.GetComponent<RectTransform>());

        if (repairButton != null && repairButton.gameObject.activeSelf)
            activeButtons.Add(repairButton.GetComponent<RectTransform>());

        if (moveButton != null && moveButton.gameObject.activeSelf)
            activeButtons.Add(moveButton.GetComponent<RectTransform>());

        if (spawnWorkerButton != null)
        {
            spawnWorkerButton.gameObject.SetActive(enableWorkerSpawn);
            if (enableWorkerSpawn)
            {
                activeButtons.Add(spawnWorkerButton.GetComponent<RectTransform>());
            }
        }

        int count = activeButtons.Count;
        if (count == 0) return;

        float angleStep360 = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float currentAngleDegree = startAngle + (i * angleStep360);
            float angleRad = currentAngleDegree * Mathf.Deg2Rad;

            float x = ringRadius * Mathf.Cos(angleRad);
            float y = ringRadius * Mathf.Sin(angleRad);

            activeButtons[i].anchoredPosition = new Vector2(x, y);
        }
    }

    private IEnumerator AnimateOpenUI()
    {
        currentAnimScale = 0f;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;

            float scaleValue = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.1f;
            if (t >= 0.8f) scaleValue = Mathf.Lerp(1.1f, 1.0f, (t - 0.8f) / 0.2f);

            currentAnimScale = scaleValue;
            yield return null;
        }

        currentAnimScale = 1.0f;
    }

    private bool IsPointerOverPanel()
    {
        if (panelRectTransform == null) return false;

        Vector2 mousePosition = Input.mousePosition;
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Camera uiCamera = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) 
            ? null 
            : Camera.main;

        return RectTransformUtility.RectangleContainsScreenPoint(panelRectTransform, mousePosition, uiCamera);
    }

    public void RefreshUI()
    {
        if (building == null) return;

        int displayLevel = building.CurrentLevel + 1;
        bool isMaxLevel = building.CurrentLevel >= building.MaxLevel - 1;
        bool isCurrentlyUpgrading = building.IsUpgrading;

        if (buildingNameText != null) buildingNameText.text = building.buildingName;
        if (levelText != null) levelText.text = $"Lv.{displayLevel}";

        if (building.IsRuined)
        {
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
            if (repairButton != null)
            {
                repairButton.gameObject.SetActive(true);
                repairButton.interactable = !isCurrentlyUpgrading;
            }
        }
        else
        {
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(true);
            if (repairButton != null) repairButton.gameObject.SetActive(false);

            if (upgradeButton != null) upgradeButton.interactable = !isMaxLevel && !isCurrentlyUpgrading;
        }

        if (upgradeButtonText != null)
        {
            if (isMaxLevel) upgradeButtonText.text = "MAX";
            else if (isCurrentlyUpgrading) upgradeButtonText.text = "...";
            else upgradeButtonText.text = (upgradeClickCount == 0) ? "Nâng" : "Duyệt";
        }

        if (isMaxLevel)
        {
            if (woodCostText != null) woodCostText.text = "-";
            if (stoneCostText != null) stoneCostText.text = "-";
            if (foodCostText != null) foodCostText.text = "-";
        }
        else
        {
            UpgradeableBuilding.UpgradeCost cost = building.GetNextUpgradeCost();
            if (woodCostText != null) woodCostText.text = cost.woodCost.ToString();
            if (stoneCostText != null) stoneCostText.text = cost.stoneCost.ToString();
            if (foodCostText != null) foodCostText.text = cost.foodCost.ToString();
        }
    }

    // ================= HANDLERS =================

    private void OnClickUpgrade()
    {
        if (building == null || building.IsUpgrading) return;

        if (upgradeClickCount == 0)
        {
            upgradeClickCount = 1;
            RefreshUI();
            return;
        }

        UpgradeableBuilding.UpgradeCost cost = building.GetNextUpgradeCost();
        if (DialogNPC.Instance != null && !DialogNPC.Instance.Consume(cost.woodCost, cost.foodCost, cost.stoneCost)) return;

        building.StartUpgradeProcess();
        CloseUI();
    }

    private void OnClickRepair()
    {
        if (building == null) return;
        building.StartRepair();
        CloseUI();
    }

    private void OnClickMove()
    {
        if (building == null) return;
        if (BuildingSystem.Ins != null) BuildingSystem.Ins.StartMoving(building);
        CloseUI();
    }

    private void OnClickToggleSpawnPanel()
    {
        HouseSpawnPanel spawnPanel = GetComponentInParent<HouseSpawnPanel>();
        if (spawnPanel == null) spawnPanel = GetComponent<HouseSpawnPanel>();
        if (spawnPanel == null) spawnPanel = FindObjectOfType<HouseSpawnPanel>();

        if (spawnPanel != null)
        {
            spawnPanel.TogglePanel();
        }
        CloseUI();
    }
}