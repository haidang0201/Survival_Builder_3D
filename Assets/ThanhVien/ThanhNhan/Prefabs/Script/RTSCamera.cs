using UnityEngine;

public class RTSCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;
    public float edgeSize = 15f;
    public bool useEdgeScrolling = true;

    [Header("Pan (Updated)")] // Updated to use left mouse
    public bool useLeftMousePan = true;
    private Vector3 dragOrigin;

    [Header("Zoom (New)")] // New Zoom section
    public float zoomSpeed = 20f;
    public float minZoomY = 5f;
    public float maxZoomY = 25f;
    private float targetZoomY; // Used for smoothing

    [Header("Bounds")]
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);

    void Start()
    {
        // Khởi tạo targetZoomY bằng vị trí Y hiện tại của camera
        targetZoomY = transform.position.y;
    }

    void Update()
    {
        // 1. XỬ LÝ THU PHÓNG (Zoom) - NEW
        HandleZoom();

        Vector3 moveDirection = Vector3.zero;

        // 2. DI CHUYỂN BẰNG RÌA MÀN HÌNH (Edge Scrolling)
        if (useEdgeScrolling)
        {
            if (Input.mousePosition.x >= Screen.width - edgeSize) moveDirection.x += 1f;
            if (Input.mousePosition.x <= edgeSize) moveDirection.x -= 1f;
            if (Input.mousePosition.y >= Screen.height - edgeSize) moveDirection.z += 1f;
            if (Input.mousePosition.y <= edgeSize) moveDirection.z -= 1f;
        }

        // 3. DI CHUYỂN BẰNG CHUỘT TRÁI (Left Mouse Pan) - UPDATED
        if (useLeftMousePan)
        {
            // Kiểm tra nhấn chuột trái (Button 0)
            if (Input.GetMouseButtonDown(0)) 
            {
                dragOrigin = Input.mousePosition;
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 difference = Input.mousePosition - dragOrigin;
                // Điều chỉnh độ nhạy kéo chuột tại đây nếu cần (ví dụ: * 0.1f)
                moveDirection.x = -difference.x * 0.1f; 
                moveDirection.z = -difference.y * 0.1f;
                
                dragOrigin = Input.mousePosition; 
            }
        }

        // --- Tính toán hướng di chuyển (giữ nguyên) ---
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 finalDirection = (forward * moveDirection.z + right * moveDirection.x).normalized;
        Vector3 newPosition = transform.position + finalDirection * moveSpeed * Time.deltaTime;

        // --- Giới hạn phạm vi (giữ nguyên) ---
        newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
        newPosition.z = Mathf.Clamp(newPosition.z, minBounds.y, maxBounds.y);

        transform.position = newPosition;
    }

    // Định nghĩa hàm xử lý thu phóng
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        // Tính toán vị trí Y mục tiêu mới
        targetZoomY -= scroll * zoomSpeed * 10f; // Nhân thêm để tăng độ nhạy
        targetZoomY = Mathf.Clamp(targetZoomY, minZoomY, maxZoomY); // Giới hạn độ cao

        // Làm mượt quá trình thu phóng bằng Lerp
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetZoomY, Time.deltaTime * zoomSpeed * 0.5f); // Tùy chỉnh độ mượt
        transform.position = pos;
    }
}