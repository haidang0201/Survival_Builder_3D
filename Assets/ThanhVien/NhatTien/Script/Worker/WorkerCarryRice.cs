using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class WorkerCarryRice : MonoBehaviour
{
    public Transform    handPoint;
    public NavMeshAgent agent;
    public Transform riceStoragePoint;   

    private RicePickup   currentRice;
    private RiceStorage  riceStorage;
    private WorkerStamina workerStamina;

    void Start()
    {
        workerStamina = GetComponent<WorkerStamina>();
        riceStorage = FindNearestRiceStorage(out Transform point);
        if (riceStorage != null) riceStoragePoint = point;
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

            // FIX: đảm bảo Stamina không bị kẹt ở trạng thái "đang ôm hàng"
            // (nếu không, isCarryingResources/isReturnPending có thể bị kẹt true vĩnh viễn
            // khi vật phẩm bị huỷ đột ngột, ví dụ do cơ chế chống-kẹt ép reset carrySystem)
            if (workerStamina != null) workerStamina.OnResourcesDeposited();
        }
    }

    /// <summary>
    /// Quét tất cả GameObject có Tag "RiceStorage", chọn kho GẦN NHẤT còn chỗ (chưa IsFull).
    /// Nếu tất cả đều đầy, trả về kho gần nhất (dù đầy) để không bị null.
    /// </summary>
    RiceStorage FindNearestRiceStorage(out Transform chosenPoint)
    {
        chosenPoint = null;

        GameObject[] candidates = GameObject.FindGameObjectsWithTag("RiceStorage");
        if (candidates == null || candidates.Length == 0)
        {
            if (riceStoragePoint != null)
            {
                RiceStorage rs = riceStoragePoint.GetComponent<RiceStorage>() ?? riceStoragePoint.GetComponentInParent<RiceStorage>() ?? riceStoragePoint.GetComponentInChildren<RiceStorage>();
                if (rs != null) { chosenPoint = riceStoragePoint; return rs; }
            }
            RiceStorage fallback = FindObjectOfType<RiceStorage>();
            if (fallback != null) { chosenPoint = FindDeliveryPoint(fallback.transform); return fallback; }
            return null;
        }

        List<(RiceStorage storage, Transform point, float dist)> found = new List<(RiceStorage, Transform, float)>();
        foreach (GameObject obj in candidates)
        {
            RiceStorage rs = obj.GetComponent<RiceStorage>() ?? obj.GetComponentInChildren<RiceStorage>();
            if (rs == null) continue;

            // Dùng cửa kho (child "DeliveryPoint") làm điểm đến thay vì tâm kho
            Transform deliveryPoint = FindDeliveryPoint(obj.transform);
            float d = Vector3.Distance(transform.position, deliveryPoint.position);
            found.Add((rs, deliveryPoint, d));
        }

        if (found.Count == 0) return null;

        var ordered = found.OrderBy(f => f.dist).ToList();

        var notFull = ordered.FirstOrDefault(f => !f.storage.IsFull);
        if (notFull.storage != null)
        {
            chosenPoint = notFull.point;
            return notFull.storage;
        }

        chosenPoint = ordered[0].point;
        return ordered[0].storage;
    }

    /// <summary>
    /// Tìm child Transform tên "DeliveryPoint" bên trong kho (cửa kho, nơi worker thực sự đi tới).
    /// Nếu không có, fallback về chính transform của kho để không bị null.
    /// </summary>
    Transform FindDeliveryPoint(Transform storageRoot)
    {
        Transform dp = storageRoot.Find("DeliveryPoint");
        if (dp != null) return dp;

        foreach (Transform child in storageRoot.GetComponentsInChildren<Transform>())
        {
            if (child.name == "DeliveryPoint") return child;
        }

        return storageRoot;
    }

    public bool IsCarrying() => currentRice != null;

    public void PickUpFakeItemForLoad()
    {
        GameObject fakeItem = new GameObject("FakeRice_Loaded");
        fakeItem.transform.SetParent(handPoint);
        fakeItem.transform.localPosition = Vector3.zero;
        currentRice = fakeItem.AddComponent<RicePickup>();
    }

    public void PickupRice(RicePickup rice)
    {
        if (rice == null || rice.IsTaken()) return;
        rice.MarkTaken();
        currentRice = rice;
        currentRice.Pickup(handPoint);
        agent.ResetPath();

        // Chọn kho gần nhất còn chỗ tại thời điểm nhặt xong (1 lần duy nhất cho chuyến này)
        RiceStorage chosen = FindNearestRiceStorage(out Transform point);
        if (chosen != null)
        {
            riceStorage = chosen;
            riceStoragePoint = point;
        }

        if (workerStamina != null) workerStamina.isCarryingResources = true;
    }

    public bool MoveToStorage() 
    {
        if (currentRice == null) return false;

        // Retry tìm kho nếu chưa có hoặc kho đã đầy
        if (riceStorage == null || riceStoragePoint == null || riceStorage.IsFull)
        {
            RiceStorage found = FindNearestRiceStorage(out Transform point);
            if (found != null) { riceStorage = found; riceStoragePoint = point; }
        }

        if (riceStoragePoint == null || !agent.isOnNavMesh)
        {
            // Chưa có kho — dừng agent, chờ đặt kho xong rồi tự chạy lại
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return false;
        }

        agent.isStopped = false;
        agent.SetDestination(riceStoragePoint.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentRice == null) return false;
        if (riceStorage == null) return false; // Chưa có kho — im lặng chờ, không log lỗi
        if (riceStorage.IsFull) return false;

        ObjectPool pool = currentRice.pool;
        if (pool != null) pool.ReturnObject(currentRice.gameObject);
        else currentRice.gameObject.SetActive(false);

        currentRice = null;
        riceStorage.AddRice(1);

        if (workerStamina != null) workerStamina.OnResourcesDeposited();

        return true;
    }
}