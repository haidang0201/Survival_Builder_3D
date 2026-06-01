using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Module Upgrade riêng biệt cho prefab MyUpgradeableBuilding.
/// Không sửa UIManager hay BuildingSystem gốc.
/// Drag & Drop vào Unity, gán prefab và UI panel trong Inspector.
/// </summary>
public class BuildingUpgradeModule : MonoBehaviour
{
    [Header("UI Panel Upgrade")]
    [SerializeField] private GameObject upgradePanel;          // Panel chứa UI upgrade
    [SerializeField] private TMP_Text buildingNameText;        // Text hiển thị tên building
    [SerializeField] private TMP_Text levelText;               // Text hiển thị cấp hiện tại
    [SerializeField] private Button upgradeButton;             // Nút nâng cấp
    [SerializeField] private TMP_Text upgradeButtonText;       // Text trên nút

    // ====BỔ SUNG: 3 ô Text hiển thị chi phí yêu cầu nâng cấp===========
    [Header("Upgrade Costs Text Elements")]
    [SerializeField] private TMP_Text woodCostText;
    [SerializeField] private TMP_Text stoneCostText;
    [SerializeField] private TMP_Text foodCostText;
    //====================================================================
    private UpgradeableBuilding selectedBuilding;

    private void Awake()
    {
        // Singleton optional, nếu cần nhiều module khác
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnClickUpgradeButton);

        if (upgradePanel != null)
            upgradePanel.SetActive(false); // ẩn panel lúc start
    }

    /// <summary>
    /// Chọn prefab để mở panel upgrade
    /// </summary>
    public void ShowUpgradePanel(UpgradeableBuilding building)
    {
        if (building == null) return;
        selectedBuilding = building;

        if (upgradePanel != null)
            upgradePanel.SetActive(true);

        RefreshUpgradePanel(building);
    }

    /// <summary>
    /// Cập nhật thông tin UI dựa trên cấp hiện tại của prefab
    /// </summary>
    public void RefreshUpgradePanel(UpgradeableBuilding building)
    {
        if (building == null) return;

        int displayLevel = building.CurrentLevel + 1;
        bool isMaxLevel = building.CurrentLevel >= building.MaxLevel - 1;

        if (buildingNameText != null) buildingNameText.text = building.buildingName;
        if (levelText != null) levelText.text = $"Cấp {displayLevel} / {building.MaxLevel}";
        if (upgradeButton != null) upgradeButton.interactable = !isMaxLevel;
        if (upgradeButtonText != null) upgradeButtonText.text = isMaxLevel ? "Đã tối đa" : "Nâng cấp";

        // ========================================================
        // XỬ LÝ HIỂN THỊ TÀI NGUYÊN CẦN NÂNG CẤP TẠI ĐÂY
        // ========================================================
        if (isMaxLevel)
        {
            // Nếu đã đạt cấp tối đa, hiển thị dấu gạch ngang hoặc chữ "Max"
            if (woodCostText != null) woodCostText.text = "-";
            if (stoneCostText != null) stoneCostText.text = "-";
            if (foodCostText != null) foodCostText.text = "-";
        }
        else
        {
            // Lấy cấu hình chi phí của cấp tiếp theo
            UpgradeableBuilding.UpgradeCost cost = building.GetNextUpgradeCost();

            // Hiển thị con số lên UI
            if (woodCostText != null) woodCostText.text = cost.woodCost.ToString();
            if (stoneCostText != null) stoneCostText.text = cost.stoneCost.ToString();
            if (foodCostText != null) foodCostText.text = cost.foodCost.ToString();

            // TÍNH NĂNG THÊM: Đổi màu chữ (Đỏ nếu thiếu tiền, Trắng nếu đủ tiền)
            if (JsonDataManager.Ins != null)
            {
                if (woodCostText != null) 
                    woodCostText.color = JsonDataManager.Ins.wood >= cost.woodCost ? Color.white : Color.red;

                if (stoneCostText != null) 
                    stoneCostText.color = JsonDataManager.Ins.stone >= cost.stoneCost ? Color.white : Color.red;

                if (foodCostText != null) 
                    foodCostText.color = JsonDataManager.Ins.food >= cost.foodCost ? Color.white : Color.red;
            }
        }
    }

    /// <summary>
    /// Đóng panel upgrade
    /// </summary>
    public void HideUpgradePanel()
    {
        selectedBuilding = null;
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    /// <summary>
    /// Listener nút Upgrade
    /// </summary>
    public void OnClickUpgradeButton()
    {
        if (selectedBuilding == null)
        {
            Debug.LogWarning("[UIManager] Không có building được chọn!");
            return;
        }

        // 1. Kiểm tra xem nhà đã đạt cấp tối đa chưa
        if (selectedBuilding.CurrentLevel >= selectedBuilding.MaxLevel - 1)
        {
            Debug.LogWarning("[UIManager] Công trình đã đạt cấp tối đa!");
            return;
        }

        // 2. Lấy chi phí cần để lên cấp tiếp theo
        UpgradeableBuilding.UpgradeCost cost = selectedBuilding.GetNextUpgradeCost();

        // 3. Sử dụng ResourceManager của bạn để kiểm tra và khấu trừ
        if (ResourceManager.Instance != null)
        {
            if (!ResourceManager.Instance.Consume(cost.woodCost, cost.foodCost, cost.stoneCost))
            {
                // Nếu hụt tài nguyên, dừng hàm tại đây (Màn hình không tăng cấp)
                return; 
            }
        }

        // 4. Trừ tài nguyên thành công -> Tiến hành lên cấp hình ảnh (Code cũ của bạn)
        selectedBuilding.NextLevel();

        // 5. Làm mới lại giao diện Panel nâng cấp để cập nhật chữ "Cấp 2/3" chẳng hạn
        RefreshUpgradePanel(selectedBuilding);
    }
}