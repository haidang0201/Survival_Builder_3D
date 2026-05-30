using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class WorkerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina       = 100f;
    public float drainPerSecond   = 5f;   
    public float recoverPerSecond = 20f;  
    public float restThreshold    = 0f;   
    public float resumeThreshold  = 100f; 

    [Header("Rest Spot")]
    public Transform restSpot;

    [Header("Events")]
    public UnityEvent onStaminaDepleted;  
    public UnityEvent onStaminaRecovered; 

    private float        currentStamina;
    private NavMeshAgent agent;
    private bool         isResting  = false;
    private bool         isDraining = false;
    private bool         isHeadingToRest = false; // FIX: Dùng flag để kiểm soát lộ trình đi ngủ

    public float CurrentStamina => currentStamina;
    public float MaxStamina     => maxStamina;
    public bool  IsResting      => isResting;

    void Awake()
    {
        agent          = GetComponent<NavMeshAgent>();
        currentStamina = maxStamina;
    }

    void Start() => FindRestSpot();

    void Update()
    {
        if (isResting) HandleResting();
        else
        {
            if (isDraining) HandleDraining();
            isDraining = false; 
        }
    }

    public void SetDraining(bool draining) => isDraining = draining;
    public bool CanWork() => !isResting;

    void HandleDraining()
    {
        currentStamina -= drainPerSecond * Time.deltaTime;
        currentStamina  = Mathf.Max(currentStamina, 0f);
        if (currentStamina <= restThreshold) StartResting();
    }

    void HandleResting()
    {
        if (restSpot != null && agent != null && agent.isOnNavMesh)
        {
            float distToRest = Vector3.Distance(transform.position, restSpot.position);
            if (distToRest > agent.stoppingDistance + 0.5f)
            {
                agent.isStopped = false;
                
                // FIX: Chỉ set đường đi 1 lần duy nhất bằng flag, không so sánh Vector3 của NavMesh
                if (!isHeadingToRest)
                {
                    isHeadingToRest = true;
                    agent.SetDestination(restSpot.position);
                }
                return; 
            }
            agent.isStopped = true;
            isHeadingToRest = false; 
        }

        currentStamina += recoverPerSecond * Time.deltaTime;
        currentStamina = Mathf.Min(currentStamina, maxStamina);

        if (currentStamina >= resumeThreshold) StopResting();
    }

    void StartResting()
    {
        if (isResting) return;
        isResting = true;
        isDraining = false;
        isHeadingToRest = false;
        onStaminaDepleted?.Invoke();
    }

    void StopResting()
    {
        isResting = false;
        isHeadingToRest = false;
        if (agent != null) agent.isStopped = false;
        onStaminaRecovered?.Invoke();
    }

    void FindRestSpot()
    {
        if (restSpot != null) return;
        GameObject obj = GameObject.FindWithTag("RestSpot");
        if (obj != null) restSpot = obj.transform;
    }
}