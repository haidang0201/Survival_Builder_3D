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

    [Tooltip("Stamina tối thiểu để worker rời bếp/nhà và đi làm lại. Khuyến nghị 80 thay vì 100.")]
    public float resumeThreshold  = 80f;

    [Tooltip("Stamina tối thiểu để được ra khỏi nhà vào buổi sáng dù chưa đạt resumeThreshold. " +
             "Tránh worker ngủ cả ngày khi lúa hết.")]
    public float morningForceWakeThreshold = 30f;

    [Header("Locations Settings")]
    [Tooltip("Tự động tìm nhà ăn qua Tag 'Kitchen'. (Nghỉ ban ngày - Tốn Lúa)")]
    public Kitchen kitchen;

    [Tooltip("Tự động tìm nhà ở qua Tag 'House'. (Ngủ ban đêm - Miễn phí)")]
    public House house;

    [Tooltip("Tỉ lệ phục hồi khi đói/đứng ngoài sân. 0.3 = chậm gấp 3 lần.")]
    [Range(0f, 1f)]
    public float hungryRecoverMultiplier = 0.3f;

    [Header("Visual Settings")]
    public GameObject workerModel;
    public GameObject[] extraModelsToHide;

    public float interactionRadius    = 2.0f;
    public float waitingScatterRadius = 2.5f;

    [Header("Fallback Spot")]
    public Transform restSpot;

    [Header("Events")]
    public UnityEvent onStaminaDepleted;
    public UnityEvent onStaminaRecovered;

    // ===================================================
    // QUẢN LÝ LỆNH CẦM VẬT PHẨM BAN ĐÊM
    // ===================================================
    [HideInInspector] public bool isCarryingResources = false;
    private bool isNightReturnPending = false;

    // ===================================================
    // KHÓA VỊ TRÍ NGHỈ — TRÁNH RESET MỖI FRAME
    // ===================================================
    private Vector3 targetRestPosition;
    private bool    hasTargetRestPosition = false;

    // ===================================================
    // INTERNAL VARIABLES
    // ===================================================
    private float        currentStamina;
    private NavMeshAgent agent;
    private bool         isResting          = false;

    // FIX: isDraining không còn bị reset mỗi frame trong Update.
    // Worker AI (FindTree/Rice/Stone) phải gọi SetDraining(false) khi không làm việc.
    private bool         isDraining         = false;

    private bool         isInsideKitchen    = false;
    private bool         isInsideHouse      = false;
    private bool         isHungryInside     = false;
    private Vector3      personalOffset     = Vector3.zero;
    private bool         _hasPersonalOffset = false;

    public float CurrentStamina => currentStamina;
    public float MaxStamina     => maxStamina;
    public bool  IsResting      => isResting;
    public float StaminaPercent => maxStamina > 0 ? currentStamina / maxStamina : 0f;

    // ===================================================
    // LIFECYCLE
    // ===================================================

    void Awake()
    {
        agent          = GetComponent<NavMeshAgent>();
        currentStamina = maxStamina;
    }

    void Start()
    {
        FindKitchen();
        FindHouse();
        FindRestSpot();
    }

    void OnEnable()
    {
        // FIX BUG 1: Giải phóng slot cũ TRƯỚC KHI reset state
        // Không làm bước này → slot bị chiếm mãi → bếp/nhà luôn báo đầy
        if (isInsideKitchen) { kitchen?.Exit(this); }
        if (isInsideHouse)   { house?.Exit(this);   }

        ShowModel();
        isInsideKitchen       = false;
        isInsideHouse         = false;
        isHungryInside        = false;
        hasTargetRestPosition = false;
        isDraining            = false;

        // FIX BUG 2: null-check trước khi đăng ký event
        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnNightStart += HandleNightfall;
            DayNightManager.Ins.OnDayStart   += HandleDaybreak;

            // Xử lý khi spawn worker lúc đang đêm
            if (DayNightManager.Ins.CurrentMode == DayNightManager.Mode.Night
                && !isResting && !isCarryingResources)
            {
                StartResting();
            }
        }
    }

    void OnDisable()
    {
        // Dọn dẹp slot khi bị tắt (ObjectPool, scene unload...)
        if (isInsideKitchen) kitchen?.Exit(this);
        if (isInsideHouse)   house?.Exit(this);

        isInsideKitchen = false;
        isInsideHouse   = false;

        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnNightStart -= HandleNightfall;
            DayNightManager.Ins.OnDayStart   -= HandleDaybreak;
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
            // FIX: Bỏ dòng "isDraining = false" ở đây.
            // Worker AI tự gọi SetDraining(true/false) → không còn phụ thuộc thứ tự Update.
            if (isDraining)
            {
                currentStamina -= drainPerSecond * Time.deltaTime;
                currentStamina  = Mathf.Max(currentStamina, 0f);
                if (currentStamina <= restThreshold) StartResting();
            }
        }
    }

    /// <summary>
    /// Worker AI gọi mỗi frame khi đang làm việc.
    /// Phải gọi SetDraining(false) khi dừng/nghỉ để stamina ngừng drain.
    /// </summary>
    public void SetDraining(bool drain)
    {
        if (isResting) { isDraining = false; return; }
        isDraining = drain;
    }

    /// <summary>Trả về true nếu worker được phép làm việc.</summary>
    public bool CanWork()
    {
        if (isResting || isNightReturnPending) return false;
        return true;
    }

    // ===================================================
    // PHÂN LUỒNG NHÀ / BẾP TÙY VÀO THỜI GIAN
    // ===================================================

    void HandleResting()
    {
        bool isNight = DayNightManager.Ins != null
                    && DayNightManager.Ins.CurrentMode == DayNightManager.Mode.Night;

        if (isNight)
        {
            // Ban đêm: thoát bếp (nếu đang trong), chuyển sang ngủ nhà
            if (isInsideKitchen)
            {
                kitchen?.Exit(this);
                isInsideKitchen       = false;
                isHungryInside        = false;
                hasTargetRestPosition = false;
                ShowModel();
            }
            HandleHouseLogic();
        }
        else
        {
            // Ban ngày: nếu vẫn đang ngủ dở trong House thì ngủ nốt đến đủ stamina
            if (isInsideHouse)
            {
                currentStamina = Mathf.Min(currentStamina + recoverPerSecond * Time.deltaTime, maxStamina);
                if (CanStopResting()) StopResting();
                return;
            }
            HandleKitchenLogic();
        }
    }

    // ===================================================
    // KITCHEN LOGIC (BAN NGÀY — TỐN LÚA)
    // ===================================================

    void HandleKitchenLogic()
    {
        // FIX BUG 3: Chỉ tìm kitchen mới khi kitchen == null.
        // KHÔNG gọi FindKitchen() mỗi frame khi IsFull → worker sẽ không đổi đích liên tục.
        if (kitchen == null)
        {
            FindKitchen();
            hasTargetRestPosition = false;
        }

        if (kitchen == null)
        {
            HandleFallback();
            return;
        }

        // Đã ở trong bếp: hồi stamina và chờ đủ để ra
        if (isInsideKitchen)
        {
            float multiplier = isHungryInside ? hungryRecoverMultiplier : 1f;
            currentStamina = Mathf.Min(currentStamina + recoverPerSecond * multiplier * Time.deltaTime, maxStamina);
            if (CanStopResting()) StopResting();
            return;
        }

        // Bếp đầy: đứng chờ bên ngoài, hồi chậm
        if (kitchen.IsFull)
        {
            MoveToWaitingArea(kitchen.EntrancePosition);
            currentStamina = Mathf.Min(currentStamina + recoverPerSecond * hungryRecoverMultiplier * Time.deltaTime, maxStamina);
            if (CanStopResting()) StopResting();
            return;
        }

        ClearOffset();

        // Chỉ lấy slot 1 lần, tránh xoay vòng slot mỗi frame
        if (!hasTargetRestPosition)
        {
            targetRestPosition    = kitchen.GetRestPosition();
            hasTargetRestPosition = true;
        }

        MoveToPosition(targetRestPosition);

        if (Vector3.Distance(transform.position, targetRestPosition) <= interactionRadius)
        {
            if (kitchen.Enter(this, out bool consumedFood))
            {
                isInsideKitchen       = true;
                isHungryInside        = !consumedFood;
                hasTargetRestPosition = false;
                HideModel();
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            }
        }
    }

    // ===================================================
    // HOUSE LOGIC (BAN ĐÊM — MIỄN PHÍ)
    // ===================================================

    void HandleHouseLogic()
    {
        // FIX BUG 3 (tương tự kitchen): chỉ tìm house mới khi null
        if (house == null)
        {
            FindHouse();
            hasTargetRestPosition = false;
        }

        if (house == null)
        {
            HandleFallback();
            return;
        }

        // Đã ở trong nhà: hồi stamina
        if (isInsideHouse)
        {
            currentStamina = Mathf.Min(currentStamina + recoverPerSecond * Time.deltaTime, maxStamina);
            if (CanStopResting()) StopResting();
            return;
        }

        // Nhà đầy: đứng chờ ngoài sân, hồi chậm
        if (house.IsFull)
        {
            MoveToWaitingArea(house.transform.position);
            currentStamina = Mathf.Min(currentStamina + recoverPerSecond * hungryRecoverMultiplier * Time.deltaTime, maxStamina);
            if (CanStopResting()) StopResting();
            return;
        }

        ClearOffset();

        // Chỉ bốc slot giường 1 lần
        if (!hasTargetRestPosition)
        {
            targetRestPosition    = house.GetRestPosition();
            hasTargetRestPosition = true;
        }

        MoveToPosition(targetRestPosition);

        if (Vector3.Distance(transform.position, targetRestPosition) <= interactionRadius)
        {
            if (house.Enter(this))
            {
                isInsideHouse         = true;
                hasTargetRestPosition = false;
                HideModel();
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            }
        }
    }

    void HandleFallback()
    {
        if (restSpot != null)
        {
            MoveToPosition(restSpot.position);
            float stopDist = (agent != null ? agent.stoppingDistance : 0.5f) + 0.5f;
            if (Vector3.Distance(transform.position, restSpot.position) <= stopDist)
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        }
        currentStamina = Mathf.Min(currentStamina + recoverPerSecond * Time.deltaTime, maxStamina);
        if (CanStopResting()) StopResting();
    }

    // ===================================================
    // EVENT NGÀY / ĐÊM
    // ===================================================

    private void HandleNightfall()
    {
        hasTargetRestPosition = false; // Xóa điểm đến cũ, tính lại đường về House

        if (isResting) return;

        if (isCarryingResources)
            isNightReturnPending = true; // Giao hàng xong mới ngủ
        else
            StartResting();
    }

    private void HandleDaybreak()
    {
        isNightReturnPending  = false;
        hasTargetRestPosition = false; // Sang ngày mới → reset điểm đến

        if (!isResting) return;

        // FIX BUG 4: Đánh thức worker đúng cách vào buổi sáng
        // Ưu tiên 1: Stamina đã đủ → ra làm bình thường
        if (currentStamina >= resumeThreshold)
        {
            StopResting();
            return;
        }

        // Ưu tiên 2: Stamina chưa đủ nhưng đang trong house → ở lại hồi tiếp trong HandleHouseLogic()
        // Không làm gì ở đây, HandleResting() sẽ xử lý.

        // Ưu tiên 3: Stamina > morningForceWakeThreshold nhưng không vào được nhà (đứng ngoài sân cả đêm)
        // → Buộc ra làm để tránh worker đứng ngẩn cả ngày
        if (!isInsideHouse && currentStamina >= morningForceWakeThreshold)
        {
            StopResting();
        }
    }

    /// <summary>Gọi ngay khi worker nộp hàng xong.</summary>
    public void OnResourcesDeposited()
    {
        isCarryingResources = false;
        if (isNightReturnPending)
        {
            isNightReturnPending = false;
            StartResting(); // Đã giao xong, về nhà ngủ
        }
    }

    // ===================================================
    // HELPERS NỘI BỘ
    // ===================================================

    private bool CanStopResting()
    {
        if (currentStamina < resumeThreshold) return false;

        // Đang đêm → không cho ra, ngủ tiếp
        if (DayNightManager.Ins != null && DayNightManager.Ins.CurrentMode == DayNightManager.Mode.Night)
            return false;

        return true;
    }

    void StartResting()
    {
        if (isResting) return;
        isResting             = true;
        isDraining            = false;
        hasTargetRestPosition = false;
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
        if (isInsideHouse)
        {
            house?.Exit(this);
            isInsideHouse = false;
        }

        isResting             = false;
        isDraining            = false;
        hasTargetRestPosition = false;
        ClearOffset();

        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        onStaminaRecovered?.Invoke();
    }

    // ===================================================
    // TÌM KIẾM CÔNG TRÌNH GẦN NHẤT
    // ===================================================

    void FindKitchen()
    {
        GameObject[] kitchens = GameObject.FindGameObjectsWithTag("Kitchen");
        float closestDist = Mathf.Infinity;
        Kitchen best = null;

        foreach (var obj in kitchens)
        {
            Kitchen k = obj.GetComponent<Kitchen>();
            if (k != null && !k.IsFull)
            {
                float d = Vector3.Distance(transform.position, obj.transform.position);
                if (d < closestDist) { closestDist = d; best = k; }
            }
        }
        // Fallback: lấy cái đầu tiên dù đầy (hơn là null)
        kitchen = best != null ? best : (kitchens.Length > 0 ? kitchens[0].GetComponent<Kitchen>() : null);
    }

    void FindHouse()
    {
        GameObject[] houses = GameObject.FindGameObjectsWithTag("House");
        float closestDist = Mathf.Infinity;
        House best = null;

        foreach (var obj in houses)
        {
            House h = obj.GetComponent<House>();
            if (h != null && !h.IsFull)
            {
                float d = Vector3.Distance(transform.position, obj.transform.position);
                if (d < closestDist) { closestDist = d; best = h; }
            }
        }
        house = best != null ? best : (houses.Length > 0 ? houses[0].GetComponent<House>() : null);
    }

    void FindRestSpot()
    {
        if (restSpot != null) return;
        GameObject obj = GameObject.FindWithTag("RestSpot");
        if (obj != null) restSpot = obj.transform;
    }

    // ===================================================
    // MOVEMENT HELPERS
    // ===================================================

    void MoveToWaitingArea(Vector3 center)
    {
        if (!_hasPersonalOffset)
        {
            Vector2 rand       = Random.insideUnitCircle * waitingScatterRadius;
            personalOffset     = new Vector3(rand.x, 0f, rand.y);
            _hasPersonalOffset = true;
        }
        MoveToPosition(center + personalOffset);
    }

    void ClearOffset()
    {
        if (_hasPersonalOffset)
        {
            personalOffset     = Vector3.zero;
            _hasPersonalOffset = false;
        }
    }

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
            foreach (var obj in extraModelsToHide) if (obj != null) obj.SetActive(true);
    }

    void HideModel()
    {
        if (workerModel != null) workerModel.SetActive(false);
        if (extraModelsToHide != null)
            foreach (var obj in extraModelsToHide) if (obj != null) obj.SetActive(false);
    }
}