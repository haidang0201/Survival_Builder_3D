using UnityEngine;
using UnityEngine.AI;

public class WorkerFindRice : MonoBehaviour
{
    public NavMeshAgent    agent;
    public WorkerCarryRice carrySystem;
    public Animator        animator;
    public WorkerStamina   stamina;

    public float harvestDistance = 1.5f;
    public float harvestTime     = 1.2f;

    private Rice  targetRice;
    private float harvestTimer            = 0f;
    private bool  hasTriggeredHarvestAnim = false;

    // Dùng để phát hiện khoảnh khắc chuyển sang nghỉ — chỉ reset anim 1 lần
    private bool wasResting = false;

    private float findRiceCooldown         = 0f;
    private const float FIND_RICE_INTERVAL = 0.5f;

    void Start()
    {
        if (stamina == null)
            stamina = GetComponent<WorkerStamina>();

        if (stamina == null)
            Debug.LogWarning($"[WorkerFindRice] '{name}': Không có WorkerStamina — worker làm không giới hạn.");
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

                if (targetRice != null)
                {
                    targetRice.Release();
                    targetRice = null;
                }

                if (animator != null)
                    animator.ResetTrigger("Chop"); // xóa trigger pending, không set Speed

                hasTriggeredHarvestAnim = false;
                harvestTimer            = 0f;
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

        // Lúa bị tắt từ bên ngoài → tìm lúa mới
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
        carrySystem.MoveToWarehouse();

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
        stamina?.SetDraining(true);

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