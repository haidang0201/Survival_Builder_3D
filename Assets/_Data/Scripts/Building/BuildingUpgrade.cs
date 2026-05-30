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

        if (buildingNameText != null)
            buildingNameText.text = building.buildingName;

        if (levelText != null)
            levelText.text = $"Cấp {displayLevel} / {building.MaxLevel}";

        if (upgradeButton != null)
            upgradeButton.interactable = !isMaxLevel;

        if (upgradeButtonText != null)
            upgradeButtonText.text = isMaxLevel ? "Đã tối đa" : "Nâng cấp";
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
    private void OnClickUpgradeButton()
    {
        if (selectedBuilding == null)
        {
            Debug.LogWarning("[MyBuildingUpgradeModule] Không có building được chọn!");
            return;
        }

        selectedBuilding.NextLevel();
        RefreshUpgradePanel(selectedBuilding);
    }
}