using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * BuildingProgressBarUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * NHIỆM VỤ: Quản lý ĐỘC LẬP TỪNG THANH TIẾN ĐỘ riêng biệt cho từng công trình khi gán qua Inspector.
 */
public class BuildingProgressBarUI : MonoBehaviour
{
    [Header("[Cấu Hình Thành Phần UI]")]
    public Slider upgradeProgressBar;       
    public TMP_Text upgradeTimerText;       

    // Lưu vết công trình sở hữu thanh UI này để phân biệt độc lập
    private UpgradeableBuilding _ownerBuilding;

    private void Awake()
    {
        // Tự động tìm xem mình đang nằm trong lòng công trình nào để đăng ký chính chủ
        _ownerBuilding = GetComponentInParent<UpgradeableBuilding>();
        
        // Ẩn các thành phần con đi lúc đầu, giữ Object cha hoạt động
        HideProgress();
    }

    private void OnEnable()
    {
        // Khi Object được kích hoạt, đăng ký cổng nhận tín hiệu riêng cho nhà này
        if (_ownerBuilding != null)
        {
            BuildingProgressBridge.RegisterUI(_ownerBuilding, this);
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký khi bị tắt để tránh rác bộ nhớ
        if (_ownerBuilding != null)
        {
            BuildingProgressBridge.UnregisterUI(_ownerBuilding);
        }
    }

    /// <summary>
    /// Cập nhật giá trị thanh tiến độ và thời gian đếm ngược thời gian thực (Độc lập từng nhà)
    /// </summary>
    public void UpdateProgress(float currentTimer, float totalDuration)
    {
        if (upgradeProgressBar != null && !upgradeProgressBar.gameObject.activeSelf) 
            upgradeProgressBar.gameObject.SetActive(true);

        if (upgradeTimerText != null && !upgradeTimerText.gameObject.activeSelf) 
            upgradeTimerText.gameObject.SetActive(true);

        if (upgradeProgressBar != null)
        {
            upgradeProgressBar.maxValue = totalDuration;
            upgradeProgressBar.value = currentTimer;
        }

        if (upgradeTimerText != null)
        {
            float timeLeft = Mathf.Max(0f, totalDuration - currentTimer);
            upgradeTimerText.text = $"{timeLeft:F1}s"; 
        }
    }

    /// <summary>
    /// Ẩn các thành phần hiển thị, giữ nguyên Object cha sống
    /// </summary>
    public void HideProgress()
    {
        if (upgradeProgressBar != null) upgradeProgressBar.gameObject.SetActive(false);
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
    }
}

// =================================================================================
// HỆ THỐNG CẦU NỐI PHÂN LUỒNG (BRIDGE DATA) - GIÚP CHUYỂN HƯỚNG CHÍNH XÁC TỪNG CÔNG TRÌNH
// =================================================================================
public static class BuildingProgressBridge
{
    private static System.Collections.Generic.Dictionary<UpgradeableBuilding, BuildingProgressBarUI> _uiRegistry = 
        new System.Collections.Generic.Dictionary<UpgradeableBuilding, BuildingProgressBarUI>();

    public static void RegisterUI(UpgradeableBuilding building, BuildingProgressBarUI ui)
    {
        if (!_uiRegistry.ContainsKey(building)) _uiRegistry.Add(building, ui);
        else _uiRegistry[building] = ui;
    }

    public static void UnregisterUI(UpgradeableBuilding building)
    {
        if (_uiRegistry.ContainsKey(building)) _uiRegistry.Remove(building);
    }

    public static BuildingProgressBarUI GetUI(UpgradeableBuilding building)
    {
        if (building != null && _uiRegistry.TryGetValue(building, out var ui)) return ui;
        return null;
    }
}

// =================================================================================
// ĐÈ LÊN EXTENSION CŨ: Ép UIManager định tuyến chuẩn xác dựa vào "Nhà đang được chọn"
// =================================================================================
public static class UIManagerExtensions
{
    public static void UpdateUpgradeProgress(this UIManager uiManager, float currentTimer, float totalDuration)
    {
        // 1. Lấy ra nhà đang thực sự chạy tiến trình đếm ngược dựa vào luồng Coroutine của UpgradeableBuilding
        // Hệ thống sẽ quét xem có thanh UI nào đăng ký chính chủ với nhà đó không
        var allBuildings = Object.FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
        foreach (var building in allBuildings)
        {
            if (building.IsUpgrading)
            {
                var targetUI = BuildingProgressBridge.GetUI(building);
                if (targetUI != null)
                {
                    targetUI.UpdateProgress(currentTimer, totalDuration);
                }
            }
        }
    }

    public static void HideUpgradeProgress(this UIManager uiManager)
    {
        // Ẩn tất cả các thanh đơn lẻ của từng nhà khi hoàn tất
        var allUIs = Object.FindObjectsByType<BuildingProgressBarUI>(FindObjectsSortMode.None);
        foreach (var ui in allUIs)
        {
            ui.HideProgress();
        }
    }
}