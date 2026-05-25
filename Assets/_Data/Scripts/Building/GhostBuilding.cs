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

        if (!isValid)
        {
            Debug.LogWarning("[GhostBuilding] ❌ Vị trí bị chồng, không thể đặt.");
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
        Debug.Log($"[GhostBuilding] ✅ Đặt {buildingType} | Pos: {transform.position} | Rot: {currentYRot}°");

        ConstructionManager.Ins.PlaceBuilding(
            buildingType,
            transform.position,
            Quaternion.Euler(0f, currentYRot, 0f)
        );

        BuildingSystem.Ins.OnPlacingCompleted();
        Destroy(gameObject);
    }

    private void CancelPlace()
    {
        Debug.Log($"[GhostBuilding] ❌ Huỷ {buildingType}");
        BuildingSystem.Ins.OnPlacingCompleted();
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
}