using UnityEngine;
using UnityEngine.AI;

public class WorkerFindTree : MonoBehaviour
{
    public NavMeshAgent agent;
    public WorkerCarryItem carrySystem;

    public Animator animator; // 🎬 animation

    public float chopDistance = 2f;
    public float chopTime = 2f;

    private Tree targetTree;
    private float chopTimer = 0f;

    private bool isChopping = false;

    void Update()
    {
        // ================= ANIMATION (Idle / Run) =================
        if (animator != null && agent != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        }

        // ================= DEBUG =================
        Debug.Log("IsCarrying: " + carrySystem.IsCarrying());

        // ===== PRIORITY 1: MANG GỖ =====
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

        // ===== PRIORITY 2: TÌM CÂY =====
        if (targetTree == null)
        {
            FindTree();
            return;
        }

        // ===== PRIORITY 3: DI CHUYỂN =====
        float dist = Vector3.Distance(transform.position, targetTree.transform.position);

        if (dist > chopDistance)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(targetTree.transform.position);
            }

            // 🎬 không chặt → tắt state chặt
            if (animator != null)
                animator.SetBool("IsChopping", false);

            isChopping = false;
            return;
        }

        // ===== PRIORITY 4: CHẶT =====
        agent.isStopped = true;

        // 🎬 bật animation chặt
        if (!isChopping)
        {
            isChopping = true;

            if (animator != null)
                animator.SetTrigger("Chop");
        }

        chopTimer += Time.deltaTime;

        if (chopTimer >= chopTime)
        {
            chopTimer = 0f;

            Debug.Log("Đang chặt cây...");

            // ✅ DAMAGE vẫn từ LOGIC (KHÔNG phải animation)
            WoodPickup[] woods = targetTree.TakeDamage(1);

            if (woods != null && woods.Length > 0)
            {
                carrySystem.PickupWood(woods[0]);
            }

            targetTree.Release();
            targetTree = null;

            agent.isStopped = false;
            isChopping = false;

            // 🎬 tắt chặt
            if (animator != null)
                animator.SetBool("IsChopping", false);
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
            Debug.Log("Chọn cây: " + targetTree.name);
        }
        else
        {
            Debug.Log("KHÔNG TÌM THẤY CÂY!");
        }
    }
}