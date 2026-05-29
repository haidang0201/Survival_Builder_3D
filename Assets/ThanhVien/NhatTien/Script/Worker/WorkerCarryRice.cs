using UnityEngine;
using UnityEngine.AI;

public class WorkerCarryRice : MonoBehaviour
{
    public Transform    handPoint;
    public NavMeshAgent agent;
    
    // Đã bỏ [HideInInspector] để bạn dễ kiểm tra xem Nông dân đang đi đâu
    public Transform riceStoragePoint;   

    private RicePickup   currentRice;
    private RiceStorage  riceStorage;

    void Start()
    {
        riceStorage = FindRiceStorage();
    }

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
        // 1. Kiểm tra điểm gán tay
        if (riceStoragePoint != null)
        {
            RiceStorage rs = riceStoragePoint.GetComponent<RiceStorage>() ?? riceStoragePoint.GetComponentInParent<RiceStorage>() ?? riceStoragePoint.GetComponentInChildren<RiceStorage>();
            if (rs != null) return rs;
        }

        // 2. Tìm bằng Tag
        GameObject obj = GameObject.FindWithTag("RiceStorage");
        if (obj != null)
        {
            RiceStorage rs = obj.GetComponent<RiceStorage>() ?? obj.GetComponentInChildren<RiceStorage>();
            if (rs != null) 
            {
                riceStoragePoint = obj.transform;
                return rs;
            }
        }

        // 3. Quét toàn map
        RiceStorage fallback = FindObjectOfType<RiceStorage>();
        if (fallback != null)
        {
            riceStoragePoint = fallback.transform;
            return fallback;
        }

        // FIX: Nếu không tìm thấy kho tạm nào, PHẢI xóa điểm đến để Nông dân không đi bậy bạ
        riceStoragePoint = null;
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

    // FIX: Đổi tên chuẩn xác thành MoveToStorage (Đi tới kho tạm)
    public bool MoveToStorage() 
    {
        if (currentRice == null || riceStoragePoint == null || !agent.isOnNavMesh) return false;
        agent.isStopped = false;
        agent.SetDestination(riceStoragePoint.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentRice == null) return false;
        
        // FIX "HỐ ĐEN": Chặn ngay lập tức nếu không tìm thấy kho tạm
        if (riceStorage == null) 
        {
            Debug.LogError($"[WorkerCarryRice] {name} KHÔNG tìm thấy RiceStorage (Kho tạm) trên Map. Hãy kiểm tra lại!");
            return false; // Trả về false để bắt Nông dân đứng đợi, tuyệt đối không vứt lúa!
        }

        if (riceStorage.IsFull) return false;

        ObjectPool pool = currentRice.pool;
        if (pool != null) pool.ReturnObject(currentRice.gameObject);
        else currentRice.gameObject.SetActive(false);

        currentRice = null;
        riceStorage.AddRice(1);
        return true;
    }
}