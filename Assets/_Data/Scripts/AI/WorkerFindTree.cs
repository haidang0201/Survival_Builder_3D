using UnityEngine;
using UnityEngine.AI;

public class WorkerFindTree : MonoBehaviour
{
    public NavMeshAgent agent;
    public WorkerCarryItem carrySystem;

    public Animator animator;

    public float chopDistance = 2f;
    public float chopTime = 2f;

    private Tree targetTree;
    private float chopTimer = 0f;

    private bool isChopping = false;

    void Update()
    {
        // Animation Idle / Run
        if (animator != null && agent != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        }

        // ===== MANG GỖ =====
        if (carrySystem.IsCarrying())
        {
            carrySystem.MoveToHouse();

            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                carrySystem.TryDeposit();
            }

            return;
        }

        // ===== TÌM CÂY =====
        if (targetTree == null)
        {
            FindTree();
            return;
        }

        // ===== DI CHUYỂN =====
        float dist = Vector3.Distance(transform.position, targetTree.transform.position);

        if (dist > chopDistance)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(targetTree.transform.position);
            }

            isChopping = false;
            return;
        }

        // ===== CHẶT =====
        agent.isStopped = true;

        // chỉ trigger 1 lần
        if (!isChopping)
        {
            isChopping = true;

            if (animator != null)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

                if (!state.IsName("ChopWorker"))
                {
                    animator.ResetTrigger("Chop");
                    animator.SetTrigger("Chop");
                }
            }
        }

        chopTimer += Time.deltaTime;

        if (chopTimer >= chopTime)
        {
            chopTimer = 0f;

            WoodPickup[] woods = targetTree.TakeDamage(1);

            if (woods != null && woods.Length > 0)
            {
                carrySystem.PickupWood(woods[0]);
            }

            targetTree.Release();
            targetTree = null;

            agent.isStopped = false;
            isChopping = false;
        }
    }

    void FindTree()
    {
        Tree[] trees = GameObject.FindObjectsOfType<Tree>();

        float minDist = Mathf.Infinity;
        Tree best = null;

        foreach (var tree in trees)
        {
            if (!tree.gameObject.activeInHierarchy) continue;
            if (!tree.TryClaim()) continue;

            float dist = Vector3.Distance(transform.position, tree.transform.position);

            if (dist < minDist)
            {
                if (best != null) best.Release();

                minDist = dist;
                best = tree;
            }
            else
            {
                tree.Release();
            }
        }

        if (best != null)
        {
            targetTree = best;
        }
    }
}