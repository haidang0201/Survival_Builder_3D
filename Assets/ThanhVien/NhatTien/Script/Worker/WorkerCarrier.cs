using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Worker vạn năng: Lấy Gỗ, Lúa, hoặc Đá từ các kho tạm mang về WarehouseStorage.
/// - Cân bằng tải tự động (ưu tiên kho nhiều tồn đọng nhất)
/// - Phân vai linh hoạt: Universal / WoodOnly / RiceOnly / StoneOnly
/// - Chống kẹt NavMesh bằng velocity thực tế
/// - Animation chuẩn hóa 0→1
/// - Chống tranh giành tài nguyên khi nhiều Carrier cùng hoạt động
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class WorkerCarrier : MonoBehaviour
{
    public enum CarrierRole  { Universal, WoodOnly, RiceOnly, StoneOnly }
    public enum ResourceType { None, Wood, Rice, Stone }

    [Header("Role Configuration")]
    [Tooltip("Universal tự động dọn kho đầy nhất. Hoặc khóa cứng vai trò tại đây.")]
    public CarrierRole role = CarrierRole.Universal;

    [Header("References")]
    public NavMeshAgent agent;
    public Transform    handPoint;
    public Transform    warehousePoint;

    [Header("Resource Pools")]
    public ObjectPool woodPool;
    public ObjectPool ricePool;
    public ObjectPool stonePool;

    [Header("Storage Points (Optional — tự tìm qua Tag nếu bỏ trống)")]
    public Transform woodStoragePoint;
    public Transform riceStoragePoint;
    public Transform stoneStoragePoint;

    [Header("Animation Settings")]
    public Animator animator;
    public string   speedParam    = "Speed";
    public string   carryingParam = "IsCarrying";

    [Header("Carrier Settings")]
    public float arriveDistance   = 1.5f;
    public float wanderRadius     = 8f;
    public float wanderInterval   = 3f;
    public float checkInterval    = 0.5f;
    public int   maxCarryCapacity = 10;
    [Tooltip("Thời gian vận tốc = 0 trước khi kích hoạt chống kẹt (giây)")]
    public float stuckTimeout     = 2f;

    // ===== INTERNAL =====
    private WoodStorage      woodStorage;
    private RiceStorage      riceStorage;
    private StoneStorage     stoneStorage;
    private WarehouseStorage warehouseStorage;

    private GameObject   currentVisualObject;
    private bool         isCarrying    = false;
    private int          carriedAmount = 0;
    private ResourceType carriedType   = ResourceType.None;

    private float   wanderTimer    = 0f;
    private float   checkTimer     = 0f;
    private Vector3 anchorPosition;
    private float   stuckTimer     = 0f;

    private enum State { Wander, MoveToStorage, MoveToWarehouse }
    private State currentState = State.Wander;

    private Transform    targetStoragePoint;
    private ResourceType targetResourceType = ResourceType.None;

    // ===================================================
    // LIFECYCLE
    // ===================================================

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        FindReferences();
        anchorPosition = transform.position;
        EnterWander();
    }

    void Update()
    {
        UpdateAnimation();
        CheckStuck();

        switch (currentState)
        {
            case State.Wander:          HandleWander();          break;
            case State.MoveToStorage:   HandleMoveToStorage();   break;
            case State.MoveToWarehouse: HandleMoveToWarehouse(); break;
        }
    }

    void OnDisable()
    {
        // Chống rò rỉ tài nguyên: trả đồ về kho tạm nếu bị tắt giữa chừng
        if (isCarrying && carriedType != ResourceType.None)
            ReturnResourcesToStorage();
    }

    // ===================================================
    // ANIMATION — Normalize 0→1, check isStopped
    // ===================================================

    void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        // Chuẩn hóa về 0→1 để Blend Tree hoạt động đúng
        float speed = agent.isStopped ? 0f
                    : (agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f);

        animator.SetFloat(speedParam, speed, 0.05f, Time.deltaTime);
        animator.SetBool(carryingParam, isCarrying);
    }

    // ===================================================
    // STATE: WANDER
    // ===================================================

    void EnterWander()
    {
        currentState       = State.Wander;
        wanderTimer        = wanderInterval; // Ép chọn điểm đi ngay frame tiếp theo
        targetStoragePoint = null;
        targetResourceType = ResourceType.None;
        // Mở khóa agent — không gọi ResetPath() để tránh giật animation
        if (agent != null) agent.isStopped = false;
    }

    void HandleWander()
    {
        // 1. Tuần tra ngẫu nhiên quanh anchorPosition
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                if (agent.isOnNavMesh)
                    agent.SetDestination(GetRandomWanderPoint());
            }
        }

        // 2. Quét kho mỗi checkInterval — reset timer dù kho rỗng hay không
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            if (TrySelectStorageToClear())
            {
                EnterMoveToStorage();
                return;
            }
        }
    }

    // Thuật toán cân bằng tải: ưu tiên kho tồn đọng nhiều nhất
    bool TrySelectStorageToClear()
    {
        int          maxAmount = 0;
        ResourceType bestType  = ResourceType.None;
        Transform    bestPoint = null;

        if ((role == CarrierRole.Universal || role == CarrierRole.WoodOnly)
            && woodStorage != null && !woodStorage.IsEmpty
            && woodStorage.CurrentAmount > maxAmount)
        {
            maxAmount = woodStorage.CurrentAmount;
            bestType  = ResourceType.Wood;
            bestPoint = woodStoragePoint;
        }

        if ((role == CarrierRole.Universal || role == CarrierRole.RiceOnly)
            && riceStorage != null && !riceStorage.IsEmpty
            && riceStorage.CurrentAmount > maxAmount)
        {
            maxAmount = riceStorage.CurrentAmount;
            bestType  = ResourceType.Rice;
            bestPoint = riceStoragePoint;
        }

        if ((role == CarrierRole.Universal || role == CarrierRole.StoneOnly)
            && stoneStorage != null && !stoneStorage.IsEmpty
            && stoneStorage.CurrentAmount > maxAmount)
        {
            maxAmount = stoneStorage.CurrentAmount;
            bestType  = ResourceType.Stone;
            bestPoint = stoneStoragePoint;
        }

        if (bestType == ResourceType.None) return false;

        targetResourceType = bestType;
        targetStoragePoint = bestPoint;
        return true;
    }

    // ===================================================
    // STATE: MOVE TO STORAGE
    // ===================================================

    void EnterMoveToStorage()
    {
        if (targetStoragePoint == null || !agent.isOnNavMesh)
        {
            EnterWander();
            return;
        }
        currentState    = State.MoveToStorage;
        agent.isStopped = false;
        agent.SetDestination(targetStoragePoint.position);
    }

    void HandleMoveToStorage()
    {
        // FIX RACE (1/2): Carrier khác dọn sạch kho trong lúc đang đi
        // → Quay đầu ngay, không đi tiếp vô ích đến tận nơi mới biết trống
        if (IsTargetStorageEmpty())
        {
            if (TrySelectStorageToClear()) EnterMoveToStorage();
            else EnterWander();
            return;
        }

        if (!HasArrived()) return;

        agent.isStopped = true;
        int taken = 0;

        switch (targetResourceType)
        {
            case ResourceType.Wood:  if (woodStorage  != null) taken = woodStorage.TakeWood(maxCarryCapacity);   break;
            case ResourceType.Rice:  if (riceStorage  != null) taken = riceStorage.TakeRice(maxCarryCapacity);   break;
            case ResourceType.Stone: if (stoneStorage != null) taken = stoneStorage.TakeStone(maxCarryCapacity); break;
        }

        // FIX RACE (2/2): 2 Carrier đến gần cùng lúc, thằng sau vẫn hết hàng dù đã kiểm tra
        // → Thử tìm kho khác còn hàng, không đứng đờ chờ Wander
        if (taken <= 0)
        {
            if (TrySelectStorageToClear()) EnterMoveToStorage();
            else EnterWander();
            return;
        }

        carriedAmount = taken;
        carriedType   = targetResourceType;
        isCarrying    = true;
        SpawnCarriedVisual();
        EnterMoveToWarehouse();
    }

    // ===================================================
    // STATE: MOVE TO WAREHOUSE
    // ===================================================

    void EnterMoveToWarehouse()
    {
        if (warehousePoint == null || !agent.isOnNavMesh)
        {
            ReturnResourcesToStorage();
            EnterWander();
            return;
        }
        currentState    = State.MoveToWarehouse;
        agent.isStopped = false;
        agent.SetDestination(warehousePoint.position);
    }

    void HandleMoveToWarehouse()
    {
        if (!HasArrived()) return;

        agent.isStopped = true;

        if (warehouseStorage != null)
        {
            switch (carriedType)
            {
                case ResourceType.Wood:  warehouseStorage.AddWood(carriedAmount);  break;
                case ResourceType.Rice:  warehouseStorage.AddRice(carriedAmount);  break;
                case ResourceType.Stone: warehouseStorage.AddStone(carriedAmount); break;
            }
        }

        ReturnVisualToPool();
        ResetCarry();

        // Sau khi nộp xong, rà soát ngay — nếu có việc thì bốc tiếp luôn
        if (TrySelectStorageToClear()) EnterMoveToStorage();
        else EnterWander();
    }

    // ===================================================
    // HELPERS
    // ===================================================

    bool HasArrived()
    {
        return !agent.pathPending
            && agent.remainingDistance <= (agent.stoppingDistance + arriveDistance);
    }

    // Kiểm tra kho mục tiêu hiện tại có trống không (dùng cho FIX RACE)
    bool IsTargetStorageEmpty()
    {
        switch (targetResourceType)
        {
            case ResourceType.Wood:  return woodStorage  == null || woodStorage.IsEmpty;
            case ResourceType.Rice:  return riceStorage  == null || riceStorage.IsEmpty;
            case ResourceType.Stone: return stoneStorage == null || stoneStorage.IsEmpty;
            default: return true;
        }
    }

    // Dùng velocity.sqrMagnitude — chính xác hơn position delta ở mọi FPS
    void CheckStuck()
    {
        if (agent == null || agent.isStopped || !agent.hasPath)
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
                if (currentState == State.Wander)
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                        agent.SetDestination(GetRandomWanderPoint());
                    }
                }
                else
                {
                    if (agent.isOnNavMesh)
                        agent.SetDestination(agent.destination);
                }
            }
        }
        else stuckTimer = 0f;
    }

    Vector3 GetRandomWanderPoint()
    {
        Vector3 randDir = Random.insideUnitSphere * wanderRadius + anchorPosition;
        if (NavMesh.SamplePosition(randDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            return hit.position;
        return anchorPosition;
    }

    // ===================================================
    // VISUAL OBJECT (CẦM TRÊN TAY)
    // ===================================================

    void SpawnCarriedVisual()
    {
        ObjectPool activePool = GetPoolForType(carriedType);
        if (activePool == null) return;

        currentVisualObject = activePool.GetObject();
        if (currentVisualObject == null) return;

        switch (carriedType)
        {
            case ResourceType.Wood:
                var wp = currentVisualObject.GetComponent<WoodPickup>();
                if (wp != null) { wp.MarkTaken(); wp.Pickup(handPoint); }
                break;
            case ResourceType.Rice:
                var rp = currentVisualObject.GetComponent<RicePickup>();
                if (rp != null) { rp.MarkTaken(); rp.Pickup(handPoint); }
                break;
            case ResourceType.Stone:
                var sp = currentVisualObject.GetComponent<StonePickup>();
                if (sp != null) { sp.MarkTaken(); sp.Pickup(handPoint); }
                break;
        }
    }

    void ReturnVisualToPool()
    {
        if (currentVisualObject == null) return;

        ObjectPool activePool = GetPoolForType(carriedType);
        if (activePool != null && currentVisualObject.activeInHierarchy)
            activePool.ReturnObject(currentVisualObject);
        else
            Destroy(currentVisualObject);

        currentVisualObject = null;
    }

    ObjectPool GetPoolForType(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood:  return woodPool;
            case ResourceType.Rice:  return ricePool;
            case ResourceType.Stone: return stonePool;
            default: return null;
        }
    }

    void ReturnResourcesToStorage()
    {
        switch (carriedType)
        {
            case ResourceType.Wood:  woodStorage?.AddWood(carriedAmount);   break;
            case ResourceType.Rice:  riceStorage?.AddRice(carriedAmount);   break;
            case ResourceType.Stone: stoneStorage?.AddStone(carriedAmount); break;
        }
        ReturnVisualToPool();
        ResetCarry();
    }

    void ResetCarry()
    {
        isCarrying    = false;
        carriedAmount = 0;
        carriedType   = ResourceType.None;
    }

    // ===================================================
    // FIND REFERENCES
    // ===================================================

    void FindReferences()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // Kho chính
        if (warehousePoint == null)
        {
            GameObject wh = GameObject.FindWithTag("Warehouse");
            if (wh != null) warehousePoint = wh.transform;
        }
        if (warehousePoint != null)
            warehouseStorage = warehousePoint.GetComponent<WarehouseStorage>()
                            ?? warehousePoint.GetComponentInChildren<WarehouseStorage>()
                            ?? warehousePoint.GetComponentInParent<WarehouseStorage>();

        if (warehouseStorage == null)
            Debug.LogError($"[WorkerCarrier] '{name}': Không tìm thấy WarehouseStorage! Kiểm tra Tag 'Warehouse'.");

        // Kho tạm Gỗ
        if (woodStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("Storage");
            if (obj != null) woodStoragePoint = obj.transform;
        }
        if (woodStoragePoint != null)
            woodStorage = woodStoragePoint.GetComponent<WoodStorage>()
                       ?? woodStoragePoint.GetComponentInChildren<WoodStorage>()
                       ?? woodStoragePoint.GetComponentInParent<WoodStorage>();

        // Kho tạm Lúa
        if (riceStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("RiceStorage");
            if (obj != null) riceStoragePoint = obj.transform;
        }
        if (riceStoragePoint != null)
            riceStorage = riceStoragePoint.GetComponent<RiceStorage>()
                       ?? riceStoragePoint.GetComponentInChildren<RiceStorage>()
                       ?? riceStoragePoint.GetComponentInParent<RiceStorage>();

        // Kho tạm Đá
        if (stoneStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("StoneStorage");
            if (obj != null) stoneStoragePoint = obj.transform;
        }
        if (stoneStoragePoint != null)
            stoneStorage = stoneStoragePoint.GetComponent<StoneStorage>()
                        ?? stoneStoragePoint.GetComponentInChildren<StoneStorage>()
                        ?? stoneStoragePoint.GetComponentInParent<StoneStorage>();
    }

    // ===================================================
    // GIZMOS
    // ===================================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? anchorPosition : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}