using UnityEngine;
using UnityEngine.AI;

public class WorkerCarryItem : MonoBehaviour
{
    public Transform handPoint;
    public NavMeshAgent agent;
    public Transform house;

    private WoodPickup currentWood;
    private WoodStorage woodStorage;

    void Start()
    {
        woodStorage = FindWoodStorage();

        if (woodStorage == null)
            Debug.LogError($"[WorkerCarryItem] {name}: Không tìm thấy WoodStorage ở bất kỳ đâu! " +
                           $"Hãy gắn WoodStorage vào House GameObject.");
        else
            Debug.Log($"[WorkerCarryItem] {name}: Tìm thấy WoodStorage trên '{woodStorage.gameObject.name}'.");
    }

    WoodStorage FindWoodStorage()
    {
        // 1. Ưu tiên: tìm trực tiếp trên house được gán trong Inspector
        if (house != null)
        {
            WoodStorage ws = house.GetComponent<WoodStorage>();
            if (ws != null) return ws;

            // 2. house là child → tìm lên parent
            ws = house.GetComponentInParent<WoodStorage>();
            if (ws != null)
            {
                Debug.Log($"[WorkerCarryItem] Tìm thấy WoodStorage trên parent '{ws.gameObject.name}' " +
                          $"(house Inspector trỏ vào child '{house.name}').");
                return ws;
            }

            // 3. house là parent → tìm xuống children
            ws = house.GetComponentInChildren<WoodStorage>();
            if (ws != null)
            {
                Debug.Log($"[WorkerCarryItem] Tìm thấy WoodStorage trên child '{ws.gameObject.name}'.");
                return ws;
            }
        }

        // 4. Fallback: tìm qua tag "House"
        GameObject houseObj = GameObject.FindWithTag("House");
        if (houseObj != null)
        {
            house = houseObj.transform;

            WoodStorage ws = houseObj.GetComponent<WoodStorage>();
            if (ws != null) return ws;

            ws = houseObj.GetComponentInChildren<WoodStorage>();
            if (ws != null) return ws;
        }

        // 5. Fallback cuối: tìm toàn scene
        WoodStorage fallback = GameObject.FindObjectOfType<WoodStorage>();
        if (fallback != null)
        {
            Debug.LogWarning($"[WorkerCarryItem] Dùng fallback — tìm thấy WoodStorage trên " +
                             $"'{fallback.gameObject.name}'. Nên gắn tag 'House' đúng chỗ.");
            return fallback;
        }

        return null;
    }

    public bool IsCarrying()
    {
        return currentWood != null;
    }

    public void PickupWood(WoodPickup wood)
    {
        if (wood == null) return;
        if (wood.IsTaken()) return;

        wood.MarkTaken();
        currentWood = wood;
        currentWood.Pickup(handPoint);

        agent.ResetPath();
    }

    // Trả về true nếu đã đến nơi
    public bool MoveToHouse()
    {
        if (currentWood == null || house == null) return false;
        if (!agent.isOnNavMesh) return false;

        agent.isStopped = false;
        agent.SetDestination(house.position);

        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance + 0.5f;
    }

    public bool TryDeposit()
    {
        if (currentWood == null) return false;

        // Kiểm tra kho trước khi deposit
        if (woodStorage != null && woodStorage.IsFull)
        {
            Debug.LogWarning($"[WorkerCarryItem] {name}: Kho đầy, không thể nộp gỗ!");
            return false;
        }

        // Trả gỗ về pool
        ObjectPool pool = currentWood.pool;

        if (pool != null)
            pool.ReturnObject(currentWood.gameObject);
        else
            currentWood.gameObject.SetActive(false);

        currentWood = null;

        // Thêm vào kho
        if (woodStorage != null)
            woodStorage.AddWood(1);
        else
            Debug.LogWarning($"[WorkerCarryItem] {name}: Không có WoodStorage, gỗ bị mất!");

        agent.ResetPath();

        return true;
    }
}