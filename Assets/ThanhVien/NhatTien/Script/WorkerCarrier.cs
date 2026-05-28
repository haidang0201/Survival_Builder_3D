using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Worker lấy gỗ từ WoodStorage (kho tạm) → mang về WarehouseStorage (kho chính → cộng UI).
/// Nếu kho tạm trống → đi lang thang.
/// Gán tag "Storage" vào WoodStorage, tag "Warehouse" vào WarehouseStorage.
/// </summary>
public class WorkerCarrier : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform handPoint;
    public ObjectPool woodPool;
    public Transform storage;          // WoodStorage (kho tạm)
    public Transform warehousePoint; // WarehouseStorage (kho chính)

    [Header("Animation (Idle/Walk Only)")]
    public Animator animator;         // param float "Speed"

    [Header("Settings")]
    public float arriveDistance = 1.5f;
    public float wanderRadius = 10f;
    public float wanderInterval = 3f;   // thời gian giữ ở điểm đến trước khi chọn điểm mới
    public float checkInterval = 1f;

    // ===== INTERNAL =====
    private WoodStorage woodStorage;
    private WarehouseStorage warehouseStorage;
    private WoodPickup currentWood;

    private bool isCarrying = false;
    private int carriedAmount = 0;

    private float wanderTimer = 0f;
    private float checkTimer = 0f;

    private enum State { Wander, MoveToStorage, MoveToWarehouse }
    private State currentState = State.Wander;

    void Start()
    {
        FindReferences();
        EnterWander();
    }

    void Update()
    {
        UpdateAnimationSpeed();

        switch (currentState)
        {
            case State.Wander:           HandleWander();           break;
            case State.MoveToStorage:    HandleMoveToStorage();    break;
            case State.MoveToWarehouse:  HandleMoveToWarehouse();  break;
        }
    }

    // ===== ANIMATION =====
    void UpdateAnimationSpeed()
    {
        if (animator == null || agent == null) return;

        // Nếu đang dừng hẳn thì ép speed = 0 để idle “chuẩn” hơn (tránh velocity còn dư)
        float speed = agent.isStopped ? 0f : agent.velocity.magnitude;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }

    void SetStopped(bool stopped)
    {
        if (agent == null) return;
        agent.isStopped = stopped;
    }

    // ===== FIND REFERENCES =====
    void FindReferences()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (storage == null)
        {
            GameObject obj = GameObject.FindWithTag("Storage");
            if (obj != null) storage = obj.transform;
        }

        if (storage != null)
        {
            woodStorage = storage.GetComponent<WoodStorage>()
                       ?? storage.GetComponentInParent<WoodStorage>()
                       ?? storage.GetComponentInChildren<WoodStorage>();
        }

        if (warehousePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("Warehouse");
            if (obj != null) warehousePoint = obj.transform;
        }

        if (warehousePoint != null)
        {
            warehouseStorage = warehousePoint.GetComponent<WarehouseStorage>()
                            ?? warehousePoint.GetComponentInParent<WarehouseStorage>()
                            ?? warehousePoint.GetComponentInChildren<WarehouseStorage>();
        }

        if (woodStorage == null)
            Debug.LogError($"[WorkerCarrier] '{name}': Không tìm thấy WoodStorage! Gán tag 'Storage'.");
        if (warehouseStorage == null)
            Debug.LogError($"[WorkerCarrier] '{name}': Không tìm thấy WarehouseStorage! Gán tag 'Warehouse'.");
        if (handPoint == null)
            Debug.LogWarning($"[WorkerCarrier] '{name}': Chưa gán handPoint!");
        if (woodPool == null)
            Debug.LogWarning($"[WorkerCarrier] '{name}': Chưa gán woodPool!");

        Debug.Log($"[WorkerCarrier] '{name}': " +
                  $"Storage='{(storage != null ? storage.name : "null")}' | " +
                  $"Warehouse='{(warehousePoint != null ? warehousePoint.name : "null")}'");
    }

    // ===== PICKUP / DROP =====
    void PickupWood()
    {
        if (woodPool == null || handPoint == null) return;

        GameObject obj = woodPool.GetObject();
        if (obj == null) return;

        currentWood = obj.GetComponent<WoodPickup>();
        if (currentWood == null) return;

        currentWood.MarkTaken();
        currentWood.Pickup(handPoint);

        Debug.Log($"[WorkerCarrier] '{name}': Cầm gỗ lên tay.");
    }

    void DropWood()
    {
        if (currentWood == null) return;

        if (currentWood.pool != null)
            currentWood.pool.ReturnObject(currentWood.gameObject);
        else
            currentWood.gameObject.SetActive(false);

        currentWood = null;

        Debug.Log($"[WorkerCarrier] '{name}': Thả gỗ xuống.");
    }

    // ===== STATE: WANDER =====
    void EnterWander()
    {
        currentState = State.Wander;
        wanderTimer = 0f;
        checkTimer = 0f;

        // Khi vào Wander, đảm bảo agent đang đi (nếu đã có destination trước đó, SetDestination sẽ làm lại)
        SetStopped(false);
        MoveToRandomPoint();
    }

    void HandleWander()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;

            if (woodStorage != null && !woodStorage.IsEmpty)
            {
                Debug.Log($"[WorkerCarrier] '{name}': Kho tạm có gỗ → đến lấy.");
                EnterMoveToStorage();
                return;
            }
        }

        bool arrived = !agent.pathPending &&
                        agent.remainingDistance <= agent.stoppingDistance + 0.1f;

        if (arrived)
        {
            // ✅ Đến điểm → idle, chờ wanderInterval rồi chọn điểm mới
            SetStopped(true);
            wanderTimer += Time.deltaTime;

            if (wanderTimer >= wanderInterval)
            {
                wanderTimer = 0f;
                MoveToRandomPoint();
                SetStopped(false);
            }

            return;
        }

        // Đang đi đến điểm → walk
        SetStopped(false);
        wanderTimer = 0f;
    }

    void MoveToRandomPoint()
    {
        Vector3 randomPos = transform.position + new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            0f,
            Random.Range(-wanderRadius, wanderRadius)
        );

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    // ===== STATE: MOVE TO STORAGE =====
    void EnterMoveToStorage()
    {
        if (storage == null) { EnterWander(); return; }

        currentState = State.MoveToStorage;
        SetStopped(false);
        agent.SetDestination(storage.position);
    }

    void HandleMoveToStorage()
    {
        if (woodStorage != null && woodStorage.IsEmpty)
        {
            Debug.Log($"[WorkerCarrier] '{name}': Kho tạm hết gỗ → lang thang.");
            EnterWander();
            return;
        }

        if (!HasArrived(storage.position)) return;

        // ✅ tới nơi
        SetStopped(true);

        int taken = woodStorage != null ? woodStorage.TakeWood(1) : 0;
        if (taken <= 0)
        {
            Debug.Log($"[WorkerCarrier] '{name}': Lấy gỗ thất bại → lang thang.");
            SetStopped(false);
            EnterWander();
            return;
        }

        carriedAmount = taken;
        isCarrying = true;

        PickupWood();

        Debug.Log($"[WorkerCarrier] '{name}': Lấy {taken} gỗ → về kho chính.");
        EnterMoveToWarehouse();
    }

    // ===== STATE: MOVE TO WAREHOUSE =====
    void EnterMoveToWarehouse()
    {
        if (warehousePoint == null)
        {
            Debug.LogWarning($"[WorkerCarrier] '{name}': Không có kho chính → trả gỗ lại kho tạm.");
            woodStorage?.AddWood(carriedAmount);
            DropWood();
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

        // ✅ tới nơi -> idle đúng frame
        SetStopped(true);

        if (warehouseStorage != null)
            warehouseStorage.AddWood(carriedAmount);
        else
            Debug.LogWarning($"[WorkerCarrier] '{name}': Không có WarehouseStorage — gỗ bị mất!");

        Debug.Log($"[WorkerCarrier] '{name}': Giao {carriedAmount} gỗ vào kho chính!");

        DropWood();
        ResetCarry();

        // Sau giao xong: nếu còn kho tạm có gỗ thì đi lấy tiếp, còn không thì lang thang
        if (woodStorage != null && !woodStorage.IsEmpty)
        {
            SetStopped(false);
            EnterMoveToStorage();
        }
        else
        {
            SetStopped(false);
            EnterWander();
        }
    }

    // ===== HELPERS =====
    bool HasArrived(Vector3 destination)
    {
        return Vector3.Distance(transform.position, destination) <= arriveDistance;
    }

    void ResetCarry()
    {
        isCarrying = false;
        carriedAmount = 0;
    }

    // ===== GIZMO =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        if (storage != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, storage.position);
        }

        if (warehousePoint != null)
        {
            Gizmos.color = isCarrying ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, warehousePoint.position);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.2f,
            $"{name} | {currentState} | Gỗ: {carriedAmount}"
        );
#endif
    }
}