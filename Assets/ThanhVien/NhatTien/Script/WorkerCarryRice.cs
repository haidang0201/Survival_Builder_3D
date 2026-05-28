using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Mang lúa về RiceStorage (kho tạm).
/// KHÔNG cộng UI — để WorkerCarrierRice lo.
/// Gán tag "RiceStorage" vào kho tạm.
/// </summary>
public class WorkerCarryRice : MonoBehaviour
{
    public Transform    handPoint;
    public NavMeshAgent agent;
    public Transform    riceStoragePoint;   // RiceStorage (kho tạm)

    private RicePickup   currentRice;
    private RiceStorage  riceStorage;

    void Start()
    {
        riceStorage = FindRiceStorage();

        if (riceStorage == null)
            Debug.LogError($"[WorkerCarryRice] '{name}': Không tìm thấy RiceStorage! " +
                           $"Gán tag 'RiceStorage' vào kho tạm.");
        else
            Debug.Log($"[WorkerCarryRice] '{name}': Tìm thấy RiceStorage '{riceStorage.gameObject.name}'.");
    }

    RiceStorage FindRiceStorage()
    {
        // 1. Ưu tiên Inspector
        if (riceStoragePoint != null)
        {
            RiceStorage rs = riceStoragePoint.GetComponent<RiceStorage>()
                          ?? riceStoragePoint.GetComponentInParent<RiceStorage>()
                          ?? riceStoragePoint.GetComponentInChildren<RiceStorage>();
            if (rs != null) return rs;
        }

        // 2. Tìm qua tag
        GameObject obj = GameObject.FindWithTag("RiceStorage");
        if (obj != null)
        {
            riceStoragePoint = obj.transform;
            RiceStorage rs = obj.GetComponent<RiceStorage>()
                          ?? obj.GetComponentInChildren<RiceStorage>();
            if (rs != null) return rs;
        }

        // 3. Fallback toàn scene
        RiceStorage fallback = FindObjectOfType<RiceStorage>();
        if (fallback != null)
        {
            Debug.LogWarning($"[WorkerCarryRice] Dùng fallback RiceStorage '{fallback.gameObject.name}'.");
            return fallback;
        }

        return null;
    }

    // ===== PUBLIC API =====

    public bool IsCarrying() => currentRice != null;

    public void PickupRice(RicePickup rice)
    {
        if (rice == null) return;
        if (rice.IsTaken()) return;

        rice.MarkTaken();
        currentRice = rice;
        currentRice.Pickup(handPoint);

        agent.ResetPath();

        Debug.Log($"[WorkerCarryRice] '{name}': Nhặt lúa → đang mang về kho tạm.");
    }

    public bool MoveToWarehouse()
    {
        if (currentRice == null || riceStoragePoint == null) return false;
        if (!agent.isOnNavMesh) return false;

        agent.isStopped = false;
        agent.SetDestination(riceStoragePoint.position);

        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance + 0.5f;
    }

    public bool TryDeposit()
    {
        if (currentRice == null) return false;

        // Kiểm tra kho tạm có đầy không
        if (riceStorage != null && riceStorage.IsFull)
        {
            Debug.Log($"[WorkerCarryRice] '{name}': Kho tạm đầy, không thể nộp!");
            return false;
        }

        // Trả object về pool
        ObjectPool pool = currentRice.pool;

        if (pool != null)
            pool.ReturnObject(currentRice.gameObject);
        else
            currentRice.gameObject.SetActive(false);

        currentRice = null;

        // Nộp vào kho TẠM — KHÔNG cộng UI, để WorkerCarrierRice lo
        if (riceStorage != null)
            riceStorage.AddRice(1);
        else
            Debug.LogWarning($"[WorkerCarryRice] '{name}': Không có RiceStorage — lúa bị mất!");

        agent.ResetPath();

        return true;
    }
}