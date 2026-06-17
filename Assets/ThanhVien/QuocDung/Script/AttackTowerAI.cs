using UnityEngine;

/*
 * AttackTowerAI.cs
 * Folder: Scripts/AI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ + ĐĂNG
 * CHỨC NĂNG: Điều khiển tháp tấn công (Cung, Pháo) bắn quái, tính toán quỹ đạo ném bom ballistic,
 * đồng thời mở cổng property "AttackRange" để UIManager.cs bóc tách dữ liệu UI chuẩn xác.
 */

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

    [Header("Cấu hình Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackParamName = "IsAttack";

    [Header("Hệ thống nhắm mục tiêu (Targeting)")]
    [SerializeField] private float attackRange = 8f; 

    // --- CỔNG KẾT NỐI PUBLIC ĐỂ UIManager ĐỌC DỮ LIỆU (KHÔNG LÀM MẤT PRIVATE BIẾN GỐC) ---
    public float AttackRange => attackRange;

    private UpgradeableBuilding upgradeableBuilding;
    private Transform currentTarget;
    private float nextFireTime;

    private void Start()
    {
        upgradeableBuilding = GetComponent<UpgradeableBuilding>();
        UpdateAnimatorReference();
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
        Debug.Log($"[AttackTowerAI] CommandAttack received. Target={(target == null ? "null" : target.name)}");
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

        PlayAttackAnimation();

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
    }

    private void PlayAttackAnimation()
    {
        UpdateAnimatorReference();
        if (animator == null)
        {
            Debug.LogWarning($"[AttackTowerAI] {name}: Animator component is missing/null!");
            return;
        }
        if (!animator.enabled)
        {
            Debug.LogWarning($"[AttackTowerAI] {name}: Animator component is disabled!");
            return;
        }
        StartCoroutine(TriggerAttackAnimationRoutine());
    }

    private void UpdateAnimatorReference()
    {
        if (upgradeableBuilding != null)
        {
            int currentLevel = upgradeableBuilding.CurrentLevel;
            var visualModels = upgradeableBuilding.VisualModels;
            if (visualModels != null && currentLevel >= 0 && currentLevel < visualModels.Length)
            {
                GameObject activeModel = visualModels[currentLevel];
                if (activeModel != null)
                {
                    Animator activeModelAnimator = activeModel.GetComponent<Animator>();
                    if (activeModelAnimator == null)
                    {
                        activeModelAnimator = activeModel.GetComponentInChildren<Animator>();
                    }

                    if (activeModelAnimator != null)
                    {
                        animator = activeModelAnimator;
                        return;
                    }
                }
            }
        }

        Animator rootAnimator = GetComponent<Animator>();
        if (rootAnimator != null)
        {
            animator = rootAnimator;
            return;
        }

        Animator activeChildAnimator = GetComponentInChildren<Animator>(false);
        if (activeChildAnimator != null)
        {
            animator = activeChildAnimator;
        }
    }

    private System.Collections.IEnumerator TriggerAttackAnimationRoutine()
    {
        AnimatorControllerParameter param = GetParameter(animator, attackParamName);
        if (param != null)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(attackParamName);
                Debug.Log($"[AttackTowerAI] {name}: Set Animator Trigger parameter '{attackParamName}'.");
            }
            else if (param.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(attackParamName, true);
                Debug.Log($"[AttackTowerAI] {name}: Set Animator bool parameter '{attackParamName}' to true.");
                yield return new WaitForSeconds(0.2f);
                if (animator != null)
                {
                    animator.SetBool(attackParamName, false);
                    Debug.Log($"[AttackTowerAI] {name}: Set Animator bool parameter '{attackParamName}' to false.");
                }
            }
            else
            {
                Debug.LogWarning($"[AttackTowerAI] {name}: Parameter '{attackParamName}' is of type {param.type}, which is not supported (only Bool or Trigger are supported).");
            }
        }
        else
        {
            Debug.LogWarning($"[AttackTowerAI] {name}: Parameter '{attackParamName}' was NOT found in the Animator Controller!");
        }
    }

    private AnimatorControllerParameter GetParameter(Animator anim, string paramName)
    {
        if (anim == null) return null;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return param;
        }
        return null;
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
            System.Collections.Generic.List<Transform> enemiesInRange = new System.Collections.Generic.List<Transform>();
            if (currentTarget != null) enemiesInRange.Add(currentTarget);

            float checkRadius = 25f;
            Collider[] colls = Physics.OverlapSphere(transform.position, checkRadius);
            foreach (var col in colls)
            {
                if (col == null || !col.gameObject.activeInHierarchy) continue;
                
                bool isEnemy = col.CompareTag("Enemy") || col.name.ToLower().Contains("enemy") || col.GetComponentInParent<EnemyHealth>() != null;
                if (isEnemy)
                {
                    var health = col.GetComponentInParent<EnemyHealth>();
                    Transform enemyTrans = (health != null) ? health.transform : col.transform;
                    if (!enemiesInRange.Contains(enemyTrans))
                    {
                        enemiesInRange.Add(enemyTrans);
                    }
                }
            }

            enemiesInRange.Sort((a, b) => {
                if (a == currentTarget) return -1;
                if (b == currentTarget) return 1;
                float distA = (a.position - transform.position).sqrMagnitude;
                float distB = (b.position - transform.position).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            if (enemiesInRange.Count > 0)
            {
                SpawnSingleArrow(enemiesInRange[0], 0f, level, damage);

                Transform target2 = enemiesInRange.Count > 1 ? enemiesInRange[1] : enemiesInRange[0];
                SpawnSingleArrow(target2, -15f, level, damage);

                Transform target3 = enemiesInRange.Count > 2 ? enemiesInRange[2] : (enemiesInRange.Count > 1 ? enemiesInRange[0] : enemiesInRange[0]);
                SpawnSingleArrow(target3, 15f, level, damage);
            }
            else
            {
                SpawnSingleArrow(currentTarget, 0f, level, damage);
                SpawnSingleArrow(null, -15f, level, damage);
                SpawnSingleArrow(null, 15f, level, damage);
            }
        }
    }

    private void SpawnSingleArrow(Transform target, float yawOffset, int level, float damage)
    {
        Vector3 dirToTarget = (target != null) ? (target.position - firePoint.position) : firePoint.forward;
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

            if (underSqrt < 0f)
            {
                Vector3 vel = (toTarget.normalized) * v;
                rb.linearVelocity = vel;
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
            }
        }
    }
}