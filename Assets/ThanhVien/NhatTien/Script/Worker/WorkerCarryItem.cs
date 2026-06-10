using UnityEngine;
using UnityEngine.AI;

public class WorkerCarryItem : MonoBehaviour
{
    public Transform    handPoint;
    public NavMeshAgent agent;
    public Transform    woodStoragePoint;   

    private WoodPickup  currentWood;
    private WoodStorage woodStorage;
    private WorkerStamina workerStamina; // Thêm tham chiếu

    void Start()
    {
        woodStorage = FindWoodStorage();
        workerStamina = GetComponent<WorkerStamina>(); // Lấy tham chiếu
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
        }
    }

    WoodStorage FindWoodStorage()
    {
        if (woodStoragePoint != null)
        {
            WoodStorage ws = woodStoragePoint.GetComponent<WoodStorage>() ?? woodStoragePoint.GetComponentInParent<WoodStorage>() ?? woodStoragePoint.GetComponentInChildren<WoodStorage>();
            if (ws != null) return ws;
        }

        GameObject obj = GameObject.FindWithTag("Storage");
        if (obj != null)
        {
            WoodStorage ws = obj.GetComponent<WoodStorage>() ?? obj.GetComponentInChildren<WoodStorage>();
            if (ws != null)
            {
                woodStoragePoint = obj.transform;
                return ws;
            }
        }

        WoodStorage fallback = FindObjectOfType<WoodStorage>();
        if (fallback != null)
        {
            woodStoragePoint = fallback.transform;
            return fallback;
        }
        
        woodStoragePoint = null;
        return null;
    }

    public bool IsCarrying() => currentWood != null;

    public void PickupWood(WoodPickup wood)
    {
        if (wood == null || wood.IsTaken()) return;
        wood.MarkTaken();
        currentWood = wood;
        currentWood.Pickup(handPoint);
        agent.ResetPath();

        // Báo cho Stamina biết đã cầm đồ
        if (workerStamina != null) workerStamina.isCarryingResources = true; 
    }

    public bool MoveToStorage()
    {
        if (currentWood == null || woodStoragePoint == null || !agent.isOnNavMesh) return false;
        agent.isStopped = false;
        agent.SetDestination(woodStoragePoint.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentWood == null) return false;
        
        if (woodStorage == null) 
        {
            Debug.LogError($"[WorkerCarryItem] {name} KHÔNG tìm thấy WoodStorage trên Map. Hãy kiểm tra Tag 'Storage'!");
            return false; 
        }

        if (woodStorage.IsFull) return false;

        ObjectPool pool = currentWood.pool;
        if (pool != null) pool.ReturnObject(currentWood.gameObject);
        else currentWood.gameObject.SetActive(false);

        currentWood = null;
        woodStorage.AddWood(1);

        // Báo cho Stamina biết đã nộp đồ xong
        if (workerStamina != null) workerStamina.OnResourcesDeposited(); 

        return true;
    }
}