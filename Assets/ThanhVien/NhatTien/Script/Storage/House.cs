using UnityEngine;
using System.Collections.Generic;

public class House : MonoBehaviour
{
    [Header("Capacity Settings")]
    [Tooltip("Số worker tối đa được vào nhà ngủ cùng lúc khi trời tối")]
    public int maxCapacity = 4;

    [Header("References")]
    [Tooltip("Các vị trí giường ngủ hoặc slot đứng trong nhà để tránh trùng tọa độ.")]
    public Transform[] restSlots;

    private List<WorkerStamina> workersInside = new List<WorkerStamina>();
    private int _nextSlotIndex = 0;

    public int  WorkerCount => workersInside.Count;
    public bool IsFull      => workersInside.Count >= maxCapacity;

    public bool Enter(WorkerStamina worker)
    {
        if (worker == null || IsFull) return false;

        if (!workersInside.Contains(worker))
        {
            workersInside.Add(worker);
            Debug.Log($"[House] {worker.name} đã chui vào nhà ngủ an toàn (Miễn phí).");
        }
        return true;
    }

    public void Exit(WorkerStamina worker)
    {
        if (worker == null) return;
        if (workersInside.Remove(worker))
        {
            Debug.Log($"[House] {worker.name} rời nhà, chuẩn bị quay lại guồng công việc.");
        }
    }

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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 2.5f);
    }
}