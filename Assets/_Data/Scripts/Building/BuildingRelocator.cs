using System.Collections.Generic;
using UnityEngine;

public class BuildingRelocator : MonoBehaviour
{
    [Header("Cấu hình Layer quét chuột")]
    public LayerMask buildingLayer;
    public LayerMask groundLayer;

    [Header("Cấu hình Chống xây đè (Overlap)")]
    [Tooltip("Layer của các vật cản không cho phép đặt nhà đè lên (thường là Building, Tree, Rock...)")]
    public LayerMask obstacleLayer;

    [Header("Màu sắc báo hiệu")]
    public Material validMaterial;   // Kéo Material Xanh lá (Transparent) vào đây
    public Material invalidMaterial; // Kéo Material Đỏ (Transparent) vào đây

    // Biến trạng thái
    private GameObject buildingToMove;
    private bool isRelocating = false;
    private bool canPlace = true;

    // Dữ liệu hỗ trợ quét va chạm và đổi màu
    private BoxCollider buildingCollider;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !isRelocating)
        {
            TryPickUpBuilding();
        }

        if (isRelocating && buildingToMove != null)
        {
            MoveBuildingWithMouse();
            CheckPlacementValidity();

            // Chỉ cho phép đặt xuống (click chuột trái) nếu vị trí hợp lệ
            if (Input.GetMouseButtonDown(0))
            {
                if (canPlace)
                {
                    PlaceBuildingDown();
                }
                else
                {
                    Debug.LogWarning("<color=orange>Vị trí bị trùng lấp, không thể đặt nhà ở đây!</color>");
                    // Tùy chọn: Thêm code phát âm thanh báo lỗi ở đây
                }
            }
        }
    }

    private void TryPickUpBuilding()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, buildingLayer))
        {
            buildingToMove = hit.collider.transform.root.gameObject;
            isRelocating = true;

            // 1. Lấy Collider để lát nữa đo kích thước check xây đè
            buildingCollider = buildingToMove.GetComponentInChildren<BoxCollider>();

            // 2. Lưu lại màu gốc và tắt Collider để tia chuột không bị kẹt
            SaveOriginalMaterials();
            SetCollidersEnabled(buildingToMove, false);

            Debug.Log($"Đã nhấc công trình: {buildingToMove.name}");
        }
    }

    private void MoveBuildingWithMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 targetPosition = hit.point;

            // [Tùy chọn] Ép tọa độ vào lưới grid để xây nhà ngay ngắn
            // targetPosition.x = Mathf.Round(targetPosition.x);
            // targetPosition.z = Mathf.Round(targetPosition.z);

            buildingToMove.transform.position = targetPosition;
        }
    }

    /// <summary> Thuật toán quét không gian để xem có bị đè nhà không </summary>
    private void CheckPlacementValidity()
    {
        if (buildingCollider == null) return;

        // Tính toán tâm và kích thước của ngôi nhà
        Vector3 center = buildingToMove.transform.TransformPoint(buildingCollider.center);

        // Nhân 0.95f để thu nhỏ vùng check một xíu, giúp các nhà đứng sát vách nhau không bị báo lỗi nhầm
        Vector3 extents = (buildingCollider.size / 2f) * 0.95f;

        // Quét hình hộp xem có đụng vật thể nào thuộc obstacleLayer không
        canPlace = !Physics.CheckBox(center, extents, buildingToMove.transform.rotation, obstacleLayer);

        // Đổi màu toàn bộ model dựa trên kết quả quét
        ApplyFeedbackMaterial(canPlace ? validMaterial : invalidMaterial);
    }

    private void PlaceBuildingDown()
    {
        // 1. Trả lại màu gốc cho đồ họa
        RestoreOriginalMaterials();

        // 2. Bật lại Collider để nhà nhận tương tác vật lý như cũ
        SetCollidersEnabled(buildingToMove, true);

        Debug.Log($"<color=green>Đã đặt công trình xuống thành công!</color>");

        buildingToMove = null;
        isRelocating = false;
    }

    // ==========================================
    // CÁC HÀM HỖ TRỢ XỬ LÝ ĐỒ HỌA VÀ VẬT LÝ
    // ==========================================

    private void SaveOriginalMaterials()
    {
        originalMaterials.Clear();
        Renderer[] renderers = buildingToMove.GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            originalMaterials[ren] = ren.materials;
        }
    }

    private void ApplyFeedbackMaterial(Material mat)
    {
        foreach (var kvp in originalMaterials)
        {
            Renderer ren = kvp.Key;
            if (ren != null)
            {
                // Tạo mảng material mới toàn màu xanh/đỏ đè lên (trường hợp model có nhiều sub-mesh)
                Material[] feedbackMats = new Material[ren.materials.Length];
                for (int i = 0; i < feedbackMats.Length; i++)
                {
                    feedbackMats[i] = mat;
                }
                ren.materials = feedbackMats;
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var kvp in originalMaterials)
        {
            Renderer ren = kvp.Key;
            if (ren != null)
            {
                ren.materials = kvp.Value;
            }
        }
        originalMaterials.Clear();
    }

    private void SetCollidersEnabled(GameObject obj, bool isEnabled)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = isEnabled;
        }
    }
}