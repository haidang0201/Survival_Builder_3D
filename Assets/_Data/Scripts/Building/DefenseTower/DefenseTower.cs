// using UnityEngine;
// using System.Collections;

// /*
//  * TowerCombatCtrl.cs
//  * Folder: Scripts/Building/
//  * Người làm: DŨNG / VŨ
//  *
//  * Component chiến đấu DÙNG CHUNG cho toàn bộ tháp phòng thủ:
//  *   WatchTower  → Tháp Canh   : tầm ngắn, target đơn, không có đạn (direct hit)
//  *   ArcherTower → Tháp Cung   : tầm xa,   target đơn, có đạn bay
//  *   Cannon      → Pháo        : tầm trung, AoE splash, chậm, damage cao
//  *   DefenseTower→ Tháp Tự Động: tầm trung, target đơn, cân bằng ngày/đêm
//  *
//  * Cấu hình trong Inspector – KHÔNG cần code riêng cho từng tháp.
//  *
//  * Cơ chế:
//  *   1. ScanRoutine() – OverlapSphereNonAlloc định kỳ, chọn target gần nhất
//  *   2. Update()      – xoay towerHead, chờ căn chỉnh xong rồi Fire()
//  *   3. Fire()        – tuỳ AttackMode: bắn đạn (Single/AoE) hoặc sát thương thẳng
//  *   4. Ngày / Đêm   – fireRate + damage lấy từ preset tương ứng
//  *
//  * Tích hợp:
//  *   - Gắn cùng prefab với BuildingCtrl (RequireComponent)
//  *   - Chỉ hoạt động khi BuildingCtrl.IsBuilt == true
//  *   - Đọc DayNightManager.Ins.IsNight để chọn preset
//  *
//  * Setup Unity cho từng tháp:
//  *   WatchTower  : attackMode = DirectHit,  detectionRadius = 6,  fireRateDay = 1.5, fireRateNight = 2
//  *   ArcherTower : attackMode = SingleShot, detectionRadius = 14, fireRateDay = 1,   fireRateNight = 1.5
//  *   Cannon      : attackMode = AoeSplash,  detectionRadius = 10, fireRateDay = 0.4, fireRateNight = 0.6
//  *   DefenseTower: attackMode = SingleShot, detectionRadius = 10, fireRateDay = 1.2, fireRateNight = 2
//  */

// [RequireComponent(typeof(BuildingCtrl))]
// public class TowerCombatCtrl : MonoBehaviour
// {
//     // ──────────────────────────────────────────────
//     // ENUM
//     // ──────────────────────────────────────────────

//     public enum AttackMode
//     {
//         DirectHit,   // Sát thương thẳng, không có đạn – WatchTower
//         SingleShot,  // Spawn 1 đạn bay về target    – ArcherTower, DefenseTower
//         AoeSplash,   // Spawn đạn + nổ diện tích      – Cannon
//     }

//     // ──────────────────────────────────────────────
//     // INSPECTOR
//     // ──────────────────────────────────────────────

//     [Header("Loại tháp – chọn cho đúng prefab")]
//     public AttackMode attackMode = AttackMode.SingleShot;

//     [Header("Detection")]
//     [Tooltip("Bán kính OverlapSphere phát hiện kẻ địch")]
//     public float detectionRadius = 10f;
//     [Tooltip("Tần suất quét (giây/lần) – nhỏ hơn = phản ứng nhanh hơn, tốn CPU hơn")]
//     public float scanInterval = 0.25f;
//     public LayerMask enemyLayer;

//     [Header("Ban ngày ☀️")]
//     public float fireRateDay = 1f;    // Viên/giây
//     public float damageDay = 15f;

//     [Header("Ban đêm 🌙 (thường cao hơn vì quái nhiều hơn)")]
//     public float fireRateNight = 2f;
//     public float damageNight = 20f;

//     [Header("AoE – chỉ dùng khi attackMode = AoeSplash")]
//     [Tooltip("Bán kính nổ tính từ điểm chạm")]
//     public float splashRadius = 3f;
//     [Tooltip("Phần trăm sát thương cho quái ngoài rìa AoE (0–1)")]
//     [Range(0f, 1f)]
//     public float splashDamageFalloff = 0.5f;

//     [Header("References")]
//     [Tooltip("Điểm xuất phát đạn (đầu nòng)")]
//     public Transform firePoint;
//     [Tooltip("Phần xoay theo mục tiêu – null = tháp không xoay")]
//     public Transform towerHead;
//     [Tooltip("Prefab đạn (TowerProjectile). Không cần cho DirectHit)")]
//     public GameObject projectilePrefab;

//     [Header("Rotation")]
//     [Tooltip("Tốc độ xoay towerHead (độ/giây)")]
//     public float rotateSpeed = 150f;
//     [Tooltip("Góc lệch tối đa cho phép bắn (độ). Nhỏ = chính xác hơn nhưng mất thời gian xoay)")]
//     [Range(1f, 30f)]
//     public float aimTolerance = 8f;

//     // ──────────────────────────────────────────────
//     // PRIVATE
//     // ──────────────────────────────────────────────

//     private BuildingCtrl buildingCtrl;
//     private Transform currentTarget;
//     private float fireCooldown;

//     // NonAlloc buffer – tránh GC alloc mỗi lần quét
//     private readonly Collider[] scanBuffer = new Collider[32];

//     // ──────────────────────────────────────────────
//     // LIFECYCLE
//     // ──────────────────────────────────────────────

//     private void Awake()
//     {
//         buildingCtrl = GetComponent<BuildingCtrl>();
//     }

//     private void Start()
//     {
//         StartCoroutine(ScanRoutine());
//     }

//     private void Update()
//     {
//         if (!buildingCtrl.IsBuilt) return;

//         fireCooldown -= Time.deltaTime;

//         // Mất target → reset
//         if (currentTarget == null || !IsInRange(currentTarget))
//         {
//             currentTarget = null;
//             return;
//         }

//         RotateToward(currentTarget);

//         if (fireCooldown <= 0f && IsAimed())
//         {
//             Fire();
//             fireCooldown = 1f / CurrentFireRate;
//         }
//     }

//     // ──────────────────────────────────────────────
//     // SCAN  –  OverlapSphere
//     // ──────────────────────────────────────────────

//     private IEnumerator ScanRoutine()
//     {
//         var wait = new WaitForSeconds(scanInterval);

//         while (true)
//         {
//             yield return wait;

//             if (!buildingCtrl.IsBuilt) continue;

//             // Giữ target cũ nếu vẫn hợp lệ → tránh giật target liên tục
//             if (currentTarget != null && IsInRange(currentTarget)) continue;

//             currentTarget = FindNearestEnemy();
//         }
//     }

//     /// <summary>
//     /// Physics.OverlapSphereNonAlloc – tìm kẻ địch gần nhất.
//     /// NonAlloc = không cấp phát heap, an toàn cho hot path.
//     /// </summary>
//     private Transform FindNearestEnemy()
//     {
//         int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, scanBuffer, enemyLayer);
//         float minDist = float.MaxValue;
//         Transform best = null;

//         for (int i = 0; i < count; i++)
//         {
//             if (scanBuffer[i] == null) continue;

//             float dist = Vector3.Distance(transform.position, scanBuffer[i].transform.position);
//             if (dist < minDist)
//             {
//                 minDist = dist;
//                 best = scanBuffer[i].transform;
//             }
//         }

//         return best;
//     }

//     private bool IsInRange(Transform t)
//         => t != null && Vector3.Distance(transform.position, t.position) <= detectionRadius;

//     // ──────────────────────────────────────────────
//     // ROTATION
//     // ──────────────────────────────────────────────

//     private void RotateToward(Transform target)
//     {
//         if (towerHead == null || target == null) return;

//         Vector3 dir = target.position - towerHead.position;
//         dir.y = 0f;
//         if (dir.sqrMagnitude < 0.001f) return;

//         Quaternion goal = Quaternion.LookRotation(dir.normalized);
//         towerHead.rotation = Quaternion.RotateTowards(towerHead.rotation, goal, rotateSpeed * Time.deltaTime);
//     }

//     /// <summary>Tháp đã căn chỉnh đủ gần về phía target để bắn không?</summary>
//     private bool IsAimed()
//     {
//         if (towerHead == null || currentTarget == null) return true; // Không cần xoay

//         Vector3 dir = currentTarget.position - towerHead.position;
//         dir.y = 0f;
//         if (dir.sqrMagnitude < 0.001f) return true;

//         return Vector3.Angle(towerHead.forward, dir.normalized) <= aimTolerance;
//     }

//     // ──────────────────────────────────────────────
//     // ATTACK
//     // ──────────────────────────────────────────────

//     private void Fire()
//     {
//         switch (attackMode)
//         {
//             case AttackMode.DirectHit:
//                 ApplyDirectHit();
//                 break;

//             case AttackMode.SingleShot:
//                 SpawnProjectile(aoe: false);
//                 break;

//             case AttackMode.AoeSplash:
//                 SpawnProjectile(aoe: true);
//                 break;
//         }

//         Debug.Log($"[TowerCombat] {buildingCtrl.buildingType} ► {currentTarget?.name} | {CurrentDamage} dmg | {PhaseLabel}");
//     }

//     /// <summary>WatchTower – không có đạn, sát thương thẳng trong frame.</summary>
//     private void ApplyDirectHit()
//     {
//         if (currentTarget == null) return;

//         var hp = currentTarget.GetComponent<EnemyHealth>();
//         hp?.TakeDamage(CurrentDamage);
//     }

//     /// <summary>ArcherTower / Cannon / DefenseTower – spawn projectile.</summary>
//     private void SpawnProjectile(bool aoe)
//     {
//         if (projectilePrefab == null)
//         {
//             // Fallback nếu chưa gán prefab đạn
//             ApplyDirectHit();
//             return;
//         }

//         Transform origin = firePoint != null ? firePoint : transform;

//         GameObject obj = Instantiate(projectilePrefab, origin.position, origin.rotation);
//         var proj = obj.GetComponent<TowerProjectile>();

//         if (proj != null)
//         {
//             proj.Init(
//                 target: currentTarget,
//                 damage: CurrentDamage,
//                 isAoe: aoe,
//                 splashRadius: splashRadius,
//                 damageFalloff: splashDamageFalloff,
//                 enemyLayer: enemyLayer
//             );
//         }
//     }

//     // ──────────────────────────────────────────────
//     // DAY / NIGHT
//     // ──────────────────────────────────────────────

//     private bool IsNight
//         => DayNightManager.Ins != null && DayNightManager.Ins.IsNight;

//     private float CurrentFireRate => IsNight ? fireRateNight : fireRateDay;
//     private float CurrentDamage => IsNight ? damageNight : damageDay;
//     private string PhaseLabel => IsNight ? "🌙 Đêm" : "☀️ Ngày";

//     // ──────────────────────────────────────────────
//     // GIZMOS
//     // ──────────────────────────────────────────────

//     private void OnDrawGizmosSelected()
//     {
//         // Vòng tròn phát hiện
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, detectionRadius);

//         // AoE splash radius (chỉ hiện khi Cannon)
//         if (attackMode == AttackMode.AoeSplash)
//         {
//             Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
//             Gizmos.DrawWireSphere(transform.position, splashRadius);
//         }

//         // Đường ngắm đến target hiện tại
//         if (currentTarget != null)
//         {
//             Gizmos.color = Color.red;
//             Vector3 from = firePoint != null ? firePoint.position : transform.position;
//             Gizmos.DrawLine(from, currentTarget.position);
//             Gizmos.DrawSphere(currentTarget.position, 0.3f);
//         }
//     }
// }