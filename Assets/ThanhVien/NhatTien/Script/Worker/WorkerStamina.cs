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
    private bool         isResting         = false;
    private bool         isDraining        = false;
    private bool         isInsideKitchen   = false;
    private bool         isHungryInside    = false;
    private float        kitchenRetryTimer = 0f;
    private Vector3      personalOffset    = Vector3.zero;

    // FIX: dùng bool riêng thay vì dùng Vector3.zero làm sentinel
    private bool _hasPersonalOffset = false;

    // ===== PROPERTIES =====
    public float CurrentStamina  => currentStamina;
    public float MaxStamina      => maxStamina;
    public bool  IsResting       => isResting;
    public float StaminaPercent  => maxStamina > 0 ? currentStamina / maxStamina : 0f;

    // Kitchen.TryEnter cần đọc trạng thái đói khi worker đã ở trong để trả về đúng
    public bool IsHungryInside => isHungryInside;

    // ===== LIFECYCLE =====

    void Awake()
    {
        agent          = GetComponent<NavMeshAgent>();
        currentStamina = maxStamina;
    }

    void Start()
    {
        FindKitchen();
        FindRestSpot();
    }

    void OnEnable()
    {
        ShowModel();
        isInsideKitchen   = false;
        isHungryInside    = false;
        kitchenRetryTimer = 0f;
    }

    void OnDisable()
    {
        if (isInsideKitchen)
        {
            kitchen?.Exit(this);
            isInsideKitchen = false;
        }
    }

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

    // ===== DRAIN =====

    void HandleDraining()
    {
        currentStamina -= drainPerSecond * Time.deltaTime;
        currentStamina  = Mathf.Max(currentStamina, 0f);
        if (currentStamina <= restThreshold) StartResting();
    }

    // ===== REST =====

    void HandleResting()
    {
        // TRƯỜNG HỢP 1: Worker đã yên vị trong bếp — hồi stamina đầy tốc độ
        if (isInsideKitchen && kitchen != null)
        {
            float rate = isHungryInside
                ? recoverPerSecond * hungryRecoverMultiplier
                : recoverPerSecond;

            currentStamina = Mathf.Min(currentStamina + rate * Time.deltaTime, maxStamina);

            if (currentStamina >= resumeThreshold) StopResting();
            return;
        }

        // TRƯỜNG HỢP 2: Đang đi tới bếp hoặc xếp hàng ngoài sân
        Vector3 baseDestination = kitchen != null
            ? kitchen.transform.position
            : (restSpot != null ? restSpot.position : transform.position);

        if (kitchen != null)
        {
            float distToKitchen = Vector3.Distance(transform.position, kitchen.transform.position);

            if (distToKitchen <= kitchenInteractionRadius)
            {
                kitchenRetryTimer -= Time.deltaTime;
                if (kitchenRetryTimer <= 0f)
                {
                    bool entered = kitchen.TryEnter(this, out bool consumedFood);
                    if (entered)
                    {
                        isInsideKitchen  = true;
                        isHungryInside   = !consumedFood;
                        HideModel();

                        // Reset offset khi vào thành công
                        personalOffset    = Vector3.zero;
                        _hasPersonalOffset = false;

                        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                        return;
                    }
                    else
                    {
                        // Bếp đầy — FIX: dùng _hasPersonalOffset thay vì so sánh Vector3.zero
                        if (!_hasPersonalOffset)
                        {
                            Vector2 randCircle = Random.insideUnitCircle * waitingScatterRadius;
                            if (randCircle.magnitude < 1.0f)
                                randCircle = randCircle.normalized * 1.0f;

                            personalOffset     = new Vector3(randCircle.x, 0f, randCircle.y);
                            _hasPersonalOffset = true;
                        }
                        kitchenRetryTimer = 1.5f;
                    }
                }
            }
        }

        // Tính điểm đến cuối cùng
        Vector3 finalDestination = baseDestination;
        if (kitchen != null)
        {
            if (kitchen.IsFull && _hasPersonalOffset)
                finalDestination = baseDestination + personalOffset;
            else if (!kitchen.IsFull)
            {
                // Bếp bớt đầy → bỏ điểm chờ, đi thẳng vào
                personalOffset     = Vector3.zero;
                _hasPersonalOffset = false;
            }
        }
        else
        {
            finalDestination = baseDestination + personalOffset;
        }

        // Di chuyển
        if (agent != null && agent.isOnNavMesh)
        {
            float distToTarget = Vector3.Distance(transform.position, finalDestination);
            if (distToTarget > agent.stoppingDistance + 0.1f)
            {
                agent.isStopped = false;
                agent.SetDestination(finalDestination);
            }
            else
            {
                agent.isStopped = true;
            }
        }

        // FIX: chỉ hồi stamina khi đã đứng yên (đứng chờ ngoài sân), không hồi khi đang đi bộ
        bool isStanding = agent == null || !agent.isOnNavMesh || agent.isStopped ||
                          agent.remainingDistance <= agent.stoppingDistance + 0.1f;

        if (isStanding)
        {
            currentStamina = Mathf.Min(
                currentStamina + recoverPerSecond * hungryRecoverMultiplier * Time.deltaTime,
                maxStamina
            );
        }

        if (currentStamina >= resumeThreshold) StopResting();
    }

    void StartResting()
    {
        if (isResting) return;

        isResting         = true;
        isDraining        = false;
        isHungryInside    = false;
        kitchenRetryTimer = 0f;

        if (kitchen != null)
        {
            // Đi thẳng đến bếp trước, chưa cần offset
            personalOffset     = Vector3.zero;
            _hasPersonalOffset = false;
        }
        else
        {
            // Fallback RestSpot: tản ra ngay
            Vector2 randCircle = Random.insideUnitCircle * waitingScatterRadius;
            personalOffset     = new Vector3(randCircle.x, 0f, randCircle.y);
            _hasPersonalOffset = true;
        }

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

    // ===== MODEL VISIBILITY =====

    // FIX: implement ShowModel / HideModel — thiếu hoàn toàn trong bản cũ
    void ShowModel()
    {
        if (workerModel != null)
            workerModel.SetActive(true);
    }

    void HideModel()
    {
        if (workerModel != null)
            workerModel.SetActive(false);
    }

    // ===== FIND REFERENCES =====

    void FindKitchen()
    {
        if (kitchen != null) return;
        GameObject obj = GameObject.FindWithTag("Kitchen");
        if (obj != null) kitchen = obj.GetComponent<Kitchen>();
    }

    void FindRestSpot()
    {
        if (restSpot != null) return;
        GameObject obj = GameObject.FindWithTag("RestSpot");
        if (obj != null) restSpot = obj.transform;
    }
}