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
        public float projectileYawOffset = 270f;
        [Tooltip("Vertical spawn height above target for AoE bombs (meters). Lower to reduce high arc.")]
        public float bombSpawnHeight = 6f;
        [Tooltip("Distance forward from the firePoint to spawn the projectile to avoid overlapping the muzzle.")]
        public float muzzleOffset = 0.5f;

    private Transform currentTarget;
    private float nextFireTime;

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

        // Tạo mũi tên tại vị trí đầu nòng cung
        // Preserve prefab's local rotation by combining firePoint rotation with prefab rotation
        Quaternion spawnRot = firePoint.rotation * projectilePrefab.transform.rotation;
        Vector3 spawnPos = firePoint.position + firePoint.forward * muzzleOffset;
        GameObject arrow = ArrowPool.Instance != null ? ArrowPool.Instance.Spawn(projectilePrefab, spawnPos, spawnRot) : Instantiate(projectilePrefab, spawnPos, spawnRot);
        Debug.Log($"[AttackTowerAI] Spawned arrow '{projectilePrefab.name}' at {firePoint.position}");

        // Try to pass target to Arrow component so it moves smoothly
        var arrowComp = arrow.GetComponent<Arrow>();
        if (arrowComp != null && currentTarget != null)
        {
            arrowComp.SetTarget(currentTarget, projectileSpeed);
            arrowComp.AdjustZByHeightAndDistance(firePoint.position, currentTarget.position);
            arrowComp.AdjustYToFaceTarget(firePoint.position, currentTarget.position, projectileYawOffset);
            Debug.Log("[AttackTowerAI] Set target on Arrow component");
        }
        else
        {
            // Fallback: apply velocity to Rigidbody if present
            Vector3 dir = (currentTarget != null) ? (currentTarget.position - firePoint.position).normalized : firePoint.forward;
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = dir * projectileSpeed;
                Debug.Log("[AttackTowerAI] Applied velocity to arrow Rigidbody (fallback)");
            }
            else
            {
                Debug.Log("[AttackTowerAI] Arrow has no Rigidbody and no Arrow component to control movement");
                // Apply yaw offset to transform so prefab orientation aligns if needed
                if (currentTarget != null)
                {
                    Vector3 dir2 = (currentTarget.position - firePoint.position);
                    dir2.y = 0f;
                    if (dir2.sqrMagnitude > 0.0001f)
                    {
                        float yaw = Mathf.Atan2(dir2.x, dir2.z) * Mathf.Rad2Deg + projectileYawOffset;
                        Vector3 e = arrow.transform.localEulerAngles;
                        e.y = yaw;
                        arrow.transform.localEulerAngles = e;
                    }
                }
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
        // Spawn the bomb at the tower's firePoint (so cannon appears to fire from the muzzle).
        // If firePoint is missing, fall back to spawning above the target using bombSpawnHeight.
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

        // move spawn slightly forward from the firePoint to avoid spawning inside the cannon model
        if (firePoint != null)
            spawnPos += firePoint.forward * muzzleOffset;

        GameObject bomb = ArrowPool.Instance != null ? ArrowPool.Instance.Spawn(projectilePrefab, spawnPos, bombRot) : Instantiate(projectilePrefab, spawnPos, bombRot);
        Debug.Log($"[AttackTowerAI] Spawned AoE bomb '{projectilePrefab.name}' at {spawnPos} (firePoint used={(firePoint!=null)})");

        // Nếu prefab có Rigidbody, tính vận tốc ban đầu để bắn theo quỹ đạo trúng mục tiêu
        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Ensure projectile isn't parented and Rigidbody is ready
            bomb.transform.SetParent(null);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 toTarget = currentTarget.position - spawnPos;
            Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
            float dx = toTargetXZ.magnitude;
            float dy = toTarget.y; // relative height from spawnPos to target

            float v = projectileSpeed;
            float v2 = v * v;
            float g = Mathf.Abs(Physics.gravity.y);

            float underSqrt = v2 * v2 - g * (g * dx * dx + 2f * dy * v2);
            Debug.Log($"[AttackTowerAI] Ballistics debug: dx={dx:F2}, dy={dy:F2}, v={v:F2}, underSqrt={underSqrt:F4}");

            if (underSqrt < 0f)
            {
                // tốc độ không đủ để bắn trúng ở quỹ đạo tính được -> fallback: ném thẳng theo hướng với velocity v
                Vector3 vel = (toTarget.normalized) * v;
                rb.linearVelocity = vel;
                Debug.LogWarning("[AttackTowerAI] projectileSpeed too low for ballistic solution, using direct velocity fallback");
            }
            else
            {
                float root = Mathf.Sqrt(underSqrt);
                // chọn góc thấp hơn để đường đạn phẳng hơn
                float tanTheta = (v2 - root) / (g * dx);
                float angle = Mathf.Atan(tanTheta);

                float vy = v * Mathf.Sin(angle);
                float vx = v * Mathf.Cos(angle);

                Vector3 vel = toTargetXZ.normalized * vx + Vector3.up * vy;
                rb.linearVelocity = vel;
                // quay projectile theo vận tốc
                if (vel.sqrMagnitude > 0.001f)
                    bomb.transform.rotation = Quaternion.LookRotation(vel.normalized);
                Debug.Log($"[AttackTowerAI] Applied ballistic velocity {vel} to bomb");
            }
        }
        else
        {
            Debug.Log("[AttackTowerAI] Bomb prefab has no Rigidbody; it will simply spawn and fall (add Rigidbody for ballistic behavior)");
        }

        // Gợi ý: Bạn nên gắn một script xử lý Rơi tự do (Rigidbody) hoặc Di chuyển xuống dưới 
        // lên Prefab quả bom để khi chạm đất nó tạo sát thương lan (AoE Explosion).
    }
}