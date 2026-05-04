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
        currentWood = wood;

        Rigidbody rb = wood.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic) // 🔥 FIX
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        currentWood.Pickup(handPoint);

        if (agent.isOnNavMesh && house != null)
        {
            agent.SetDestination(house.position);
        }
    }

    void Update()
    {
        if (currentWood == null || house == null) return;

        if (agent.isOnNavMesh &&
            !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            Deposit();
        }
    }

    void Deposit()
    {
        if (currentWood == null) return;

        Debug.Log("Worker nộp gỗ!");

        ObjectPool pool = currentWood.pool;

        if (pool != null)
            pool.ReturnObject(currentWood.gameObject);
        else
            currentWood.gameObject.SetActive(false);

        currentWood = null;

        agent.ResetPath();
    }
}