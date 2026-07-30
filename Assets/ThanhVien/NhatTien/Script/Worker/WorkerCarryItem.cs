using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class WorkerCarryItem : MonoBehaviour
{
    public Transform handPoint;
    public NavMeshAgent agent;
    public Transform woodStoragePoint;

    private WoodPickup currentWood;
    private WoodStorage woodStorage;
    private WorkerStamina workerStamina; // Thêm tham chiếu

    void Start()
    {
        workerStamina = GetComponent<WorkerStamina>(); // Lấy tham chiếu
        // Chọn tạm 1 kho làm mặc định (sẽ được chọn lại chính xác khi PickupWood)
        woodStorage = FindNearestWoodStorage(out Transform point);
        if (woodStorage != null) woodStoragePoint = point;
    }

    void OnDisable()
    {
        if (currentWood != null)
        {
            ObjectPool pool = currentWood.pool;
            if (pool != null && currentWood.gameObject.activeInHierarchy)
                pool.ReturnObject(currentWood.gameObject);
            else
                Destroy(currentWood.gameObject);

            currentWood = null;

            // FIX: đảm bảo Stamina không bị kẹt ở trạng thái "đang ôm hàng"
            // (nếu không, isCarryingResources/isReturnPending có thể bị kẹt true vĩnh viễn
            // khi vật phẩm bị huỷ đột ngột, ví dụ do cơ chế chống-kẹt ép reset carrySystem)
            if (workerStamina != null) workerStamina.OnResourcesDeposited();
        }
    }

    /// <summary>
    /// Quét tất cả GameObject có Tag "Storage", sắp xếp theo khoảng cách gần -> xa
    /// tính từ vị trí worker hiện tại, và chọn kho GẦN NHẤT còn chỗ (chưa IsFull).
    /// Nếu tất cả đều đầy, trả về kho gần nhất (dù đầy) để không bị null.
    /// </summary>
    WoodStorage FindNearestWoodStorage(out Transform chosenPoint)
    {
        chosenPoint = null;

        GameObject[] candidates = GameObject.FindGameObjectsWithTag("Storage");
        if (candidates == null || candidates.Length == 0)
        {
            // Fallback: dùng field cũ nếu có gán tay, hoặc FindObjectOfType
            if (woodStoragePoint != null)
            {
                WoodStorage ws = woodStoragePoint.GetComponent<WoodStorage>() ?? woodStoragePoint.GetComponentInParent<WoodStorage>() ?? woodStoragePoint.GetComponentInChildren<WoodStorage>();
                if (ws != null) { chosenPoint = woodStoragePoint; return ws; }
            }
            WoodStorage fallback = FindObjectOfType<WoodStorage>();
            if (fallback != null) { chosenPoint = FindDeliveryPoint(fallback.transform); return fallback; }
            return null;
        }

        List<(WoodStorage storage, Transform point, float dist)> found = new List<(WoodStorage, Transform, float)>();
        foreach (GameObject obj in candidates)
        {
            WoodStorage ws = obj.GetComponent<WoodStorage>() ?? obj.GetComponentInChildren<WoodStorage>();
            if (ws == null) continue;

            // Dùng cửa kho (child "DeliveryPoint") làm điểm đến thay vì tâm kho
            Transform deliveryPoint = FindDeliveryPoint(obj.transform);
            float d = Vector3.Distance(transform.position, deliveryPoint.position);
            found.Add((ws, deliveryPoint, d));
        }

        if (found.Count == 0) return null;

        var ordered = found.OrderBy(f => f.dist).ToList();

        // Ưu tiên kho gần nhất còn chỗ
        var notFull = ordered.FirstOrDefault(f => !f.storage.IsFull);
        if (notFull.storage != null)
        {
            chosenPoint = notFull.point;
            return notFull.storage;
        }

        // Tất cả đều đầy -> chọn kho gần nhất
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

        // Tìm sâu hơn trong trường hợp DeliveryPoint không phải con trực tiếp
        foreach (Transform child in storageRoot.GetComponentsInChildren<Transform>())
        {
            if (child.name == "DeliveryPoint") return child;
        }

        return storageRoot;
    }

    public bool IsCarrying() => currentWood != null;

    public void PickUpFakeItemForLoad()
    {
        // Khi load game, sinh ngay một cục tài nguyên giả trên tay để mang về nộp
        GameObject fakeItem = new GameObject("FakeWood_Loaded");
        fakeItem.transform.SetParent(handPoint);
        fakeItem.transform.localPosition = Vector3.zero;
        currentWood = fakeItem.AddComponent<WoodPickup>();
    }

    public void PickupWood(WoodPickup wood)
    {
        if (wood == null || wood.IsTaken()) return;
        wood.MarkTaken();
        currentWood = wood;
        currentWood.Pickup(handPoint);
        agent.ResetPath();

        // Chọn kho gần nhất còn chỗ tại thời điểm nhặt xong (1 lần duy nhất cho chuyến này)
        WoodStorage chosen = FindNearestWoodStorage(out Transform point);
        if (chosen != null)
        {
            woodStorage = chosen;
            woodStoragePoint = point;
        }

        // Báo cho Stamina biết đã cầm đồ
        if (workerStamina != null) workerStamina.isCarryingResources = true;
    }

    public bool MoveToStorage()
    {
        if (currentWood == null) return false;

        // Retry tìm kho nếu chưa có hoặc kho đã đầy
        if (woodStorage == null || woodStoragePoint == null || woodStorage.IsFull)
        {
            WoodStorage found = FindNearestWoodStorage(out Transform point);
            if (found != null) { woodStorage = found; woodStoragePoint = point; }
        }

        if (woodStoragePoint == null || !agent.isOnNavMesh)
        {
            // Chưa có kho — dừng agent, chờ đặt kho xong rồi tự chạy lại
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return false;
        }

        agent.isStopped = false;
        agent.SetDestination(woodStoragePoint.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentWood == null) return false;
        if (woodStorage == null) return false; // Chưa có kho — im lặng chờ, không log lỗi
        if (woodStorage.IsFull) return false;

        ObjectPool pool = currentWood.pool;
        if (pool != null) pool.ReturnObject(currentWood.gameObject);
        else Destroy(currentWood.gameObject);

        currentWood = null;
        woodStorage.AddWood(1);
        RoKQuestMissionGuideRouter.Instance?.RegisterWorkerWoodGathered(1);

        // Báo cho Stamina biết đã nộp đồ xong
        if (workerStamina != null) workerStamina.OnResourcesDeposited();

        return true;
    }
}