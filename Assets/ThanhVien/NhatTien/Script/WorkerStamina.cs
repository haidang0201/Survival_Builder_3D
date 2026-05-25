using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;


public class WorkerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina       = 100f;
    public float drainPerSecond   = 5f;   // hao thể lực mỗi giây khi làm việc
    public float recoverPerSecond = 20f;  // hồi thể lực mỗi giây khi nghỉ
    public float restThreshold    = 0f;   // về nghỉ khi thể lực <= ngưỡng này
    public float resumeThreshold  = 100f; // ra làm khi thể lực >= ngưỡng này

    [Header("Rest Spot")]
    public Transform restSpot;

    [Header("Events")]
    public UnityEvent onStaminaDepleted;  // khi hết thể lực
    public UnityEvent onStaminaRecovered; // khi thể lực đầy

    // ===== INTERNAL =====
    private float        currentStamina;
    private NavMeshAgent agent;
    private bool         isResting  = false;
    private bool         isDraining = false;

    // BUG FIX: lưu trạng thái draining trước khi reset để Gizmo hiển thị đúng
    // isDraining bị reset về false cuối mỗi frame nên Gizmo không thể đọc được
    private bool wasDrainingLastFrame = false;

    // ===== PROPERTIES =====
    public float CurrentStamina => currentStamina;
    public float MaxStamina     => maxStamina;
    public bool  IsResting      => isResting;
    public float StaminaPercent => maxStamina > 0 ? currentStamina / maxStamina : 0f;

    // ===== LIFECYCLE =====

    void Awake()
    {
        agent          = GetComponent<NavMeshAgent>();
        currentStamina = maxStamina;
    }

    void Start()
    {
        FindRestSpot();
    }

    void Update()
    {
        if (isResting)
        {
            HandleResting();
        }
        else
        {
            if (isDraining)
                HandleDraining();

            // Lưu lại trước khi reset để Gizmo đọc được
            wasDrainingLastFrame = isDraining;

            // Reset mỗi cuối frame — worker phải gọi SetDraining(true)
            // liên tục mỗi frame khi làm việc, nếu không thì tự động false
            isDraining = false;
        }
    }

    // ===== PUBLIC API =====

    /// <summary>
    /// Gọi mỗi frame khi worker đang thực sự làm việc (chặt/gặt).
    /// KHÔNG gọi khi đang đi đường hoặc mang hàng.
    /// </summary>
    public void SetDraining(bool draining)
    {
        isDraining = draining;
    }

    /// <summary>Worker có thể làm việc không?</summary>
    public bool CanWork() => !isResting;

    // ===== INTERNAL =====

    void HandleDraining()
    {
        currentStamina -= drainPerSecond * Time.deltaTime;
        currentStamina  = Mathf.Max(currentStamina, 0f);

        if (currentStamina <= restThreshold)
            StartResting();
    }

    void HandleResting()
    {
        if (restSpot != null && agent != null && agent.isOnNavMesh)
        {
            float distToRest = Vector3.Distance(transform.position, restSpot.position);

            if (distToRest > agent.stoppingDistance + 0.5f)
            {
                agent.isStopped = false;
                agent.SetDestination(restSpot.position);
                return; // chưa đến nơi → chưa hồi
            }

            agent.isStopped = true;
        }

        // Đã đến RestSpot (hoặc không có RestSpot) → hồi thể lực
        currentStamina += recoverPerSecond * Time.deltaTime;
        currentStamina  = Mathf.Min(currentStamina, maxStamina);

        if (currentStamina >= resumeThreshold)
            StopResting();
    }

    void StartResting()
    {
        if (isResting) return;

        isResting            = true;
        isDraining           = false;
        wasDrainingLastFrame = false;

        Debug.Log($"[WorkerStamina] '{name}': Hết thể lực → về nghỉ.");

        onStaminaDepleted?.Invoke();
    }

    void StopResting()
    {
        isResting = false;

        if (agent != null)
            agent.isStopped = false;

        Debug.Log($"[WorkerStamina] '{name}': Thể lực đầy → tiếp tục làm việc.");

        onStaminaRecovered?.Invoke();
    }

    void FindRestSpot()
    {
        if (restSpot != null) return;

        GameObject obj = GameObject.FindWithTag("RestSpot");
        if (obj != null)
        {
            restSpot = obj.transform;
            Debug.Log($"[WorkerStamina] '{name}': Tìm thấy RestSpot '{restSpot.name}'.");
        }
        else
        {
            Debug.LogWarning($"[WorkerStamina] '{name}': Không tìm thấy RestSpot! " +
                             $"Worker sẽ hồi tại chỗ khi hết thể lực.");
        }
    }

    void OnValidate()
    {
        maxStamina       = Mathf.Max(maxStamina, 1f);
        restThreshold    = Mathf.Clamp(restThreshold, 0f, maxStamina);
        resumeThreshold  = Mathf.Clamp(resumeThreshold, restThreshold, maxStamina);
        drainPerSecond   = Mathf.Max(drainPerSecond, 0f);
        recoverPerSecond = Mathf.Max(recoverPerSecond, 0f);
    }

    // ===== GIZMO DEBUG =====
    void OnDrawGizmosSelected()
    {
        if (restSpot != null)
        {
            Gizmos.color = isResting ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, restSpot.position);
            Gizmos.DrawWireSphere(restSpot.position, 0.8f);
        }

#if UNITY_EDITOR
        // BUG FIX: dùng wasDrainingLastFrame thay vì isDraining
        // vì isDraining luôn = false tại thời điểm Gizmo được vẽ (sau Update)
        string stateLabel = isResting          ? "[NGHỈ]"
                          : wasDrainingLastFrame ? "[LÀM]"
                          : "[ĐI]";

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Stamina: {currentStamina:F0}/{maxStamina} {stateLabel}"
        );
#endif
    }
}