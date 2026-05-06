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
    private bool hasTriggeredChopAnim = false;

    private float findTreeCooldown = 0f;
    private const float FIND_TREE_INTERVAL = 0.5f;

    void Update()
    {
        UpdateAnimationSpeed();

        if (carrySystem.IsCarrying())
        {
            HandleCarrying();
            return;
        }

        if (targetTree == null)
        {
            HandleFindTree();
            return;
        }

        float dist = Vector3.Distance(transform.position, targetTree.transform.position);

        if (dist > chopDistance)
        {
            HandleMoveToTree();
            return;
        }

        HandleChopping();
    }

    // ===== ANIMATION =====
    void UpdateAnimationSpeed()
    {
        if (animator != null && agent != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        }
    }

    // ===== MANG GỖ VỀ NHÀ =====
    void HandleCarrying()
    {
        carrySystem.MoveToHouse();

        bool arrived = !agent.pathPending &&
                       agent.remainingDistance <= agent.stoppingDistance + 0.5f;

        if (arrived)
        {
            carrySystem.TryDeposit();
        }
    }

    // ===== TÌM CÂY =====
    void HandleFindTree()
    {
        findTreeCooldown -= Time.deltaTime;

        if (findTreeCooldown <= 0f)
        {
            findTreeCooldown = FIND_TREE_INTERVAL;
            FindNearestTree();
        }
    }

    void FindNearestTree()
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
            chopTimer = 0f;
            hasTriggeredChopAnim = false;
            isChopping = false;
        }
    }

    // ===== DI CHUYỂN ĐẾN CÂY =====
    void HandleMoveToTree()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(targetTree.transform.position);
        }

        isChopping = false;
        hasTriggeredChopAnim = false;
    }

    // ===== CHẶT CÂY =====
    void HandleChopping()
    {
        agent.isStopped = true;

        // Chỉ trigger animation 1 lần mỗi chu kỳ chặt
        if (!hasTriggeredChopAnim)
        {
            hasTriggeredChopAnim = true;
            isChopping = true;
            TriggerChopAnimation();
        }

        chopTimer += Time.deltaTime;

        if (chopTimer >= chopTime)
        {
            chopTimer = 0f;
            hasTriggeredChopAnim = false;

            WoodPickup[] woods = targetTree.TakeDamage(1);

            if (woods != null && woods.Length > 0)
            {
                // Cây đã chết → nhặt gỗ và tìm cây mới
                carrySystem.PickupWood(woods[0]);
                ReleaseCurrentTree();
            }
            // Nếu woods == null → cây chưa chết, giữ nguyên target và chặt tiếp
        }
    }

    void TriggerChopAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger("Chop");
        animator.SetTrigger("Chop");
    }

    void ReleaseCurrentTree()
    {
        if (targetTree != null)
        {
            targetTree.Release();
            targetTree = null;
        }

        agent.isStopped = false;
        isChopping = false;
        hasTriggeredChopAnim = false;
    }
}