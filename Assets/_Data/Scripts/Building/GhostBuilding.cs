using UnityEngine;

/*
 * GhostBuilding.cs
 * Folder: Scripts/Building/
 * Người làm: VŨ
 *
 * Preview công trình khi đang đặt (Build Mode)
 *
 * Điều khiển:
 *   R            → xoay 90° theo chiều Y
 *   Click trái   → xác nhận đặt
 *   Click phải   → huỷ
 *   Màu xanh     → vị trí hợp lệ
 *   Màu đỏ       → vị trí không hợp lệ
 *
 * Luồng:
 *   TestBuildingPlacement.SpawnGhost()
 *   → GhostBuilding di chuyển theo chuột
 *   → R xoay 90°
 *   → Click trái → ConstructionManager.PlaceBuilding()
 */

public class GhostBuilding : MonoBehaviour
{
    // ================= INSPECTOR =================

    [Header("Loại công trình đang đặt")]
    public BuildingType buildingType;

    [Header("Materials")]
    public Material validMat;       // Màu xanh – có thể đặt
    public Material invalidMat;     // Màu đỏ   – không thể đặt

    [Header("Settings")]
    public LayerMask groundLayer;   // Layer terrain/ground
    public float snapStep = 1f; // Bước snap lưới

    // ================= PRIVATE =================

    private Renderer[] renderers;
    private bool isValid = false;
    private float currentYRot = 0f;

    private const float ROT_STEP = 90f;

    // ================= LIFECYCLE =================

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
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
        // TODO: thêm Physics.OverlapBox kiểm tra overlap sau
        isValid = true;
        ApplyMaterial(isValid ? validMat : invalidMat);
    }

    private void ApplyMaterial(Material mat)
    {
        if (mat == null) return;
        foreach (var r in renderers)
            r.material = mat;
    }

    // ================= CONFIRM / CANCEL =================

    private void HandleConfirmInput()
    {
        if (!Input.GetMouseButtonDown(0) || !isValid) return;
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

        Destroy(gameObject);
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
}