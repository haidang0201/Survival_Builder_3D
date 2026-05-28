using UnityEngine;
using UnityEngine.AI;

public class WorkerCarryRice : MonoBehaviour
{
    public Transform    handPoint;
    public NavMeshAgent agent;
    [HideInInspector] public Transform riceStoragePoint;   

    private RicePickup   currentRice;
    private RiceStorage  riceStorage;

    void Start()
    {
        riceStorage = FindRiceStorage();
    }

    // FIX: Giải phóng dọn dẹp lúa trên tay khi Worker bị hủy hoặc tắt kích hoạt đột ngột
    void OnDisable()
    {
        if (currentRice != null)
        {
            ObjectPool pool = currentRice.pool;
            if (pool != null && currentRice.gameObject.activeInHierarchy)
                pool.ReturnObject(currentRice.gameObject);
            else
                Destroy(currentRice.gameObject);
            
            currentRice = null;
        }
    }

    RiceStorage FindRiceStorage()
    {
        if (riceStoragePoint != null)
        {
            RiceStorage rs = riceStoragePoint.GetComponent<RiceStorage>() ?? riceStoragePoint.GetComponentInParent<RiceStorage>() ?? riceStoragePoint.GetComponentInChildren<RiceStorage>();
            if (rs != null) return rs;
        }

        GameObject obj = GameObject.FindWithTag("RiceStorage");
        if (obj != null)
        {
            riceStoragePoint = obj.transform;
            RiceStorage rs = obj.GetComponent<RiceStorage>() ?? obj.GetComponentInChildren<RiceStorage>();
            if (rs != null) return rs;
        }

        RiceStorage fallback = FindObjectOfType<RiceStorage>();
        if (fallback != null)
        {
            riceStoragePoint = fallback.transform;
            return fallback;
        }
        return null;
    }

    public bool IsCarrying() => currentRice != null;

    public void PickupRice(RicePickup rice)
    {
        if (rice == null || rice.IsTaken()) return;
        rice.MarkTaken();
        currentRice = rice;
        currentRice.Pickup(handPoint);
        agent.ResetPath();
    }

    public bool MoveToWarehouse() // Giữ nguyên tên gốc của bạn dù thực tế đi tới RiceStoragePoint
    {
        if (currentRice == null || riceStoragePoint == null || !agent.isOnNavMesh) return false;
        agent.isStopped = false;
        agent.SetDestination(riceStoragePoint.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentRice == null) return false;
        if (riceStorage != null && riceStorage.IsFull) return false;

        ObjectPool pool = currentRice.pool;
        if (pool != null) pool.ReturnObject(currentRice.gameObject);
        else currentRice.gameObject.SetActive(false);

        currentRice = null;
        if (riceStorage != null) riceStorage.AddRice(1);
        return true;
    }
}