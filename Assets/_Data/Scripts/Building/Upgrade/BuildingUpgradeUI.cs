using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/*
 * BuildingUpgradeUI.cs
 * Dự án: KHẨN HOANG (PENTA DEV)
 * CHỨC NĂNG: Quản lý Upgrade UI hình tròn dưới chân công trình
 * - Gắn trực tiếp trên Object Công Trình (Cha)
 * - Tự động xòe nút theo vòng tròn (Radial Layout)
 * - Hiệu ứng nảy/xòe UI mượt mà khi bật (Pop Animation)
 * - Tự động ẩn khi người chơi click/chạm ra ngoài
 * - GIỮ NGUYÊN KÍCH THƯỚC UI: Bù scale cho Panel con khi Camera xa/gần
 * - CHỐNG XOAY UI: Khóa góc xoay của Panel con, không làm xoay công trình cha
 */

public class BuildingUpgradeUI : MonoBehaviour
{   
    public bool IsOpen => localUpgradePanel != null && localUpgradePanel.activeSelf;
    
    [Header("UI Canvas & Panel")]
    [SerializeField] private GameObject localUpgradePanel;
    [SerializeField] private RectTransform panelRectTransform;

    [Header("CHỐNG XOAY & GIỮ KÍCH THƯỚC (CHỈ ÁP DỤNG PANLE CON)")]
    [SerializeField] private bool lockUIRotation = true;       // Khóa không cho Panel UI con xoay theo nhà
    [SerializeField] private bool faceCamera = true;          // True: UI con luôn nhìn về Cam | False: Đứng thẳng trục World
    [SerializeField] private bool keepConstantSize = true;    // True: UI con không bị bé đi khi Cam xa
    [SerializeField] private float baseDistance = 15f;          // Khoảng cách chuẩn giữa Cam và UI (Dùng cho Cam 3D)
    [SerializeField] private float baseOrthographicSize = 5f;   // Size chuẩn của Camera (Dùng cho Cam 2D)

    [Header("MẸO 1: XẾP NÚT TỰ ĐỘNG THEO VÒNG TRÒN")]
    [SerializeField] private bool autoArrangeButtons = true;
    [SerializeField] private float ringRadius = 110f;       // Bán kính xòe nút (pixel)
    [SerializeField] private float startAngle = 45f;        // Góc xòe nút đầu tiên (45 độ)
    [SerializeField] private float angleStep = 45f;         // Khoảng cách góc giữa các nút

    [Header("MẸO 2: HIỆU ỨNG XÒE/NẢY NÚT (POP ANIMATION)")]
    [SerializeField] private float animDuration = 0.18f;     // Thời gian xòe nút (0.18 giây)

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
    // Thêm 2 thuộc tính này vào trong file BuildingUpgradeUI.cs (đặt ở khu vực public)
    public Button UpgradeButton => upgradeButton;
    public GameObject LocalUpgradePanel => localUpgradePanel;

    private void Awake()
    {
        building = GetComponentInParent<UpgradeableBuilding>();

        if (panelRectTransform == null && localUpgradePanel != null)
        {
            panelRectTransform = localUpgradePanel.GetComponent<RectTransform>();
        }

        // Đăng ký sự kiện nút
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnClickUpgrade);
        if (repairButton != null) repairButton.onClick.AddListener(OnClickRepair);
        if (moveButton != null) moveButton.onClick.AddListener(OnClickMove);

        CloseUI();
    }

    private void Update()
    {
        if (localUpgradePanel == null || !localUpgradePanel.activeSelf) return;

        // 1. Chỉ khóa xoay riêng panelRectTransform con, không đụng vào transform cha
        UpdateUIRotation();

        // 2. Chỉ bù Scale riêng panelRectTransform con
        UpdateUIScale();

        // Bỏ qua frame đầu tiên vừa bấm mở
        if (isOpeningFrame)
        {
            isOpeningFrame = false;
            return;
        }

        // Click ra ngoài vòng tròn để tắt UI
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverPanel())
            {
                CloseUI();
            }
        }
    }

    /// <summary>
    /// Triệt tiêu góc xoay của nhà cha tác động lên Panel UI con
    /// </summary>
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

    /// <summary>
    /// Bù trừ Scale liên tục riêng cho Panel UI con
    /// </summary>
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

        panelRectTransform.localScale = Vector3.one * (distanceFactor * currentAnimScale);
    }

    public void OpenUI()
    {
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
                ArrangeButtonsInCircle();
            }

            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateOpenUI());
        }
    }

    public void CloseUI()
    {
        if (localUpgradePanel != null)
        {
            localUpgradePanel.SetActive(false);
        }
        upgradeClickCount = 0;
    }

    private void ArrangeButtonsInCircle()
    {
        List<RectTransform> activeButtons = new List<RectTransform>();

        if (upgradeButton != null && upgradeButton.gameObject.activeSelf)
            activeButtons.Add(upgradeButton.GetComponent<RectTransform>());

        if (repairButton != null && repairButton.gameObject.activeSelf)
            activeButtons.Add(repairButton.GetComponent<RectTransform>());

        if (moveButton != null && moveButton.gameObject.activeSelf)
            activeButtons.Add(moveButton.GetComponent<RectTransform>());

        for (int i = 0; i < activeButtons.Count; i++)
        {
            float currentAngleDegree = startAngle + (i * angleStep);
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
}