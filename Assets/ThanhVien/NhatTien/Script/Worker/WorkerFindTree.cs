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

    [Header("VFX Settings")]
    public float chopImpactRatio = 0.5f; // % thời gian chop khi rìu chạm cây
    private bool hasPlayedImpactVFX = false;

    [Header("Lift/Pickup Settings")]
    public string liftTriggerName = "Lift";
    public float  liftTime        = 1f;   // tổng thời gian animation Lift chạy (khớp với độ dài clip)
    public float  liftGrabRatio   = 0.6f; // % thời gian khi tay chạm gỗ để gắn vào tay

    private bool       isLifting            = false;
    private float      liftTimer            = 0f;
    private bool       hasGrabbedDuringLift = false;
    private WoodPickup pendingWood          = null;

    [Header("Idle/Wander Settings")]
    public float wanderRadius = 5f;
    public float wanderInterval = 3f;
    private float wanderTimer = 0f;
    private Vector3 anchorPosition;

    [Header("Settings Nâng Cấp")]
    public float stuckTimeout = 2.0f;
    private float stuckTimer = 0f;
    private float depositRetryTimer = 0f; 
    private float totalWaitTimer = 0f; 

    private Tree  targetTree;
    private float chopTimer            = 0f;
    private bool  hasTriggeredChopAnim = false;
    private bool  wasResting           = false;
    private float findTreeCooldown     = 0f;
    private const float FIND_TREE_INTERVAL = 0.5f;

    private bool isHeadingToTree    = false;
    private bool isHeadingToDeposit = false;

    void Start()
    {
        if (stamina == null) stamina = GetComponent<WorkerStamina>();
        anchorPosition = transform.position;
    }

    void Update()
    {
        UpdateAnimationSpeed();
        CheckStuck(); 

        if (isLifting)
        {
            HandleLifting();
            return;
        }

        if (carrySystem.IsCarrying())
        {
            isHeadingToTree = false; 
            HandleCarrying();
            return; 
        }

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
        isHeadingToDeposit = false; 

        if (targetTree == null || !targetTree.gameObject.activeInHierarchy)
        {
            if (targetTree != null) ReleaseCurrentTree();
            
            HandleFindTree();
            
            if (targetTree == null)
            {
                stamina?.SetDraining(false); 
                HandleWander();
                return;
            }
        }

        stamina?.SetDraining(true);

        float dist = Vector3.Distance(transform.position, targetTree.transform.position);
        if (dist > chopDistance)
        {
            HandleMoveToTree();
            return;
        }

        HandleChopping();
    }

    void HandleWander()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                Vector3 randDir = Random.insideUnitSphere * wanderRadius + anchorPosition;
                if (NavMesh.SamplePosition(randDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
            }
        }
    }

    void UpdateAnimationSpeed()
    {
        if (animator == null || agent == null) return;
        float speed = agent.isStopped ? 0f : (agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f);
        animator.SetFloat("Speed", speed, 0.05f, Time.deltaTime);
    }

    void HandleCarrying()
    {
        if (!isHeadingToDeposit)
        {
            bool moved = carrySystem.MoveToStorage();
            if (moved)
            {
                isHeadingToDeposit = true;
            }
            else
            {
                // Chưa có kho → đứng yên chờ, thử lại sau
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            }
            return;
        }

        bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
        if (arrived)
        {
            agent.isStopped = true;
            depositRetryTimer -= Time.deltaTime;
            totalWaitTimer += Time.deltaTime;

            if (depositRetryTimer <= 0f)
            {
                if (carrySystem.TryDeposit())
                {
                    depositRetryTimer = 0f;
                    totalWaitTimer = 0f;
                    isHeadingToDeposit = false;
                }
                else
                {
                    depositRetryTimer = 2.5f;
                    if (totalWaitTimer >= 15f)
                    {
                        // Kho đầy hoặc mất — tìm kho khác, KHÔNG drop item
                        totalWaitTimer = 0f;
                        depositRetryTimer = 0f;
                        isHeadingToDeposit = false;
                        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
                    }
                }
            }
        }
        else
        {
            depositRetryTimer = 0f;
            totalWaitTimer = 0f;
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
            isHeadingToTree = false; 
        }
    }

    void HandleMoveToTree()
    {
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
        isHeadingToTree = false; 

        if (!hasTriggeredChopAnim)
        {
            hasTriggeredChopAnim = true;
            hasPlayedImpactVFX = false; // reset cho lần chop mới
            if (animator != null) { animator.ResetTrigger("Chop"); animator.SetTrigger("Chop"); }
        }

        chopTimer += Time.deltaTime;

        // Bắn VFX vụn gỗ đúng lúc rìu chạm cây
        if (!hasPlayedImpactVFX && chopTimer >= chopTime * chopImpactRatio)
        {
            hasPlayedImpactVFX = true;
            targetTree.PlayChopHitVFX();
        }

        if (chopTimer < chopTime) return;

        chopTimer = 0f;
        hasTriggeredChopAnim = false;

        WoodPickup[] woods = targetTree.TakeDamage(1);
        if (woods != null && woods.Length > 0)
        {
            // Cây đã ngã: nhả claim ngay, rồi chơi animation Lift trước khi gắn gỗ vào tay
            ReleaseCurrentTree();
            StartLifting(woods[0]);
        }
    }

    void StartLifting(WoodPickup wood)
    {
        pendingWood          = wood;
        isLifting            = true;
        liftTimer            = 0f;
        hasGrabbedDuringLift = false;

        if (animator != null) { animator.ResetTrigger(liftTriggerName); animator.SetTrigger(liftTriggerName); }
    }

    void HandleLifting()
    {
        agent.isStopped = true;
        liftTimer += Time.deltaTime;

        // Gắn gỗ vào tay đúng lúc tay chạm xuống trong animation Lift
        if (!hasGrabbedDuringLift && liftTimer >= liftTime * liftGrabRatio)
        {
            hasGrabbedDuringLift = true;
            if (pendingWood != null) carrySystem.PickupWood(pendingWood);
        }

        if (liftTimer < liftTime) return;

        isLifting   = false;
        pendingWood = null;
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
        if (agent == null || agent.isStopped || !agent.hasPath || isResting) return;

        if (agent.velocity.sqrMagnitude < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
            {
                stuckTimer = 0f;
                agent.ResetPath();
                if (carrySystem.IsCarrying()) carrySystem.MoveToStorage();
                isHeadingToTree = false;
                isHeadingToDeposit = false;
            }
        }
        else stuckTimer = 0f;
    }
}