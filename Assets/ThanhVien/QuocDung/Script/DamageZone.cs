using UnityEngine;

public class DamageZone : MonoBehaviour
{
    private float damagePerSecond;
    private float radius;
    private float duration;
    private GameObject vfxPrefab;
    private float timer;
    private float nextDamageTime;
    private GameObject spawnedVfx;

    public void Setup(float damagePerSecond, float radius, float duration, GameObject vfxPrefab)
    {
        this.damagePerSecond = damagePerSecond;
        this.radius = radius;
        this.duration = duration;
        this.vfxPrefab = vfxPrefab;

        if (vfxPrefab != null)
        {
            spawnedVfx = Instantiate(vfxPrefab, transform.position, Quaternion.identity, transform);
        }

        timer = 0f;
        nextDamageTime = 0f;
        
        Debug.Log($"[DamageZone] Created at {transform.position} with radius {radius}, dps {damagePerSecond}, duration {duration}");
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= nextDamageTime)
        {
            ApplyAreaDamage();
            nextDamageTime = Time.time + 0.5f; // Damage ticks every 0.5 seconds
        }
    }

    private void ApplyAreaDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var c in hits)
        {
            if (c == null) continue;
            
            // Only damage enemies
            if (c.CompareTag("Enemy") || c.name.ToLower().Contains("enemy") || c.GetComponentInParent<EnemyHealth>() != null)
            {
                var dmg = c.GetComponentInParent<IDamageable>();
                if (dmg != null && !(dmg is Shield)) // Avoid damaging friendly shields
                {
                    float damageThisTick = damagePerSecond * 0.5f;
                    dmg.TakeDamage(damageThisTick, c.ClosestPoint(transform.position));
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (spawnedVfx != null)
        {
            Destroy(spawnedVfx);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
