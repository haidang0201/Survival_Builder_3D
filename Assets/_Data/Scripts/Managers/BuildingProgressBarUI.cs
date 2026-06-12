using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * BuildingProgressBarUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * NHIỆM VỤ: Quản lý độc lập thanh tiến độ và thời gian đếm ngược dạng World Space trên đầu công trình.
 */
public class BuildingProgressBarUI : MonoBehaviour
{
    // Sử dụng bộ đôi public hoặc [SerializeField] kèm chỉ định rõ ràng để ép Unity hiển thị lên Inspector
    [Header("[Cấu Hình Thành Phần UI]")]
    [SerializeField] public Slider upgradeProgressBar;       
    [SerializeField] public TMP_Text upgradeTimerText;       

    private void Awake()
    {
        // Ẩn thanh tiến độ đi khi khởi tạo, giữ Object cha hoạt động để gán Inspector không lỗi
        HideProgress();
    }

    /// <summary>
    /// Cập nhật giá trị thanh tiến độ và thời gian đếm ngược thời gian thực
    /// </summary>
    public void UpdateProgress(float currentTimer, float totalDuration)
    {
        // Bật các thành phần hiển thị lên nếu chúng đang bị ẩn
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
    /// Ẩn các thành phần con hiển thị khi nâng cấp xong, GIỮ nguyên Object cha sống
    /// </summary>
    public void HideProgress()
    {
        // Chỉ tắt các thành phần hiển thị bên trong
        if (upgradeProgressBar != null) upgradeProgressBar.gameObject.SetActive(false);
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
    }
}