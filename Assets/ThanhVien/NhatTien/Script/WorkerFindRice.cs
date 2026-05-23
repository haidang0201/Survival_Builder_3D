using UnityEngine;
using UnityEngine.AI;

public class WorkerFindRice : MonoBehaviour
{
    public NavMeshAgent    agent;
    public WorkerCarryRice carrySystem;
    public Animator        animator;

    public float harvestDistance = 1.5f;
    public float harvestTime     = 1.2f;

    private Rice  targetRice;
    private float harvestTimer            = 0f;
    private bool  hasTriggeredHarvestAnim = false;

    private float findRiceCooldown         = 0f;
    private const float FIND_RICE_INTERVAL = 0.5f;

    void Update()
    {
        UpdateAnimationSpeed();

        if (carrySystem.IsCarrying())
        {
            HandleCarrying();
            return;
        }

        // FIX: Sau khi Rice.SetActive(false), targetRice vẫn không null
        // nhưng gameObject đã inactive → cần clear target để tìm cây mới
        if (targetRice != null && !targetRice.gameObject.activeInHierarchy)
        {
            Debug.Log($"[WorkerFindRice] '{name}': Lúa đã bị tắt từ bên ngoài → tìm lúa mới.");
            ReleaseCurrentRice();
        }

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

    // ===== ANIMATION =====
    void UpdateAnimationSpeed()
    {
        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }

    // ===== MANG LÚA VỀ KHO =====
    void HandleCarrying()
    {
        carrySystem.MoveToBarn();

        bool arrived = !agent.pathPending &&
                       agent.remainingDistance <= agent.stoppingDistance + 0.5f;

        if (arrived)
            carrySystem.TryDeposit();
    }

    // ===== TÌM LÚA =====
    void HandleFindRice()
    {
        findRiceCooldown -= Time.deltaTime;

        if (findRiceCooldown <= 0f)
        {
            findRiceCooldown = FIND_RICE_INTERVAL;
            FindNearestRice();
        }
    }

    void FindNearestRice()
    {
        Rice[] riceFields = GameObject.FindObjectsOfType<Rice>();

        float minDist = Mathf.Infinity;
        Rice  best    = null;

        foreach (var rice in riceFields)
        {
            if (!rice.gameObject.activeInHierarchy) continue;
            if (!rice.TryClaim()) continue;

            float dist = Vector3.Distance(transform.position, rice.transform.position);

            if (dist < minDist)
            {
                if (best != null) best.Release();
                minDist = dist;
                best    = rice;
            }
            else
            {
                rice.Release();
            }
        }

        if (best != null)
        {
            targetRice              = best;
            harvestTimer            = 0f;
            hasTriggeredHarvestAnim = false;

            Debug.Log($"[WorkerFindRice] '{name}': Tìm thấy lúa '{best.name}'.");
        }
        else
        {
            Debug.Log($"[WorkerFindRice] '{name}': Không tìm thấy lúa nào.");
        }
    }

    // ===== DI CHUYỂN ĐẾN LÚA =====
    void HandleMoveToRice()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(targetRice.transform.position);
        }

        hasTriggeredHarvestAnim = false;
    }

    // ===== GẶT LÚA =====
    void HandleHarvesting()
    {
        agent.isStopped = true;

        if (!hasTriggeredHarvestAnim)
        {
            hasTriggeredHarvestAnim = true;
            TriggerHarvestAnimation();
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

    void TriggerHarvestAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger("Chop");
        animator.SetTrigger("Chop");
    }

    void ReleaseCurrentRice()
    {
        if (targetRice != null)
        {
            targetRice.Release();
            targetRice = null;
        }

        agent.isStopped         = false;
        hasTriggeredHarvestAnim = false;
        harvestTimer            = 0f;
    }
}