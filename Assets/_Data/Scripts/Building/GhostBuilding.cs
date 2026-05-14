using UnityEngine;

/*
 * GhostBuilding.cs
 * Folder: Scripts/Building/
 * Người làm: VŨ
 *
 * Preview công trình khi đang đặt (Build Mode)
 *
 * Điều khiển:
 *   R            → xoay 90°
 *   Click trái   → xác nhận đặt (chỉ khi xanh)
 *   Click phải   → huỷ
 *
 * Kiểm tra chồng:
 *   Physics.OverlapBox → nếu có collider khác → đỏ, không đặt được
 *   Không chồng        → xanh, đặt được
 *
 * Setup trong Unity:
 *   - Ghost prefab cần có Collider (Box/Mesh) để tính kích thước
 *   - Layer "Building" gán cho tất cả prefab building thật
 *   - Layer "Ground" gán cho terrain
 */

public class GhostBuilding : MonoBehaviour
{
    // ================= INSPECTOR =================

    [Header("Loại công trình đang đặt")]
    public BuildingType buildingType;

    [Header("Materials")]
    public Material validMat;       // Màu xanh – có thể đặt
    public Material invalidMat;     // Màu đỏ   – không thể đặt
    public Material constructingMat;// THÊM: Material mờ khi đang chờ xây 10s

    [Header("Layer Settings")]
    public LayerMask groundLayer;   // Layer terrain/ground để raycast
    public LayerMask buildingLayer; // Layer building thật để kiểm tra chồng

    [Header("Settings")]
    public float snapStep = 1f;   // Bước snap lưới
    public float checkYSize = 2f;   // Chiều cao box check (tuỳ model)

    // ================= PRIVATE =================

    private Renderer[] renderers;
    private Collider ghostCollider;
    private bool isValid = false;
    private float currentYRot = 0f;

    private const float ROT_STEP = 90f;
    private bool isConstructing = false; // Trạng thái đang đếm ngược 10s

    // ================= LIFECYCLE =================

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        ghostCollider = GetComponentInChildren<Collider>();

        if (ghostCollider == null)
            Debug.LogWarning("[GhostBuilding] Không tìm thấy Collider! Kiểm tra chồng sẽ không hoạt động.");
    }

    void Update()
    {
        // THÊM: Nếu đã click đặt nhà và đang đếm ngược thì không chạy logic theo chuột nữa
        if (isConstructing) return;
        FollowMouse();
        HandleRotateInput();
        HandleConfirmInput();
        HandleCancelInput();
    }

    // ================= FOLLOW MOUSE =================

    private void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 300f, groundLayer)) return;

        transform.position = SnapToGrid(hit.point);

        CheckValidity();
    }

    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.Round(worldPos.x / snapStep) * snapStep,
            worldPos.y,
            Mathf.Round(worldPos.z / snapStep) * snapStep
        );
    }

    // ================= ROTATION =================

    private void HandleRotateInput()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        RotateStep();
    }

    private void RotateStep()
    {
        currentYRot = (currentYRot + ROT_STEP) % 360f;
        transform.rotation = Quaternion.Euler(0f, currentYRot, 0f);
        Debug.Log($"[GhostBuilding] Xoay: {currentYRot}°");
    }

    // ================= VALIDITY =================

    private void CheckValidity()
    {
        isValid = !IsOverlapping();
        ApplyMaterial(isValid ? validMat : invalidMat);
    }
    public void Show()
    {
        gameObject.SetActive(true);  // Kích hoạt đối tượng "ghost"
        ApplyMaterial(validMat);     // Áp dụng màu xanh nếu có thể xây dựng
    }

    /// <summary>
    /// Dùng Physics.OverlapBox để kiểm tra có building nào ở vị trí này không
    /// </summary>
    private bool IsOverlapping()
    {
        if (ghostCollider == null) return false;

        // Lấy kích thước và tâm của collider
        Vector3 center = GetColliderCenter();
        Vector3 halfSize = GetColliderHalfSize();

        // Kiểm tra có collider nào trong vùng này không (trừ chính nó)
        Collider[] hits = Physics.OverlapBox(
            center,
            halfSize,
            transform.rotation,
            buildingLayer
        );

        // Lọc bỏ collider của chính ghost
        foreach (var hit in hits)
        {
            if (hit.transform.root != transform.root)
                return true; // Có building khác → bị chồng
        }

        return false; // Không chồng
    }

    private Vector3 GetColliderCenter()
    {
        if (ghostCollider is BoxCollider box)
            return transform.TransformPoint(box.center);

        return ghostCollider.bounds.center;
    }

    private Vector3 GetColliderHalfSize()
    {
        if (ghostCollider is BoxCollider box)
        {
            // Nhân với scale để tính đúng kích thước thực tế
            return new Vector3(
                box.size.x * transform.lossyScale.x * 0.5f,
                box.size.y * transform.lossyScale.y * 0.5f,
                box.size.z * transform.lossyScale.z * 0.5f
            );
        }

        return ghostCollider.bounds.extents;
    }

    // ================= MATERIAL =================

    private void ApplyMaterial(Material mat)
    {
        if (mat == null) return;
        foreach (var r in renderers)
            r.material = mat;
    }

    // ================= CONFIRM / CANCEL =================

    private void HandleConfirmInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (!isValid)
        {
            Debug.LogWarning("[GhostBuilding] ❌ Vị trí bị chồng! Không thể đặt.");
            return;
        }

        ConfirmPlace();
    }

    private void HandleCancelInput()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        CancelPlace();
    }

    private void ConfirmPlace()
    {
        Debug.Log($"[GhostBuilding] ⏳ Bắt đầu xây {buildingType} trong 10s | Pos: {transform.position}");

        isConstructing = true; // Khóa di chuyển/xoay
        ApplyMaterial(constructingMat); // Đổi sang màu mờ

        // Báo cho BuildingSystem đếm ngược, truyền chính gameObject của ghost này vào
        BuildingSystem.Ins.StartConstruction(
            buildingType,
            transform.position,
            Quaternion.Euler(0f, currentYRot, 0f),
            gameObject
        );
    }

    private void CancelPlace()
    {
        Debug.Log($"[GhostBuilding] ❌ Huỷ {buildingType}");
        Destroy(gameObject);
    }

    // ================= PUBLIC =================

    public void SetInitialRotation(float yDegrees)
    {
        currentYRot = yDegrees % 360f;
        transform.rotation = Quaternion.Euler(0f, currentYRot, 0f);
    }

    // ================= DEBUG =================

    // Vẽ box kiểm tra trong Scene view để dễ debug
    void OnDrawGizmos()
    {
        if (ghostCollider == null) return;

        Gizmos.color = isValid ? Color.green : Color.red;
        Gizmos.matrix = Matrix4x4.TRS(
            GetColliderCenter(),
            transform.rotation,
            Vector3.one
        );
        Gizmos.DrawWireCube(Vector3.zero, GetColliderHalfSize() * 2f);
    }
}