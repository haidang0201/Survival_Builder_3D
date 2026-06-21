using UnityEngine;

/*
 * TowerSoundPlayer.cs  (v3 – Đã fix lỗi không nhận Trigger & trễ Physics)
 * Folder: Scripts/Building/Audio/
 */

[RequireComponent(typeof(AttackTowerAI))]
[RequireComponent(typeof(AudioSource))]
public class TowerSoundPlayer : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════

    [Header("🏹 Âm thanh Cung (Archer) – 3 slot = 3 cấp độ")]
    public AudioClip[] archerFireClips = new AudioClip[3];

    [Header("💣 Âm thanh Pháo (Cannon) – 3 slot = 3 cấp độ")]
    public AudioClip[] cannonFireClips = new AudioClip[3];

    [Header("⚙️ Cấu hình Âm thanh")]
    [Range(0f, 1f)]
    public float fireVolume = 0.9f;

    [Range(1f, 50f)]
    public float maxAudioDistance = 30f;

    [Header("🔍 Nhận biết Đạn (để detect lúc bắn)")]
    [Tooltip("Tag gán cho prefab Arrow và CanonBall. Ví dụ: 'Projectile'")]
    public string projectileTag = "Projectile";

    [Tooltip("Layer của prefab Arrow và CanonBall (nếu có). Để None = dùng Tag.")]
    public LayerMask projectileLayer;

    [Range(0.3f, 5f)]
    [Tooltip("Bán kính quét quanh muzzle. Tăng lên một chút nếu đạn bay quá nhanh")]
    public float muzzleCheckRadius = 2.5f;

    [Header("⏱️ Cooldown tối thiểu (giây)")]
    [Range(0.05f, 5f)]
    public float minSoundInterval = 0.3f;

    // ═══════════════════════════════════════════════════
    //  PRIVATE
    // ═══════════════════════════════════════════════════

    private AttackTowerAI       attackAI;
    private UpgradeableBuilding upgradeBuilding;
    private AudioSource         audioSource;

    private int   lastProjectileCount = 0;
    private float lastSoundTime       = -99f;
    private Transform cachedFirePoint;

    // ═══════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════

    private void Start()
    {
        attackAI        = GetComponent<AttackTowerAI>();
        upgradeBuilding = GetComponent<UpgradeableBuilding>();
        audioSource     = GetComponent<AudioSource>();

        audioSource.playOnAwake  = false;
        // Chỉnh xuống 0.5f (Semi-3D) để nghe tiếng bắn rõ hơn khi Camera ở trên cao
        audioSource.spatialBlend = 0.5f; 
        audioSource.rolloffMode  = AudioRolloffMode.Logarithmic;
        audioSource.minDistance  = 3f;
        audioSource.maxDistance  = maxAudioDistance;
        audioSource.dopplerLevel = 0f;

        ValidateClipArrays();
    }

    private void Update()
    {
        if (attackAI == null) return;

        RefreshFirePoint();
        if (cachedFirePoint == null) return;

        int currentCount = CountProjectilesNearMuzzle();

        if (currentCount > lastProjectileCount)
        {
            float timeSinceLast = Time.time - lastSoundTime;
            if (timeSinceLast >= minSoundInterval)
            {
                PlayFireSound();
                lastSoundTime = Time.time;
            }
        }

        lastProjectileCount = currentCount;
    }

    // ═══════════════════════════════════════════════════
    //  ĐẾM ĐẠN QUANH MUZZLE (ĐÃ ĐƯỢC FIX)
    // ═══════════════════════════════════════════════════

    private int CountProjectilesNearMuzzle()
    {
        Vector3 checkPos = cachedFirePoint.position;
        int count = 0;

        // BẮT BUỘC: Ép Unity đồng bộ vật lý ngay trong frame này để thấy viên đạn vừa Instantiate
        Physics.SyncTransforms();

        // Ưu tiên Layer
        if (projectileLayer.value != 0)
        {
            // BẮT BUỘC: Dùng QueryTriggerInteraction.Collide để bắt được các đạn dùng Trigger
            Collider[] hits = Physics.OverlapSphere(checkPos, muzzleCheckRadius, projectileLayer, QueryTriggerInteraction.Collide);
            return hits.Length;
        }

        // Fallback: Tag
        if (!string.IsNullOrEmpty(projectileTag))
        {
            // BẮT BUỘC: Dùng ~0 (tìm trên mọi Layer) và QueryTriggerInteraction.Collide
            Collider[] allHits = Physics.OverlapSphere(checkPos, muzzleCheckRadius, ~0, QueryTriggerInteraction.Collide);
            foreach (var col in allHits)
            {
                if (col == null) continue;
                try { if (col.CompareTag(projectileTag)) count++; } catch { }
            }
        }

        return count;
    }

    // ═══════════════════════════════════════════════════
    //  PHÁT TIẾNG BẮN
    // ═══════════════════════════════════════════════════

    private void PlayFireSound()
    {
        int level = upgradeBuilding != null ? upgradeBuilding.CurrentLevel : 0;

        AudioClip[] clips = (attackAI.towerType == AttackTowerType.Archer)
            ? archerFireClips
            : cannonFireClips;

        AudioClip clip = GetClipForLevel(clips, level);
        if (clip == null) return;

        audioSource.PlayOneShot(clip, fireVolume);
    }

    private AudioClip GetClipForLevel(AudioClip[] clips, int level)
    {
        if (clips == null || clips.Length == 0) return null;

        int idx = Mathf.Clamp(level, 0, clips.Length - 1);
        if (clips[idx] != null) return clips[idx];

        for (int i = idx - 1; i >= 0; i--)
            if (clips[i] != null) return clips[i];

        for (int i = idx + 1; i < clips.Length; i++)
            if (clips[i] != null) return clips[i];

        return null;
    }

    // ═══════════════════════════════════════════════════
    //  CACHE FIREPOINT
    // ═══════════════════════════════════════════════════

    private void RefreshFirePoint()
    {
        if (attackAI.firePoint != null && attackAI.firePoint != cachedFirePoint)
        {
            cachedFirePoint = attackAI.firePoint;
            lastProjectileCount = CountProjectilesNearMuzzle();
        }
    }

    // ═══════════════════════════════════════════════════
    //  TIỆN ÍCH & GIZMO
    // ═══════════════════════════════════════════════════

    private void ValidateClipArrays()
    {
        if (archerFireClips == null || archerFireClips.Length < 3)
        {
            AudioClip[] newArr = new AudioClip[3];
            if (archerFireClips != null) System.Array.Copy(archerFireClips, newArr, Mathf.Min(archerFireClips.Length, 3));
            archerFireClips = newArr;
        }
        if (cannonFireClips == null || cannonFireClips.Length < 3)
        {
            AudioClip[] newArr = new AudioClip[3];
            if (cannonFireClips != null) System.Array.Copy(cannonFireClips, newArr, Mathf.Min(cannonFireClips.Length, 3));
            cannonFireClips = newArr;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackAI == null) attackAI = GetComponent<AttackTowerAI>();
        if (attackAI == null || attackAI.firePoint == null) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(attackAI.firePoint.position, muzzleCheckRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackAI.firePoint.position, muzzleCheckRadius);
    }
}