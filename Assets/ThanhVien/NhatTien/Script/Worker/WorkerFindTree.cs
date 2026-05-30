using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class WorkerFindTree : MonoBehaviour
{
    public static List<Tree> Registry = new List<Tree>(); 

    public NavMeshAgent    agent;
    public WorkerCarryItem carrySystem;
    public Animator        animator;
    public WorkerStamina   stamina;

    public float chopDistance = 2f;
    public float chopTime     = 2f;

    [Header("Settings Nâng Cấp")]
    public float stuckTimeout = 2.0f;
    private float stuckTimer = 0f;
    private float depositRetryTimer = 0f; 

    private Tree  targetTree;
    private float chopTimer            = 0f;
    private bool  hasTriggeredChopAnim = false;
    private bool  wasResting           = false;
    private float findTreeCooldown     = 0f;
    private const float FIND_TREE_INTERVAL = 0.5f;

    // FIX: Tạo các Flag ngăn chặn việc gọi trùng lệnh di chuyển mỗi frame
    private bool isHeadingToTree    = false;
    private bool isHeadingToDeposit = false;

    void Start()
    {
        if (stamina == null) stamina = GetComponent<WorkerStamina>();
    }

    void Update()
    {
        UpdateAnimationSpeed();
        CheckStuck(); 

        if (stamina != null && !stamina.CanWork())
        {
            if (!wasResting)
            {
                wasResting = true;
                ReleaseCurrentTree();
                if (animator != null) animator.ResetTrigger("Chop");
                hasTriggeredChopAnim = false;
                chopTimer            = 0f;
                isHeadingToTree      = false;
                isHeadingToDeposit   = false;
            }
            return; 
        }

        wasResting = false;

        if (carrySystem.IsCarrying())
        {
            isHeadingToTree = false; // Reset trạng thái đi tìm cây cũ
            HandleCarrying();
            return;
        }

        isHeadingToDeposit = false; // Reset trạng thái đi cất hàng cũ

        if (targetTree == null)
        {
            HandleFindTree();
            return;
        }

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

    void UpdateAnimationSpeed()
    {
        if (animator == null || agent == null) return;
        animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    void HandleCarrying()
    {
        // FIX: Chỉ gọi SetDestination đi cất hàng 1 lần duy nhất thay vì mỗi frame
        if (!isHeadingToDeposit)
        {
            isHeadingToDeposit = true;
            carrySystem.MoveToHouse();
        }

        bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
        if (arrived)
        {
            agent.isStopped = true;
            depositRetryTimer -= Time.deltaTime;
            if (depositRetryTimer <= 0f)
            {
                bool success = carrySystem.TryDeposit();
                if (!success)
                {
                    depositRetryTimer = 2.5f; 
                    Debug.LogWarning($"[WorkerFindTree] {name}: Nhà kho đầy gỗ!");
                }
                else
                {
                    depositRetryTimer = 0f;
                    isHeadingToDeposit = false;
                }
            }
        }
        else
        {
            depositRetryTimer = 0f;
        }
    }

    void HandleFindTree()
    {
        findTreeCooldown -= Time.deltaTime;
        if (findTreeCooldown <= 0f)
        {
            findTreeCooldown = FIND_TREE_INTERVAL;
            FindNearestTreeOptimized();
        }
    }

    void FindNearestTreeOptimized()
    {
        float minDist = Mathf.Infinity;
        Tree best = null;

        for (int i = Registry.Count - 1; i >= 0; i--)
        {
            Tree tree = Registry[i];
            if (tree == null || !tree.gameObject.activeInHierarchy) { Registry.RemoveAt(i); continue; }
            if (!tree.TryClaim()) continue;

            float dist = Vector3.Distance(transform.position, tree.transform.position);
            if (dist < minDist) { if (best != null) best.Release(); minDist = dist; best = tree; }
            else tree.Release();
        }

        if (best == null)
        {
            Tree[] trees = GameObject.FindObjectsOfType<Tree>();
            foreach (var tree in trees)
            {
                if (!tree.gameObject.activeInHierarchy || !tree.TryClaim()) continue;
                float dist = Vector3.Distance(transform.position, tree.transform.position);
                if (dist < minDist) { if (best != null) best.Release(); minDist = dist; best = tree; }
                else tree.Release();
            }
        }

        if (best != null)
        {
            targetTree = best;
            chopTimer = 0f;
            hasTriggeredChopAnim = false;
            isHeadingToTree = false; // Sẵn sàng kích hoạt lệnh di chuyển mới
        }
    }

    void HandleMoveToTree()
    {
        // FIX: Chỉ kích hoạt SetDestination tìm cây 1 lần duy nhất khi có mục tiêu mới
        if (agent.isOnNavMesh && !isHeadingToTree)
        {
            isHeadingToTree = true;
            agent.isStopped = false;
            agent.SetDestination(targetTree.transform.position);
        }
        hasTriggeredChopAnim = false;
    }

    void HandleChopping()
    {
        agent.isStopped = true;
        isHeadingToTree = false; // Đã đến nơi, giải phóng flag đường đi
        stamina?.SetDraining(true);

        if (!hasTriggeredChopAnim)
        {
            hasTriggeredChopAnim = true;
            if (animator != null) { animator.ResetTrigger("Chop"); animator.SetTrigger("Chop"); }
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

    void ReleaseCurrentTree()
    {
        if (targetTree != null)
        {
            targetTree.Release();
            targetTree = null;
        }
        isHeadingToTree = false;
    }

    void CheckStuck()
    {
        bool isResting = stamina != null && !stamina.CanWork();
        if (agent == null || agent.isStopped || !agent.hasPath || isResting)
        {
            stuckTimer = 0f;
            return;
        }

        if (agent.velocity.sqrMagnitude < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
            {
                stuckTimer = 0f;
                agent.ResetPath();
                isHeadingToTree = false;
                isHeadingToDeposit = false;
            }
        }
        else stuckTimer = 0f;
    }
}