using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform villageCenter;
    public Transform player; // optional, can be assigned or found by tag "Player"
    public Animator animator;

    [Header("Patrol")]
    public float patrolRadius = 8f;
    public float pointReachDistance = 1f;
    public float repathInterval = 2f;

    [Header("Chase")]
    public float chaseTriggerRange = 6f;
    public float loseChaseRange = 12f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Animation")]
    public string attackTrigger = "Attack";

    [Header("Day / Night")]
    [Tooltip("If true use 'isNight' checkbox to force night for testing. If false, use Sun Light (optional).")]
    public bool useManualNight = true;
    public bool isNight = true; // inspector tick to test night/day
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
    private float nextDebugLogTime;

    // render/collider list to hide/show on day/night
    private Renderer[] renderers;
    private Collider[] colliders;
    private bool lastNightState = true;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null && debugLogs) Debug.LogError("EnemyAI requires a NavMeshAgent");
        if (animator == null) animator = GetComponentInChildren<Animator>();
        // no dynamic resolution — use moveParam directly

        // cache renderers and colliders for hide/show
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        // try to ensure agent is on NavMesh
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
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (agent != null) agent.speed = patrolSpeed;
        PickNewPatrolPoint();
        SetDestination(currentPatrolPoint);
        UpdateAnimationState();

        // ensure initial visibility according to night state
        lastNightState = GetNightState();
        ApplyNightState(lastNightState);
    }

    private void Update()
    {
        // Day/night check
        bool nightNow = GetNightState();
        if (nightNow != lastNightState)
        {
            lastNightState = nightNow;
            if (debugLogs) Debug.LogFormat("[EnemyAI] Night state changed: {0}", nightNow);
            ApplyNightState(nightNow);
        }

        if (!nightNow)
        {
            // if day, skip behavior
            UpdateAnimationState();
            return;
        }

        // Check for chase start
        if (chaseTarget == null)
        {
            if (player != null && Vector3.Distance(transform.position, player.position) <= chaseTriggerRange)
            {
                chaseTarget = player;
                if (agent != null) agent.speed = chaseSpeed;
                if (debugLogs) Debug.Log("EnemyAI: start chasing player");
            }
            else if (villageCenter != null && Vector3.Distance(transform.position, villageCenter.position) <= chaseTriggerRange)
            {
                chaseTarget = villageCenter;
                if (agent != null) agent.speed = chaseSpeed;
                if (debugLogs) Debug.Log("EnemyAI: start chasing village");
            }
        }
        else
        {
            float d = Vector3.Distance(transform.position, chaseTarget.position);
            if (d > loseChaseRange)
            {
                // stop chase, resume patrol
                chaseTarget = null;
                if (agent != null) agent.speed = patrolSpeed;
                if (debugLogs) Debug.Log("EnemyAI: lost chase target, resuming patrol");
                PickNewPatrolPoint();
                SetDestination(currentPatrolPoint);
            }
            else
            {
                SetDestination(chaseTarget.position);
            }
        }

        // Patrol behavior when not chasing
        if (chaseTarget == null)
        {
            if (!hasPatrolPoint || Time.time >= nextRepathTime || Vector3.Distance(transform.position, currentPatrolPoint) <= pointReachDistance)
            {
                PickNewPatrolPoint();
                SetDestination(currentPatrolPoint);
            }
        }

        UpdateAnimationState();
    }

    private bool GetNightState()
    {
        if (useManualNight)
            return isNight;

        if (sunLight != null)
            return sunLight.intensity <= nightLightThreshold;

        // default to night if no control available
        return true;
    }

    private void ApplyNightState(bool night)
    {
        // show/hide renderers
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
            // when night begins, reposition to a patrol point and start
            if (night)
            {
                PickNewPatrolPoint();
                SetDestination(currentPatrolPoint);
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
        Vector3 center = villageCenter != null ? villageCenter.position : transform.position;
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

        // fallback: stay in place
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
            else return;
        }

        agent.SetDestination(dest);
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
                "[EnemyAI] move={0} chaseTarget={1} hasPath={2} pathPending={3} remainingDistance={4:F2} velocity={5:F2} desiredVelocity={6:F2}",
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

