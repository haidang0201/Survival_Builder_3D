using UnityEngine;

/*
 * GhostBuilding.cs
 * Folder: Scripts/Building/
 * Người làm: VŨ
 *
 * Preview công trình khi đang đặt (Build Mode).
 *
 * Điều khiển:
 *   R           → xoay 90°
 *   Click trái  → xác nhận đặt (chỉ khi xanh)
 *   Click phải  → huỷ
 *
 * Kiểm tra chồng:
 *   Physics.OverlapBox → có collider khác → đỏ, không đặt được
 *   Không chồng        → xanh, đặt được
 *
 * Setup trong Unity:
 *   - Ghost prefab cần có Collider (Box/Mesh) để tính kích thước
 *   - Layer "Building" gán cho tất cả prefab building thật
 *   - Layer "Ground"   gán cho terrain
 */

public class GhostBuilding : MonoBehaviour
{
    // ================= INSPECTOR =================
    [Header("Mảng Model Ghost theo Cấp Độ")]
    public GameObject[] levelModels; // Mảng này sẽ tạo ra dấu + ngoài Inspector để Vũ kéo các model Ghost vào

    [Header("Loại công trình đang đặt")]
    public BuildingType buildingType;

    [Header("Materials")]
    public Material validMat;       // Màu xanh – có thể đặt
    public Material invalidMat;     // Màu đỏ   – không thể đặt

    [Header("Layer Settings")]
    public LayerMask groundLayer;   // Layer terrain/ground để raycast
    public LayerMask buildingLayer; // Layer building thật để kiểm tra chồng

    [Header("Settings")]
    public float snapStep = 1f;    // Bước snap lưới
    public float checkYSize = 2f;   // Chiều cao box check (tuỳ model)

    // ================= PRIVATE =================

    private Renderer[] renderers;
    private Collider ghostCollider;
    private bool isValid = false;
    private float currentYRot = 0f;

    private const float ROT_STEP = 90f;
    private bool isHoveringGround = false;

    // ================= LIFECYCLE =================

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        ghostCollider = GetComponentInChildren<Collider>();

        if (ghostCollider == null)
            Debug.LogWarning("[GhostBuilding] Không tìm thấy Collider! Kiểm tra chồng sẽ không hoạt động.");
    }

    private void Update()
    {
        FollowMouse();
        HandleRotateInput();
        HandleConfirmInput();
        HandleCancelInput();
    }

    private void HandleMoveAndSnap()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // [ĐÃ SỬA] Tăng từ 100f lên 1000f để phù hợp với tầm nhìn Camera Top-Down
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 targetPos = hit.point;

            if (snapStep > 0f)
            {
                targetPos.x = Mathf.Round(targetPos.x / snapStep) * snapStep;
                targetPos.z = Mathf.Round(targetPos.z / snapStep) * snapStep;
            }

            transform.position = targetPos;
            isHoveringGround = true; // Xác nhận chuột đang chỉ vào bản đồ
        }
        else
        {
            isHoveringGround = false; // Chuột chỉ ra ngoài khoảng không hoặc UI, cấm đặt!
        }
    }

    private void HandleOverlapCheck()
    {
        // [BẢO VỆ MỚI]: Nếu tia Raycast không chạm đất, mặc định không cho xây, chống lỗi (0,0,0)
        if (!isHoveringGround)
        {
            isValid = false;
            ApplyMaterial(invalidMat); // Chuyển màu đỏ cảnh báo
            return;
        }

        if (ghostCollider == null)
        {
            isValid = true;
            return;
        }

        Vector3 center = ghostCollider.bounds.center;
        Vector3 halfExtents = ghostCollider.bounds.extents;
        halfExtents.y = checkYSize / 2f;

        Collider[] colliders = Physics.OverlapBox(center, halfExtents, transform.rotation, buildingLayer);
        bool noOverlap = colliders.Length == 0;

        bool managerCanBuild = BuildingManager.Ins.CanBuild(transform.position, buildingType, null);

        isValid = noOverlap && managerCanBuild;

        Material targetMat = isValid ? validMat : invalidMat;
        ApplyMaterial(targetMat);
    }

    // Thêm hàm Public này vào cuối file GhostBuilding.cs để BuildingSystem gọi kích hoạt nhanh
    public void InstantSnapToMouse()
    {
        HandleMoveAndSnap();
    }

    // ================= HIỂN THỊ =================

    /// <summary>Kích hoạt ghost và áp material xanh mặc định.</summary>
    public void Show()
    {
        gameObject.SetActive(true);
        ApplyMaterial(validMat);
    }

    /// <summary>Ẩn ghost (không destroy – dùng khi tạm tắt chế độ đặt).</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
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

    /// <summary>Physics.OverlapBox kiểm tra chồng lên building thật.</summary>
    private bool IsOverlapping()
    {
        if (ghostCollider == null) return false;

        Vector3 center = GetColliderCenter();
        Vector3 halfSize = GetColliderHalfSize();

        Collider[] hits = Physics.OverlapBox(center, halfSize, transform.rotation, buildingLayer);

        foreach (var hit in hits)
        {
            if (hit.transform.root != transform.root)
                return true;
        }

        return false;
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

        // [QUAN TRỌNG VÀ BẮT BUỘC]: Nếu vị trí đang không hợp lệ (Màu đỏ), khóa ngay lập tức!
        if (!isValid)
        {
            Debug.LogWarning($"[GhostBuilding] Vị trí bị chiếm hoặc không hợp lệ! Không thể xây {buildingType}.");
            // Bạn có thể gọi UIManager.Ins.ShowWarning("Vị trí này đã có công trình!") tại đây để báo cho người chơi.
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
        Debug.Log($"[GhostBuilding] ✅ Đặt {buildingType} | Pos: {transform.position}");

        ConstructionManager.Ins.PlaceBuilding(
            buildingType,
            transform.position,
            Quaternion.Euler(0f, currentYRot, 0f)
        );

        // [SỬA TẠI ĐÂY]: Truyền FALSE để TẮT PANEL chọn nhà sau khi đặt thành công
        BuildingSystem.Ins.OnPlacingCompleted(false);

        Destroy(gameObject);
    }

    private void CancelPlace()
    {
        Debug.Log($"[GhostBuilding] ❌ Huỷ {buildingType}");

        // [SỬA TẠI ĐÂY]: Truyền TRUE để HIỆN LẠI PANEL chọn nhà giúp người chơi chọn lại con khác
        // Nếu bạn muốn hủy xây cũng TẮT HẲN panel luôn, hãy đổi thành false.
        BuildingSystem.Ins.OnPlacingCompleted(true);

        Destroy(gameObject);
    }

    // ================= PUBLIC =================

    /// <summary>Set góc xoay ban đầu – gọi từ BuildingSystem.StartPlacing() nếu cần.</summary>
    public void SetInitialRotation(float yDegrees)
    {
        currentYRot = yDegrees % 360f;
        transform.rotation = Quaternion.Euler(0f, currentYRot, 0f);
    }

    // ================= DEBUG =================

    private void OnDrawGizmos()
    {
        if (ghostCollider == null) return;

        Gizmos.color = isValid ? Color.green : Color.red;
        Gizmos.matrix = Matrix4x4.TRS(GetColliderCenter(), transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, GetColliderHalfSize() * 2f);
    }

    // Hàm này sẽ tắt bật Model Ghost theo level truyền vào
    public void SetGhostLevel(int level)
    {
        if (levelModels == null || levelModels.Length == 0) return;

        for (int i = 0; i < levelModels.Length; i++)
        {
            if (levelModels[i] != null)
            {
                // Chỉ bật model khớp với level hiện tại, các model khác ẩn đi
                levelModels[i].SetActive(i == level);
            }
        }
    }
}