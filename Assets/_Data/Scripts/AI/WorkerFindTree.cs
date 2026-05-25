using UnityEngine;
using UnityEngine.AI;

public class WorkerFindTree : MonoBehaviour
{
    public NavMeshAgent    agent;
    public WorkerCarryItem carrySystem;
    public Animator        animator;
    public WorkerStamina   stamina;

    public float chopDistance = 2f;
    public float chopTime     = 2f;

    private Tree  targetTree;
    private float chopTimer            = 0f;
    private bool  hasTriggeredChopAnim = false;

    // Dùng để phát hiện khoảnh khắc chuyển sang nghỉ — chỉ reset anim 1 lần
    private bool wasResting = false;

    private float findTreeCooldown         = 0f;
    private const float FIND_TREE_INTERVAL = 0.5f;

    void Start()
    {
        if (stamina == null)
            stamina = GetComponent<WorkerStamina>();

        if (stamina == null)
            Debug.LogWarning($"[WorkerFindTree] '{name}': Không có WorkerStamina — worker làm không giới hạn.");
    }

    void Update()
    {
        UpdateAnimationSpeed();

        bool isResting = stamina != null && !stamina.CanWork();

        if (isResting)
        {
            // Chỉ reset anim đúng 1 lần tại frame chuyển sang nghỉ
            // BUG FIX: không set Speed=0f mỗi frame vì UpdateAnimationSpeed()
            // ở trên sẽ ghi đè lại → walk animation không chạy khi đi về RestSpot
            if (!wasResting)
            {
                wasResting = true;

                if (targetTree != null)
                {
                    targetTree.Release();
                    targetTree = null;
                }

                if (animator != null)
                    animator.ResetTrigger("Chop"); // xóa trigger pending, không set Speed

                hasTriggeredChopAnim = false;
                chopTimer            = 0f;
            }

            return; // nhường agent cho WorkerStamina
        }

        // Vừa hết nghỉ → reset flag
        wasResting = false;

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

        // Cây bị tắt từ bên ngoài → tìm cây mới
        if (!targetTree.gameObject.activeInHierarchy)
        {
            ReleaseCurrentTree();
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
        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }

    // ===== MANG GỖ VỀ NHÀ =====
    void HandleCarrying()
    {
        carrySystem.MoveToHouse();

        bool arrived = !agent.pathPending &&
                       agent.remainingDistance <= agent.stoppingDistance + 0.5f;

        if (arrived)
            carrySystem.TryDeposit();
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
        Tree  best    = null;

        foreach (var tree in trees)
        {
            if (!tree.gameObject.activeInHierarchy) continue;
            if (!tree.TryClaim()) continue;

            float dist = Vector3.Distance(transform.position, tree.transform.position);

            if (dist < minDist)
            {
                if (best != null) best.Release();
                minDist = dist;
                best    = tree;
            }
            else
            {
                tree.Release();
            }
        }

        if (best != null)
        {
            targetTree           = best;
            chopTimer            = 0f;
            hasTriggeredChopAnim = false;
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

        hasTriggeredChopAnim = false;
    }

    // ===== CHẶT CÂY =====
    void HandleChopping()
    {
        agent.isStopped = true;
        stamina?.SetDraining(true);

        if (!hasTriggeredChopAnim)
        {
            hasTriggeredChopAnim = true;
            TriggerChopAnimation();
        }

        chopTimer += Time.deltaTime;

        if (chopTimer < chopTime) return;

        chopTimer            = 0f;
        hasTriggeredChopAnim = false;

        WoodPickup[] woods = targetTree.TakeDamage(1);

        if (woods != null && woods.Length > 0)
        {
            carrySystem.PickupWood(woods[0]);
            ReleaseCurrentTree();
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

        agent.isStopped      = false;
        hasTriggeredChopAnim = false;
        chopTimer            = 0f;
    }
}