using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform villageCenter;
    public Animator animator;

    [Header("Patrol (Deprecated)")]
    public float patrolRadius = 8f;
    public float pointReachDistance = 1f;
    public float repathInterval = 2f;

    [Header("Chase")]
    public float chaseTriggerRange = 6f;
    public float loseChaseRange = 12f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float attackRange = 2f;

    public enum EnemyAttackType { Melee, Ranged }

    [Header("Combat")]
    public EnemyAttackType attackType = EnemyAttackType.Melee;
    public float attackDamage = 10f;
    public float attackRate = 1.5f;
    private float nextAttackTime;

    [Header("Ranged Config")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;
    public float rangedAttackRange = 8f;

    public float CurrentAttackRange => (attackType == EnemyAttackType.Ranged) ? rangedAttackRange : attackRange;

    [Header("Animation")]
    public string moveBoolParam = "IsMove";
    public string attackBoolParam = "IsAttack";
    public string shootBoolParam = "IsShoot";

    [Header("Debug")]
    public bool debugLogs = true;
    public float debugLogInterval = 1f;
    public bool attackMainDirectly = false; // Checkbox để test đánh trực tiếp Main

    [Header("Move Animation")]
    public float moveThreshold = 0.1f;

    private NavMeshAgent agent;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;
    private float nextRepathTime;
    private Transform chaseTarget;
    private float nextDebugLogTime;
    private Vector3 targetClosestPoint;
    private Transform lastChaseTarget;
    private float nextTargetClosestPointUpdateTime;

    // Static variables for group coordination and target sharing
    private static List<EnemyAI> activeEnemies = new List<EnemyAI>();
    private static Dictionary<Transform, EnemyAI> targetAssignments = new Dictionary<Transform, EnemyAI>();
    private float myNextScanTime;
    private static float scanInterval = 0.2f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null && debugLogs) Debug.LogError("EnemyAI requires a NavMeshAgent");
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // try to ensure agent is on NavMesh
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                if (debugLogs) Debug.Log("EnemyAI: warped agent to nearest NavMesh");
            }
            else if (debugLogs)
            {
                Debug.LogWarning("EnemyAI: no NavMesh near agent position. Bake NavMesh or move agent onto NavMesh.");
            }
        }
    }

    private void OnEnable()
    {
        if (!activeEnemies.Contains(this))
        {
            activeEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
        // Clear target assignment for this enemy
        List<Transform> keys = new List<Transform>(targetAssignments.Keys);
        foreach (var key in keys)
        {
            if (targetAssignments[key] == this)
            {
                targetAssignments.Remove(key);
            }
        }
    }

    private void OnDestroy()
    {
        activeEnemies.Remove(this);
        // Clear target assignment for this enemy
        List<Transform> keys = new List<Transform>(targetAssignments.Keys);
        foreach (var key in keys)
        {
            if (targetAssignments[key] == this)
            {
                targetAssignments.Remove(key);
            }
        }
    }

    private void Start()
    {
        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 0.1f;
            agent.angularSpeed = 360f;
        }
        
        // Register this enemy to start marching
        if (!activeEnemies.Contains(this))
        {
            activeEnemies.Add(this);
        }

        UpdateAnimationState();
    }

    private void Update()
    {
        // Keep list clean of destroyed objects
        activeEnemies.RemoveAll(e => e == null);

        if (activeEnemies.Count == 0) return;

        // Auto-find villageCenter using tag "Main" if null
        if (villageCenter == null)
        {
            GameObject mainObj = FindGameObjectWithSafeTag("Main");
            if (mainObj != null)
            {
                villageCenter = mainObj.transform;
            }
        }

        // Each enemy scans for its target individually (seqential list index)
        FindIndividualTarget();

        // Target to move/attack
        Transform target = chaseTarget;

        if (target != null)
        {
            // Kiểm soát tần số cập nhật điểm tiếp cận để tránh chạy vòng tròn quanh công trình tĩnh
            bool needUpdatePoint = false;
            if (target != lastChaseTarget)
            {
                lastChaseTarget = target;
                needUpdatePoint = true;
            }
            else if (IsSoldier(target.gameObject))
            {
                needUpdatePoint = true; // Soldier di động cập nhật liên tục
            }
            else if (Time.time >= nextTargetClosestPointUpdateTime)
            {
                needUpdatePoint = true; // Công trình tĩnh cập nhật mỗi 1.0s
                nextTargetClosestPointUpdateTime = Time.time + 1.0f;
            }

            if (needUpdatePoint)
            {
                UpdateTargetClosestPoint(target);
            }

            int myIndex = activeEnemies.IndexOf(this);
            if (myIndex < 0) myIndex = 0;

            Vector3 dest = GetFormationPosition(target, myIndex);
            
            // Calculate distance to boundary of target collider
            float distToTarget = GetDistanceToCollider(target.gameObject);

            if (agent != null)
            {
                agent.speed = chaseSpeed;
                
                // Determine if in range based on target type
                float actualAttackRange = CurrentAttackRange;
                if (attackType == EnemyAttackType.Melee && 
                    (SafeCompareTag(target, "Main") || IsMainHouse(target.gameObject) || SafeCompareTag(target, "Tower") || IsTower(target.gameObject)))
                {
                    // Stand closer to Main house or Tower (1.0m is very close, right at the boundary)
                    actualAttackRange = 1.0f;
                }

                // If close to actual target, stop and attack
                if (distToTarget <= actualAttackRange)
                {
                    agent.isStopped = true;
                    
                    // Rotate towards target when attacking (very fast rotation speed using RotateTowards)
                    Vector3 lookDir = (target.position - transform.position);
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 720f * Time.deltaTime);
                    }
                    
                    if (Time.time >= nextAttackTime)
                    {
                        ExecuteAttack(target);
                        nextAttackTime = Time.time + attackRate;
                    }
                }
                else
                {
                    agent.isStopped = false;
                    SetDestination(dest);
                }
            }
        }
        else
        {
            if (agent != null) agent.isStopped = true;
        }

        UpdateAnimationState();
    }

    private static IDamageable GetDamageable(GameObject go)
    {
        if (go == null) return null;
        
        // 1. Thử tìm trong chính nó và các cha
        IDamageable dmg = go.GetComponentInParent<IDamageable>();
        if (dmg != null) return dmg;

        // 2. Thử tìm trong các con
        dmg = go.GetComponentInChildren<IDamageable>();
        if (dmg != null) return dmg;

        // 3. Thử đi lên cha tối đa 3 cấp (tránh chạm tới Root Folder lớn như "Main Game" hay Scene Root)
        Transform curr = go.transform.parent;
        int depth = 0;
        while (curr != null && depth < 3)
        {
            if (curr.parent == null) break;
            
            try
            {
                if (curr.CompareTag("Main") || curr.CompareTag("Player"))
                {
                    break;
                }
            }
            catch {}

            string nameLower = curr.name.ToLower();
            if (nameLower.Contains("game") || nameLower.Contains("manager"))
            {
                break;
            }

            IDamageable parentChildDmg = curr.GetComponentInChildren<IDamageable>();
            if (parentChildDmg != null) return parentChildDmg;
            curr = curr.parent;
            depth++;
        }

        return null;
    }

    private static void CleanupTargetAssignments()
    {
        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in targetAssignments)
        {
            if (kvp.Value == null || kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy)
            {
                toRemove.Add(kvp.Key);
                continue;
            }
            IDamageable damageable = GetDamageable(kvp.Key.gameObject);
            if (damageable != null && damageable.CurrentHealth <= 0f)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove)
        {
            targetAssignments.Remove(key);
        }
    }

    private void AssignTarget(Transform newTarget)
    {
        // Clear this enemy's old target assignment
        List<Transform> keys = new List<Transform>(targetAssignments.Keys);
        foreach (var key in keys)
        {
            if (targetAssignments[key] == this)
            {
                targetAssignments.Remove(key);
            }
        }

        // Assign new target (except if it's the main house/villageCenter, multiple enemies CAN target Main)
        if (newTarget != null && newTarget != villageCenter && !SafeCompareTag(newTarget, "Main"))
        {
            targetAssignments[newTarget] = this;
        }
    }

    private void FindIndividualTarget()
    {
        // 1. Giữ mục tiêu hiện tại (đánh đến cùng) cho đến khi mục tiêu chết hoặc biến mất
        if (chaseTarget != null && chaseTarget.gameObject.activeInHierarchy)
        {
            bool isCurrentTargetMain = SafeCompareTag(chaseTarget, "Main") || chaseTarget == villageCenter || IsMainHouse(chaseTarget.gameObject);
            
            // Nếu mục tiêu hiện tại là Main hoặc Tower, ta cho phép quét lại để ưu tiên đánh mục tiêu có độ ưu tiên cao hơn (Soldier > Tower > Main)
            bool shouldStick = true;
            if (isCurrentTargetMain)
            {
                shouldStick = false;
            }
            else
            {
                bool isCurrentTargetTower = SafeCompareTag(chaseTarget, "Tower") || IsTower(chaseTarget.gameObject);
                if (isCurrentTargetTower)
                {
                    shouldStick = false;
                }
            }

            if (shouldStick && (!attackMainDirectly || isCurrentTargetMain))
            {
                IDamageable damageable = GetDamageable(chaseTarget.gameObject);
                bool isAlive = damageable == null || damageable.CurrentHealth > 0f;

                if (isAlive)
                {
                    // Vẫn giữ gán mục tiêu
                    AssignTarget(chaseTarget);
                    return;
                }
            }
        }

        if (Time.time < myNextScanTime) return;
        myNextScanTime = Time.time + scanInterval;

        CleanupTargetAssignments();

        float searchRadius = Mathf.Max(chaseTriggerRange * 5f, 40f);
        
        // Tạo tập hợp các đối tượng quét thấy, kết hợp OverlapSphere và FindObjectsOfType trực tiếp để đề phòng không có Collider
        List<GameObject> detectedObjects = new List<GameObject>();

        // 1. Quét bằng Physics.OverlapSphere
        Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius);
        foreach (var col in colliders)
        {
            if (col != null && col.gameObject.activeInHierarchy && !detectedObjects.Contains(col.gameObject))
            {
                detectedObjects.Add(col.gameObject);
            }
        }

        // 2. Dự phòng: Quét bằng các component để đối phó triệt để trường hợp thiếu Collider
        try
        {
            UnitController[] allUnits = FindObjectsOfType<UnitController>();
            foreach (var unit in allUnits)
            {
                if (unit != null && unit.gameObject.activeInHierarchy)
                {
                    float dist = Vector3.Distance(transform.position, unit.transform.position);
                    if (dist <= searchRadius && !detectedObjects.Contains(unit.gameObject))
                    {
                        detectedObjects.Add(unit.gameObject);
                    }
                }
            }
        }
        catch {}

        try
        {
            HPSoldier[] allHPSoldiers = FindObjectsOfType<HPSoldier>();
            foreach (var hpSoldier in allHPSoldiers)
            {
                if (hpSoldier != null && hpSoldier.gameObject.activeInHierarchy)
                {
                    float dist = Vector3.Distance(transform.position, hpSoldier.transform.position);
                    if (dist <= searchRadius && !detectedObjects.Contains(hpSoldier.gameObject))
                    {
                        detectedObjects.Add(hpSoldier.gameObject);
                    }
                }
            }
        }
        catch {}

        try
        {
            WatchTowerAI[] allWatchTowers = FindObjectsOfType<WatchTowerAI>();
            foreach (var tower in allWatchTowers)
            {
                if (tower != null && tower.gameObject.activeInHierarchy)
                {
                    float dist = Vector3.Distance(transform.position, tower.transform.position);
                    if (dist <= searchRadius && !detectedObjects.Contains(tower.gameObject))
                    {
                        detectedObjects.Add(tower.gameObject);
                    }
                }
            }
        }
        catch {}

        try
        {
            AttackTowerAI[] allAttackTowers = FindObjectsOfType<AttackTowerAI>();
            foreach (var tower in allAttackTowers)
            {
                if (tower != null && tower.gameObject.activeInHierarchy)
                {
                    float dist = Vector3.Distance(transform.position, tower.transform.position);
                    if (dist <= searchRadius && !detectedObjects.Contains(tower.gameObject))
                    {
                        detectedObjects.Add(tower.gameObject);
                    }
                }
            }
        }
        catch {}

        try
        {
            DefenceTowerAI[] allDefenceTowers = FindObjectsOfType<DefenceTowerAI>();
            foreach (var tower in allDefenceTowers)
            {
                if (tower != null && tower.gameObject.activeInHierarchy)
                {
                    float dist = Vector3.Distance(transform.position, tower.transform.position);
                    if (dist <= searchRadius && !detectedObjects.Contains(tower.gameObject))
                    {
                        detectedObjects.Add(tower.gameObject);
                    }
                }
            }
        }
        catch {}

        if (debugLogs)
        {
            Debug.Log($"[EnemyAI] {gameObject.name} quét mục tiêu trong bán kính {searchRadius}. Tìm thấy {detectedObjects.Count} đối tượng hợp lệ.");
        }

        Transform selected = null;

        // Nếu người dùng bật tùy chọn test đánh thẳng Main
        if (attackMainDirectly)
        {
            Transform bestMain = null;
            float minMainDist = float.MaxValue;

            foreach (var go in detectedObjects)
            {
                if (go == null || !go.activeInHierarchy) continue;

                if (IsMainHouse(go))
                {
                    IDamageable damageable = GetDamageable(go);
                    if (damageable != null && damageable.CurrentHealth <= 0f) continue;

                    Transform t = GetMainHouseRoot(go);
                    if (t == null) continue;

                    float dist = GetDistanceToCollider(go);
                    if (dist < minMainDist)
                    {
                        minMainDist = dist;
                        bestMain = t;
                    }
                }
            }

            if (bestMain != null)
            {
                selected = bestMain;
            }
            else
            {
                if (villageCenter != null)
                {
                    selected = villageCenter;
                }
            }

            AssignTarget(selected);
            chaseTarget = selected;
            return;
        }

        // --- Tìm kiếm theo thứ tự ưu tiên tuyệt đối ---

        // Bước 1: Quét và phân loại tất cả mục tiêu hợp lệ còn sống trong tầm
        List<Transform> aliveSoldiers = new List<Transform>();
        List<Transform> aliveTowers = new List<Transform>();
        Transform bestMainInScan = null;
        float minMainDistInScan = float.MaxValue;

        foreach (var go in detectedObjects)
        {
            if (go == null || !go.activeInHierarchy) continue;

            // Kiểm tra xem mục tiêu có còn sống không
            IDamageable damageable = GetDamageable(go);
            if (damageable != null && damageable.CurrentHealth <= 0f)
            {
                if (debugLogs) Debug.Log($"[EnemyAI] Bỏ qua mục tiêu đã chết {go.name} (HP: {damageable.CurrentHealth})");
                continue;
            }

            if (IsSoldier(go))
            {
                Transform t = GetSoldierRoot(go);
                if (t != null)
                {
                    if (debugLogs) Debug.Log($"[EnemyAI] Phát hiện Soldier sống: {go.name} -> root: {t.name}");
                    if (!aliveSoldiers.Contains(t))
                    {
                        aliveSoldiers.Add(t);
                    }
                }
                else
                {
                    if (debugLogs) Debug.LogWarning($"[EnemyAI] {go.name} được nhận dạng là Soldier nhưng GetSoldierRoot trả về null!");
                }
            }
            else if (IsTower(go))
            {
                Transform t = GetTowerRoot(go);
                if (t != null)
                {
                    if (debugLogs) Debug.Log($"[EnemyAI] Phát hiện Tower sống: {go.name} -> root: {t.name}");
                    if (!aliveTowers.Contains(t))
                    {
                        aliveTowers.Add(t);
                    }
                }
            }
            else if (IsMainHouse(go))
            {
                Transform t = GetMainHouseRoot(go);
                if (t != null)
                {
                    if (debugLogs) Debug.Log($"[EnemyAI] Phát hiện Main House: {go.name} -> root: {t.name}");
                    float dist = GetDistanceToCollider(go);
                    if (dist < minMainDistInScan)
                    {
                        minMainDistInScan = dist;
                        bestMainInScan = t;
                    }
                }
            }
        }

        // Bước 2: Chọn mục tiêu dựa trên các danh sách đã lọc
        
        // A. Nếu còn bất kỳ Soldier nào sống, CHỈ chọn Soldier
        if (aliveSoldiers.Count > 0)
        {
            // Tìm Soldier trống gần nhất
            Transform bestUnoccupiedSoldier = null;
            float minUnoccupiedSoldierDist = float.MaxValue;

            foreach (var soldier in aliveSoldiers)
            {
                if (targetAssignments.TryGetValue(soldier, out EnemyAI assignedEnemy) && assignedEnemy != this)
                    continue; // Đã bị chiếm

                float dist = GetDistanceToCollider(soldier.gameObject);
                if (dist < minUnoccupiedSoldierDist)
                {
                    minUnoccupiedSoldierDist = dist;
                    bestUnoccupiedSoldier = soldier;
                }
            }

            if (bestUnoccupiedSoldier != null)
            {
                selected = bestUnoccupiedSoldier;
                if (debugLogs) Debug.Log($"[EnemyAI] Chọn Soldier chưa bị chiếm: {selected.name}");
            }
            else
            {
                // Dư lính: Chọn Soldier bất kỳ gần nhất (đã bị gán)
                float minAnySoldierDist = float.MaxValue;
                foreach (var soldier in aliveSoldiers)
                {
                    float dist = GetDistanceToCollider(soldier.gameObject);
                    if (dist < minAnySoldierDist)
                    {
                        minAnySoldierDist = dist;
                        selected = soldier;
                    }
                }
                if (debugLogs && selected != null) Debug.Log($"[EnemyAI] Chọn Soldier đã bị chiếm (hội đồng): {selected.name}");
            }
        }
        // B. Nếu hết Soldier nhưng còn bất kỳ Tower nào sống, CHỈ chọn Tower
        else if (aliveTowers.Count > 0)
        {
            // Tìm Tower trống gần nhất
            Transform bestUnoccupiedTower = null;
            float minUnoccupiedTowerDist = float.MaxValue;

            foreach (var tower in aliveTowers)
            {
                if (targetAssignments.TryGetValue(tower, out EnemyAI assignedEnemy) && assignedEnemy != this)
                    continue; // Đã bị chiếm

                float dist = GetDistanceToCollider(tower.gameObject);
                if (dist < minUnoccupiedTowerDist)
                {
                    minUnoccupiedTowerDist = dist;
                    bestUnoccupiedTower = tower;
                }
            }

            if (bestUnoccupiedTower != null)
            {
                selected = bestUnoccupiedTower;
                if (debugLogs) Debug.Log($"[EnemyAI] Chọn Tower chưa bị chiếm: {selected.name}");
            }
            else
            {
                // Dư lính: Chọn Tower bất kỳ gần nhất (đã bị gán)
                float minAnyTowerDist = float.MaxValue;
                foreach (var tower in aliveTowers)
                {
                    float dist = GetDistanceToCollider(tower.gameObject);
                    if (dist < minAnyTowerDist)
                    {
                        minAnyTowerDist = dist;
                        selected = tower;
                    }
                }
                if (debugLogs && selected != null) Debug.Log($"[EnemyAI] Chọn Tower đã bị chiếm (hội đồng): {selected.name}");
            }
        }
        // C. Nếu không còn Soldier và Tower nào, tiến hành đánh thẳng vào Main
        else
        {
            if (bestMainInScan != null)
            {
                selected = bestMainInScan;
                if (debugLogs) Debug.Log($"[EnemyAI] Chọn Main House phát hiện trong tầm quét: {selected.name}");
            }
            else
            {
                // Fallback cuối cùng về villageCenter
                if (villageCenter == null)
                {
                    GameObject mainObj = FindGameObjectWithSafeTag("Main");
                    if (mainObj != null)
                    {
                        villageCenter = mainObj.transform;
                    }
                }

                if (villageCenter != null)
                {
                    IDamageable damageable = GetDamageable(villageCenter.gameObject);
                    if (damageable == null || damageable.CurrentHealth > 0f)
                    {
                        selected = villageCenter;
                        if (debugLogs) Debug.Log($"[EnemyAI] Fallback về Village Center mặc định: {selected.name}");
                    }
                }
            }
        }

        AssignTarget(selected);
        chaseTarget = selected;
    }

    private void ExecuteAttack(Transform target)
    {
        PlayAttackAnimation();

        if (attackType == EnemyAttackType.Melee)
        {
            IDamageable damageable = GetDamageable(target.gameObject);
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage, target.position);
            }
        }
        else if (attackType == EnemyAttackType.Ranged)
        {
            if (projectilePrefab != null)
            {
                Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
                Collider targetCollider = target.GetComponentInChildren<Collider>();
                Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : target.position + Vector3.up * 1f;
                Vector3 direction = (targetCenter - spawnPos).normalized;
                Quaternion spawnRot = Quaternion.LookRotation(direction);

                GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);
                
                Arrow arrowComp = proj.GetComponent<Arrow>();
                if (arrowComp != null)
                {
                    arrowComp.SetLauncher(gameObject);
                    arrowComp.SetDamage(attackDamage);
                    arrowComp.SetTarget(target, projectileSpeed);
                }
                else
                {
                    Rigidbody rb = proj.GetComponent<Rigidbody>();
                    if (rb == null) rb = proj.AddComponent<Rigidbody>();
                    rb.linearVelocity = direction * projectileSpeed;
                }
            }
            else
            {
                IDamageable damageable = GetDamageable(target.gameObject);
                if (damageable != null)
                {
                    damageable.TakeDamage(attackDamage, target.position);
                }
            }
        }
    }

    private static Transform GetEntityRootByComponent<T>(GameObject go) where T : Component
    {
        if (go == null) return null;
        
        // 1. Thử tìm trong chính nó và các cha
        T comp = go.GetComponentInParent<T>();
        if (comp != null) return comp.transform;

        // 2. Thử tìm trong các con trực tiếp hoặc gián tiếp
        T childComp = go.GetComponentInChildren<T>();
        if (childComp != null) return childComp.transform;
        
        // 3. Thử đi lên cha tối đa 3 cấp và tìm trong các con
        Transform curr = go.transform.parent;
        int depth = 0;
        while (curr != null && depth < 3)
        {
            if (curr.parent == null) break;

            if (SafeCompareTag(curr, "Main") || SafeCompareTag(curr, "Player"))
            {
                break;
            }

            if (curr.name.ToLower().Contains("main") || curr.name.ToLower().Contains("game") || curr.name.ToLower().Contains("manager"))
            {
                break;
            }

            T subChildComp = curr.GetComponentInChildren<T>();
            if (subChildComp != null) return subChildComp.transform;
            curr = curr.parent;
            depth++;
        }
        
        return null;
    }

    private Transform GetSoldierRoot(GameObject go)
    {
        if (go == null) return null;

        // 1. Tìm tag "Soldier" hoặc "soldier" hoặc "Player" hoặc "player"
        Transform t = GetEntityRoot(go, "Soldier");
        if (t != null) return t;

        t = GetEntityRoot(go, "soldier");
        if (t != null) return t;

        t = GetEntityRoot(go, "Player");
        if (t != null) return t;

        t = GetEntityRoot(go, "player");
        if (t != null) return t;

        // 2. Tìm theo component
        Transform unitControllerTrans = GetEntityRootByComponent<UnitController>(go);
        if (unitControllerTrans != null) return unitControllerTrans;

        Transform hpSoldierTrans = GetEntityRootByComponent<HPSoldier>(go);
        if (hpSoldierTrans != null) return hpSoldierTrans;

        Transform playerMoveTrans = GetEntityRootByComponent<PlayerMove>(go);
        if (playerMoveTrans != null) return playerMoveTrans;

        // 3. Fallback theo tên đối tượng
        Transform curr = go.transform;
        while (curr != null)
        {
            string nameLower = curr.name.ToLower();
            if (nameLower.Contains("soldier") || nameLower.Contains("knight") || nameLower.Contains("archer") || nameLower.Contains("player"))
            {
                if (!nameLower.Contains("manager") && 
                    !nameLower.Contains("pool") && 
                    !nameLower.Contains("game") &&
                    !nameLower.Contains("camera") &&
                    !nameLower.Contains("light"))
                {
                    return curr;
                }
            }
            curr = curr.parent;
        }

        return null;
    }

    private Transform GetTowerRoot(GameObject go)
    {
        if (go == null) return null;

        // 1. Tìm tag "Tower" hoặc "tower"
        Transform t = GetEntityRoot(go, "Tower");
        if (t != null) return t;

        t = GetEntityRoot(go, "tower");
        if (t != null) return t;

        // 2. Tìm theo component
        Transform watchTowerTrans = GetEntityRootByComponent<WatchTowerAI>(go);
        if (watchTowerTrans != null) return watchTowerTrans;

        Transform attackTowerTrans = GetEntityRootByComponent<AttackTowerAI>(go);
        if (attackTowerTrans != null) return attackTowerTrans;

        Transform defenceTowerTrans = GetEntityRootByComponent<DefenceTowerAI>(go);
        if (defenceTowerTrans != null) return defenceTowerTrans;

        Transform hpTowerTrans = GetEntityRootByComponent<HPTower>(go);
        if (hpTowerTrans != null) return hpTowerTrans;

        // 3. Fallback theo tên đối tượng
        Transform curr = go.transform;
        while (curr != null)
        {
            string nameLower = curr.name.ToLower();
            if (nameLower.Contains("tower") || nameLower.Contains("canon") || nameLower.Contains("watchtower"))
            {
                if (!nameLower.Contains("manager") && 
                    !nameLower.Contains("pool") && 
                    !nameLower.Contains("game") &&
                    !nameLower.Contains("camera") &&
                    !nameLower.Contains("light"))
                {
                    return curr;
                }
            }
            curr = curr.parent;
        }

        return null;
    }

    private Transform GetMainHouseRoot(GameObject go)
    {
        if (go == null) return null;

        // 1. Tìm tag "Main" hoặc "main"
        Transform t = GetEntityRoot(go, "Main");
        if (t != null) return t;

        t = GetEntityRoot(go, "main");
        if (t != null) return t;

        if (villageCenter != null && (go.transform == villageCenter || go.transform.IsChildOf(villageCenter)))
        {
            return villageCenter;
        }

        if (go.name.ToLower().Contains("mainhouse") || go.name.ToLower().Contains("mainbuilding") || go.name.ToLower() == "main" || go.name.ToLower().Contains("villagecenter"))
        {
            return go.transform;
        }

        return null;
    }

    private bool IsSoldier(GameObject go)
    {
        return GetSoldierRoot(go) != null;
    }

    private bool IsTower(GameObject go)
    {
        return GetTowerRoot(go) != null;
    }

    private bool IsMainHouse(GameObject go)
    {
        return GetMainHouseRoot(go) != null;
    }

    private Transform GetEntityRoot(GameObject go, string tag)
    {
        if (go == null) return null;
        Transform curr = go.transform;
        while (curr != null)
        {
            if (SafeCompareTag(curr, tag))
                return curr;
            curr = curr.parent;
        }
        return null;
    }

    private void UpdateTargetClosestPoint(Transform target)
    {
        if (target == null) return;

        if (IsSoldier(target.gameObject))
        {
            targetClosestPoint = target.position;
            return;
        }

        Vector3 basePosition = target.position;
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        if (colliders != null && colliders.Length > 0)
        {
            float minDistance = float.MaxValue;
            Vector3 bestPoint = target.position;
            Vector3 flatSelf = new Vector3(transform.position.x, 0f, transform.position.z);

            foreach (var c in colliders)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger) continue;

                Vector3 closestPoint;
                MeshCollider meshCol = c as MeshCollider;
                if (meshCol != null && !meshCol.convex)
                {
                    closestPoint = c.bounds.ClosestPoint(transform.position);
                }
                else
                {
                    closestPoint = c.ClosestPoint(transform.position);
                }

                Vector3 flatClosest = new Vector3(closestPoint.x, 0f, closestPoint.z);
                float dist = Vector3.Distance(flatSelf, flatClosest);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestPoint = closestPoint;
                }
            }

            if (minDistance != float.MaxValue)
            {
                basePosition = bestPoint;
            }
        }

        targetClosestPoint = basePosition;
    }

    private float GetDistanceToCollider(GameObject targetGo)
    {
        if (targetGo == null) return float.MaxValue;
        
        Collider[] colliders = targetGo.GetComponentsInChildren<Collider>();
        if (colliders == null || colliders.Length == 0)
        {
            Vector3 fSelf = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 fTarget = new Vector3(targetGo.transform.position.x, 0f, targetGo.transform.position.z);
            return Vector3.Distance(fSelf, fTarget);
        }

        Vector3 flatSelf = new Vector3(transform.position.x, 0f, transform.position.z);
        float minDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
            if (col.isTrigger) continue; // Bỏ qua các trigger zone

            Vector3 closestPoint;
            MeshCollider meshCol = col as MeshCollider;
            if (meshCol != null && !meshCol.convex)
            {
                // Fallback cho MeshCollider không convex (vì Unity không hỗ trợ ClosestPoint)
                closestPoint = col.bounds.ClosestPoint(transform.position);
            }
            else
            {
                closestPoint = col.ClosestPoint(transform.position);
            }

            Vector3 flatClosest = new Vector3(closestPoint.x, 0f, closestPoint.z);
            float dist = Vector3.Distance(flatSelf, flatClosest);
            if (dist < minDistance)
            {
                minDistance = dist;
            }
        }

        if (minDistance == float.MaxValue)
        {
            Vector3 fSelf = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 fTarget = new Vector3(targetGo.transform.position.x, 0f, targetGo.transform.position.z);
            return Vector3.Distance(fSelf, fTarget);
        }

        return minDistance;
    }

    private Vector3 GetFormationPosition(Transform target, int index)
    {
        if (target == null) return transform.position;

        // Sử dụng điểm gần nhất tĩnh đã được tính toán sẵn
        Vector3 basePosition = targetClosestPoint;

        // Grid formation: 3 columns, spacing 1.5m
        int columns = 3;
        float spacing = 1.5f;
        int row = index / columns;
        int col = index % columns;

        float offsetX = (col - (columns - 1) * 0.5f) * spacing;
        float offsetZ = -row * spacing; // Đứng lùi về phía sau hướng tiếp cận

        Vector3 dir = (basePosition - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            dir.Normalize();
        }
        else
        {
            dir = Vector3.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(dir);
        Vector3 localOffset = new Vector3(offsetX, 0f, offsetZ);
        Vector3 worldOffset = rotation * localOffset;

        Vector3 targetPos = basePosition + worldOffset;

        // Tăng bán kính sample lên 3f để chắc chắn tìm thấy điểm NavMesh sát rìa tháp/nhà
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return targetPos;
    }

    public void PlayAttackAnimation()
    {
        if (animator == null) return;

        string paramToSet = (attackType == EnemyAttackType.Ranged) ? shootBoolParam : attackBoolParam;
        if (!string.IsNullOrWhiteSpace(paramToSet))
        {
            if (HasAnimatorParameter(animator, paramToSet, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(paramToSet);
            }
            else
            {
                StartCoroutine(TriggerBoolAnimation(paramToSet));
            }
        }
    }

    private bool HasAnimatorParameter(Animator anim, string paramName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }
        return false;
    }

    private System.Collections.IEnumerator TriggerBoolAnimation(string paramName)
    {
        animator.SetBool(paramName, true);
        yield return new WaitForSeconds(0.1f);
        animator.SetBool(paramName, false);
    }

    private void SetDestination(Vector3 dest)
    {
        if (agent == null) return;
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else return;
        }

        // Chỉ set destination khi điểm đích mới khác biệt đáng kể (>0.25m) so với điểm đích hiện tại
        if (Vector3.Distance(agent.destination, dest) > 0.25f)
        {
            agent.SetDestination(dest);
        }
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        bool isMoving = false;

        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                bool hasMeaningfulPath = !agent.isStopped && agent.hasPath && !agent.pathPending && agent.remainingDistance > agent.stoppingDistance + moveThreshold;
                bool hasMeaningfulVelocity = !agent.isStopped && agent.velocity.sqrMagnitude > moveThreshold * moveThreshold;
                isMoving = hasMeaningfulPath || hasMeaningfulVelocity;
            }
            else
            {
                isMoving = !agent.isStopped && agent.desiredVelocity.sqrMagnitude > moveThreshold * moveThreshold;
            }
        }

        animator.SetBool(moveBoolParam, isMoving);

        if (debugLogs && Time.time >= nextDebugLogTime)
        {
            nextDebugLogTime = Time.time + debugLogInterval;
            Debug.LogFormat(
                "[EnemyAI] move={0} chaseTarget={1} hasPath={2} pathPending={3} remainingDistance={4:F2} velocity={5:F2} desiredVelocity={6:F2}",
                isMoving,
                chaseTarget != null ? chaseTarget.name : "none",
                agent != null && agent.hasPath,
                agent != null && agent.pathPending,
                agent != null ? agent.remainingDistance : -1f,
                agent != null ? agent.velocity.magnitude : -1f,
                agent != null ? agent.desiredVelocity.magnitude : -1f
            );
        }
    }

    public void Knockback(Vector3 direction, float distance, float duration)
    {
        StartCoroutine(KnockbackRoutine(direction, distance, duration));
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 direction, float distance, float duration)
    {
        if (agent != null)
        {
            agent.enabled = false;
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * distance;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, distance, NavMesh.AllAreas))
        {
            targetPos = hit.position;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
        }
    }

    private static bool SafeCompareTag(GameObject go, string tag)
    {
        if (go == null) return false;
        try
        {
            // So sánh trực tiếp chuỗi tag để tránh native error log của Unity trong Console
            return string.Equals(go.tag, tag, System.StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool SafeCompareTag(Transform t, string tag)
    {
        if (t == null) return false;
        return SafeCompareTag(t.gameObject, tag);
    }

    private static GameObject FindGameObjectWithSafeTag(string tag)
    {
        try
        {
            return GameObject.FindGameObjectWithTag(tag);
        }
        catch
        {
            // Fallback: tìm theo tên
            GameObject[] allGo = GameObject.FindObjectsOfType<GameObject>();
            foreach (var go in allGo)
            {
                if (go != null && go.activeInHierarchy)
                {
                    string nameLower = go.name.ToLower();
                    if (nameLower.Contains("villagecenter") || nameLower.Contains("mainhouse") || nameLower == "main")
                    {
                        return go;
                    }
                }
            }
            return null;
        }
    }
}

