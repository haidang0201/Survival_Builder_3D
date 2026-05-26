using UnityEngine;
using UnityEngine.AI;


public class WorkerCarryRice : MonoBehaviour
{
    public Transform    handPoint;
    public NavMeshAgent agent;
    public Transform    riceBarn;

    private RicePickup  currentRice;
    private RiceStorage riceStorage;

    void Start()
    {
        riceStorage = FindRiceStorage();

        if (riceStorage == null)
            Debug.LogError($"[WorkerCarryRice] '{name}': Không tìm thấy RiceStorage! " +
                           $"Gắn tag 'RiceBarn' vào kho lúa hoặc kéo thả vào Inspector.");
        else
            Debug.Log($"[WorkerCarryRice] '{name}': Tìm thấy RiceStorage trên '{riceStorage.gameObject.name}'.");
    }

    RiceStorage FindRiceStorage()
    {
        if (riceBarn != null)
        {
            RiceStorage rs = riceBarn.GetComponent<RiceStorage>();
            if (rs != null) return rs;

            rs = riceBarn.GetComponentInParent<RiceStorage>();
            if (rs != null)
            {
                Debug.Log($"[WorkerCarryRice] Tìm thấy RiceStorage trên parent '{rs.gameObject.name}'.");
                return rs;
            }

            rs = riceBarn.GetComponentInChildren<RiceStorage>();
            if (rs != null)
            {
                Debug.Log($"[WorkerCarryRice] Tìm thấy RiceStorage trên child '{rs.gameObject.name}'.");
                return rs;
            }
        }

        GameObject barnObj = GameObject.FindWithTag("RiceBarn");
        if (barnObj != null)
        {
            riceBarn = barnObj.transform;

            RiceStorage rs = barnObj.GetComponent<RiceStorage>();
            if (rs != null) return rs;

            rs = barnObj.GetComponentInChildren<RiceStorage>();
            if (rs != null) return rs;
        }

        RiceStorage fallback = GameObject.FindObjectOfType<RiceStorage>();
        if (fallback != null)
        {
            Debug.LogWarning($"[WorkerCarryRice] Dùng fallback — tìm thấy RiceStorage trên " +
                             $"'{fallback.gameObject.name}'. Nên gắn tag 'RiceBarn' đúng chỗ.");
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

        Debug.Log($"[WorkerCarryRice] '{name}': Nhặt lúa → đang mang về kho.");
    }

    public bool MoveToBarn()
    {
        if (currentRice == null || riceBarn == null) return false;
        if (!agent.isOnNavMesh) return false;

        agent.isStopped = false;
        agent.SetDestination(riceBarn.position);

        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance + 0.5f;
    }

    public bool TryDeposit()
    {
        if (currentRice == null) return false;

        if (riceStorage != null && riceStorage.IsFull)
        {
            Debug.Log($"[WorkerCarryRice] '{name}': Kho lúa đầy, không thể nộp!");
            return false;
        }

        // FIX: Lưu pool trước khi null currentRice
        // Nếu null currentRice trước rồi mới lấy pool → NullReferenceException
        ObjectPool pool = currentRice.pool;

        if (pool != null)
            pool.ReturnObject(currentRice.gameObject);
        else
            currentRice.gameObject.SetActive(false);

        currentRice = null; // null SAU khi đã dùng xong

        if (riceStorage != null)
            riceStorage.AddRice(1);
        else
            Debug.LogWarning($"[WorkerCarryRice] '{name}': Không có RiceStorage, lúa bị mất!");

        agent.ResetPath();

        return true;
    }
}