using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Cần thiết để dùng Coroutine
using System.Collections.Generic;

// 1. Định nghĩa các Trạng thái (State Machine)
public enum UnitState
{
    Idle,      // Đứng yên
    Moving,    // Di chuyển đến vị trí
    Attacking  // Tấn công mục tiêu
}

public class UnitController : MonoBehaviour
{
    private static readonly Dictionary<int, UnitController> claimedEnemies = new Dictionary<int, UnitController>();

    // 2. Khai báo các thành phần cần thiết
    private NavMeshAgent agent;
    public UnitState currentState = UnitState.Idle;
    public GameObject currentTarget;
    public float scanFrequency = 0.25f; // Tần suất quét: 4 lần/giây (Tối ưu hóa: 3 đến 5 lần/giây)
    public float attackRange = 2f;
    [SerializeField] LayerMask targetLayerMask = ~0;
    [SerializeField] float raycastDistance = 6f;
    [SerializeField] float raycastHeight = 0.8f;
    [SerializeField] float visionAngle = 180f;
    [SerializeField] int visionRayCount = 7;
    [SerializeField] float destinationUpdateThreshold = 0.5f;
    [SerializeField] string enemyTag = "Enemy";
    [SerializeField] float enemyReacquireDistance = 8f;

    // Internal caches
    private Coroutine lowFreqCoroutine;
    private Vector3 lastDestinationPos = Vector3.positiveInfinity;

    bool IsCurrentTargetClaimedByOther()
    {
        if (currentTarget == null) return false;

        int id = currentTarget.GetInstanceID();
        if (!claimedEnemies.TryGetValue(id, out UnitController owner)) return false;
        return owner != null && owner != this;
    }

    bool ClaimEnemy(GameObject enemy)
    {
        if (enemy == null) return false;

        int id = enemy.GetInstanceID();
        if (claimedEnemies.TryGetValue(id, out UnitController owner) && owner != null && owner != this)
        {
            return false;
        }

        claimedEnemies[id] = this;
        return true;
    }
        private int currentTargetInstanceId = 0;

    void ReleaseCurrentTargetClaim()
    {
        if (currentTarget == null) return;

        int id = currentTarget.GetInstanceID();
        if (claimedEnemies.TryGetValue(id, out UnitController owner) && owner == this)
        {
            claimedEnemies.Remove(id);
        }
    }

    void Start()
    {
        // Lấy thành phần NavMeshAgent (hệ thống tìm đường)
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("UnitController requires a NavMeshAgent component.");
            enabled = false;
            return;
        }

        // Bắt đầu Coroutine để xử lý các logic tối ưu (chạy không liên tục)
        lowFreqCoroutine = StartCoroutine(LowFrequencyUpdate());

        // Nếu đã có target được gán từ Inspector và state là Attacking, khởi tạo đường đi ngay
        if (currentState == UnitState.Attacking && currentTarget != null)
        {
            agent.isStopped = false;
            Vector3 tpos = currentTarget.transform.position;
            agent.SetDestination(tpos);
            lastDestinationPos = tpos;
        }
        
        // Ghi chú: Để tối ưu tránh va chạm vật lý, bạn cần CẤU HÌNH NavMeshAgent
        // trong Inspector để sử dụng 'Avoidance' thay vì 'Physics Collision'.
    }

    // Coroutine: Hàm chạy ngắt quãng (Không chạy liên tục trong Update())
    IEnumerator LowFrequencyUpdate()
    {
        // Vòng lặp tối ưu
        while (true)
        {
            // Chờ một khoảng thời gian (scanFrequency) trước khi chạy logic tiếp theo
            yield return new WaitForSeconds(scanFrequency);
            
            // Chỉ cập nhật đuổi theo khi đang có target enemy
            if (currentState == UnitState.Attacking && currentTarget != null)
            {
                if (IsCurrentTargetClaimedByOther())
                {
                    currentTarget = null;
                    currentState = UnitState.Idle;
                    agent.isStopped = true;
                    continue;
                }

                Vector3 targetPos = currentTarget.transform.position;
                if ((transform.position - targetPos).sqrMagnitude > attackRange * attackRange)
                {
                    if ((targetPos - lastDestinationPos).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(targetPos);
                        lastDestinationPos = targetPos;
                    }
                }
            }
        }
    }

    void Update()
    {
        // Raycast 180 độ phía trước: thấy Enemy thì chuyển sang đuổi ngay
        if (currentTarget == null || currentTarget.CompareTag(enemyTag) == false)
        {
            if (TryAcquireEnemyByVision())
            {
                return;
            }
        }

        // Logic chính chạy liên tục (nhưng nhẹ hơn)
        switch (currentState)
        {
            case UnitState.Attacking:
                HandleAttacking();
                break;
            case UnitState.Moving:
                HandleMovement();
                break;
            default: // Idle
                // Không làm gì nhiều khi Idle
                break;
        }
    }
    
    // Xử lý lệnh của người chơi (Ưu tiên 1)
    public void SetNewTarget(GameObject target)
    {
        if (target == null) return;

        if (!target.CompareTag(enemyTag)) return;

        ReleaseCurrentTargetClaim();

        if (!ClaimEnemy(target)) return;

        // Ưu tiên 1: Unit chỉ nhận enemy làm mục tiêu
        currentTarget = target;
        currentState = UnitState.Attacking;

        // Đảm bảo agent đang cho phép di chuyển
        agent.isStopped = false;

        // Tính toán đường đi NGAY LẬP TỨC khi nhận lệnh (không chờ Coroutine)
        Vector3 tpos = target.transform.position;
        agent.SetDestination(tpos);
        lastDestinationPos = tpos;
    }

    // Logic xử lý khi đang tấn công
    void HandleAttacking()
    {
        if (currentTarget == null)
        {
            ReleaseCurrentTargetClaim();
            currentState = UnitState.Idle;
            return;
        }

        if (IsCurrentTargetClaimedByOther())
        {
            currentTarget = null;
            currentState = UnitState.Idle;
            agent.isStopped = true;
            return;
        }
        
        float sqrDistance = (transform.position - currentTarget.transform.position).sqrMagnitude;

        if (sqrDistance <= attackRange * attackRange)
        {
            // Đã tới tầm: Dừng di chuyển và Tấn công
            agent.isStopped = true;
            // Gọi hàm tấn công (ví dụ: FireWeapon())
        }
        else
        {
            // Chưa tới tầm: Tiếp tục chạy và TÍNH TOÁN ĐƯỜNG ĐI đã được tối ưu trong Coroutine
            agent.isStopped = false;
        }
    }

    // Logic xử lý khi đang di chuyển (nếu lính đang đi đến một vị trí cụ thể)
    void HandleMovement()
    {
        if (!agent.pathPending)
        {
            if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
            {
                currentState = UnitState.Idle;
            }
        }
    }

    // Hàm quét mục tiêu đơn giản (chạy trong Coroutine với tần suất thấp)
    void ScanForTarget()
    {
        // Không dùng vòng tròn cảm ứng nữa; giữ hàm này để tương thích nhưng không làm gì.
    }

    bool TryAcquireEnemyByVision()
    {
        if (visionRayCount < 1) visionRayCount = 1;

        Vector3 origin = transform.position + Vector3.up * raycastHeight;
        float halfAngle = visionAngle * 0.5f;
        float step = visionRayCount == 1 ? 0f : visionAngle / (visionRayCount - 1);

        for (int i = 0; i < visionRayCount; i++)
        {
            float angleOffset = -halfAngle + step * i;
            Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * transform.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, raycastDistance, targetLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.CompareTag(enemyTag))
                {
                    if (!ClaimEnemy(hit.collider.gameObject))
                    {
                        continue;
                    }

                    ReleaseCurrentTargetClaim();
                    currentTarget = hit.collider.gameObject;
                    currentState = UnitState.Attacking;
                    agent.isStopped = false;
                    Vector3 targetPos = currentTarget.transform.position;
                    agent.SetDestination(targetPos);
                    lastDestinationPos = targetPos;
                    return true;
                }
            }
        }

        return false;
    }

    void OnDisable()
    {
        ReleaseCurrentTargetClaim();

        if (lowFreqCoroutine != null)
        {
            StopCoroutine(lowFreqCoroutine);
            lowFreqCoroutine = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = transform.position + Vector3.up * raycastHeight;
        float halfAngle = visionAngle * 0.5f;
        float step = visionRayCount <= 1 ? 0f : visionAngle / (visionRayCount - 1);

        for (int i = 0; i < Mathf.Max(1, visionRayCount); i++)
        {
            float angleOffset = -halfAngle + step * i;
            Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * transform.forward;
            Gizmos.DrawRay(origin, dir * raycastDistance);
        }
    }
}
