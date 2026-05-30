using UnityEngine;
using UnityEngine.AI;

public class WorkerCarryItem : MonoBehaviour
{
    public Transform handPoint;
    public NavMeshAgent agent;
    [HideInInspector] public Transform house; 

    private WoodPickup currentWood;
    private WoodStorage woodStorage;

    void Start() => woodStorage = FindWoodStorage();

    void OnDisable()
    {
        if (currentWood != null)
        {
            ObjectPool pool = currentWood.pool;
            if (pool != null && currentWood.gameObject.activeInHierarchy) pool.ReturnObject(currentWood.gameObject);
            else Destroy(currentWood.gameObject);
            currentWood = null;
        }
    }

    WoodStorage FindWoodStorage()
    {
        if (house != null) return house.GetComponentInChildren<WoodStorage>() ?? house.GetComponentInParent<WoodStorage>();

        // FIX: Thống nhất tìm tag "Storage" theo cấu trúc của WoodStorage.cs thay vì "House"
        GameObject storageObj = GameObject.FindWithTag("Storage");
        if (storageObj != null)
        {
            house = storageObj.transform;
            return storageObj.GetComponent<WoodStorage>() ?? storageObj.GetComponentInChildren<WoodStorage>();
        }

        WoodStorage fallback = GameObject.FindObjectOfType<WoodStorage>();
        if (fallback != null) { house = fallback.transform; return fallback; }
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
    }

    public bool MoveToHouse()
    {
        if (currentWood == null || house == null || !agent.isOnNavMesh) return false;
        agent.isStopped = false;
        agent.SetDestination(house.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentWood == null) return false;
        if (woodStorage != null && woodStorage.IsFull) return false;

        ObjectPool pool = currentWood.pool;
        if (pool != null) pool.ReturnObject(currentWood.gameObject);
        else currentWood.gameObject.SetActive(false);

        currentWood = null;
        if (woodStorage != null) woodStorage.AddWood(1);
        return true;
    }
}