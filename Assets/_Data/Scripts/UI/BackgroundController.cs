using UnityEngine;
using UnityEngine.UI; // Cần thiết để điều khiển UI

public class BackgroundController : MonoBehaviour
{
    // Kéo UI Image component vào đây trong Inspector
    public Image backgroundImageComponent;

    // Kéo 6 Asset Sprite vào đây trong Inspector theo đúng thứ tự
    public Sprite[] backgroundSprites;

    // Chỉ số của ảnh đang hiển thị (0 đến 5)
    private int currentBackgroundIndex = 0;

    void Start()
    {
        // Khởi đầu bằng việc hiện ảnh đầu tiên
        UpdateBackground(0);
    }

    // Hàm public có thể gọi từ code khác hoặc Event (ví dụ nhấn chuột)
    public void ChangeBackground(int index)
    {
        if (index >= 0 && index < backgroundSprites.Length)
        {
            UpdateBackground(index);
        }
        else
        {
            Debug.LogError("Chỉ số nền không hợp lệ!");
        }
    }

    // Hàm phụ trợ để thay đổi Sprite và cập nhật Index
    private void UpdateBackground(int index)
    {
        currentBackgroundIndex = index;
        backgroundImageComponent.sprite = backgroundSprites[index];
    }
}