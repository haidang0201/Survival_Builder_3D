using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// 1. Định nghĩa các Trạng thái (State Machine)
public enum UnitState
{
    Idle,
    Moving,
    Attacking
}

public enum AttackMode
{
    Melee,
    Ranged
}

public class UnitController : MonoBehaviour
{
    private static readonly Dictionary<int, UnitController> claimedEnemies = new Dictionary<int, UnitController>();

    // 2. Khai báo các thành phần cần thiết
    private NavMeshAgent agent;
    public UnitState currentState = UnitState.Idle;
    public GameObject currentTarget;
    public float scanFrequency = 0.25f;
    [SerializeField] AttackMode attackMode = AttackMode.Melee;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float rangedAttackRange = 5f;
    [SerializeField] LayerMask targetLayerMask = ~0;
    [SerializeField] float raycastDistance = 6f;
    [SerializeField] float raycastHeight = 0.8f;
    [SerializeField] float visionAngle = 180f;
    [SerializeField] int visionRayCount = 7;
    [SerializeField] float destinationUpdateThreshold = 0.5f;
    [SerializeField] string enemyTag = "Enemy";

    // Internal caches
    private Coroutine lowFreqCoroutine;
    private Vector3 lastDestinationPos = Vector3.positiveInfinity;
    private int currentTargetInstanceId = 0;

    float GetAttackStopDistance()
    {
        return attackMode == AttackMode.Ranged ? rangedAttackRange : attackRange;
    }

    bool IsCurrentTargetClaimedByOther()
    {
        if (currentTarget == null && currentTargetInstanceId == 0) return false;

        int id = currentTargetInstanceId != 0 ? currentTargetInstanceId : currentTarget.GetInstanceID();
        if (!claimedEnemies.TryGetValue(id, out UnitController owner)) return false;
        return owner != null && owner != this;
    }

    bool ClaimEnemy(GameObject enemy)
    {
        if (enemy == null) return false;

        int id = enemy.GetInstanceID();
        if (claimedEnemies.TryGetValue(id, out UnitController owner) && owner != null && owner != this)
        {
            return false;
        }

        claimedEnemies[id] = this;
        currentTargetInstanceId = id;
        return true;
    }

    void ReleaseCurrentTargetClaim()
    {
        int id = currentTargetInstanceId;
        if (id == 0 && currentTarget != null)
        {
            id = currentTarget.GetInstanceID();
        }

        if (id == 0)
        {
            return;
        }

        if (claimedEnemies.TryGetValue(id, out UnitController owner) && owner == this)
        {
            claimedEnemies.Remove(id);
        }

        currentTargetInstanceId = 0;
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("UnitController requires a NavMeshAgent component.");
            enabled = false;
            return;
        }

        lowFreqCoroutine = StartCoroutine(LowFrequencyUpdate());

        if (currentState == UnitState.Attacking && currentTarget != null)
        {
            agent.isStopped = false;
            Vector3 tpos = currentTarget.transform.position;
            agent.SetDestination(tpos);
            lastDestinationPos = tpos;
        }
    }

    IEnumerator LowFrequencyUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(scanFrequency);

            if (currentState == UnitState.Attacking && currentTarget != null)
            {
                if (IsCurrentTargetClaimedByOther())
                {
                    ReleaseCurrentTargetClaim();
                    currentTarget = null;
                    currentState = UnitState.Idle;
                    agent.isStopped = true;
                    continue;
                }

                Vector3 targetPos = currentTarget.transform.position;
                float stopDistance = GetAttackStopDistance();

                if ((transform.position - targetPos).sqrMagnitude > stopDistance * stopDistance)
                {
                    if ((targetPos - lastDestinationPos).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(targetPos);
                        lastDestinationPos = targetPos;
                    }
                }
            }
        }
    }

    void Update()
    {
        if (currentTarget == null || currentTarget.CompareTag(enemyTag) == false)
        {
            if (TryAcquireEnemyByVision())
            {
                return;
            }
        }

        switch (currentState)
        {
            case UnitState.Attacking:
                HandleAttacking();
                break;
            case UnitState.Moving:
                HandleMovement();
                break;
        }
    }

    public void SetNewTarget(GameObject target)
    {
        if (target == null) return;
        if (!target.CompareTag(enemyTag)) return;
        if (currentTarget == target) return;

        ReleaseCurrentTargetClaim();
        if (!ClaimEnemy(target)) return;

        currentTarget = target;
        currentState = UnitState.Attacking;
        agent.isStopped = false;

        Vector3 tpos = target.transform.position;
        agent.SetDestination(tpos);
        lastDestinationPos = tpos;
    }

    void HandleAttacking()
    {
        if (currentTarget == null)
        {
            ReleaseCurrentTargetClaim();
            currentState = UnitState.Idle;
            return;
        }

        if (IsCurrentTargetClaimedByOther())
        {
            ReleaseCurrentTargetClaim();
            currentTarget = null;
            currentState = UnitState.Idle;
            agent.isStopped = true;
            return;
        }

        float sqrDistance = (transform.position - currentTarget.transform.position).sqrMagnitude;
        float stopDistance = GetAttackStopDistance();

        if (sqrDistance <= stopDistance * stopDistance)
        {
            agent.isStopped = true;
            // Melee: có thể gọi đánh cận chiến ở đây.
            // Ranged: có thể gọi bắn ở đây.
        }
        else
        {
            agent.isStopped = false;
        }
    }

    void HandleMovement()
    {
        if (!agent.pathPending)
        {
            if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
            {
                currentState = UnitState.Idle;
            }
        }
    }

    bool TryAcquireEnemyByVision()
    {
        if (visionRayCount < 1) visionRayCount = 1;

        Vector3 origin = transform.position + Vector3.up * raycastHeight;
        float halfAngle = visionAngle * 0.5f;
        float step = visionRayCount == 1 ? 0f : visionAngle / (visionRayCount - 1);

        for (int i = 0; i < visionRayCount; i++)
        {
            float angleOffset = -halfAngle + step * i;
            Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * transform.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, raycastDistance, targetLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.CompareTag(enemyTag))
                {
                    GameObject enemy = hit.collider.gameObject;

                    if (currentTarget == enemy)
                    {
                        return true;
                    }

                    ReleaseCurrentTargetClaim();

                    if (!ClaimEnemy(enemy))
                    {
                        continue;
                    }

                    currentTarget = enemy;
                    currentState = UnitState.Attacking;
                    agent.isStopped = false;

                    Vector3 targetPos = currentTarget.transform.position;
                    agent.SetDestination(targetPos);
                    lastDestinationPos = targetPos;
                    return true;
                }
            }
        }

        return false;
    }

    void OnDisable()
    {
        ReleaseCurrentTargetClaim();

        if (lowFreqCoroutine != null)
        {
            StopCoroutine(lowFreqCoroutine);
            lowFreqCoroutine = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = transform.position + Vector3.up * raycastHeight;
        float halfAngle = visionAngle * 0.5f;
        float step = visionRayCount <= 1 ? 0f : visionAngle / (visionRayCount - 1);

        for (int i = 0; i < Mathf.Max(1, visionRayCount); i++)
        {
            float angleOffset = -halfAngle + step * i;
            Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * transform.forward;
            Gizmos.DrawRay(origin, dir * raycastDistance);
        }
    }
}
