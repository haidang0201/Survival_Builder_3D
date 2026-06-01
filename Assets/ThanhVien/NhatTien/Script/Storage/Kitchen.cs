using UnityEngine;
using System.Collections.Generic;

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

    [Tooltip("Các vị trí đứng bên ngoài bếp (tùy chọn). Chống việc bị kẹt tụ tập một chỗ.")]
    public Transform[] restSlots;

    private List<WorkerStamina> workersInside = new List<WorkerStamina>();

    // Round-robin cho restSlots — tránh nhiều worker cùng đứng 1 slot
    private int _nextSlotIndex = 0;

    public int  WorkerCount => workersInside.Count;
    public bool IsFull      => workersInside.Count >= maxCapacity;
    public bool HasFood     => warehouseStorage != null && warehouseStorage.CurrentRice >= foodPerWorkerRest;

    void Start()
    {
        if (warehouseStorage == null)
        {
            GameObject wh = GameObject.FindWithTag("Warehouse");
            if (wh != null)
                warehouseStorage = wh.GetComponent<WarehouseStorage>()
                                ?? wh.GetComponentInChildren<WarehouseStorage>();
        }

        if (warehouseStorage == null)
            Debug.LogError($"[Kitchen] '{name}': Không tìm thấy WarehouseStorage! " +
                           $"Gán Tag 'Warehouse' cho kho chính.");
    }

    /// <summary>
    /// Worker xin vào bếp nghỉ.
    /// Trả về true nếu còn slot trống (worker sẽ được ẩn model).
    /// out consumedFood = true nếu worker được ăn lúa, false nếu đói.
    /// </summary>
    public bool TryEnter(WorkerStamina worker, out bool consumedFood)
    {
        consumedFood = false;

        // FIX: null guard
        if (worker == null)
        {
            Debug.LogWarning("[Kitchen] TryEnter nhận null worker!");
            return false;
        }

        // FIX: tránh add trùng worker
        if (workersInside.Contains(worker))
        {
            Debug.LogWarning($"[Kitchen] {worker.name} đã ở trong bếp rồi!");
            consumedFood = !worker.IsHungryInside; // trả lại trạng thái cũ
            return true;
        }

        if (IsFull)
        {
            Debug.Log($"[Kitchen] Bếp đầy ({workersInside.Count}/{maxCapacity}). " +
                      $"{worker.name} đứng hóng gió ngoài cửa.");
            return false;
        }

        workersInside.Add(worker);

        // FIX: dùng ConsumeRice() thay vì AddRice(-amount)
        if (HasFood)
        {
            warehouseStorage.ConsumeRice(foodPerWorkerRest);
            consumedFood = true;
            Debug.Log($"[Kitchen] {worker.name} vào bếp nghỉ ✅ ĐÃ ĐƯỢC ĂN. " +
                      $"Tiêu {foodPerWorkerRest} lúa. Kho còn: {warehouseStorage.CurrentRice}");
        }
        else
        {
            Debug.Log($"[Kitchen] BẾP HẾT LÚA! {worker.name} vào nhà trú ẩn nhưng nhịn đói " +
                      $"(hồi stamina chậm). Kho còn: 0");
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

    /// <summary>
    /// Trả vị trí đứng chờ cho worker.
    /// FIX: Round-robin thay vì random — tránh nhiều worker chồng lên cùng 1 slot.
    /// </summary>
    public Vector3 GetRestPosition()
    {
        if (restSlots != null && restSlots.Length > 0)
        {
            // Bỏ qua slot null
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
        Gizmos.DrawWireSphere(transform.position, 2f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Bếp: {workersInside.Count}/{maxCapacity}"
        );
#endif
    }
}