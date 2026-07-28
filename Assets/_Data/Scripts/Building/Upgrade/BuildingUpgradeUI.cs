using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/*
 * BuildingUpgradeUI.cs
 * Dự án: KHẨN HOANG (PENTA DEV)
 * CHỨC NĂNG: Quản lý Upgrade UI hình tròn dưới chân công trình
 * - Tự động xòe nút theo vòng tròn (Radial Layout)
 * - Hiệu ứng nảy/xòe UI mượt mà khi bật (Pop Animation)
 * - Tự động ẩn khi người chơi click/chạm ra ngoài
 */

public class BuildingUpgradeUI : MonoBehaviour
{   
    // Thêm dòng này vào trong class BuildingUpgradeUI
    public bool IsOpen => localUpgradePanel != null && localUpgradePanel.activeSelf;
    [Header("UI Canvas & Panel")]
    [SerializeField] private GameObject localUpgradePanel;
    [SerializeField] private RectTransform panelRectTransform;

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

        // Bỏ qua frame đầu tiên vừa bấm mở
        if (isOpeningFrame)
        {
            isOpeningFrame = false;
            return;
        }

        // MẸO 3: CLICK RA NGOÀI VÒNG TRÒN ĐỂ TẮT UI
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverPanel())
            {
                CloseUI();
            }
        }
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

            // Tự động xếp vị trí các nút theo vòng tròn nếu bật
            if (autoArrangeButtons)
            {
                ArrangeButtonsInCircle();
            }

            // Chạy hiệu ứng xòe nút nảy lên
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

    /// <summary>
    /// Mẹo: Tự động tính toán vị trí X, Y cho các nút xòe đều xung quanh tâm vòng tròn
    /// </summary>
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
            // Tính góc xoay bằng radian
            float currentAngleDegree = startAngle + (i * angleStep);
            float angleRad = currentAngleDegree * Mathf.Deg2Rad;

            // Tọa độ X = R * cos(a), Y = R * sin(a)
            float x = ringRadius * Mathf.Cos(angleRad);
            float y = ringRadius * Mathf.Sin(angleRad);

            activeButtons[i].anchoredPosition = new Vector2(x, y);
        }
    }

    /// <summary>
    /// Mẹo: Hiệu ứng Scale biến panel từ nhỏ bung ra lớn có độ nảy (Elastic Pop)
    /// </summary>
    private IEnumerator AnimateOpenUI()
    {
        panelRectTransform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;

            // Công thức Ease-Out Overshoot (Bung ra quá 100% một chút rồi nảy về chuẩn)
            float scaleValue = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.1f;
            if (t >= 0.8f) scaleValue = Mathf.Lerp(1.1f, 1.0f, (t - 0.8f) / 0.2f);

            panelRectTransform.localScale = Vector3.one * scaleValue;
            yield return null;
        }

        panelRectTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// Kiểm tra xem ngón tay/chuột có đang nằm trong khung tròn Panel hay không
    /// </summary>
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

    // private void OnMouseDown()
    // {
    //     // Gọi thẳng UI lên khi click vào công trình
    //     BuildingUpgradeUI localUI = GetComponentInChildren<BuildingUpgradeUI>(true);

    //     if (localUI != null)
    //     {
    //         Debug.Log($"[OK] Đã tìm thấy UI trên {gameObject.name} -> Đang gọi OpenUI()...");
    //         localUI.OpenUI();
    //     }
    //     else
    //     {
    //         Debug.LogError($"[LỖI] Không tìm thấy script BuildingUpgradeUI trong các Object con của {gameObject.name}!");
    //     }
    // }
}