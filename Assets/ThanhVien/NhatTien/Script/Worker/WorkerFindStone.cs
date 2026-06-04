using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class WorkerFindStone : MonoBehaviour
{
    public static List<Stone> Registry = new List<Stone>(); 

    public NavMeshAgent     agent;
    public WorkerCarryStone carrySystem;
    public Animator         animator;
    public WorkerStamina    stamina;

    public float mineDistance = 1.8f;
    public float mineTime     = 1.5f;

    [Header("Animation Settings")]
    public string mineTriggerName = "Mine"; 

    [Header("Settings Nâng Cấp")]
    public float stuckTimeout = 2.0f;
    private float stuckTimer = 0f;
    private float depositRetryTimer = 0f;
    private float totalWaitTimer = 0f; 

    private Stone targetStone;
    private float mineTimer            = 0f;
    private bool  hasTriggeredMineAnim = false;
    private bool  wasResting           = false;
    private float findStoneCooldown    = 0f;
    private const float FIND_STONE_INTERVAL = 0.5f;

    private bool isHeadingToStone   = false;
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
            isHeadingToStone = false;
            HandleCarrying();
            return;
        }

        // 2. CHẶN THỂ LỰC
        if (stamina != null && !stamina.CanWork())
        {
            if (!wasResting)
            {
                wasResting = true;
                ReleaseCurrentStone();
                if (animator != null) animator.ResetTrigger(mineTriggerName);
                hasTriggeredMineAnim = false;
                mineTimer            = 0f;
                isHeadingToStone     = false;
                isHeadingToDeposit   = false;
            }
            return; 
        }

        wasResting = false;
        isHeadingToDeposit = false;

        if (targetStone != null && !targetStone.gameObject.activeInHierarchy)
            ReleaseCurrentStone();

        if (targetStone == null)
        {
            HandleFindStone();
            return;
        }

        float dist = Vector3.Distance(transform.position, targetStone.transform.position);
        if (dist > mineDistance)
        {
            HandleMoveToStone();
            return;
        }

        HandleMining();
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
                        Debug.LogWarning($"[WorkerFindStone] {name}: Kho đầy quá 15s! Vứt hàng.");
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

    void HandleFindStone()
    {
        findStoneCooldown -= Time.deltaTime;
        if (findStoneCooldown <= 0f)
        {
            findStoneCooldown = FIND_STONE_INTERVAL;
            FindNearestStoneOptimized();
        }
    }

    void FindNearestStoneOptimized()
    {
        float minDist = Mathf.Infinity;
        Stone best = null;

        for (int i = Stone.Registry.Count - 1; i >= 0; i--)
        {
            Stone stone = Stone.Registry[i];
            if (stone == null || !stone.gameObject.activeInHierarchy) { Stone.Registry.RemoveAt(i); continue; }
            if (!stone.TryClaim()) continue;

            float dist = Vector3.Distance(transform.position, stone.transform.position);
            if (dist < minDist) { if (best != null) best.Release(); minDist = dist; best = stone; }
            else stone.Release();
        }

        if (best == null)
        {
            Stone[] stones = GameObject.FindObjectsOfType<Stone>();
            foreach (var stone in stones)
            {
                if (!stone.gameObject.activeInHierarchy || !stone.TryClaim()) continue;
                float dist = Vector3.Distance(transform.position, stone.transform.position);
                if (dist < minDist) { if (best != null) best.Release(); minDist = dist; best = stone; }
                else stone.Release();
            }
        }

        if (best != null)
        {
            targetStone = best;
            mineTimer = 0f;
            hasTriggeredMineAnim = false;
            isHeadingToStone = false;
        }
    }

    void HandleMoveToStone()
    {
        if (agent.isOnNavMesh && !isHeadingToStone)
        {
            isHeadingToStone = true;
            agent.isStopped = false;
            agent.SetDestination(targetStone.transform.position);
        }
        hasTriggeredMineAnim = false;
    }

    void HandleMining()
    {
        agent.isStopped = true;
        isHeadingToStone = false; 
        stamina?.SetDraining(true);

        if (!hasTriggeredMineAnim)
        {
            hasTriggeredMineAnim = true;
            if (animator != null) { animator.ResetTrigger(mineTriggerName); animator.SetTrigger(mineTriggerName); }
        }

        mineTimer += Time.deltaTime;
        if (mineTimer < mineTime) return;

        mineTimer            = 0f;
        hasTriggeredMineAnim = false;

        StonePickup[] drops = targetStone.TakeDamage(1);
        if (drops != null && drops.Length > 0)
        {
            carrySystem.PickupStone(drops[0]);
            ReleaseCurrentStone();
        }
    }

    void ReleaseCurrentStone()
    {
        if (targetStone != null)
        {
            targetStone.Release();
            targetStone = null;
        }
        isHeadingToStone = false;
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
                isHeadingToStone = false;
                isHeadingToDeposit = false;
            }
        }
        else stuckTimer = 0f;
    }
}