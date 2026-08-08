using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * BuildingUpgradeSidePanelUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Phong cách: Demacia Rising Side-by-Side Building Upgrade & Details Panel
 */

public class BuildingUpgradeSidePanelUI : MonoBehaviour
{
    public static BuildingUpgradeSidePanelUI Ins { get; private set; }

    [Header("=== THÀNH PHẦN HEADER & ĐÓNG PANEL ===")]
    [SerializeField] private TextMeshProUGUI levelBadgeTMP; // VD: "Lv. 1"
    [SerializeField] private Button closeBtn;

    [Header("=== THÔNG TIN VÀ ẢNH MINH HỌA ===")]
    [SerializeField] private TextMeshProUGUI buildingNameTMP; // VD: "Lumberyard" / "Trại Mộc"
    [SerializeField] private Image artworkImage;

    [Header("=== SO SÁNH CHỈ SỐ CẤP HIỆN TẠI & CẤP TIẾP THEO ===")]
    [SerializeField] private TextMeshProUGUI currentLevelStatTMP; // VD: "15 Lumber per turn"
    [SerializeField] private TextMeshProUGUI nextLevelStatTMP;    // VD: "30 Lumber per turn"

    [Header("=== CẢNH BÁO VÀ CHI PHÍ NÂNG CẤP ===")]
    [SerializeField] private TextMeshProUGUI warningNoticeTMP;    // VD: "Must upgrade Settlement"
    [SerializeField] private TextMeshProUGUI buildDurationTMP;    // VD: "2" (icon ⏳)
    [SerializeField] private TextMeshProUGUI woodCostTMP;
    [SerializeField] private TextMeshProUGUI stoneCostTMP;
    [SerializeField] private TextMeshProUGUI foodCostTMP;

    [Header("=== CÁC NÚT THAO TÁC ===")]
    [SerializeField] private Button upgradeBtn;  // Nút 🔨 UPGRADE
    [SerializeField] private Button demolishBtn; // Nút ❌ Phá dỡ

    [Header("=== TÙY CHỈNH MÀU CHI PHÍ ===")]
    [SerializeField] private Color affordableColor = new Color(0.2f, 0.9f, 0.3f, 1f);   // Xanh lá
    [SerializeField] private Color unaffordableColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Đỏ

    private UpgradeableBuilding targetBuilding;

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (closeBtn != null) closeBtn.onClick.AddListener(ClosePanel);
        if (upgradeBtn != null) upgradeBtn.onClick.AddListener(OnClickUpgrade);
        if (demolishBtn != null) demolishBtn.onClick.AddListener(OnClickDemolish);
    }

    /// <summary>
    /// Hiển thị Bảng Nâng Cấp liền kề bên phải Panel Thủ Đô
    /// </summary>
    public void ShowUpgradePanel(UpgradeableBuilding building)
    {
        if (building == null) return;

        targetBuilding = building;
        gameObject.SetActive(true);

        RefreshPanel();
    }

    /// <summary>
    /// Làm mới toàn bộ thông số, so sánh chỉ số và chi phí tài nguyên
    /// </summary>
    public void RefreshPanel()
    {
        if (targetBuilding == null) return;

        int currentLevel = targetBuilding.CurrentLevel + 1;
        int maxLevel = targetBuilding.MaxLevel;
        bool isMaxLevel = currentLevel >= maxLevel;

        // 1. Header & Tên nhà
        if (levelBadgeTMP != null) levelBadgeTMP.text = $"Lv. {currentLevel}";
        if (buildingNameTMP != null) buildingNameTMP.text = targetBuilding.buildingName;

        // 2. Ảnh Art Preview (nếu có Sprite)
        if (artworkImage != null)
        {
            var rend = targetBuilding.GetComponentInChildren<SpriteRenderer>();
            if (rend != null && rend.sprite != null)
            {
                artworkImage.sprite = rend.sprite;
                artworkImage.gameObject.SetActive(true);
            }
        }

        // 3. So sánh Chỉ số Cấp hiện tại & Cấp tiếp theo
        if (currentLevelStatTMP != null)
        {
            currentLevelStatTMP.text = $"Cấp {currentLevel}: Sản lượng tối ưu";
        }

        if (nextLevelStatTMP != null)
        {
            if (isMaxLevel)
            {
                nextLevelStatTMP.text = "ĐÃ ĐẠT CẤP TỐI ĐA (MAX)";
            }
            else
            {
                nextLevelStatTMP.text = $"Cấp {currentLevel + 1}: +100% Sản lượng & Độ bền";
            }
        }

        // 4. Lấy chi phí nâng cấp từ ConstructionManager
        int woodCost = 0, stoneCost = 0, foodCost = 0;
        if (ConstructionManager.Ins != null)
        {
            var costData = ConstructionManager.Ins.GetBuildingCost(targetBuilding.buildingType);
            woodCost = Mathf.RoundToInt(costData.woodCost * 1.5f);
            stoneCost = Mathf.RoundToInt(costData.stoneCost * 1.5f);
            foodCost = Mathf.RoundToInt(costData.foodCost * 1.5f);
        }

        // 5. Kiểm tra tài nguyên & Cấp Thủ đô
        bool hasEnoughWood = true, hasEnoughStone = true, hasEnoughFood = true;
        bool canAfford = true;

        if (JsonDataManager.Ins != null)
        {
            hasEnoughWood = JsonDataManager.Ins.wood >= woodCost;
            hasEnoughStone = JsonDataManager.Ins.stone >= stoneCost;
            hasEnoughFood = JsonDataManager.Ins.food >= foodCost;
            canAfford = JsonDataManager.Ins.HasEnoughResources(woodCost, stoneCost, foodCost);
        }

        // Cảnh báo Cấp Thủ Đô (Nếu có SettlementSidePanelUI)
        bool settlementLevelOk = true;
        if (SettlementSidePanelUI.Ins != null)
        {
            // Cần cấp thủ đô >= cấp công trình muốn nâng
            // settlementLevelOk = SettlementSidePanelUI.Ins.SettlementLevel >= currentLevel;
        }

        if (warningNoticeTMP != null)
        {
            if (!settlementLevelOk)
            {
                warningNoticeTMP.text = "Cần nâng cấp Thủ Đô trước!";
                warningNoticeTMP.gameObject.SetActive(true);
            }
            else if (!canAfford)
            {
                warningNoticeTMP.text = "Không đủ tài nguyên nâng cấp!";
                warningNoticeTMP.gameObject.SetActive(true);
            }
            else
            {
                warningNoticeTMP.gameObject.SetActive(false);
            }
        }

        // Cập nhật chữ chi phí
        if (woodCostTMP != null)
        {
            woodCostTMP.text = woodCost.ToString();
            woodCostTMP.color = hasEnoughWood ? affordableColor : unaffordableColor;
        }

        if (stoneCostTMP != null)
        {
            stoneCostTMP.text = stoneCost.ToString();
            stoneCostTMP.color = hasEnoughStone ? affordableColor : unaffordableColor;
        }

        if (foodCostTMP != null)
        {
            foodCostTMP.text = foodCost.ToString();
            foodCostTMP.color = hasEnoughFood ? affordableColor : unaffordableColor;
        }

        if (buildDurationTMP != null)
        {
            buildDurationTMP.text = "2"; // 2 đợt / 2 lượt
        }

        // Cập nhật trạng thái nút Nâng Cấp
        if (upgradeBtn != null)
        {
            upgradeBtn.interactable = canAfford && settlementLevelOk && !isMaxLevel;
        }
    }

    private void OnClickUpgrade()
    {
        if (targetBuilding == null) return;

        targetBuilding.Upgrade();
        RefreshPanel();

        if (SettlementSidePanelUI.Ins != null)
        {
            SettlementSidePanelUI.Ins.RefreshPanel();
        }
    }

    private void OnClickDemolish()
    {
        if (targetBuilding == null) return;

        Destroy(targetBuilding.gameObject);
        ClosePanel();

        if (SettlementSidePanelUI.Ins != null)
        {
            SettlementSidePanelUI.Ins.RefreshPanel();
        }
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
