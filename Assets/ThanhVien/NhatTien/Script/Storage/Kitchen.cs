using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Nhà bếp — nơi Worker vào nghỉ ngơi và tiêu thụ lúa từ WarehouseStorage.
/// - Có slot giới hạn số worker bên trong cùng lúc
/// - Khi vào: tiêu lúa, ẩn model worker
/// - Khi ra: hiện model worker, giải phóng slot
/// - Hết lúa hoặc đầy slot: worker đứng ngoài phục hồi chậm
/// Gán tag "Kitchen" để WorkerStamina tự tìm.
/// </summary>
public class Kitchen : MonoBehaviour
{
    [Header("Capacity")]
    [Tooltip("Số worker tối đa được vào bếp cùng lúc")]
    public int maxCapacity = 3;

    [Header("Food Settings")]
    [Tooltip("Số lúa tiêu thụ mỗi lần worker vào bếp nghỉ")]
    public int foodPerWorkerRest = 1;

    [Header("References")]
    [Tooltip("Kho chính — lấy lúa từ đây. Tự tìm qua Tag 'Warehouse' nếu bỏ trống.")]
    public WarehouseStorage warehouseStorage;

    [Tooltip("Vị trí Cửa bếp để Worker đi tới. Nếu bỏ trống sẽ tự động lấy tâm của Kitchen.")]
    public Transform entrancePoint;

    [Tooltip("Các vị trí đứng bên ngoài bếp (tùy chọn). Chống việc bị kẹt tụ tập một chỗ.")]
    public Transform[] restSlots;

    private List<WorkerStamina> workersInside = new List<WorkerStamina>();
    private int _nextSlotIndex = 0;

    public int  WorkerCount      => workersInside.Count;
    public bool IsFull           => workersInside.Count >= maxCapacity;
    public bool HasFood          => warehouseStorage != null && warehouseStorage.CurrentRice >= foodPerWorkerRest;
    public Vector3 EntrancePosition => entrancePoint != null ? entrancePoint.position : transform.position;

    void Start()
    {
        if (warehouseStorage == null)
        {
            GameObject wh = GameObject.FindWithTag("Warehouse");
            if (wh != null)
                warehouseStorage = wh.GetComponent<WarehouseStorage>()
                                ?? wh.GetComponentInChildren<WarehouseStorage>();
        }

        // FIX: log lỗi rõ ràng nếu vẫn không tìm thấy
        if (warehouseStorage == null)
            Debug.LogError($"[Kitchen] '{name}': Không tìm thấy WarehouseStorage! " +
                           $"Gán Tag 'Warehouse' cho kho chính.");
    }

    /// <summary>
    /// Worker xin vào bếp.
    /// FIX: thêm out consumedFood để WorkerStamina biết chính xác worker có ăn được không,
    /// tránh bug kiểm tra HasFood SAU KHI đã ConsumeRice (kết quả sai khi lúa vừa hết).
    /// </summary>
    public bool Enter(WorkerStamina worker, out bool consumedFood)
    {
        consumedFood = false;

        if (worker == null) return false;
        if (workersInside.Contains(worker)) return true;
        if (IsFull) return false;

        workersInside.Add(worker);

        // FIX: kiểm tra HasFood TRƯỚC rồi mới ConsumeRice
        // (nếu kiểm tra sau thì lúa vừa hết → HasFood = false dù worker đã ăn)
        if (HasFood)
        {
            warehouseStorage.ConsumeRice(foodPerWorkerRest);
            consumedFood = true;
            Debug.Log($"[Kitchen] {worker.name} vào bếp nghỉ ✅ ĂN {foodPerWorkerRest} lúa. " +
                      $"Kho lúa còn: {warehouseStorage.CurrentRice}");
        }
        else
        {
            Debug.Log($"[Kitchen] {worker.name} vào nhà trú ẩn nhưng nhịn đói " +
                      $"(hồi stamina chậm). Kho lúa: 0");
        }

        return true;
    }

    public void Exit(WorkerStamina worker)
    {
        if (worker == null) return;

        if (workersInside.Remove(worker))
            Debug.Log($"[Kitchen] {worker.name} no nê đi làm. " +
                      $"Slot còn trống: {maxCapacity - workersInside.Count}/{maxCapacity}");
    }

    /// <summary>Round-robin — tránh nhiều worker chồng lên cùng 1 slot.</summary>
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(EntrancePosition, 2f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            EntrancePosition + Vector3.up * 2.5f,
            $"Bếp: {workersInside.Count}/{maxCapacity}\nLúa: {(warehouseStorage != null ? warehouseStorage.CurrentRice.ToString() : "?")}"
        );
#endif
    }
}