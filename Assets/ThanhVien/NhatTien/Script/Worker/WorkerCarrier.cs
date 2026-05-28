using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Worker lấy gỗ từ WoodStorage → mang về WarehouseStorage.
/// Đã sửa lỗi Dupe tài nguyên và tối ưu thuật toán chống kẹt (Anti-Stuck).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class WorkerCarrier : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform handPoint;
    public ObjectPool woodPool;
    public Transform storage;
    public Transform warehousePoint;

    [Header("Animation Settings")]
    public Animator animator;
    public string speedParam = "Speed";
    public string carryingParam = "IsCarrying";

    [Header("Settings")]
    public float arriveDistance = 1.5f;
    public float wanderRadius = 8f;
    public float wanderInterval = 3f;
    public float checkInterval = 0.5f;
    [Tooltip("Thời gian chờ khi vận tốc agent bằng 0 dù đang có đường đi (giây)")]
    public float stuckTimeout = 1.5f;

    // Internal
    private WoodStorage woodStorage;
    private WarehouseStorage warehouseStorage;
    private WoodPickup currentWood;
    
    private bool isCarrying = false;
    private int carriedAmount = 0;
    
    private float wanderTimer = 0f;
    private float checkTimer = 0f;
    private Vector3 anchorPosition;
    private float stuckTimer = 0f;

    private enum State { Wander, MoveToStorage, MoveToWarehouse }
    private State currentState = State.Wander;

    void Start()
    {
        FindReferences();
        anchorPosition = storage != null ? storage.position : transform.position;
        EnterWander();
    }

    void OnDisable()
    {
        // FIX: Chỉ hoàn trả gỗ về nơi xuất phát (kho tạm), tuyệt đối không cộng vào kho chính 
        // để tránh lỗi người chơi lạm dụng tắt/bật AI để farm gỗ.
        if (isCarrying)
        {
            if (woodStorage != null)
            {
                woodStorage.AddWood(carriedAmount);
            }
            ReturnWoodToPool();
            ResetCarry();
        }
    }

    void Update()
    {
        UpdateAnimation();
        CheckStuck();

        switch (currentState)
        {
            case State.Wander: HandleWander(); break;
            case State.MoveToStorage: HandleMoveToStorage(); break;
            case State.MoveToWarehouse: HandleMoveToWarehouse(); break;
        }
    }

    // ===== ANIMATION =====
    void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        float currentSpeed = agent.velocity.magnitude;
        float speedRatio = agent.speed > 0 ? currentSpeed / agent.speed : 0f;
        if (agent.isStopped) speedRatio = 0f;

        animator.SetFloat(speedParam, speedRatio, 0.05f, Time.deltaTime);
        animator.SetBool(carryingParam, isCarrying);
    }

    // ===== KẸT & DI CHUYỂN =====
    void CheckStuck()
    {
        // Bỏ qua nếu đang cố tình đứng im hoặc đang đi lang thang
        if (agent.isStopped || !agent.hasPath || currentState == State.Wander) 
        {
            stuckTimer = 0f;
            return;
        }

        // FIX: Kiểm tra kẹt dựa trên vận tốc thực (velocity) của NavMeshAgent thay vì tọa độ từng frame.
        // Tránh lỗi nhận diện nhầm khi game chạy ở FPS cao.
        if (agent.velocity.sqrMagnitude < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
            {
                Debug.LogWarning($"[WorkerCarrier] {name} bị kẹt, đang tìm lại đường...");
                stuckTimer = 0f;
                agent.ResetPath();
                
                // Trực tiếp gán lại điểm đến dựa trên trạng thái hiện tại
                if (currentState == State.MoveToStorage && storage != null)
                    agent.SetDestination(storage.position);
                else if (currentState == State.MoveToWarehouse && warehousePoint != null)
                    agent.SetDestination(warehousePoint.position);
            }
        }
        else
        {
            stuckTimer = 0f; // Reset ngay khi agent bắt đầu di chuyển lại
        }
    }

    void SetStopped(bool stopped)
    {
        if (agent == null) return;
        if (agent.isStopped != stopped)
        {
            agent.isStopped = stopped;
            if (stopped) 
            {
                agent.ResetPath();
            }
            stuckTimer = 0f;
        }
    }

    bool HasArrived(Vector3 destination)
    {
        if (agent.pathPending) return false;
        
        // Dùng Distance thay vì remainingDistance vì đôi khi remainingDistance 
        // tính toán đường vòng (corners) chưa chính xác 100% trên bề mặt phức tạp
        float distance = Vector3.Distance(transform.position, destination);
        return distance <= (agent.stoppingDistance + arriveDistance);
    }

    // ===== REFERENCES =====
    void FindReferences()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        if (storage == null)
        {
            GameObject obj = GameObject.FindWithTag("Storage");
            if (obj != null) storage = obj.transform;
        }

        if (storage != null)
        {
            woodStorage = storage.GetComponent<WoodStorage>() 
                          ?? storage.GetComponentInChildren<WoodStorage>() 
                          ?? storage.GetComponentInParent<WoodStorage>();
        }

        if (warehousePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("Warehouse");
            if (obj != null) warehousePoint = obj.transform;
        }

        if (warehousePoint != null)
        {
            warehouseStorage = warehousePoint.GetComponent<WarehouseStorage>() 
                               ?? warehousePoint.GetComponentInChildren<WarehouseStorage>() 
                               ?? warehousePoint.GetComponentInParent<WarehouseStorage>();
        }

        if (woodStorage == null) Debug.LogError($"[WorkerCarrier] {name} : Không tìm thấy WoodStorage!");
        if (warehouseStorage == null) Debug.LogError($"[WorkerCarrier] {name} : Không tìm thấy WarehouseStorage!");
    }

    // ===== PICKUP / DROP =====
    void PickupWood()
    {
        if (woodPool == null || handPoint == null) return;

        GameObject obj = woodPool.GetObject();
        if (obj == null) return;

        currentWood = obj.GetComponent<WoodPickup>();
        if (currentWood == null)
        {
            obj.SetActive(false); // Trả lại nếu gameObject lấy ra không hợp lệ
            return;
        }

        currentWood.MarkTaken();
        currentWood.Pickup(handPoint);
    }

    void ReturnWoodToPool()
    {
        if (currentWood == null) return;
        
        // Kiểm tra kĩ phòng trường hợp Pool đã bị dọn dẹp khi đổi Scene
        if (woodPool != null && currentWood.gameObject.activeInHierarchy)
            woodPool.ReturnObject(currentWood.gameObject);
        else
            Destroy(currentWood.gameObject);
            
        currentWood = null;
    }

    void ResetCarry()
    {
        isCarrying = false;
        carriedAmount = 0;
    }

    // ===== STATE: WANDER =====
    void EnterWander()
    {
        currentState = State.Wander;
        wanderTimer = wanderInterval;
        checkTimer = 0f;
        SetStopped(false);
    }

    void HandleWander()
{
    checkTimer += Time.deltaTime;
    if (checkTimer >= checkInterval)
    {
        checkTimer = 0f; // ✅ Reset dù kho rỗng hay không
        if (woodStorage != null && !woodStorage.IsEmpty)
        {
            EnterMoveToStorage();
            return;
        }
    }
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            SetStopped(true);
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval)
            {
                wanderTimer = 0f;
                MoveToRandomPoint();
            }
        }
    }

    void MoveToRandomPoint()
    {
        if (storage != null) anchorPosition = storage.position;

        Vector3 randomPos = anchorPosition + new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            0f,
            Random.Range(-wanderRadius, wanderRadius)
        );

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            SetStopped(false);
            agent.SetDestination(hit.position);
        }
    }

    // ===== STATE: MOVE TO STORAGE =====
    void EnterMoveToStorage()
    {
        if (storage == null)
        {
            EnterWander();
            return;
        }

        currentState = State.MoveToStorage;
        SetStopped(false);
        agent.SetDestination(storage.position);
    }

    void HandleMoveToStorage()
    {
        if (woodStorage == null || woodStorage.IsEmpty)
        {
            EnterWander();
            return;
        }

        if (!HasArrived(storage.position)) return;

        SetStopped(true);
        int taken = woodStorage.TakeWood(1);
        if (taken <= 0)
        {
            EnterWander();
            return;
        }

        carriedAmount = taken;
        isCarrying = true;
        PickupWood();
        EnterMoveToWarehouse();
    }

    // ===== STATE: MOVE TO WAREHOUSE =====
    void EnterMoveToWarehouse()
    {
        if (warehousePoint == null)
        {
            woodStorage?.AddWood(carriedAmount);
            ReturnWoodToPool();
            ResetCarry();
            EnterWander();
            return;
        }

        currentState = State.MoveToWarehouse;
        SetStopped(false);
        agent.SetDestination(warehousePoint.position);
    }

    void HandleMoveToWarehouse()
    {
        if (!HasArrived(warehousePoint.position)) return;

        SetStopped(true);
        if (warehouseStorage != null)
            warehouseStorage.AddWood(carriedAmount);

        ReturnWoodToPool();
        ResetCarry();

        if (woodStorage != null && !woodStorage.IsEmpty)
            EnterMoveToStorage();
        else
            EnterWander();
    }

    // ===== GIZMOS =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? anchorPosition : (storage != null ? storage.position : transform.position);
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}