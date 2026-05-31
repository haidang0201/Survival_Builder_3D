
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Arrow : MonoBehaviour
{
    [SerializeField] private float damage = 5f;
    [Header("Movement")]
    [SerializeField] private float speed = 20f;
    [Header("Orientation")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(0f, -90f, 0f);
    public enum TipAxis { ForwardZ, BackwardZ, RightX, LeftX, UpY, DownY, None }
    [Tooltip("Which local axis of the model represents the arrow tip (pointing to the arrow head).")]
    public TipAxis tipAxis = TipAxis.LeftX;

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

            // Orient arrow to travel direction with either tip-axis correction or offset
            if (dir.sqrMagnitude > 0.0001f)
            {
                if (tipAxis != TipAxis.None)
                {
                    Vector3 localTip = GetTipAxisVector(tipAxis);
                    Quaternion correction = Quaternion.FromToRotation(localTip, Vector3.forward);
                    transform.rotation = Quaternion.LookRotation(dir.normalized) * correction;
                }
                else
                {
                    transform.rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(rotationOffset);
                }
            }
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
            // Ensure orientation uses either tip-axis correction or offset so prefab forward aligns correctly
            if (tipAxis != TipAxis.None)
            {
                Vector3 localTip = GetTipAxisVector(tipAxis);
                Quaternion correction = Quaternion.FromToRotation(localTip, Vector3.forward);
                transform.rotation = Quaternion.LookRotation(transform.forward) * correction;
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(rotationOffset);
            }
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
            if (dir.sqrMagnitude > 0.0001f)
            {
                if (tipAxis != TipAxis.None)
                {
                    Vector3 localTip = GetTipAxisVector(tipAxis);
                    Quaternion correction = Quaternion.FromToRotation(localTip, Vector3.forward);
                    transform.rotation = Quaternion.LookRotation(dir) * correction;
                }
                else
                {
                    transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(rotationOffset);
                }
            }
        }
        else
        {
            // orient to target with offset
            Vector3 dir = (targetPosition - transform.position).normalized;
            if (dir.sqrMagnitude > 0.0001f)
            {
                if (tipAxis != TipAxis.None)
                {
                    Vector3 localTip = GetTipAxisVector(tipAxis);
                    Quaternion correction = Quaternion.FromToRotation(localTip, Vector3.forward);
                    transform.rotation = Quaternion.LookRotation(dir) * correction;
                }
                else
                {
                    transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(rotationOffset);
                }
            }
        }
        lifeTimer = 0f;
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

        Destroy(gameObject);
    }
    private Vector3 GetTipAxisVector(TipAxis a)
    {
        switch (a)
        {
            case TipAxis.ForwardZ: return Vector3.forward;
            case TipAxis.BackwardZ: return Vector3.back;
            case TipAxis.RightX: return Vector3.right;
            case TipAxis.LeftX: return Vector3.left;
            case TipAxis.UpY: return Vector3.up;
            case TipAxis.DownY: return Vector3.down;
            default: return Vector3.forward;
        }
    }
}
