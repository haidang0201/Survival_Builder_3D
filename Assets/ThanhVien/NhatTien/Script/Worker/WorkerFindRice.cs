using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class WorkerFindRice : MonoBehaviour
{
    public static List<Rice> Registry = new List<Rice>();

    public NavMeshAgent    agent;
    public WorkerCarryRice carrySystem;
    public Animator        animator;
    public WorkerStamina   stamina;

    public float harvestDistance = 1.5f;
    public float harvestTime     = 1.2f;

    [Header("Animation Settings")]
    public string harvestTriggerName = "Harvest"; 

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

    private Rice  targetRice;
    private float harvestTimer            = 0f;
    private bool  hasTriggeredHarvestAnim = false;
    private bool  wasResting              = false;
    private float findRiceCooldown        = 0f;
    private const float FIND_RICE_INTERVAL = 0.5f;

    private bool isHeadingToRice    = false;
    private bool isHeadingToDeposit = false;

    void Start()
    {
        if (stamina == null) stamina = GetComponent<WorkerStamina>();
        anchorPosition = transform.position; // Đánh dấu khu vực làm việc
    }

    void Update()
    {
        UpdateAnimationSpeed();
        CheckStuck();

        if (carrySystem.IsCarrying())
        {
            isHeadingToRice = false;
            HandleCarrying();
            return;
        }

        if (stamina != null && !stamina.CanWork())
        {
            if (!wasResting)
            {
                wasResting = true;
                ReleaseCurrentRice();
                if (animator != null) animator.ResetTrigger(harvestTriggerName);
                hasTriggeredHarvestAnim = false;
                harvestTimer            = 0f;
                isHeadingToRice         = false;
                isHeadingToDeposit      = false;
            }
            return; 
        }

        wasResting = false;
        isHeadingToDeposit = false;

        // Rảnh rỗi (Không có lúa) -> Lang thang và tắt trừ Stamina
        if (targetRice == null || !targetRice.gameObject.activeInHierarchy)
        {
            if (targetRice != null) ReleaseCurrentRice();
            
            HandleFindRice();
            
            if (targetRice == null)
            {
                stamina?.SetDraining(false); 
                HandleWander();
                return;
            }
        }

        // Đang đi gặt lúa -> Bật trừ Stamina
        stamina?.SetDraining(true);

        float dist = Vector3.Distance(transform.position, targetRice.transform.position);
        if (dist > harvestDistance)
        {
            HandleMoveToRice();
            return;
        }

        HandleHarvesting();
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
            isHeadingToDeposit = true;
            carrySystem.MoveToStorage(); 
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
                        totalWaitTimer = 0f;
                        depositRetryTimer = 0f;
                        isHeadingToDeposit = false;
                        if (agent.isOnNavMesh) agent.isStopped = false;
                        carrySystem.enabled = false;
                        carrySystem.enabled = true;
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

    void HandleFindRice()
    {
        findRiceCooldown -= Time.deltaTime;
        if (findRiceCooldown <= 0f)
        {
            findRiceCooldown = FIND_RICE_INTERVAL;
            FindNearestRiceOptimized();
        }
    }

    void FindNearestRiceOptimized()
    {
        float minDist = Mathf.Infinity;
        Rice best = null;

        for (int i = Registry.Count - 1; i >= 0; i--)
        {
            Rice rice = Registry[i];
            if (rice == null || !rice.gameObject.activeInHierarchy) { Registry.RemoveAt(i); continue; }
            if (!rice.TryClaim()) continue;

            float dist = Vector3.Distance(transform.position, rice.transform.position);
            if (dist < minDist) { if (best != null) best.Release(); minDist = dist; best = rice; }
            else rice.Release();
        }

        if (best == null)
        {
            Rice[] riceFields = GameObject.FindObjectsOfType<Rice>();
            foreach (var rice in riceFields)
            {
                if (!rice.gameObject.activeInHierarchy || !rice.TryClaim()) continue;
                float dist = Vector3.Distance(transform.position, rice.transform.position);
                if (dist < minDist) { if (best != null) best.Release(); minDist = dist; best = rice; }
                else rice.Release();
            }
        }

        if (best != null)
        {
            targetRice = best;
            harvestTimer = 0f;
            hasTriggeredHarvestAnim = false;
            isHeadingToRice = false;
        }
    }

    void HandleMoveToRice()
    {
        if (agent.isOnNavMesh && !isHeadingToRice)
        {
            isHeadingToRice = true;
            agent.isStopped = false;
            agent.SetDestination(targetRice.transform.position);
        }
        hasTriggeredHarvestAnim = false;
    }

    void HandleHarvesting()
    {
        agent.isStopped = true;
        isHeadingToRice = false; 

        if (!hasTriggeredHarvestAnim)
        {
            hasTriggeredHarvestAnim = true;
            if (animator != null) { animator.ResetTrigger(harvestTriggerName); animator.SetTrigger(harvestTriggerName); }
        }

        harvestTimer += Time.deltaTime;
        if (harvestTimer < harvestTime) return;

        harvestTimer            = 0f;
        hasTriggeredHarvestAnim = false;

        RicePickup[] drops = targetRice.TakeDamage(1);
        if (drops != null && drops.Length > 0)
        {
            carrySystem.PickupRice(drops[0]);
            ReleaseCurrentRice();
        }
    }

    void ReleaseCurrentRice()
    {
        if (targetRice != null)
        {
            targetRice.Release();
            targetRice = null;
        }
        isHeadingToRice = false;
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
                isHeadingToRice = false;
                isHeadingToDeposit = false;
            }
        }
        else stuckTimer = 0f;
    }
}