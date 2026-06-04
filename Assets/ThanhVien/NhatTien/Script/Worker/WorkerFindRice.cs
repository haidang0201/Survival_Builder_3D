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
    }

    void Update()
    {
        UpdateAnimationSpeed();
        CheckStuck();

        // 1. ƯU TIÊN TUYỆT ĐỐI: NẾU ĐANG CẦM ĐỒ THÌ PHẢI ĐI CẤT TRƯỚC!
        if (carrySystem.IsCarrying())
        {
            isHeadingToRice = false;
            HandleCarrying();
            return;
        }

        // 2. CHẶN THỂ LỰC
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

        if (targetRice != null && !targetRice.gameObject.activeInHierarchy)
            ReleaseCurrentRice();

        if (targetRice == null)
        {
            HandleFindRice();
            return;
        }

        float dist = Vector3.Distance(transform.position, targetRice.transform.position);
        if (dist > harvestDistance)
        {
            HandleMoveToRice();
            return;
        }

        HandleHarvesting();
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
                        Debug.LogWarning($"[WorkerFindRice] {name}: Kho đầy quá 15s! Vứt hàng.");
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
        stamina?.SetDraining(true);

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
                if (carrySystem.IsCarrying()) carrySystem.MoveToStorage();
                isHeadingToRice = false;
                isHeadingToDeposit = false;
            }
        }
        else stuckTimer = 0f;
    }
}