using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform buildingTarget;
    public Transform squadLeader;
    public bool isLeader = false;
    public Animator animator;

    [Header("Patrol")]
    public float patrolRadius = 8f;
    public float pointReachDistance = 1f;
    public float repathInterval = 2f;

    [Header("Chase")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float followSpeed = 4f;

    [Header("Animation")]
    public string attackTrigger = "Attack";

    [Header("Day / Night")]
    [Tooltip("If true use 'isNight' checkbox to force night for testing. If false, use Sun Light (optional).")]
    public bool useManualNight = true;
    public bool isNight = true;
    public Light sunLight;
    public float nightLightThreshold = 0.2f;

    [Header("Debug")]
    public bool debugLogs = true;
    public float debugLogInterval = 1f;

    [Header("Move Animation")]
    public float moveThreshold = 0.1f;

    private NavMeshAgent agent;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;
    private float nextRepathTime;
    private Transform chaseTarget;
    private Vector3 currentAttackPoint;
    private bool hasCurrentAttackPoint;
    private float nextDebugLogTime;
    private EnemyAI squadLeaderAI;

    private Renderer[] renderers;
    private Collider[] colliders;
    private bool lastNightState = true;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null && debugLogs) Debug.LogError("EnemyAI requires a NavMeshAgent");
        if (animator == null) animator = GetComponentInChildren<Animator>();

        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                if (debugLogs) Debug.Log("EnemyAI: warped agent to nearest NavMesh");
            }
            else if (debugLogs)
            {
                Debug.LogWarning("EnemyAI: no NavMesh near agent position. Bake NavMesh or move agent onto NavMesh.");
            }
        }
    }

    private void Start()
    {
        ResolveSquadLeader();

        if (agent != null) agent.speed = patrolSpeed;
        lastNightState = GetNightState();
        ApplyNightState(lastNightState);

        if (lastNightState)
        {
            RefreshBehaviorForCurrentRole();
        }

        UpdateAnimationState();
    }

    private void Update()
    {
        ResolveSquadLeader();

        bool nightNow = GetNightState();
        if (nightNow != lastNightState)
        {
            lastNightState = nightNow;
            if (debugLogs) Debug.LogFormat("[EnemyAI] Night state changed: {0}", nightNow);
            ApplyNightState(nightNow);
        }

        if (!nightNow)
        {
            UpdateAnimationState();
            return;
        }

        RefreshBehaviorForCurrentRole();
        UpdateAnimationState();
    }

    private void ResolveSquadLeader()
    {
        if (squadLeaderAI == null && squadLeader != null)
        {
            squadLeaderAI = squadLeader.GetComponent<EnemyAI>();
        }
    }

    private void RefreshBehaviorForCurrentRole()
    {
        if (isLeader || squadLeaderAI == null)
        {
            UpdateLeaderBehavior();
        }
        else
        {
            UpdateFollowerBehavior();
        }
    }

    private void UpdateLeaderBehavior()
    {
        if (buildingTarget == null)
        {
            UpdatePatrolBehavior();
            return;
        }

        chaseTarget = buildingTarget;
        if (agent != null) agent.speed = chaseSpeed;

        currentAttackPoint = GetChaseTargetPosition(buildingTarget);
        hasCurrentAttackPoint = true;
        SetDestination(currentAttackPoint);
    }

    private void UpdateFollowerBehavior()
    {
        Vector3 targetPoint;
        if (squadLeaderAI != null && squadLeaderAI.hasCurrentAttackPoint)
        {
            targetPoint = squadLeaderAI.GetCurrentAttackPoint();
            chaseTarget = squadLeaderAI.transform;
        }
        else if (buildingTarget != null)
        {
            targetPoint = GetChaseTargetPosition(buildingTarget);
            chaseTarget = buildingTarget;
        }
        else
        {
            UpdatePatrolBehavior();
            return;
        }

        currentAttackPoint = targetPoint;
        hasCurrentAttackPoint = true;
        if (agent != null) agent.speed = followSpeed;
        SetDestination(targetPoint);
    }

    private void UpdatePatrolBehavior()
    {
        chaseTarget = null;

        if (agent != null) agent.speed = patrolSpeed;

        if (!hasPatrolPoint || Time.time >= nextRepathTime || Vector3.Distance(transform.position, currentPatrolPoint) <= pointReachDistance)
        {
            PickNewPatrolPoint();
            SetDestination(currentPatrolPoint);
        }
    }

    private bool GetNightState()
    {
        if (useManualNight)
            return isNight;

        if (sunLight != null)
            return sunLight.intensity <= nightLightThreshold;

        return true;
    }

    private void ApplyNightState(bool night)
    {
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = night;
            }
        }

        if (colliders != null)
        {
            foreach (var c in colliders)
            {
                if (c != null) c.enabled = night;
            }
        }

        if (animator != null) animator.enabled = night;

        if (agent != null)
        {
            agent.isStopped = !night;
            agent.updatePosition = night;

            if (night)
            {
                RefreshBehaviorForCurrentRole();
            }
        }
    }

    public void PlayAttackAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(attackTrigger)) return;
        animator.SetTrigger(attackTrigger);
    }

    private void PickNewPatrolPoint()
    {
        Vector3 center = transform.position;
        for (int i = 0; i < 8; i++)
        {
            Vector3 rand = Random.insideUnitSphere * patrolRadius;
            rand.y = 0f;
            Vector3 candidate = center + rand;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                currentPatrolPoint = hit.position;
                hasPatrolPoint = true;
                nextRepathTime = Time.time + repathInterval;
                if (debugLogs) Debug.Log("EnemyAI: new patrol point " + currentPatrolPoint);
                return;
            }
        }

        currentPatrolPoint = transform.position;
        hasPatrolPoint = false;
        if (debugLogs) Debug.LogWarning("EnemyAI: failed to find patrol point on NavMesh");
    }

    private void SetDestination(Vector3 dest)
    {
        if (agent == null) return;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                return;
            }
        }

        agent.SetDestination(dest);
    }

    public Vector3 GetCurrentAttackPoint()
    {
        if (hasCurrentAttackPoint)
        {
            return currentAttackPoint;
        }

        if (buildingTarget != null)
        {
            return GetChaseTargetPosition(buildingTarget);
        }

        return transform.position;
    }

    private Vector3 GetChaseTargetPosition(Transform target)
    {
        if (target == null)
        {
            return transform.position;
        }

        Collider[] targetColliders = target.GetComponentsInChildren<Collider>(true);
        if (targetColliders != null && targetColliders.Length > 0)
        {
            Collider bestCollider = targetColliders[0];
            float bestDistance = Vector3.SqrMagnitude(bestCollider.bounds.center - transform.position);

            for (int i = 1; i < targetColliders.Length; i++)
            {
                Collider currentCollider = targetColliders[i];
                float currentDistance = Vector3.SqrMagnitude(currentCollider.bounds.center - transform.position);
                if (currentDistance < bestDistance)
                {
                    bestCollider = currentCollider;
                    bestDistance = currentDistance;
                }
            }

            return bestCollider.bounds.center;
        }

        Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>(true);
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            Renderer bestRenderer = targetRenderers[0];
            float bestDistance = Vector3.SqrMagnitude(bestRenderer.bounds.center - transform.position);

            for (int i = 1; i < targetRenderers.Length; i++)
            {
                Renderer currentRenderer = targetRenderers[i];
                float currentDistance = Vector3.SqrMagnitude(currentRenderer.bounds.center - transform.position);
                if (currentDistance < bestDistance)
                {
                    bestRenderer = currentRenderer;
                    bestDistance = currentDistance;
                }
            }

            return bestRenderer.bounds.center;
        }

        return target.position;
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        bool isMoving = false;

        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                bool hasMeaningfulPath = agent.hasPath && !agent.pathPending && agent.remainingDistance > agent.stoppingDistance + moveThreshold;
                bool hasMeaningfulVelocity = agent.velocity.sqrMagnitude > moveThreshold * moveThreshold;
                isMoving = hasMeaningfulPath || hasMeaningfulVelocity;
            }
            else
            {
                isMoving = agent.desiredVelocity.sqrMagnitude > moveThreshold * moveThreshold;
            }
        }

        animator.SetBool("isMove", isMoving);

        if (debugLogs && Time.time >= nextDebugLogTime)
        {
            nextDebugLogTime = Time.time + debugLogInterval;
            Debug.LogFormat(
                "[EnemyAI] role={0} move={1} chaseTarget={2} hasPath={3} pathPending={4} remainingDistance={5:F2} velocity={6:F2} desiredVelocity={7:F2}",
                isLeader ? "leader" : (squadLeaderAI != null ? "follower" : "solo"),
                isMoving,
                chaseTarget != null ? chaseTarget.name : "none",
                agent != null && agent.hasPath,
                agent != null && agent.pathPending,
                agent != null ? agent.remainingDistance : -1f,
                agent != null ? agent.velocity.magnitude : -1f,
                agent != null ? agent.desiredVelocity.magnitude : -1f
            );
        }
    }
}

