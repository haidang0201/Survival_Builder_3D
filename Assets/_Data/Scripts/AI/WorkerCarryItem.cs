using UnityEngine;
using UnityEngine.AI;

public class WorkerCarryItem : MonoBehaviour
{
    public Transform handPoint;
    public NavMeshAgent agent;
    public Transform house;

    private WoodPickup currentWood;

    void Start()
    {
        if (house == null)
        {
            house = GameObject.FindWithTag("House")?.transform;
        }
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

        ObjectPool pool = currentWood.pool;

        if (pool != null)
            pool.ReturnObject(currentWood.gameObject);
        else
            currentWood.gameObject.SetActive(false);

        currentWood = null;

        agent.ResetPath();

        return true;
    }
}