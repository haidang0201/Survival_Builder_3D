using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cài đặt thời gian")]
    [Tooltip("Độ dài một ngày thực tế tính bằng phút (Ví dụ: 1 nghĩa là mất 1 phút để trôi qua 24h trong game)")]
    public float dayDurationInMinutes = 1f;

    [Range(0, 24)]
    [Tooltip("Thời gian hiện tại (0 = nửa đêm, 12 = trưa)")]
    public float currentTime = 8f; // Bắt đầu lúc 8 giờ sáng

    void Update()
    {
        // Tính toán tốc độ xoay dựa trên thời gian thực
        // 360 độ / (số phút * 60 giây)
        float rotationSpeed = 360f / (dayDurationInMinutes * 60f);
        
        // Xoay mặt trời quanh trục X
        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);

        // Cập nhật biến currentTime để bạn dễ theo dõi trong Inspector
        currentTime += (Time.deltaTime / (dayDurationInMinutes * 60f)) * 24f;
        
        if (currentTime >= 24f)
        {
            currentTime = 0f;
        }
    }
}