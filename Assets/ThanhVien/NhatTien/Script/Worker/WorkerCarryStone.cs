using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class WorkerCarryStone : MonoBehaviour
{
    public Transform    handPoint;
    public NavMeshAgent agent;
    public Transform stoneStoragePoint;   

    private StonePickup  currentStone;
    private StoneStorage stoneStorage;
    private WorkerStamina workerStamina;

    void Start()
    {
        workerStamina = GetComponent<WorkerStamina>();
        stoneStorage = FindNearestStoneStorage(out Transform point);
        if (stoneStorage != null) stoneStoragePoint = point;
    }

    void OnDisable()
    {
        if (currentStone != null)
        {
            ObjectPool pool = currentStone.pool;
            if (pool != null && currentStone.gameObject.activeInHierarchy) 
                pool.ReturnObject(currentStone.gameObject);
            else 
                Destroy(currentStone.gameObject);
            
            currentStone = null;

            // FIX: đảm bảo Stamina không bị kẹt ở trạng thái "đang ôm hàng"
            // (nếu không, isCarryingResources/isReturnPending có thể bị kẹt true vĩnh viễn
            // khi vật phẩm bị huỷ đột ngột, ví dụ do cơ chế chống-kẹt ép reset carrySystem)
            if (workerStamina != null) workerStamina.OnResourcesDeposited();
        }
    }

    /// <summary>
    /// Quét tất cả GameObject có Tag "StoneStorage", chọn kho GẦN NHẤT còn chỗ (chưa IsFull).
    /// Nếu tất cả đều đầy, trả về kho gần nhất (dù đầy) để không bị null.
    /// </summary>
    StoneStorage FindNearestStoneStorage(out Transform chosenPoint)
    {
        chosenPoint = null;

        GameObject[] candidates = GameObject.FindGameObjectsWithTag("StoneStorage");
        if (candidates == null || candidates.Length == 0)
        {
            if (stoneStoragePoint != null)
            {
                StoneStorage ss = stoneStoragePoint.GetComponent<StoneStorage>() ?? stoneStoragePoint.GetComponentInParent<StoneStorage>() ?? stoneStoragePoint.GetComponentInChildren<StoneStorage>();
                if (ss != null) { chosenPoint = stoneStoragePoint; return ss; }
            }
            StoneStorage fallback = FindObjectOfType<StoneStorage>();
            if (fallback != null) { chosenPoint = FindDeliveryPoint(fallback.transform); return fallback; }
            return null;
        }

        List<(StoneStorage storage, Transform point, float dist)> found = new List<(StoneStorage, Transform, float)>();
        foreach (GameObject obj in candidates)
        {
            StoneStorage ss = obj.GetComponent<StoneStorage>() ?? obj.GetComponentInChildren<StoneStorage>();
            if (ss == null) continue;

            // Dùng cửa kho (child "DeliveryPoint") làm điểm đến thay vì tâm kho
            Transform deliveryPoint = FindDeliveryPoint(obj.transform);
            float d = Vector3.Distance(transform.position, deliveryPoint.position);
            found.Add((ss, deliveryPoint, d));
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

    public bool IsCarrying() => currentStone != null;

    public void PickupStone(StonePickup stone)
    {
        if (stone == null || stone.IsTaken()) return;
        stone.MarkTaken();
        currentStone = stone;
        currentStone.Pickup(handPoint);
        agent.ResetPath();

        // Chọn kho gần nhất còn chỗ tại thời điểm nhặt xong (1 lần duy nhất cho chuyến này)
        StoneStorage chosen = FindNearestStoneStorage(out Transform point);
        if (chosen != null)
        {
            stoneStorage = chosen;
            stoneStoragePoint = point;
        }

        if (workerStamina != null) workerStamina.isCarryingResources = true;
    }

    public bool MoveToStorage()
    {
        if (currentStone == null) return false;

        // Retry tìm kho nếu chưa có hoặc kho đã đầy
        if (stoneStorage == null || stoneStoragePoint == null || stoneStorage.IsFull)
        {
            StoneStorage found = FindNearestStoneStorage(out Transform point);
            if (found != null) { stoneStorage = found; stoneStoragePoint = point; }
        }

        if (stoneStoragePoint == null || !agent.isOnNavMesh)
        {
            // Chưa có kho — dừng agent, chờ đặt kho xong rồi tự chạy lại
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return false;
        }

        agent.isStopped = false;
        agent.SetDestination(stoneStoragePoint.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentStone == null) return false;
        if (stoneStorage == null) return false; // Chưa có kho — im lặng chờ, không log lỗi
        if (stoneStorage.IsFull) return false;

        ObjectPool pool = currentStone.pool;
        if (pool != null) pool.ReturnObject(currentStone.gameObject);
        else currentStone.gameObject.SetActive(false);

        currentStone = null;
        stoneStorage.AddStone(1);

        if (workerStamina != null) workerStamina.OnResourcesDeposited();

        return true;
    }
}