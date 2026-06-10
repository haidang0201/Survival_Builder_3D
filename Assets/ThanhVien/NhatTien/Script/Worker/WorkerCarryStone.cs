using UnityEngine;
using UnityEngine.AI;

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
        stoneStorage = FindStoneStorage();
        workerStamina = GetComponent<WorkerStamina>();
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
        }
    }

    StoneStorage FindStoneStorage()
    {
        if (stoneStoragePoint != null)
        {
            StoneStorage ss = stoneStoragePoint.GetComponent<StoneStorage>() ?? stoneStoragePoint.GetComponentInParent<StoneStorage>() ?? stoneStoragePoint.GetComponentInChildren<StoneStorage>();
            if (ss != null) return ss;
        }

        GameObject obj = GameObject.FindWithTag("StoneStorage");
        if (obj != null)
        {
            StoneStorage ss = obj.GetComponent<StoneStorage>() ?? obj.GetComponentInChildren<StoneStorage>();
            if (ss != null)
            {
                stoneStoragePoint = obj.transform;
                return ss;
            }
        }

        StoneStorage fallback = FindObjectOfType<StoneStorage>();
        if (fallback != null)
        {
            stoneStoragePoint = fallback.transform;
            return fallback;
        }
        
        stoneStoragePoint = null;
        return null;
    }

    public bool IsCarrying() => currentStone != null;

    public void PickupStone(StonePickup stone)
    {
        if (stone == null || stone.IsTaken()) return;
        stone.MarkTaken();
        currentStone = stone;
        currentStone.Pickup(handPoint);
        agent.ResetPath();

        if (workerStamina != null) workerStamina.isCarryingResources = true;
    }

    public bool MoveToStorage()
    {
        if (currentStone == null || stoneStoragePoint == null || !agent.isOnNavMesh) return false;
        agent.isStopped = false;
        agent.SetDestination(stoneStoragePoint.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentStone == null) return false;
        
        if (stoneStorage == null) 
        {
            Debug.LogError($"[WorkerCarryStone] {name} KHÔNG tìm thấy StoneStorage (Kho tạm đá) trên Map. Hãy kiểm tra Tag!");
            return false; 
        }

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