using UnityEngine;
using System.Collections.Generic;

/*
 * House.cs
 * Folder: Scripts/Civilian/ (Hoặc tùy thư mục dự án của bạn)
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ
 * CHỨC NĂNG: Quản lý sức chứa worker, vị trí ngủ/nghỉ và cung cấp thông số 
 * Sức chứa + Tốc độ khai thác cho hệ thống Upgrade của UpgradeableBuilding.
 */

public class House : MonoBehaviour
{
    [Header("[THÔNG SỐ CẤP ĐỘ - ĐỒNG BỘ UI UPGRADE]")]
    [Tooltip("Số worker tối đa được vào nhà ngủ cùng lúc ở cấp độ này")]
    public int maxCapacity = 4;

    [Tooltip("Tốc độ khai thác của worker ở cấp độ này (Ví dụ: 2.0 giây / 1 tài nguyên)")]
    public float gatherSpeed = 2.0f;

    [Header("References")]
    [Tooltip("Vị trí cửa nhà để Worker đi tới. Nếu bỏ trống sẽ tự động lấy tâm của House.")]
    public Transform entrancePoint;

    [Tooltip("Các vị trí giường ngủ hoặc slot đứng trong nhà để tránh trùng tọa độ.")]
    public Transform[] restSlots;

    // Danh sách nội bộ quản lý các worker đang ở bên trong nhà
    private List<WorkerStamina> workersInside = new List<WorkerStamina>();
    private int _nextSlotIndex = 0;

    // Cổng Properties công khai để UIManager hoặc AI truy vấn dữ liệu nhanh
    public int     WorkerCount      => workersInside.Count;
    public bool    IsFull           => workersInside.Count >= maxCapacity;
    public Vector3 EntrancePosition => entrancePoint != null ? entrancePoint.position : transform.position;

    /// <summary>
    /// Hàm thiết lập/đồng bộ dữ liệu khi công trình nâng cấp (Nếu Penta Dev dùng cơ chế gọi hàm Setup giống WoodStorage)
    /// </summary>
    public void SetupLevel(int currentLevel)
    {
        Debug.Log($"[House] Đang đồng bộ dữ liệu cho nhà ở thuộc Model cấp: {currentLevel + 1}");
        // Bạn có thể xử lý thêm logic riêng tại đây nếu cần thiết khi nhà được active
    }

    /// <summary>
    /// Worker gọi hàm này để xin đi vào nhà nghỉ ngơi
    /// </summary>
    public bool Enter(WorkerStamina worker)
    {
        if (worker == null || IsFull) return false;

        if (!workersInside.Contains(worker))
        {
            workersInside.Add(worker);
            Debug.Log($"[House] {worker.name} đã chui vào nhà ngủ an toàn. Sức chứa hiện tại: {workersInside.Count}/{maxCapacity}");
        }
        return true;
    }

    /// <summary>
    /// Worker gọi hàm này khi thức dậy để rời nhà quay lại làm việc
    /// </summary>
    public void Exit(WorkerStamina worker)
    {
        if (worker == null) return;
        if (workersInside.Remove(worker))
        {
            Debug.Log($"[House] {worker.name} rời nhà, chuẩn bị quay lại guồng công việc.");
        }
    }

    /// <summary>
    /// Trả về vị trí giường ngủ tiếp theo cho Worker (Tránh tình trạng đứng đè lên nhau)
    /// </summary>
    public Vector3 GetRestPosition()
    {
        if (restSlots != null && restSlots.Length > 0)
        {
            for (int i = 0; i < restSlots.Length; i++)
            {
                int idx = (_nextSlotIndex + i) % restSlots.Length;
                if (restSlots[idx] != null)
                {
                    _nextSlotIndex = (idx + 1) % restSlots.Length;
                    return restSlots[idx].position;
                }
            }
        }
        return transform.position;
    }

    /// <summary>
    /// Hiển thị vùng cửa và thông số trực quan ngoài Scene khi click chọn nhà (Phục vụ Debug)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(EntrancePosition, 2.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            EntrancePosition + Vector3.up * 2.5f,
            $"Nhà: {workersInside.Count}/{maxCapacity} | Tốc độ: {gatherSpeed:F1}s"
        );
#endif
    }
}