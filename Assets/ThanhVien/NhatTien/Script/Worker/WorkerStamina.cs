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

    [Header("Kitchen Settings")]
    [Tooltip("Nhà bếp để worker vào nghỉ. Tự tìm qua Tag 'Kitchen' nếu bỏ trống.")]
    public Kitchen kitchen;

    [Tooltip("Tỉ lệ phục hồi stamina khi đói (hết lúa hoặc bếp đầy). 0.3 = chậm gấp 3 lần.")]
    [Range(0f, 1f)]
    public float hungryRecoverMultiplier = 0.3f;

    [Tooltip("Model của worker để ẩn khi vào bếp. Gán object chứa Renderer của worker.")]
    public GameObject workerModel;

    [Tooltip("Danh sách các object cần ẩn theo (Cuốc, Rìu, Liềm, HandPoint đang cầm đồ...).")]
    public GameObject[] extraModelsToHide;

    [Tooltip("Bán kính nhận diện cửa bếp. Vừa đến khoảng cách này là ẩn luôn, đỡ chen lấn.")]
    public float kitchenInteractionRadius = 2.0f;

    [Tooltip("Bán kính tản ra đứng đợi ngoài sân khi bếp đầy chỗ.")]
    public float waitingScatterRadius = 2.5f;

    [Header("Rest Spot (Fallback nếu không có Kitchen)")]
    public Transform restSpot;

    [Header("Events")]
    public UnityEvent onStaminaDepleted;
    public UnityEvent onStaminaRecovered;

    // ===== INTERNAL =====
    private float        currentStamina;
    private NavMeshAgent agent;
    private bool         isResting          = false;

    // FIX: isDraining phải được reset về false mỗi frame.
    // Nếu không reset, worker drain mãi ngay cả khi không làm gì
    // (WorkerFindTree chỉ gọi SetDraining(true) khi chặt cây, không gọi SetDraining(false))
    private bool         isDraining         = false;

    private bool         isInsideKitchen    = false;
    private bool         isHungryInside     = false;
    private Vector3      personalOffset     = Vector3.zero;
    private bool         _hasPersonalOffset = false;

    public float CurrentStamina  => currentStamina;
    public float MaxStamina      => maxStamina;
    public bool  IsResting       => isResting;
    public float StaminaPercent  => maxStamina > 0 ? currentStamina / maxStamina : 0f;

    // ===== LIFECYCLE =====

    void Awake()
    {
        agent          = GetComponent<NavMeshAgent>();
        currentStamina = maxStamina;
    }

    void Start()
    {
        FindKitchen();
        FindRestSpot(); // FIX: bản cũ thiếu FindRestSpot() → restSpot không bao giờ tự tìm được
    }

    // FIX: OnEnable / OnDisable phải có để tránh slot leak trong Kitchen
    // khi worker bị disable (vd: chết, pool trả về) mà không gọi Exit()
    void OnEnable()
    {
        ShowModel();
        isInsideKitchen = false;
        isHungryInside  = false;
    }

    void OnDisable()
    {
        // FIX: nếu bị disable khi đang trong bếp → trả slot về, tránh bếp bị chiếm mãi
        if (isInsideKitchen)
        {
            kitchen?.Exit(this);
            isInsideKitchen = false;
        }
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
            {
                currentStamina -= drainPerSecond * Time.deltaTime;
                currentStamina  = Mathf.Max(currentStamina, 0f);
                if (currentStamina <= restThreshold) StartResting();
            }

            // FIX: reset isDraining mỗi frame — worker scripts gọi SetDraining(true)
            // mỗi frame khi đang làm việc, nếu frame đó không gọi thì hiểu là đang nghỉ
            isDraining = false;
        }
    }

    /// <summary>
    /// Gọi mỗi frame khi worker đang làm việc nặng (chặt cây, gặt lúa...).
    /// Không gọi frame nào = frame đó không drain.
    /// </summary>
    public void SetDraining(bool drain)
    {
        if (isResting) return;
        isDraining = drain;
    }

    public bool CanWork() => !isResting;

    // ===== HANDLE RESTING =====

    void HandleResting()
    {
        // Không có bếp lẫn restSpot → hồi tại chỗ
        if (kitchen == null && restSpot == null)
        {
            currentStamina = Mathf.Min(currentStamina + recoverPerSecond * Time.deltaTime, maxStamina);
            if (currentStamina >= resumeThreshold) StopResting();
            return;
        }

        if (kitchen != null)
        {
            // CASE 1: Đã vào trong bếp → hồi stamina theo trạng thái đói/no
            if (isInsideKitchen)
            {
                float multiplier = isHungryInside ? hungryRecoverMultiplier : 1f;
                currentStamina = Mathf.Min(
                    currentStamina + recoverPerSecond * multiplier * Time.deltaTime,
                    maxStamina
                );
                if (currentStamina >= resumeThreshold) StopResting();
                return;
            }

            // CASE 2: Bếp đang đầy → đứng chờ ngoài sân, hồi chậm
            if (kitchen.IsFull)
            {
                if (!_hasPersonalOffset)
                {
                    Vector2 rand       = Random.insideUnitCircle * waitingScatterRadius;
                    if (rand.magnitude < 1f) rand = rand.normalized;
                    personalOffset     = new Vector3(rand.x, 0f, rand.y);
                    _hasPersonalOffset = true;
                }

                Vector3 waitPos = kitchen.GetRestPosition() + personalOffset;
                MoveToPosition(waitPos);

                // Hồi chậm khi đứng yên ngoài sân
                bool isStanding = agent == null || agent.velocity.sqrMagnitude < 0.01f;
                if (isStanding)
                {
                    currentStamina = Mathf.Min(
                        currentStamina + recoverPerSecond * hungryRecoverMultiplier * Time.deltaTime,
                        maxStamina
                    );
                }

                if (currentStamina >= resumeThreshold) StopResting();
                return;
            }

            // CASE 3: Bếp còn chỗ → đi thẳng đến cửa bếp
            if (_hasPersonalOffset)
            {
                personalOffset     = Vector3.zero;
                _hasPersonalOffset = false;
            }

            MoveToPosition(kitchen.EntrancePosition);

            float distToKitchen = Vector3.Distance(transform.position, kitchen.EntrancePosition);
            if (distToKitchen <= kitchenInteractionRadius)
            {
                // FIX: dùng out consumedFood từ Kitchen.Enter()
                // Bản cũ kiểm tra kitchen.HasFood SAU KHI Enter() đã ConsumeRice
                // → nếu lúa vừa hết thì HasFood = false dù worker vừa được ăn → isHungryInside sai
                bool enterSuccess = kitchen.Enter(this, out bool consumedFood);
                if (enterSuccess)
                {
                    isInsideKitchen = true;
                    isHungryInside  = !consumedFood; // đúng: dựa vào kết quả thực tế
                    HideModel();
                    if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                }
            }
            return;
        }

        // FALLBACK: Không có Kitchen → đi về restSpot hồi stamina
        if (restSpot != null)
        {
            float distToRest = Vector3.Distance(transform.position, restSpot.position);
            if (distToRest > (agent != null ? agent.stoppingDistance : 0f) + 0.5f)
            {
                MoveToPosition(restSpot.position);
                return;
            }
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        }

        currentStamina = Mathf.Min(currentStamina + recoverPerSecond * Time.deltaTime, maxStamina);
        if (currentStamina >= resumeThreshold) StopResting();
    }

    // ===== START / STOP RESTING =====

    void StartResting()
    {
        if (isResting) return;
        isResting  = true;
        isDraining = false;
        onStaminaDepleted?.Invoke();
    }

    void StopResting()
    {
        ShowModel();

        if (isInsideKitchen)
        {
            kitchen?.Exit(this);
            isInsideKitchen = false;
            isHungryInside  = false;
        }

        isResting          = false;
        personalOffset     = Vector3.zero;
        _hasPersonalOffset = false;

        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        onStaminaRecovered?.Invoke();
    }

    // ===== HELPERS =====

    void MoveToPosition(Vector3 pos)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(pos);
        }
    }

    void ShowModel()
    {
        if (workerModel != null) workerModel.SetActive(true);

        if (extraModelsToHide != null)
            foreach (var obj in extraModelsToHide)
                if (obj != null) obj.SetActive(true);
    }

    void HideModel()
    {
        if (workerModel != null) workerModel.SetActive(false);

        if (extraModelsToHide != null)
            foreach (var obj in extraModelsToHide)
                if (obj != null) obj.SetActive(false);
    }

    void FindKitchen()
    {
        if (kitchen != null) return;
        GameObject obj = GameObject.FindWithTag("Kitchen");
        if (obj != null) kitchen = obj.GetComponent<Kitchen>();
    }

    // FIX: bản cũ thiếu method này → restSpot không bao giờ được tự tìm
    void FindRestSpot()
    {
        if (restSpot != null) return;
        GameObject obj = GameObject.FindWithTag("RestSpot");
        if (obj != null) restSpot = obj.transform;
    }
}