using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cài đặt thời gian")]
    [Tooltip("Độ dài một ngày thực tế tính bằng phút (Ví dụ: 1 nghĩa là mất 1 phút để trôi qua 24h trong game)")]
    // Thời lượng phần ban ngày (từ `sunriseTime` tới `sunsetTime`) tính bằng phút thực
    public float dayDurationInMinutes = 0.5f;

    // Thời lượng phần ban đêm (từ `sunsetTime` tới lần `sunriseTime` tiếp theo) tính bằng phút thực
    public float nightDurationInMinutes = 0.5f;

    [Range(0, 24)]
    [Tooltip("Thời gian hiện tại (0 = nửa đêm, 12 = trưa)")]
    public float currentTime = 8f; // Bắt đầu lúc 8 giờ sáng

    [Range(0, 24)]
    [Tooltip("Giờ mặt trời mọc (ví dụ 6)")]
    public float sunriseTime = 6f;

    [Range(0, 24)]
    [Tooltip("Giờ mặt trời lặn (ví dụ 18)")]
    public float sunsetTime = 18f;

    void Update()
    {
        // Tính số giờ thuộc ban ngày (sunrise -> sunset) và ban đêm (phần còn lại)
        float dayHours = Mathf.Repeat(sunsetTime - sunriseTime + 24f, 24f);
        if (dayHours <= 0f) dayHours = 12f; // fallback an toàn
        float nightHours = 24f - dayHours;

        // Kiểm tra đang ở ban ngày hay ban đêm theo currentTime
        bool isDay = IsTimeInRange(currentTime, sunriseTime, sunsetTime);

        // Tính số giờ in-game sẽ trôi qua trong frame này
        float hoursThisFrame = 0f;
        if (isDay)
        {
            float daySeconds = Mathf.Max(0.0001f, dayDurationInMinutes * 60f);
            hoursThisFrame = (Time.deltaTime / daySeconds) * dayHours;
        }
        else
        {
            float nightSeconds = Mathf.Max(0.0001f, nightDurationInMinutes * 60f);
            hoursThisFrame = (Time.deltaTime / nightSeconds) * nightHours;
        }

        // Cập nhật thời gian và xoay mặt trời tương ứng
        currentTime += hoursThisFrame;
        if (currentTime >= 24f) currentTime -= 24f;

        // 1 giờ in-game = 15 độ (360 / 24)
        float rotationDegrees = hoursThisFrame * 15f;
        transform.Rotate(Vector3.right, rotationDegrees, Space.Self);
    }

    // Hàm tiện ích kiểm tra time có nằm trong khoảng [start, end) với xử lý wrap qua nửa đêm
    private bool IsTimeInRange(float time, float start, float end)
    {
        if (start < end) return time >= start && time < end;
        return time >= start || time < end;
    }
}