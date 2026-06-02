using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Canon : MonoBehaviour
{
    [SerializeField] private float damage = 20f;
    [Header("Explosion")]
    [Tooltip("Radius of the AoE explosion (meters)")]
    [SerializeField] private float explosionRadius = 3f;
    [Tooltip("Force applied to rigidbodies in explosion")]
    [SerializeField] private float explosionForce = 300f;
    [Tooltip("Optional VFX prefab spawned at explosion point")]
    [SerializeField] private GameObject explosionVfx;
    private bool hasHit = false;

    private void OnEnable()
    {
        hasHit = false;
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        hasHit = true;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Explode(hitPoint);
        CleanupAndRelease();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        Vector3 hitPoint = collision.GetContact(0).point;
        Explode(hitPoint);
        CleanupAndRelease();
    }

    private void Explode(Vector3 point)
    {
        // Spawn VFX if provided
        if (explosionVfx != null)
        {
            GameObject vfx = ArrowPool.Instance != null
                ? ArrowPool.Instance.Spawn(explosionVfx, point, Quaternion.identity)
                : Instantiate(explosionVfx, point, Quaternion.identity);

            if (vfx != null)
            {
                float releaseDelay = GetExplosionVfxDuration(vfx);
                var autoRelease = vfx.GetComponent<AutoReleaseToPool>();
                if (autoRelease == null)
                    autoRelease = vfx.AddComponent<AutoReleaseToPool>();
                autoRelease.PlayAndRelease(releaseDelay);
            }
        }

        // Find all colliders in radius and apply damage/physics
        Collider[] hits = Physics.OverlapSphere(point, explosionRadius);
        foreach (var c in hits)
        {
            if (c == null) continue;
            var dmg = c.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                float dist = Vector3.Distance(point, c.ClosestPoint(point));
                float t = Mathf.Clamp01(1f - (dist / explosionRadius));
                float applied = damage * t; // falloff
                Vector3 hitPt = c.ClosestPoint(point);
                dmg.TakeDamage(applied, hitPt);
            }

            var rb = c.attachedRigidbody;
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, point, explosionRadius);
            }
        }

        Debug.Log($"[Canon] Explosion at {point} applied within radius {explosionRadius}");
    }

    private void CleanupAndRelease()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (ArrowPool.Instance != null && GetComponent<PooledItem>() != null)
            ArrowPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    private float GetExplosionVfxDuration(GameObject vfx)
    {
        float duration = 1f;
        var particles = vfx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
            float total = main.duration + main.startLifetime.constantMax;
            if (total > duration) duration = total;
        }

        return Mathf.Max(0.1f, duration);
    }
}
