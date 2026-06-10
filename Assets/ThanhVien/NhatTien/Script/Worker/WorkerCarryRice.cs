using UnityEngine;
using UnityEngine.AI;

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
        riceStorage = FindRiceStorage();
        workerStamina = GetComponent<WorkerStamina>();
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
        if (riceStoragePoint != null)
        {
            RiceStorage rs = riceStoragePoint.GetComponent<RiceStorage>() ?? riceStoragePoint.GetComponentInParent<RiceStorage>() ?? riceStoragePoint.GetComponentInChildren<RiceStorage>();
            if (rs != null) return rs;
        }

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

        RiceStorage fallback = FindObjectOfType<RiceStorage>();
        if (fallback != null)
        {
            riceStoragePoint = fallback.transform;
            return fallback;
        }

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

        if (workerStamina != null) workerStamina.isCarryingResources = true;
    }

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
        
        if (riceStorage == null) 
        {
            Debug.LogError($"[WorkerCarryRice] {name} KHÔNG tìm thấy RiceStorage (Kho tạm) trên Map. Hãy kiểm tra lại!");
            return false; 
        }

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