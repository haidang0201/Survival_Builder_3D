using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

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
    public Transform    warehousePoint; // Fallback thủ công nếu không tìm được theo Tag

    [Header("Resource Pools")]
    public ObjectPool woodPool;
    public ObjectPool ricePool;
    public ObjectPool stonePool;

    [Header("Storage Points (Optional - fallback thủ công)")]
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

    // Storage/warehouse cụ thể (kho + điểm giao hàng) được chọn động theo khoảng cách
    private Transform    targetStoragePoint;   // = DeliveryPoint của kho tạm đã chọn

    public bool IsCarrying() => isCarrying;
    public ResourceType GetCarriedType() => carriedType;
    public int GetCarriedAmount() => carriedAmount;

    public void PickUpFakeItemForLoad(ResourceType type, int amount)
    {
        if (type == ResourceType.None || amount <= 0) return;

        isCarrying = true;
        carriedType = type;
        carriedAmount = amount;

        // Sinh visual ảo tuỳ loại
        string itemName = "FakeItem_Loaded";
        if (type == ResourceType.Wood) itemName = "FakeWood_Loaded";
        else if (type == ResourceType.Rice) itemName = "FakeRice_Loaded";
        else if (type == ResourceType.Stone) itemName = "FakeStone_Loaded";

        GameObject fakeItem = new GameObject(itemName);
        fakeItem.transform.SetParent(handPoint);
        fakeItem.transform.localPosition = Vector3.zero;
        
        currentVisualObject = fakeItem;
        currentState = State.MoveToWarehouse;
        if (animator != null) animator.SetBool(carryingParam, true);
    }
    private object        targetStorageComponent; // WoodStorage/RiceStorage/StoneStorage tương ứng, dùng object vì 3 kiểu khác nhau
    private ResourceType targetResourceType = ResourceType.None;

    private Transform         targetWarehousePoint;   // = DeliveryPoint của warehouse đã chọn
    private WarehouseStorage  targetWarehouseStorage;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        stamina = GetComponent<WorkerStamina>() ?? GetComponentInChildren<WorkerStamina>() ?? GetComponentInParent<WorkerStamina>();

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
        targetStorageComponent = null;
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

    /// <summary>
    /// Với mỗi loại resource (Wood/Rice/Stone) được role cho phép, tìm kho GẦN NHẤT còn hàng
    /// (cùng Tag tương ứng). Sau đó so sánh CurrentAmount giữa 3 loại để chọn loại cần dọn nhất,
    /// giữ đúng tiêu chí ưu tiên cũ (loại nào tồn nhiều hơn thì ưu tiên loại đó).
    /// </summary>
    bool TrySelectStorageToClear()
    {
        int          maxAmount = 0;
        ResourceType bestType  = ResourceType.None;
        Transform    bestPoint = null;
        object       bestStorage = null;

        if (role == CarrierRole.Universal || role == CarrierRole.WoodOnly)
        {
            var (ws, point) = FindNearestNonEmptyStorage<WoodStorage>("Storage", woodStoragePoint,
                s => !s.IsEmpty, s => s.CurrentAmount);
            if (ws != null && ws.CurrentAmount > maxAmount)
            {
                maxAmount   = ws.CurrentAmount;
                bestType    = ResourceType.Wood;
                bestPoint   = point;
                bestStorage = ws;
            }
        }

        if (role == CarrierRole.Universal || role == CarrierRole.RiceOnly)
        {
            var (rs, point) = FindNearestNonEmptyStorage<RiceStorage>("RiceStorage", riceStoragePoint,
                s => !s.IsEmpty, s => s.CurrentAmount);
            if (rs != null && rs.CurrentAmount > maxAmount)
            {
                maxAmount   = rs.CurrentAmount;
                bestType    = ResourceType.Rice;
                bestPoint   = point;
                bestStorage = rs;
            }
        }

        if (role == CarrierRole.Universal || role == CarrierRole.StoneOnly)
        {
            var (ss, point) = FindNearestNonEmptyStorage<StoneStorage>("StoneStorage", stoneStoragePoint,
                s => !s.IsEmpty, s => s.CurrentAmount);
            if (ss != null && ss.CurrentAmount > maxAmount)
            {
                maxAmount   = ss.CurrentAmount;
                bestType    = ResourceType.Stone;
                bestPoint   = point;
                bestStorage = ss;
            }
        }

        if (bestType == ResourceType.None) return false;

        targetResourceType     = bestType;
        targetStoragePoint     = bestPoint;
        targetStorageComponent = bestStorage;
        return true;
    }

    /// <summary>
    /// Quét tất cả GameObject có Tag chỉ định, lấy component T, sắp gần -> xa tính từ vị trí worker,
    /// và trả về kho GẦN NHẤT thỏa điều kiện isEligible (ví dụ: không rỗng).
    /// Điểm trả về là DeliveryPoint (child) của kho, không phải tâm kho.
    /// Nếu không tìm được gì theo Tag, fallback về Transform thủ công đã gán (nếu có).
    /// </summary>
    (T storage, Transform point) FindNearestNonEmptyStorage<T>(string tag, Transform manualFallback, System.Func<T, bool> isEligible, System.Func<T, int> amountSelector) where T : Component
    {
        GameObject[] candidates = GameObject.FindGameObjectsWithTag(tag);
        List<(T storage, Transform point, float dist)> found = new List<(T, Transform, float)>();

        if (candidates != null)
        {
            foreach (GameObject obj in candidates)
            {
                T storage = obj.GetComponent<T>() ?? obj.GetComponentInChildren<T>();
                if (storage == null || !isEligible(storage)) continue;

                Transform deliveryPoint = FindDeliveryPoint(obj.transform);
                float d = Vector3.Distance(transform.position, deliveryPoint.position);
                found.Add((storage, deliveryPoint, d));
            }
        }

        if (found.Count > 0)
        {
            var nearest = found.OrderBy(f => f.dist).First();
            return (nearest.storage, nearest.point);
        }

        // Fallback: Transform gán tay thủ công trong Inspector
        if (manualFallback != null)
        {
            T storage = manualFallback.GetComponent<T>() ?? manualFallback.GetComponentInChildren<T>() ?? manualFallback.GetComponentInParent<T>();
            if (storage != null && isEligible(storage))
                return (storage, FindDeliveryPoint(manualFallback));
        }

        return (null, null);
    }

    /// <summary>
    /// Tìm child Transform tên "DeliveryPoint" bên trong 1 kho/warehouse (cửa vào, nơi worker thực sự đi tới).
    /// Nếu không có, fallback về chính transform gốc để không bị null.
    /// </summary>
    Transform FindDeliveryPoint(Transform root)
    {
        Transform dp = root.Find("DeliveryPoint");
        if (dp != null) return dp;

        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child.name == "DeliveryPoint") return child;
        }

        return root;
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
            case ResourceType.Wood:  if (targetStorageComponent is WoodStorage  ws) taken = ws.TakeWood(maxCarryCapacity);   break;
            case ResourceType.Rice:  if (targetStorageComponent is RiceStorage  rs) taken = rs.TakeRice(maxCarryCapacity);   break;
            case ResourceType.Stone: if (targetStorageComponent is StoneStorage ss) taken = ss.TakeStone(maxCarryCapacity);  break;
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
        var (wh, point) = FindNearestNonEmptyStorage<WarehouseStorage>("Warehouse", warehousePoint,
            s => true, s => 0); // Warehouse luôn hợp lệ để nộp hàng, không cần điều kiện rỗng/đầy ở đây

        targetWarehouseStorage = wh;
        targetWarehousePoint   = point;

        if (targetWarehousePoint == null || !agent.isOnNavMesh)
        {
            ReturnResourcesToStorage();
            EnterWander();
            return;
        }
        currentState    = State.MoveToWarehouse;
        agent.isStopped = false;
        agent.SetDestination(targetWarehousePoint.position);

        stamina?.SetDraining(true); 
    }

    void HandleMoveToWarehouse()
    {
        CheckStuck(); // Cần kiểm tra kẹt khi đang nộp hàng

        if (!HasArrived()) return;

        agent.isStopped = true;

        if (targetWarehouseStorage != null)
        {
            switch (carriedType)
            {
                case ResourceType.Wood:  targetWarehouseStorage.AddWood(carriedAmount);  break;
                case ResourceType.Rice:  targetWarehouseStorage.AddRice(carriedAmount);  break;
                case ResourceType.Stone: targetWarehouseStorage.AddStone(carriedAmount); break;
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
            case ResourceType.Wood:  return !(targetStorageComponent is WoodStorage  ws && !ws.IsEmpty);
            case ResourceType.Rice:  return !(targetStorageComponent is RiceStorage  rs && !rs.IsEmpty);
            case ResourceType.Stone: return !(targetStorageComponent is StoneStorage ss && !ss.IsEmpty);
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
        // Trả hàng về kho gần nhất còn tương ứng loại (fallback an toàn nếu warehouse không tới được)
        switch (carriedType)
        {
            case ResourceType.Wood:
                var (ws, _) = FindNearestNonEmptyStorage<WoodStorage>("Storage", woodStoragePoint, s => true, s => 0);
                ws?.AddWood(carriedAmount);
                break;
            case ResourceType.Rice:
                var (rs, _) = FindNearestNonEmptyStorage<RiceStorage>("RiceStorage", riceStoragePoint, s => true, s => 0);
                rs?.AddRice(carriedAmount);
                break;
            case ResourceType.Stone:
                var (ss, _) = FindNearestNonEmptyStorage<StoneStorage>("StoneStorage", stoneStoragePoint, s => true, s => 0);
                ss?.AddStone(carriedAmount);
                break;
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
}