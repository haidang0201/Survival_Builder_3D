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

    [Header("Cấu hình Cấp độ")]
    private int level = 1;
    private GameObject launcher;

    // Cấp độ 3 (Zone config)
    private float burnRadius = 4f;
    private float burnDamagePerSec = 10f;
    private float burnDuration = 3f;
    private GameObject burnVfxPrefab;

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

    private bool IsLauncherOrRelated(Collider other)
    {
        if (other == null) return false;
        if (other.gameObject == gameObject) return true;
        if (launcher != null)
        {
            if (other.gameObject == launcher) return true;
            if (other.transform.IsChildOf(launcher.transform)) return true;
            if (launcher.transform.IsChildOf(other.transform)) return true;

            var myHp = launcher.GetComponentInParent<HPTower>();
            var otherHp = other.GetComponentInParent<HPTower>();
            if (myHp != null && otherHp != null && myHp == otherHp) return true;

            var myBuilding = launcher.GetComponentInParent<UpgradeableBuilding>();
            var otherBuilding = other.GetComponentInParent<UpgradeableBuilding>();
            if (myBuilding != null && otherBuilding != null && myBuilding == otherBuilding) return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (IsLauncherOrRelated(other)) return;
        hasHit = true;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        HandleHit(other, hitPoint);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        Collider other = collision.collider;
        if (IsLauncherOrRelated(other)) return;
        hasHit = true;

        Vector3 hitPoint = collision.GetContact(0).point;
        HandleHit(other, hitPoint);
    }

    private void HandleHit(Collider other, Vector3 hitPoint)
    {
        if (IsLauncherOrRelated(other)) return;

        if (level == 1)
        {
            bool isEnemy = other.CompareTag("Enemy") || other.name.ToLower().Contains("enemy") || other.GetComponentInParent<EnemyHealth>() != null;
            if (isEnemy)
            {
                var dmg = other.GetComponentInParent<IDamageable>();
                if (dmg != null)
                {
                    dmg.TakeDamage(damage, hitPoint);
                }
            }
        }
        else
        {
            Explode(hitPoint);

            if (level == 3)
            {
                SpawnDamageZone(hitPoint);
            }
        }

        CleanupAndRelease();
    }

    private void SpawnDamageZone(Vector3 position)
    {
        GameObject zoneObj = new GameObject("CannonFireDamageZone");
        zoneObj.transform.position = position;
        DamageZone zone = zoneObj.AddComponent<DamageZone>();
        zone.Setup(burnDamagePerSec, burnRadius, burnDuration, burnVfxPrefab);
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

        // Find all colliders in radius and apply damage/physics ONLY to enemies
        Collider[] hits = Physics.OverlapSphere(point, explosionRadius);
        foreach (var c in hits)
        {
            if (c == null) continue;
            if (IsLauncherOrRelated(c)) continue;

            bool isEnemy = c.CompareTag("Enemy") || c.name.ToLower().Contains("enemy") || c.GetComponentInParent<EnemyHealth>() != null;
            if (!isEnemy) continue;

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

    public void SetLevel(int lv)
    {
        level = lv;
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void SetZoneConfig(float radius, float dps, float dur, GameObject vfx)
    {
        burnRadius = radius;
        burnDamagePerSec = dps;
        burnDuration = dur;
        burnVfxPrefab = vfx;
    }

    public void SetLauncher(GameObject launcherObj)
    {
        launcher = launcherObj;
    }
}
