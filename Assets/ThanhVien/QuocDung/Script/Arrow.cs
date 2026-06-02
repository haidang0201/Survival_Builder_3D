
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Arrow : MonoBehaviour
{
    [SerializeField] private float damage = 5f;
    [Header("Movement")]
    [SerializeField] private float speed = 20f;
    // Orientation removed: Arrow will not change rotation at runtime

    private Rigidbody rb;
    private Transform target;
    private Vector3 targetPosition;
    private float lifeTime = 6f;
    private float lifeTimer;
    [Header("Collision")]
    [SerializeField] private float hitRadius = 0.1f;

    void Start()
    {
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            if (ArrowPool.Instance != null)
                ArrowPool.Instance.Release(gameObject);
            else
                Destroy(gameObject);
            return;
        }

        Vector3 currentPos = transform.position;

        if (target != null)
        {
            // Move toward the recorded target position (so arrow isn't jittery if target moves)
            float step = speed * Time.deltaTime;
            Vector3 dir = (targetPosition - transform.position);
            Vector3 move;
            if (dir.sqrMagnitude <= step * step)
            {
                move = targetPosition - transform.position;
            }
            else
            {
                move = dir.normalized * step;
            }

            // Raycast/SphereCast along movement to reliably detect hits even when moving by transform
            if (move.sqrMagnitude > 0f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(currentPos, hitRadius, move.normalized, move.magnitude);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    if (h.collider.gameObject == gameObject) continue;
                    // ignore trigger colliders that are not meant for collisions
                    // process first valid hit
                    HandleHit(h.collider, h.point);
                    return; // arrow destroyed in HandleHit
                }
            }

            transform.position = transform.position + move;

            // No rotation change: keep prefab orientation while flying toward target
        }
        else
        {
            // Nếu prefab không có Rigidbody hoặc Rigidbody được set kinematic,
            // tự di chuyển mũi tên bằng transform (dự phòng khi tháp không áp vận tốc)
            Vector3 move = transform.forward * speed * Time.deltaTime;
            if (rb == null || rb.isKinematic)
            {
                // check collisions along forward
                RaycastHit[] hits = Physics.SphereCastAll(currentPos, hitRadius, move.normalized, move.magnitude);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    if (h.collider.gameObject == gameObject) continue;
                    HandleHit(h.collider, h.point);
                    return;
                }

                transform.position += move;
            }
            // No rotation change when moving by transform
        }
    }

    public void SetTarget(Transform t, float moveSpeed)
    {
        if (t == null) return;
        target = t;
        targetPosition = t.position;
        speed = moveSpeed;

        // If Rigidbody present and non-kinematic, apply velocity once
        if (rb != null && !rb.isKinematic)
        {
            Vector3 dir = (targetPosition - transform.position).normalized;
            rb.linearVelocity = dir * speed;
        }
        else
        {
            // No rotation change on SetTarget
        }
        lifeTimer = 0f;
    }

    // Adjust local Z rotation based on vertical difference and horizontal distance
    public void AdjustZByHeightAndDistance(Vector3 fromPosition, Vector3 toPosition, float multiplier = 1f)
    {
        float dy = toPosition.y - fromPosition.y;
        Vector3 a = new Vector3(fromPosition.x, 0f, fromPosition.z);
        Vector3 b = new Vector3(toPosition.x, 0f, toPosition.z);
        float horiz = Vector3.Distance(a, b);
        if (horiz <= 0.0001f)
            return;

        // angle in degrees: positive means target is higher -> tilt accordingly
        float angle = Mathf.Atan2(dy, horiz) * Mathf.Rad2Deg * multiplier;

        Vector3 e = transform.localEulerAngles;
        e.z = angle;
        transform.localEulerAngles = e;
    }

    // Rotate local Y so arrow faces the horizontal direction to the target
    public void AdjustYToFaceTarget(Vector3 fromPosition, Vector3 toPosition, float yawOffset = 0f)
    {
        Vector3 dir = toPosition - fromPosition;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f) return;

        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; // degrees
        Vector3 e = transform.localEulerAngles;
        e.y = yaw + yawOffset;
        transform.localEulerAngles = e;
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        HandleHit(other, hitPoint);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;
        Vector3 hitPoint = collision.GetContact(0).point;
        HandleHit(other, hitPoint);
    }

    private void HandleHit(Collider other, Vector3 hitPoint)
    {
        // Prefer applying damage to IDamageable components if present
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log($"[Arrow] Dealing {damage} damage to {other.name}");
            damageable.TakeDamage(damage, hitPoint);
        }
        else
        {
            Debug.Log($"[Arrow] Hit collider '{other.name}' (no IDamageable). Destroying arrow.");
        }

        if (ArrowPool.Instance != null)
            ArrowPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
    // TipAxis helper removed since Arrow no longer rotates at runtime
}
