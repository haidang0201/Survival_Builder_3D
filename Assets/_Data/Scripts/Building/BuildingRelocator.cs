using UnityEngine;

public class BuildingRelocator : MonoBehaviour
{
    [Header("Cấu hình Layer quét chuột")]
    [Tooltip("Layer của công trình để tia Raycast nhận diện được nhà")]
    public LayerMask buildingLayer;
    [Tooltip("Layer của mặt đất để nhà trượt lên đó")]
    public LayerMask groundLayer;

    // Các biến lưu trữ trạng thái
    private GameObject buildingToMove;
    private bool isRelocating = false;

    void Update()
    {
        // 1. NHẤC LÊN: Khi nhấn phím TAB và đang không cầm công trình nào
        if (Input.GetKeyDown(KeyCode.Tab) && !isRelocating)
        {
            TryPickUpBuilding();
        }

        // 2. DI CHUYỂN: Nếu đang cầm công trình trên tay
        if (isRelocating && buildingToMove != null)
        {
            MoveBuildingWithMouse();

            // 3. ĐẶT XUỐNG: Khi click chuột trái
            if (Input.GetMouseButtonDown(0))
            {
                PlaceBuildingDown();
            }
        }
    }

    /// <summary> Quét chuột tìm công trình và nhấc nó lên </summary>
    private void TryPickUpBuilding()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Bắn tia tìm công trình dưới con trỏ chuột
        if (Physics.Raycast(ray, out hit, 1000f, buildingLayer))
        {
            // Lấy Object cao nhất của công trình (để lấy trọn vẹn cả cụm model và script)
            buildingToMove = hit.collider.transform.root.gameObject;
            isRelocating = true;

            // Tạm thời tắt Collider để tia chuột có thể xuyên qua nhà, bắn thẳng xuống đất
            SetCollidersEnabled(buildingToMove, false);

            Debug.Log($"<color=yellow>Đã nhấc công trình: {buildingToMove.name}</color>");
        }
    }

    /// <summary> Cập nhật tọa độ công trình đi theo chuột </summary>
    private void MoveBuildingWithMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Bắn tia xuyên xuống mặt đất để lấy tọa độ XYZ
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 targetPosition = hit.point;

            // [TÙY CHỌN] Bật 2 dòng này lên nếu team muốn nhà tự bắt dính vào ô vuông (Snap to Grid)
            // targetPosition.x = Mathf.Round(targetPosition.x);
            // targetPosition.z = Mathf.Round(targetPosition.z);

            buildingToMove.transform.position = targetPosition;
        }
    }

    /// <summary> Thả công trình xuống đất và kết thúc di chuyển </summary>
    private void PlaceBuildingDown()
    {
        // Bật lại Collider để nhà nhận tương tác chuột như bình thường
        SetCollidersEnabled(buildingToMove, true);

        Debug.Log($"<color=green>Đã đặt công trình {buildingToMove.name} xuống vị trí mới!</color>");

        // Xóa dữ liệu và đưa hệ thống về trạng thái nghỉ
        buildingToMove = null;
        isRelocating = false;
    }

    /// <summary> Hàm hỗ trợ bật/tắt toàn bộ Collider của công trình </summary>
    private void SetCollidersEnabled(GameObject obj, bool isEnabled)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = isEnabled;
        }
    }
}