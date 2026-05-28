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

    [Header("Settings Nâng Cấp")]
    public float stuckTimeout = 2.0f;
    private float stuckTimer = 0f;
    private float depositRetryTimer = 0f;

    private Rice  targetRice;
    private float harvestTimer            = 0f;
    private bool  hasTriggeredHarvestAnim = false;
    private bool  wasResting              = false;
    private float findRiceCooldown         = 0f;
    private const float FIND_RICE_INTERVAL = 0.5f;

    // FIX: Flags ngăn spam lộ trình di chuyển lên NavMesh mỗi frame
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

        if (stamina != null && !stamina.CanWork())
        {
            if (!wasResting)
            {
                wasResting = true;
                ReleaseCurrentRice();
                // FIX: Sửa tên trigger chuẩn xác cho nông dân ("Harvest" thay vì "Chop")
                if (animator != null) animator.ResetTrigger("Harvest");
                hasTriggeredHarvestAnim = false;
                harvestTimer            = 0f;
                isHeadingToRice         = false;
                isHeadingToDeposit      = false;
            }
            return; 
        }

        wasResting = false;

        if (carrySystem.IsCarrying())
        {
            isHeadingToRice = false;
            HandleCarrying();
            return;
        }

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
        animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    void HandleCarrying()
    {
        // FIX: Chỉ gọi SetDestination đi cất lúa 1 lần duy nhất
        if (!isHeadingToDeposit)
        {
            isHeadingToDeposit = true;
            carrySystem.MoveToWarehouse();
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
                    Debug.LogWarning($"[WorkerFindRice] {name}: Kho tạm lúa đầy!");
                }
                else
                {
                    depositRetryTimer = 0f;
                    isHeadingToDeposit = false;
                }
            }
        }
        else depositRetryTimer = 0f;
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
        // FIX: Chỉ gọi SetDestination đi tìm lúa 1 lần duy nhất thay vì mỗi frame
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
            // FIX: Thay thế toàn bộ "Chop" thành "Harvest" đồng bộ cấu trúc nông dân
            if (animator != null) { animator.ResetTrigger("Harvest"); animator.SetTrigger("Harvest"); }
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
                isHeadingToRice = false;
                isHeadingToDeposit = false;
            }
        }
        else stuckTimer = 0f;
    }
}