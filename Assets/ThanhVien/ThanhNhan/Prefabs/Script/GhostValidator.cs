using UnityEngine;

public class GhostValidator : MonoBehaviour
{
    [Header("Cài đặt Material")]
    public Material validMaterial;   // Kéo Material Xanh (Cho phép xây) vào đây
    public Material invalidMaterial; // Kéo Material Đỏ (Cấm xây) vào đây

    private Renderer meshRenderer;
    private int overlapCount = 0; // Đếm số vật cản đang bị đè lên

    void Start()
    {
        InitializeComponents();
    }

    // 1. Khởi tạo và lấy Component
    private void InitializeComponents()
    {
        meshRenderer = GetComponentInChildren<Renderer>();
        UpdateVisuals(); // Set màu mặc định ban đầu
    }

    // 2. Chạy khi bắt đầu đụng trúng một vật
    private void OnTriggerEnter(Collider other)
    {
        if (IsObstacle(other))
        {
            overlapCount++;
            UpdateVisuals();
        }
    }

    // 3. Chạy khi rời khỏi vật đó
    private void OnTriggerExit(Collider other)
    {
        if (IsObstacle(other))
        {
            overlapCount--;
            UpdateVisuals();
        }
    }

    // 4. Hàm lọc xem vật vừa đụng có phải là chướng ngại vật không
    private bool IsObstacle(Collider other)
    {
        return other.gameObject.layer == LayerMask.NameToLayer("Obstacle");
    }

    // 5. Hàm cập nhật màu sắc hiển thị
    private void UpdateVisuals()
    {
        if (meshRenderer != null)
        {
            // Nếu đếm = 0 (không chạm gì) -> Xanh. Có chạm -> Đỏ.
            meshRenderer.material = IsValidPosition() ? validMaterial : invalidMaterial;
        }
    }

    // 6. Hàm public để các Script khác gọi ra hỏi xem "Có đang xây được không?"
    public bool IsValidPosition()
    {
        return overlapCount == 0;
    }
}