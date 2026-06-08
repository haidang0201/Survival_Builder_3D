using UnityEngine;

public enum AttackTowerType { Archer, Cannon }

public class AttackTowerAI : MonoBehaviour
{
    [Header("Cấu hình Loại Tháp")]
    public AttackTowerType towerType;
    public float fireRate = 1f;          // Tốc độ bắn (số phát / giây)
    public Transform firePoint;          // Kéo Object trống ở đầu nòng/họng pháo vào đây
    public GameObject projectilePrefab;  // Prefab Mũi tên (Arrow) hoặc Quả bom (Bomb)
        [Header("Projectile")]
        public float projectileSpeed = 20f; // speed applied if projectile has Rigidbody
        [Tooltip("Yaw offset (degrees) to apply so projectile model faces correctly. Common: 270")]
        public float projectileYawOffset = 0f;
        [Tooltip("Vertical spawn height above target for AoE bombs (meters). Lower to reduce high arc.")]
        public float bombSpawnHeight = 6f;
        [Tooltip("Distance forward from the firePoint to spawn the projectile to avoid overlapping the muzzle.")]
        public float muzzleOffset = 0.5f;

    [Header("Cấu hình Nâng cấp (Upgrade)")]
    public float damageLv1 = 10f;
    public float damageLv2 = 15f;
    public float damageLv3 = 20f;

    [Header("Cấu hình Vùng Cháy (Lv3)")]
    public float burnRadius = 3f;
    public float burnDamagePerSec = 5f;
    public float burnDuration = 3f;
    public GameObject fireVfxPrefab;

    private UpgradeableBuilding upgradeableBuilding;
    private Transform currentTarget;
    private float nextFireTime;

    private void Start()
    {
        upgradeableBuilding = GetComponent<UpgradeableBuilding>();
        if (firePoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                string nameLower = child.name.ToLower();
                if (nameLower.Contains("firepoint") || nameLower.Contains("muzzle") || nameLower.Contains("spawn") || nameLower.Contains("shoot"))
                {
                    firePoint = child;
                    break;
                }
            }
            if (firePoint == null)
            {
                firePoint = transform;
                Debug.LogWarning($"[AttackTowerAI] {name}: Không tìm thấy firePoint, tự động dùng chính tháp làm firePoint!");
            }
        }
    }

    // Hàm nhận lệnh tấn công do Tháp Canh truyền mục tiêu sang
    public void CommandAttack(Transform target)
    {
        currentTarget = target;
        Debug.Log($"[AttackTowerAI] CommandAttack received. Target={(target==null?"null":target.name)}");
    }

    private void Update()
    {
        // Nếu không có mục tiêu được chỉ định từ tháp canh -> Bỏ qua không bắn
        if (currentTarget == null) return;

        if (!currentTarget.gameObject.activeInHierarchy)
        {
            Debug.Log($"[AttackTowerAI] Current target {currentTarget.name} is not active -> clearing");
            currentTarget = null;
            return;
        }

        // Kiểm tra giãn cách thời gian giữa các loạt bắn
        if (Time.time >= nextFireTime)
        {
            ExecuteAttack();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    private void ExecuteAttack()
    {
        // Kiểm tra lại xem mục tiêu còn sống/tồn tại không trước khi bắn
        if (currentTarget == null) { Debug.Log("[AttackTowerAI] ExecuteAttack called but currentTarget is null"); return; }

        if (towerType == AttackTowerType.Archer)
        {
            Debug.Log($"[ArcherTower] 🏹 Bắn cung vào mục tiêu: {currentTarget.name} (Tọa độ: {currentTarget.position})");
            SpawnArrow();
        }
        else if (towerType == AttackTowerType.Cannon)
        {
            Debug.Log($"[Cannon] 💣 Dội bom/Pháo kích vào vị trí: {currentTarget.position}");
            SpawnAoEBomb();
        }

        // Note: currentTarget is intentionally kept so tower can continue firing until target dies
    }

    private void SpawnArrow()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("[AttackTowerAI] SpawnArrow aborted: projectilePrefab or firePoint is null");
            return;
        }

        int level = upgradeableBuilding != null ? upgradeableBuilding.CurrentLevel : 0;
        float damage = damageLv1;
        if (level == 1) damage = damageLv2;
        else if (level == 2) damage = damageLv3;

        if (level == 0)
        {
            SpawnSingleArrow(currentTarget, 0f, level, damage);
        }
        else
        {
            SpawnSingleArrow(currentTarget, 0f, level, damage);
            SpawnSingleArrow(null, -15f, level, damage);
            SpawnSingleArrow(null, 15f, level, damage);
        }
    }

    private void SpawnSingleArrow(Transform target, float yawOffset, int level, float damage)
    {
        Vector3 dirToTarget = (currentTarget != null) ? (currentTarget.position - firePoint.position) : firePoint.forward;
        dirToTarget.y = 0f;
        if (dirToTarget.sqrMagnitude < 0.0001f) dirToTarget = firePoint.forward;

        float baseYaw = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
        float finalYaw = baseYaw + yawOffset + projectileYawOffset;
        
        Quaternion spawnRot = Quaternion.Euler(0f, finalYaw, 0f);
        Vector3 spawnPos = firePoint.position + spawnRot * Vector3.forward * muzzleOffset;

        GameObject arrow = ArrowPool.Instance != null ? ArrowPool.Instance.Spawn(projectilePrefab, spawnPos, spawnRot) : Instantiate(projectilePrefab, spawnPos, spawnRot);

        var arrowComp = arrow.GetComponent<Arrow>();
        if (arrowComp != null)
        {
            arrowComp.SetLauncher(gameObject);
            arrowComp.SetDamage(damage);
            
            if (level == 2 && towerType == AttackTowerType.Archer)
            {
                arrowComp.SetFireArrow(true, burnRadius, burnDamagePerSec, burnDuration, fireVfxPrefab);
            }

            if (target != null)
            {
                arrowComp.SetTarget(target, projectileSpeed);
                arrowComp.AdjustZByHeightAndDistance(firePoint.position, target.position);
                arrowComp.AdjustYToFaceTarget(firePoint.position, target.position, projectileYawOffset);
            }
            else
            {
                arrowComp.SetTarget(null, projectileSpeed);
            }
        }
        else
        {
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = (spawnRot * Vector3.forward) * projectileSpeed;
            }
        }
    }

    private void SpawnAoEBomb()
    {
        if (projectilePrefab == null || currentTarget == null)
        {
            Debug.LogWarning("[AttackTowerAI] SpawnAoEBomb aborted: projectilePrefab or currentTarget is null");
            return;
        }

        int level = upgradeableBuilding != null ? upgradeableBuilding.CurrentLevel : 0;
        float damage = damageLv1;
        if (level == 1) damage = damageLv2;
        else if (level == 2) damage = damageLv3;

        Vector3 spawnPos;
        Quaternion bombRot;
        if (firePoint != null)
        {
            spawnPos = firePoint.position;
            bombRot = firePoint.rotation * projectilePrefab.transform.rotation;
        }
        else
        {
            spawnPos = currentTarget.position + Vector3.up * bombSpawnHeight;
            bombRot = projectilePrefab.transform.rotation;
        }

        if (firePoint != null)
            spawnPos += firePoint.forward * muzzleOffset;

        GameObject bomb = ArrowPool.Instance != null ? ArrowPool.Instance.Spawn(projectilePrefab, spawnPos, bombRot) : Instantiate(projectilePrefab, spawnPos, bombRot);
        Debug.Log($"[AttackTowerAI] Spawned AoE bomb '{projectilePrefab.name}' at {spawnPos} (firePoint used={(firePoint!=null)})");

        var canonComp = bomb.GetComponent<Canon>();
        if (canonComp != null)
        {
            canonComp.SetLauncher(gameObject);
            canonComp.SetLevel(level + 1);
            canonComp.SetDamage(damage);

            if (level == 2)
            {
                canonComp.SetZoneConfig(burnRadius, burnDamagePerSec, burnDuration, fireVfxPrefab);
            }
        }

        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            bomb.transform.SetParent(null);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 toTarget = currentTarget.position - spawnPos;
            Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
            float dx = toTargetXZ.magnitude;
            float dy = toTarget.y;

            float v = projectileSpeed;
            float v2 = v * v;
            float g = Mathf.Abs(Physics.gravity.y);

            float underSqrt = v2 * v2 - g * (g * dx * dx + 2f * dy * v2);
            Debug.Log($"[AttackTowerAI] Ballistics debug: dx={dx:F2}, dy={dy:F2}, v={v:F2}, underSqrt={underSqrt:F4}");

            if (underSqrt < 0f)
            {
                Vector3 vel = (toTarget.normalized) * v;
                rb.linearVelocity = vel;
                Debug.LogWarning("[AttackTowerAI] projectileSpeed too low for ballistic solution, using direct velocity fallback");
            }
            else
            {
                float root = Mathf.Sqrt(underSqrt);
                float tanTheta = (v2 - root) / (g * dx);
                float angle = Mathf.Atan(tanTheta);

                float vy = v * Mathf.Sin(angle);
                float vx = v * Mathf.Cos(angle);

                Vector3 vel = toTargetXZ.normalized * vx + Vector3.up * vy;
                rb.linearVelocity = vel;
                if (vel.sqrMagnitude > 0.001f)
                    bomb.transform.rotation = Quaternion.LookRotation(vel.normalized);
                Debug.Log($"[AttackTowerAI] Applied ballistic velocity {vel} to bomb");
            }
        }
        else
        {
            Debug.Log("[AttackTowerAI] Bomb prefab has no Rigidbody; it will simply spawn and fall (add Rigidbody for ballistic behavior)");
        }
    }
}