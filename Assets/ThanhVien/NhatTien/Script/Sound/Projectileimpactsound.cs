using UnityEngine;

/*
 * ProjectileImpactSound.cs  (v2)
 * Folder: Scripts/Building/Audio/
 *
 * Gắn vào prefab ĐẠN PHÁO (CanonBall).
 * Phát tiếng CHỈ KHI chạm Enemy, bỏ qua ground / tường / collider khác.
 *
 * SETUP:
 *   1. Add Component vào prefab CanonBall
 *   2. Kéo AudioClip vào "Impact Clip"
 *   3. Điền tag enemy vào "Enemy Tag" (mặc định "Enemy")
 *      HOẶC gán "Enemy Layer" nếu dùng Layer thay Tag
 */

public class ProjectileImpactSound : MonoBehaviour
{
    [Header("💥 Âm thanh chạm Enemy")]
    public AudioClip impactClip;

    [Range(0f, 1f)]
    public float impactVolume = 1f;

    [Header("🎯 Lọc mục tiêu – chỉ phát khi chạm Enemy")]
    [Tooltip("Tag của enemy. Mặc định: 'Enemy'")]
    public string enemyTag = "Enemy";

    [Tooltip("Layer của enemy (ưu tiên hơn Tag nếu được gán)")]
    public LayerMask enemyLayer;

    // ── Object Pool safe ──
    private bool hasHit = false;

    private void OnEnable()  => hasHit = false;
    private void OnDisable() => hasHit = false;

    // Canon dùng Rigidbody + gravity → OnCollisionEnter
    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        if (!IsEnemy(collision.gameObject)) return;

        hasHit = true;

        if (impactClip != null)
        {
            // PlayClipAtPoint: âm thanh tồn tại dù đạn bị destroy/pool ngay sau đó
            Vector3 hitPoint = collision.contacts.Length > 0
                ? collision.contacts[0].point
                : transform.position;

            AudioSource.PlayClipAtPoint(impactClip, hitPoint, impactVolume);
            Debug.Log($"[ProjectileImpactSound] 💥 Chạm enemy '{collision.gameObject.name}' → phát '{impactClip.name}'");
        }
    }

    // Arrow dùng Trigger → OnTriggerEnter (để dùng chung script cho cả 2 nếu cần)
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (other.isTrigger) return;
        if (!IsEnemy(other.gameObject)) return;

        hasHit = true;

        if (impactClip != null)
        {
            AudioSource.PlayClipAtPoint(impactClip, transform.position, impactVolume);
            Debug.Log($"[ProjectileImpactSound] 💥 Trigger enemy '{other.gameObject.name}' → phát '{impactClip.name}'");
        }
    }

    // ── Kiểm tra có phải Enemy không ──
    private bool IsEnemy(GameObject obj)
    {
        // Ưu tiên Layer
        if (enemyLayer.value != 0)
        {
            bool match = ((enemyLayer.value >> obj.layer) & 1) == 1;
            if (match) return true;
        }

        // Fallback: Tag
        try { if (obj.CompareTag(enemyTag)) return true; } catch { }

        // Fallback cuối: có EnemyHealth component không
        if (obj.GetComponentInParent<EnemyHealth>() != null) return true;

        return false;
    }
}