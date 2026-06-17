using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WorkerCarrier : MonoBehaviour
{
    public enum CarrierRole  { Universal, WoodOnly, RiceOnly, StoneOnly }
    public enum ResourceType { None, Wood, Rice, Stone }

    [Header("Role Configuration")]
    public CarrierRole role = CarrierRole.Universal;

    [Header("References")]
    public NavMeshAgent agent;
    public Transform    handPoint;
    public Transform    warehousePoint;

    [Header("Resource Pools")]
    public ObjectPool woodPool;
    public ObjectPool ricePool;
    public ObjectPool stonePool;

    [Header("Storage Points (Optional)")]
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
    public float stuckTimeout     = 2f;

    private WorkerStamina    stamina;
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
    private bool    wasResting     = false;

    private enum State { Wander, MoveToStorage, MoveToWarehouse }
    private State currentState = State.Wander;

    private Transform    targetStoragePoint;
    private ResourceType targetResourceType = ResourceType.None;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        stamina = GetComponent<WorkerStamina>() ?? GetComponentInChildren<WorkerStamina>() ?? GetComponentInParent<WorkerStamina>();
        
        FindReferences();
        anchorPosition = transform.position;
        EnterWander();
    }

    void Update()
    {
        UpdateAnimation();

        // 1. ƯU TIÊN 1: NẾU ĐANG ÔM HÀNG MÀ GẶP LỆNH CHỜ (Trời tối/Kiệt sức) -> CỐ MÀ NỘP CHO XONG!
        if (isCarrying)
        {
            wasResting = false;
            if (currentState != State.MoveToWarehouse) EnterMoveToWarehouse();
            HandleMoveToWarehouse();
            return; // Khóa không cho nhận lệnh gì khác cho đến khi nộp xong
        }

        // 2. ƯU TIÊN 2: KIỂM TRA QUYỀN LÀM VIỆC TỪ STAMINA
        // Nếu thể lực yếu hoặc trời đã tối (CanWork == false) -> Dừng mọi hoạt động, nhường quyền điều khiển NavMesh cho Stamina
        if (stamina != null && !stamina.CanWork())
        {
            if (!wasResting)
            {
                wasResting = true;
                ResetCarry(); // Vứt bỏ dự định lấy hàng
                currentState = State.Wander; // Reset state về cơ bản để hôm sau dậy tính tiếp
                // TUYỆT ĐỐI KHÔNG DÙNG agent.isStopped = true Ở ĐÂY để Stamina còn dắt nó về nhà!
            }
            return; 
        }

        // 3. ƯU TIÊN 3: KHỞI ĐỘNG LẠI KHI NGỦ DẬY
        if (wasResting)
        {
            wasResting = false;
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
            EnterWander();
        }

        // 4. ƯU TIÊN 4: VÒNG LẶP CÔNG VIỆC BÌNH THƯỜNG (Ban ngày, Khỏe mạnh, Tay không)
        CheckStuck();
        switch (currentState)
        {
            case State.Wander:          HandleWander();          break;
            case State.MoveToStorage:   HandleMoveToStorage();   break;
        }
    }

    void OnDisable()
    {
        if (isCarrying && carriedType != ResourceType.None)
            ReturnResourcesToStorage();
    }

    void UpdateAnimation()
    {
        if (animator == null || agent == null) return;
        float speed = agent.isStopped ? 0f : (agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f);
        animator.SetFloat(speedParam, speed, 0.05f, Time.deltaTime);
        animator.SetBool(carryingParam, isCarrying);
    }

    void EnterWander()
    {
        currentState       = State.Wander;
        wanderTimer        = wanderInterval;
        targetStoragePoint = null;
        targetResourceType = ResourceType.None;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        
        stamina?.SetDraining(false); 
    }

    void HandleWander()
    {
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

    bool TrySelectStorageToClear()
    {
        int          maxAmount = 0;
        ResourceType bestType  = ResourceType.None;
        Transform    bestPoint = null;

        if ((role == CarrierRole.Universal || role == CarrierRole.WoodOnly)
            && woodStorage != null && !woodStorage.IsEmpty && woodStorage.CurrentAmount > maxAmount)
        {
            maxAmount = woodStorage.CurrentAmount;
            bestType  = ResourceType.Wood;
            bestPoint = woodStoragePoint;
        }

        if ((role == CarrierRole.Universal || role == CarrierRole.RiceOnly)
            && riceStorage != null && !riceStorage.IsEmpty && riceStorage.CurrentAmount > maxAmount)
        {
            maxAmount = riceStorage.CurrentAmount;
            bestType  = ResourceType.Rice;
            bestPoint = riceStoragePoint;
        }

        if ((role == CarrierRole.Universal || role == CarrierRole.StoneOnly)
            && stoneStorage != null && !stoneStorage.IsEmpty && stoneStorage.CurrentAmount > maxAmount)
        {
            maxAmount = stoneStorage.CurrentAmount;
            bestType  = ResourceType.Stone;
            
            // ĐÃ SỬA LỖI Ở DÒNG NÀY: bestPoint = stoneStoragePoint (thay vì riceStoragePoint)
            bestPoint = stoneStoragePoint; 
        }

        if (bestType == ResourceType.None) return false;

        targetResourceType = bestType;
        targetStoragePoint = bestPoint;
        return true;
    }

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

        stamina?.SetDraining(true); 
    }

    void HandleMoveToStorage()
    {
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

        if (taken <= 0)
        {
            if (TrySelectStorageToClear()) EnterMoveToStorage();
            else EnterWander();
            return;
        }

        carriedAmount = taken;
        carriedType   = targetResourceType;
        isCarrying    = true;

        if (stamina != null) stamina.isCarryingResources = true;

        SpawnCarriedVisual();
        EnterMoveToWarehouse();
    }

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

        stamina?.SetDraining(true); 
    }

    void HandleMoveToWarehouse()
    {
        CheckStuck(); // Cần kiểm tra kẹt khi đang nộp hàng

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

        // Báo cho Stamina biết đã nộp hàng xong. 
        // Nếu trời đang đêm, Stamina sẽ ngay lập tức chiếm quyền dắt nó về nhà ngủ!
        if (stamina != null) stamina.OnResourcesDeposited();

        // Chỉ khi Stamina cho phép làm việc tiếp thì mới đi tìm kho để dọn, nếu không thì đứng chờ Stamina ra lệnh
        if (stamina == null || stamina.CanWork())
        {
            if (TrySelectStorageToClear()) EnterMoveToStorage();
            else EnterWander();
        }
    }

    bool HasArrived()
    {
        if (agent == null || !agent.isOnNavMesh) return false;
        return !agent.pathPending && agent.remainingDistance <= (agent.stoppingDistance + arriveDistance);
    }

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
        if (stamina != null) stamina.isCarryingResources = false;
    }

    void FindReferences()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (warehousePoint == null)
        {
            GameObject wh = GameObject.FindWithTag("Warehouse");
            if (wh != null) warehousePoint = wh.transform;
        }
        if (warehousePoint != null)
            warehouseStorage = warehousePoint.GetComponent<WarehouseStorage>() ?? warehousePoint.GetComponentInChildren<WarehouseStorage>() ?? warehousePoint.GetComponentInParent<WarehouseStorage>();

        if (woodStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("Storage");
            if (obj != null) woodStoragePoint = obj.transform;
        }
        if (woodStoragePoint != null)
            woodStorage = woodStoragePoint.GetComponent<WoodStorage>() ?? woodStoragePoint.GetComponentInChildren<WoodStorage>() ?? woodStoragePoint.GetComponentInParent<WoodStorage>();

        if (riceStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("RiceStorage");
            if (obj != null) riceStoragePoint = obj.transform;
        }
        if (riceStoragePoint != null)
            riceStorage = riceStoragePoint.GetComponent<RiceStorage>() ?? riceStoragePoint.GetComponentInChildren<RiceStorage>() ?? riceStoragePoint.GetComponentInParent<RiceStorage>();

        if (stoneStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("StoneStorage");
            if (obj != null) stoneStoragePoint = obj.transform;
        }
        if (stoneStoragePoint != null)
            stoneStorage = stoneStoragePoint.GetComponent<StoneStorage>() ?? stoneStoragePoint.GetComponentInChildren<StoneStorage>() ?? stoneStoragePoint.GetComponentInParent<StoneStorage>();
    }
}