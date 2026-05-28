using UnityEngine;
using UnityEngine.UI;
using TMPro; // Đảm bảo giữ nguyên vì font chữ nghiêng của nhóm bạn rất đẹp!

public class TimeUIController : MonoBehaviour
{
    [Header("[1. Cấu Hình Đồng Hồ Đếm Ngược]")]
    [Tooltip("Kéo Text hiển thị thời gian (00:30) trong TimeGroup vào đây")]
    public TextMeshProUGUI clockTextTMP;
    //public Text clockTextLegacy; // Dự phòng nếu dùng UI Text thường

    [Header("[2. Cấu Hình Số Ngày Ở Giữa UI]")]
    [Tooltip("Kéo Object DayText trong CenterGroup vào đây")]
    public TextMeshProUGUI dayCounterTextTMP;
    //public Text dayCounterTextLegacy;

    private void Update()
    {
        // Kiểm tra an toàn hệ thống
        if (DayNightManager.Ins == null) return;

        UpdateGameClockUI();
    }

    private void UpdateGameClockUI()
    {
        // ----------------------------------------------------
        // XỬ LÝ ĐỒNG HỒ ĐẾM NGƯỢC (BAN NGÀY / BAN ĐÊM TỰ ĐỘNG CHUYỂN CHU KỲ)
        // ----------------------------------------------------
        float timeLeft = DayNightManager.Ins.CurrentTimer;
        if (timeLeft < 0) timeLeft = 0; // Chặn số âm khi đổi khung hình

        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);

        // Định dạng chuỗi hiển thị đúng chuẩn MM:SS (Ví dụ: 00:30)
        string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (clockTextTMP != null) clockTextTMP.text = formattedTime;
        //if (clockTextLegacy != null) clockTextLegacy.text = formattedTime;

        // ----------------------------------------------------
        // XỬ LÝ SỐ NGÀY CHÍNH GIỮA MÀN HÌNH (Day 0, Day 1, Day 2...)
        // ----------------------------------------------------
        int currentDayNumber = DayNightManager.Ins.CurrentDay;
        string formattedDayText = $"Day {currentDayNumber}";

        if (dayCounterTextTMP != null) dayCounterTextTMP.text = formattedDayText;
        //if (dayCounterTextLegacy != null) dayCounterTextLegacy.text = formattedDayText;
    }
}