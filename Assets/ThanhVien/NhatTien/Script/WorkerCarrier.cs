using UnityEngine;
using UnityEngine.AI;


public class WorkerCarrier : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform    storage;      // Kho gỗ (có WoodStorage component)
    public Transform    deliveryPoint; // Điểm giao gỗ đến

    [Header("Settings")]
    public float arriveDistance  = 1.5f; // khoảng cách tính là "đã đến nơi"
    public float wanderRadius    = 10f;  // bán kính đi lang thang
    public float wanderInterval  = 3f;   // thời gian đứng mỗi điểm khi lang thang
    public float checkInterval   = 1f;   // tần suất check kho có gỗ không (giây)

    // ===== INTERNAL =====
    private WoodStorage woodStorage;
    private bool isCarrying      = false;
    private int  carriedAmount   = 0;

    private float wanderTimer = 0f;
    private float checkTimer  = 0f;

    private enum State { Wander, MoveToStorage, MoveToDelivery }
    private State currentState = State.Wander;

    // ===== LIFECYCLE =====

    void Start()
    {
        FindReferences();
        EnterWander();
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Wander:         HandleWander();         break;
            case State.MoveToStorage:  HandleMoveToStorage();  break;
            case State.MoveToDelivery: HandleMoveToDelivery(); break;
        }
    }

    // ===== FIND REFERENCES =====

    void FindReferences()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        // Tìm Storage
        if (storage == null)
        {
            GameObject obj = GameObject.FindWithTag("Storage");
            if (obj != null) storage = obj.transform;
        }

        if (storage != null)
        {
            woodStorage = storage.GetComponent<WoodStorage>();
            if (woodStorage == null)
                woodStorage = storage.GetComponentInParent<WoodStorage>();
            if (woodStorage == null)
                woodStorage = storage.GetComponentInChildren<WoodStorage>();
        }

        // Tìm DeliveryPoint
        if (deliveryPoint == null)
        {
            GameObject obj = GameObject.FindWithTag("Delivery");
            if (obj != null) deliveryPoint = obj.transform;
        }

        // Log kết quả
        if (woodStorage == null)
            Debug.LogError($"[WorkerCarrier] '{name}': Không tìm thấy WoodStorage! " +
                           $"Gắn tag 'Storage' vào kho hoặc kéo thả vào Inspector.");

        if (deliveryPoint == null)
            Debug.LogError($"[WorkerCarrier] '{name}': Không tìm thấy deliveryPoint! " +
                           $"Gắn tag 'Delivery' vào điểm giao hoặc kéo thả vào Inspector.");

        Debug.Log($"[WorkerCarrier] '{name}': " +
                  $"Storage='{(storage != null ? storage.name : "null")}' | " +
                  $"Delivery='{(deliveryPoint != null ? deliveryPoint.name : "null")}'");
    }

    // ===== STATE: WANDER =====

    void EnterWander()
    {
        currentState = State.Wander;
        wanderTimer  = 0f;
        checkTimer   = 0f;

        Debug.Log($"[WorkerCarrier] '{name}': Kho trống → bắt đầu đi lang thang.");

        MoveToRandomPoint();
    }

    void HandleWander()
    {
        // Kiểm tra định kỳ xem kho có gỗ chưa
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;

            if (woodStorage != null && !woodStorage.IsEmpty)
            {
                Debug.Log($"[WorkerCarrier] '{name}': Kho có gỗ ({woodStorage.CurrentAmount}) → đến lấy.");
                EnterMoveToStorage();
                return;
            }
        }

        // Đi đến điểm ngẫu nhiên tiếp theo sau wanderInterval giây
        wanderTimer += Time.deltaTime;

        bool arrived = !agent.pathPending &&
                        agent.remainingDistance <= agent.stoppingDistance + 0.1f;

        if (arrived && wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f;
            MoveToRandomPoint();
        }
    }

    void MoveToRandomPoint()
    {
        Vector3 randomPos = transform.position +
                            new Vector3(
                                Random.Range(-wanderRadius, wanderRadius),
                                0f,
                                Random.Range(-wanderRadius, wanderRadius)
                            );

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
            Debug.Log($"[WorkerCarrier] '{name}': Lang thang đến {hit.position}");
        }
    }

    // ===== STATE: MOVE TO STORAGE =====

    void EnterMoveToStorage()
    {
        if (storage == null)
        {
            Debug.LogWarning($"[WorkerCarrier] '{name}': Không có storage để đến!");
            EnterWander();
            return;
        }

        currentState    = State.MoveToStorage;
        agent.isStopped = false;
        agent.SetDestination(storage.position);

        Debug.Log($"[WorkerCarrier] '{name}': Đang đi đến kho '{storage.name}'.");
    }

    void HandleMoveToStorage()
    {
        // Nếu kho hết gỗ trong lúc đang đi → quay lại lang thang
        if (woodStorage != null && woodStorage.IsEmpty)
        {
            Debug.Log($"[WorkerCarrier] '{name}': Kho hết gỗ trong lúc đi → quay lại lang thang.");
            EnterWander();
            return;
        }

        if (!HasArrived(storage.position)) return;

        // Đã đến kho → lấy gỗ
        int taken = woodStorage != null ? woodStorage.TakeWood(1) : 0;

        if (taken <= 0)
        {
            Debug.Log($"[WorkerCarrier] '{name}': Lấy gỗ thất bại (kho trống) → lang thang.");
            EnterWander();
            return;
        }

        carriedAmount = taken;
        isCarrying    = true;

        Debug.Log($"[WorkerCarrier] '{name}': Lấy {taken} gỗ từ kho → đi giao.");
        EnterMoveToDelivery();
    }

    // ===== STATE: MOVE TO DELIVERY =====

    void EnterMoveToDelivery()
    {
        if (deliveryPoint == null)
        {
            Debug.LogWarning($"[WorkerCarrier] '{name}': Không có deliveryPoint → trả gỗ lại kho.");
            woodStorage?.AddWood(carriedAmount);
            ResetCarry();
            EnterWander();
            return;
        }

        currentState    = State.MoveToDelivery;
        agent.isStopped = false;
        agent.SetDestination(deliveryPoint.position);

        Debug.Log($"[WorkerCarrier] '{name}': Đang giao {carriedAmount} gỗ đến '{deliveryPoint.name}'.");
    }

    void HandleMoveToDelivery()
    {
        if (!HasArrived(deliveryPoint.position)) return;

        // Đã giao xong
        Debug.Log($"[WorkerCarrier] '{name}': Giao {carriedAmount} gỗ thành công! " +
                  $"Kho còn: {(woodStorage != null ? woodStorage.CurrentAmount : 0)}");

        ResetCarry();

        // Nếu kho còn gỗ → lấy tiếp, không thì lang thang
        if (woodStorage != null && !woodStorage.IsEmpty)
        {
            Debug.Log($"[WorkerCarrier] '{name}': Kho còn gỗ → quay lại lấy tiếp.");
            EnterMoveToStorage();
        }
        else
        {
            EnterWander();
        }
    }

    // ===== HELPERS =====

    bool HasArrived(Vector3 destination)
    {
        float dist = Vector3.Distance(transform.position, destination);
        return dist <= arriveDistance;
    }

    void ResetCarry()
    {
        isCarrying    = false;
        carriedAmount = 0;
    }

    // ===== GIZMO DEBUG =====
    void OnDrawGizmosSelected()
    {
        // Vùng lang thang
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // Đường đến storage
        if (storage != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, storage.position);
        }

        // Đường đến deliveryPoint
        if (deliveryPoint != null)
        {
            Gizmos.color = isCarrying ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, deliveryPoint.position);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.2f,
            $"{name} | {currentState} | Gỗ: {carriedAmount}"
        );
#endif
    }
}