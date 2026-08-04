using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform villageCenter;
    public Animator animator;

    [Header("Attack Button Settings")]
    [Tooltip("Kéo thả nút Tấn công (Button / Canvas) từ Hierarchy vào ô này.")]
    public GameObject attackButtonUI;
    [Tooltip("Tên scene battle sẽ chuyển sang khi bấm nút.")]
    public string battleSceneName = "SceneBattle";
    [Tooltip("Góc xoay bù cho nút UI (Ví dụ X:0, Y:0, Z:90 để xoay ngang nút lại).")]
    public Vector3 buttonRotationOffset = new Vector3(0, 0, 90);

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
    public float projectileSpawnDelay = 0.35f;

    public float CurrentAttackRange => (attackType == EnemyAttackType.Ranged) ? rangedAttackRange : attackRange;

    [Header("Animation")]
    public string moveBoolParam = "IsMove";
    public string attackBoolParam = "IsAttack";
    public string shootBoolParam = "IsShoot";

    [Header("Debug")]
    public bool debugLogs = true;
    public float debugLogInterval = 1f;
    public bool attackMainDirectly = false; // Checkbox để test đánh trực tiếp Main

    [Header("Exit Play Mode Config")]
    [Tooltip("Khi bật, nếu trên bản đồ không còn bất kỳ tháp, nhà chính hay công trình nào thì game sẽ tự động Exit chế độ Play.")]
    public bool exitPlayModeWhenNoBuildings = false;

    [Header("Move Animation")]
    public float moveThreshold = 0.1f;

    public static bool HasAnyActiveBuildings()
    {
        // 1. Kiểm tra các tháp có HPTower
        HPTower[] towers = Object.FindObjectsByType<HPTower>(FindObjectsSortMode.None);
        foreach (var t in towers)
        {
            if (t != null && t.gameObject.activeInHierarchy && !t.IsDestroyed && t.CurrentHealth > 0f)
                return true;
        }

        // 2. Kiểm tra các công trình có UpgradeableBuilding
        UpgradeableBuilding[] ubs = Object.FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
        foreach (var ub in ubs)
        {
            if (ub != null && ub.gameObject.activeInHierarchy && !ub.IsRuined)
                return true;
        }

        // 3. Kiểm tra các GameObject theo Tag
        string[] buildingTags = new string[] { "Main", "Tower", "DefenseTower" };
        foreach (string tag in buildingTags)
        {
            try
            {
                GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
                foreach (var go in objs)
                {
                    if (go != null && go.activeInHierarchy)
                    {
                        IDamageable d = go.GetComponentInParent<IDamageable>();
                        if (d == null || d.CurrentHealth > 0f)
                            return true;
                    }
                }
            }
            catch { }
        }

        return false;
    }

    [Header("Marching Settings")]
    public float marchSpacingX = 1.5f;
    public float marchSpacingZ = 1.5f;
    public int marchColumns = 3;
    public float marchMergeDistance = 10f;

    private NavMeshAgent agent;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;
    private float nextRepathTime;
    private Transform chaseTarget;
    private float nextDebugLogTime;
    private Vector3 targetClosestPoint;
    private Transform lastChaseTarget;
    private float nextTargetClosestPointUpdateTime;

    // Biến tối ưu di chuyển mượt & chống giật (smooth movement & hysteresis)
    private bool isAttackingTarget = false;
    private Vector3 lastSetDestination;
    private float nextSetDestinationTime;
    private Transform lastDestinationTarget;
    private Vector3 cachedTargetDestination;
    private float nextTargetDestCacheTime;

    // Static variables for group coordination and target sharing
    private static List<EnemyAI> globalActiveEnemies = new List<EnemyAI>();
    public List<EnemyAI> squadEnemies;
    private List<EnemyAI> ActiveEnemiesList => squadEnemies != null ? squadEnemies : globalActiveEnemies;
    private static Dictionary<Transform, EnemyAI> targetAssignments = new Dictionary<Transform, EnemyAI>();
    private float myNextScanTime;
    private static float scanInterval = 0.2f;

    [Header("Combat State Control")]
    public bool isCombatActive = false;
    public bool isWaitingAtTarget = false;
    private UIEnemyWaveButton spawnedAttackButton;

    public void EnableCombat()
    {
        if (isCombatActive) return;
        isCombatActive = true;
        isWaitingAtTarget = false;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (squadEnemies != null)
        {
            foreach (var squadEnemy in squadEnemies)
            {
                if (squadEnemy != null && squadEnemy != this && !squadEnemy.isCombatActive)
                {
                    squadEnemy.EnableCombat();
                }
            }
        }
    }

    public bool IsLeader()
    {
        if (squadEnemies == null || squadEnemies.Count == 0) return true;
        for (int i = 0; i < squadEnemies.Count; i++)
        {
            var e = squadEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy)
            {
                return e == this;
            }
        }
        return true;
    }

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
        if (!globalActiveEnemies.Contains(this))
        {
            globalActiveEnemies.Add(this);
        }
        if (squadEnemies != null && !squadEnemies.Contains(this))
        {
            squadEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        globalActiveEnemies.Remove(this);
        if (squadEnemies != null)
        {
            squadEnemies.Remove(this);
        }
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
        globalActiveEnemies.Remove(this);
        if (squadEnemies != null)
        {
            squadEnemies.Remove(this);
        }
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
            agent.angularSpeed = 720f;
            agent.acceleration = 30f;
            agent.autoBraking = false;
        }
        
        // Register this enemy to start marching
        if (!globalActiveEnemies.Contains(this))
        {
            globalActiveEnemies.Add(this);
        }
        if (squadEnemies != null && !squadEnemies.Contains(this))
        {
            squadEnemies.Add(this);
        }

        SetupAttackButton();

        UpdateAnimationState();
    }

    private void LateUpdate()
    {
        // Billboard effect: Giúp nút UI luôn quay hướng thẳng song song với Camera (chỉ cho Thủ Lĩnh)
        if (IsLeader() && attackButtonUI != null && attackButtonUI.activeSelf)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                attackButtonUI.transform.rotation = mainCam.transform.rotation * Quaternion.Euler(buttonRotationOffset);
            }
        }
    }

    public void SetupAttackButton()
    {
        if (attackButtonUI != null)
        {
            // Ban đầu ẩn nút đi, chỉ khi tới mục tiêu mới hiện lên
            attackButtonUI.SetActive(false);

            UnityEngine.UI.Button btn = attackButtonUI.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = attackButtonUI.GetComponentInChildren<UnityEngine.UI.Button>();

            if (btn != null)
            {
                btn.onClick.RemoveListener(OnAttackButtonClicked);
                btn.onClick.AddListener(OnAttackButtonClicked);
            }
        }
    }

    public void OnAttackButtonClicked()
    {
        Time.timeScale = 1f;
        int waveCount = (squadEnemies != null && squadEnemies.Count > 0) ? squadEnemies.Count : 1;
        BattleData.RecordCurrentSceneState(waveCount);

        Debug.Log($"[EnemyAI] Bấm nút Tấn Công (Wave = {waveCount} Enemy) -> Đang chuyển sang Scene: {battleSceneName}");
        if (!string.IsNullOrEmpty(battleSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
        }
        else
        {
            Debug.LogError("[EnemyAI] Chưa cài đặt tên battleSceneName!");
        }
    }

    private void Update()
    {
        // 1. Kiểm tra tùy chọn tự động thoát chế độ Play khi không còn công trình (chỉ ở scene chính)
        if (exitPlayModeWhenNoBuildings && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SceneBattle" && !HasAnyActiveBuildings())
        {
            Debug.Log("[EnemyAI] 🔥 Không còn bất kỳ tháp/công trình nào trên bản đồ! Tự động thoát chế độ Play.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        // Keep list clean of destroyed objects
        ActiveEnemiesList.RemoveAll(e => e == null);

        if (ActiveEnemiesList.Count == 0) return;

        // Auto-find villageCenter using tag "Main" if null
        if (villageCenter == null)
        {
            GameObject mainObj = GameObject.FindGameObjectWithTag("Main");
            if (mainObj != null)
            {
                villageCenter = mainObj.transform;
            }
        }

        // --- BƯỚC 1: Xử lý khi chưa vào trạng thái Giao Tranh (isCombatActive == false) ---
        if (!isCombatActive)
        {
            if (villageCenter != null)
            {
                float distToMain = GetDistanceToCollider(villageCenter.gameObject);
                float stopRange = Mathf.Max(CurrentAttackRange, 2.5f);

                if (distToMain <= stopRange)
                {
                    // Đến mục tiêu (Main) -> Đứng yên
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                    }
                    isWaitingAtTarget = true;

                    // Hiện nút Tấn công CHỈ KHI là con Thủ Lĩnh (Leader) và đã đến mục tiêu
                    if (IsLeader() && attackButtonUI != null && !attackButtonUI.activeSelf)
                    {
                        attackButtonUI.SetActive(true);
                    }
                    else if (!IsLeader() && attackButtonUI != null && attackButtonUI.activeSelf)
                    {
                        attackButtonUI.SetActive(false);
                    }
                }
                else
                {
                    // Tiếp tục hành quân đến Nhà Chính
                    isWaitingAtTarget = false;

                    // Ẩn nút Tấn công khi đang hành quân
                    if (attackButtonUI != null && attackButtonUI.activeSelf)
                    {
                        attackButtonUI.SetActive(false);
                    }

                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.speed = chaseSpeed;
                        Vector3 moveDest = GetTargetDestination(villageCenter);
                        SetDestination(moveDest);
                    }
                }
            }

            UpdateAnimationState();
            return; // Đứng yên / Di chuyển tới nhà chính, KHÔNG tự động tấn công
        }

        // --- BƯỚC 2: Khi đã vào trạng thái Giao Tranh (isCombatActive == true) ---
        FindIndividualTarget();

        // Target to move/attack
        Transform target = chaseTarget;

        if (target != null)
        {
            // Calculate distance to boundary of target collider
            float distToTarget = GetDistanceToCollider(target.gameObject);

            agent.speed = chaseSpeed;
            
            float actualAttackRange = CurrentAttackRange;
            if (attackType == EnemyAttackType.Melee && (SafeCompareTag(target.gameObject, "Main") || IsMainHouse(target.gameObject) || SafeCompareTag(target.gameObject, "Tower") || SafeCompareTag(target.gameObject, "DefenseTower") || IsTower(target.gameObject)))
            {
                actualAttackRange = Mathf.Max(actualAttackRange, 2.5f);
            }

            // Hysteresis để ngăn việc bật/tắt isStopped làm giật/lết hoạt cảnh ở ranh giới tầm đánh
            float stopThreshold = actualAttackRange;
            float resumeThreshold = actualAttackRange + 0.6f;

            bool shouldAttack = isAttackingTarget ? (distToTarget <= resumeThreshold) : (distToTarget <= stopThreshold);
            isAttackingTarget = shouldAttack;

            if (shouldAttack)
            {
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped) agent.isStopped = true;
                
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
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && agent.isStopped) agent.isStopped = false;
                Vector3 moveDest = GetTargetDestination(target);
                SetDestination(moveDest);
            }
        }
        else
        {
            isAttackingTarget = false;
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
        }

        UpdateAnimationState();
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
            IDamageable damageable = kvp.Key.GetComponentInParent<IDamageable>();
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
        if (newTarget != null && newTarget != villageCenter && !SafeCompareTag(newTarget.gameObject, "Main") && !IsMainHouse(newTarget.gameObject))
        {
            targetAssignments[newTarget] = this;
        }
    }

    private Transform FindMainTarget()
    {
        // Tìm tất cả GameObject có tag "Main" trong toàn bộ Scene (không bị giới hạn bởi bán kính quét)
        GameObject[] mainObjs = null;
        try { mainObjs = GameObject.FindGameObjectsWithTag("Main"); } catch { mainObjs = null; }
        Transform bestMain = null;
        float minDist = float.MaxValue;

        if (mainObjs != null && mainObjs.Length > 0)
        {
            foreach (var go in mainObjs)
            {
                if (go == null || !go.activeInHierarchy) continue;

                // Kiểm tra xem mục tiêu Main này có HP và còn sống không
                IDamageable damageable = go.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.CurrentHealth <= 0f) continue;

                Transform root = GetEntityRoot(go, "Main");
                Transform t = (root != null) ? root : go.transform;

                float dist = GetDistanceToCollider(go);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestMain = t;
                }
            }
        }

        // Fallback dùng villageCenter nếu chưa tìm thấy và villageCenter còn sống
        if (bestMain == null && villageCenter != null && villageCenter.gameObject.activeInHierarchy)
        {
            IDamageable damageable = villageCenter.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.CurrentHealth > 0f)
            {
                bestMain = villageCenter;
            }
        }

        return bestMain;
    }

    public Transform GetCurrentTarget()
    {
        // 1. Nếu đang có chaseTarget hợp lệ (Watch Tower, Lính, Nhà Chính...)
        if (chaseTarget != null && chaseTarget.gameObject.activeInHierarchy)
        {
            IDamageable d = chaseTarget.GetComponentInParent<IDamageable>();
            if (d == null || d.CurrentHealth > 0f)
            {
                return chaseTarget;
            }
        }

        // 2. Tìm các tháp (Watch Tower, Defence Tower...) gần nhất trên đường đi
        float searchRadius = chaseTriggerRange * 4f;
        Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius);
        Transform closestTower = null;
        float minTowerDist = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;
            GameObject go = col.gameObject;

            if (IsTower(go))
            {
                IDamageable damageable = go.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.CurrentHealth <= 0f) continue;

                Transform t = GetEntityRoot(go, "Tower");
                if (t == null) t = go.transform;

                float dist = GetDistanceToCollider(go);
                if (dist < minTowerDist)
                {
                    minTowerDist = dist;
                    closestTower = t;
                }
            }
        }

        if (closestTower != null)
        {
            return closestTower;
        }

        // 3. Nếu villageCenter được gán thủ công và còn sống
        if (villageCenter != null && villageCenter.gameObject.activeInHierarchy)
        {
            IDamageable d = villageCenter.GetComponentInParent<IDamageable>();
            if (d == null || d.CurrentHealth > 0f)
            {
                return villageCenter;
            }
        }

        // 4. Fallback: Tìm Nhà Chính
        return FindMainTarget();
    }

    private Transform SelectClosestTargetFromList(List<Transform> targetList)
    {
        if (targetList == null || targetList.Count == 0) return null;

        Transform bestUnoccupied = null;
        float minUnoccupiedDist = float.MaxValue;

        foreach (var t in targetList)
        {
            if (t == null) continue;
            if (targetAssignments.TryGetValue(t, out EnemyAI assignedEnemy) && assignedEnemy != this)
                continue;

            float dist = GetDistanceToCollider(t.gameObject);
            if (dist < minUnoccupiedDist)
            {
                minUnoccupiedDist = dist;
                bestUnoccupied = t;
            }
        }

        if (bestUnoccupied != null) return bestUnoccupied;

        Transform bestAny = null;
        float minAnyDist = float.MaxValue;
        foreach (var t in targetList)
        {
            if (t == null) continue;
            float dist = GetDistanceToCollider(t.gameObject);
            if (dist < minAnyDist)
            {
                minAnyDist = dist;
                bestAny = t;
            }
        }
        return bestAny;
    }

    private void FindIndividualTarget()
    {
        // 1. Giữ mục tiêu hiện tại cho đến khi mục tiêu chết hoặc biến mất
        if (chaseTarget != null && chaseTarget.gameObject.activeInHierarchy)
        {
            IDamageable damageable = chaseTarget.GetComponentInParent<IDamageable>();
            bool isAlive = damageable == null || damageable.CurrentHealth > 0f;

            if (isAlive)
            {
                bool isCurrentTargetMain = SafeCompareTag(chaseTarget.gameObject, "Main") || chaseTarget == villageCenter || IsMainHouse(chaseTarget.gameObject);
                
                if (!isCurrentTargetMain || attackMainDirectly)
                {
                    AssignTarget(chaseTarget);
                    return;
                }
            }
            else
            {
                chaseTarget = null;
            }
        }

        if (Time.time < myNextScanTime) return;
        myNextScanTime = Time.time + scanInterval;

        CleanupTargetAssignments();

        float searchRadius = isCombatActive ? 250f : (chaseTriggerRange * 3f);
        Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius);

        Transform selected = null;

        if (attackMainDirectly)
        {
            selected = FindMainTarget();
            AssignTarget(selected);
            chaseTarget = selected;
            return;
        }

        // --- Tìm kiếm theo thứ tự ưu tiên tuyệt đối: Melee Soldier -> Archer -> Tower -> Main ---
        List<Transform> aliveMeleeSoldiers = new List<Transform>();
        List<Transform> aliveArcherSoldiers = new List<Transform>();
        List<Transform> aliveTowers = new List<Transform>();

        foreach (var col in colliders)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;
            GameObject go = col.gameObject;

            IDamageable damageable = go.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.CurrentHealth <= 0f) continue;

            if (IsSoldier(go))
            {
                Transform t = GetEntityRoot(go, "Soldier");
                if (t == null) t = go.transform;

                if (IsArcher(go))
                {
                    if (!aliveArcherSoldiers.Contains(t)) aliveArcherSoldiers.Add(t);
                }
                else
                {
                    if (!aliveMeleeSoldiers.Contains(t)) aliveMeleeSoldiers.Add(t);
                }
            }
            else if (IsTower(go))
            {
                Transform t = GetEntityRoot(go, "Tower");
                if (t == null) t = go.transform;
                if (!aliveTowers.Contains(t)) aliveTowers.Add(t);
            }
        }

        // Ưu tiên 1: Lính Cận Chiến (Melee Soldiers) - Đánh con ở gần trước
        if (aliveMeleeSoldiers.Count > 0)
        {
            selected = SelectClosestTargetFromList(aliveMeleeSoldiers);
        }
        // Ưu tiên 2: Lính Bắn Xa (Archer) - Đánh con ở gần trước
        else if (aliveArcherSoldiers.Count > 0)
        {
            selected = SelectClosestTargetFromList(aliveArcherSoldiers);
        }
        // Ưu tiên 3: Các Tháp (Towers) - Đánh tháp ở gần trước
        else if (aliveTowers.Count > 0)
        {
            selected = SelectClosestTargetFromList(aliveTowers);
        }
        // Fallback: Tìm trực tiếp UnitController trên toàn bản đồ
        if (selected == null && isCombatActive)
        {
            UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            float minDist = float.MaxValue;
            foreach (var u in units)
            {
                if (u != null && u.gameObject.activeInHierarchy)
                {
                    IDamageable d = u.GetComponentInParent<IDamageable>();
                    if (d != null && d.CurrentHealth <= 0f) continue;

                    float distSq = (u.transform.position - transform.position).sqrMagnitude;
                    if (distSq < minDist)
                    {
                        minDist = distSq;
                        selected = u.transform;
                    }
                }
            }
        }

        if (selected == null)
        {
            selected = FindMainTarget();
        }

        AssignTarget(selected);
        chaseTarget = selected;
    }

    private void ExecuteAttack(Transform target)
    {
        PlayAttackAnimation();

        if (attackType == EnemyAttackType.Melee)
        {
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage, target.position);
            }
        }
        else if (attackType == EnemyAttackType.Ranged)
        {
            StartCoroutine(SpawnProjectileDelayed(target, projectileSpawnDelay));
        }
    }

    private System.Collections.IEnumerator SpawnProjectileDelayed(Transform target, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target != null && target.gameObject.activeInHierarchy)
        {
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            bool isAlive = damageable == null || damageable.CurrentHealth > 0f;
            if (isAlive)
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
                    if (damageable != null)
                    {
                        damageable.TakeDamage(attackDamage, target.position);
                    }
                }
            }
        }
    }

    private static bool SafeCompareTag(GameObject go, string tagName)
    {
        if (go == null || string.IsNullOrEmpty(tagName)) return false;
        return go.tag == tagName;
    }

    private bool IsSoldier(GameObject go)
    {
        if (go == null) return false;
        if (SafeCompareTag(go, "Soldier")) return true;
        if (go.GetComponentInParent<HPSoldier>() != null) return true;
        return GetEntityRoot(go, "Soldier") != null;
    }

    private bool IsArcher(GameObject go)
    {
        if (go == null) return false;
        UnitController uc = go.GetComponentInParent<UnitController>();
        if (uc != null && uc.AttackMode == AttackMode.Ranged) return true;
        if (go.name.ToLower().Contains("archer")) return true;
        return false;
    }

    private bool IsMeleeSoldier(GameObject go)
    {
        if (!IsSoldier(go)) return false;
        return !IsArcher(go);
    }

    private bool IsTower(GameObject go)
    {
        if (go == null) return false;
        if (SafeCompareTag(go, "Main") || GetEntityRoot(go, "Main") != null) return false;
        if (SafeCompareTag(go, "Tower") || SafeCompareTag(go, "DefenseTower")) return true;
        if (go.GetComponentInParent<HPTower>() != null) return true;
        return GetEntityRoot(go, "Tower") != null;
    }

    private bool IsMainHouse(GameObject go)
    {
        if (go == null) return false;
        if (SafeCompareTag(go, "Main")) return true;
        return GetEntityRoot(go, "Main") != null;
    }

    private Transform GetEntityRoot(GameObject go, string tag)
    {
        if (go == null) return null;
        Transform curr = go.transform;
        while (curr != null)
        {
            if (SafeCompareTag(curr.gameObject, tag))
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
                if (attackType == EnemyAttackType.Ranged)
                {
                    animator.SetBool(paramToSet, true);
                }
                else
                {
                    StartCoroutine(TriggerBoolAnimation(paramToSet));
                }
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

    private Vector3 GetTargetDestination(Transform target)
    {
        if (target == null) return transform.position;

        // Nếu là Lính -> Di chuyển trực tiếp đến vị trí lính
        if (IsSoldier(target.gameObject))
        {
            return target.position;
        }

        // Cache vị trí đích của công trình trong 0.3s để tránh gọi GetComponentsInChildren mỗi frame
        if (target == lastDestinationTarget && Time.time < nextTargetDestCacheTime)
        {
            return cachedTargetDestination;
        }

        lastDestinationTarget = target;
        nextTargetDestCacheTime = Time.time + 0.3f;

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

        // Tìm điểm NavMesh hợp lệ gần nhất ở mép ngoài công trình
        if (NavMesh.SamplePosition(basePosition, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            cachedTargetDestination = hit.position;
        }
        else
        {
            cachedTargetDestination = basePosition;
        }

        return cachedTargetDestination;
    }

    private void SetDestination(Vector3 dest)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }

            if (agent.isOnNavMesh)
            {
                if (Time.time < nextSetDestinationTime && Vector3.Distance(lastSetDestination, dest) < 0.5f && agent.hasPath)
                {
                    return;
                }

                nextSetDestinationTime = Time.time + 0.2f;
                lastSetDestination = dest;
                agent.SetDestination(dest);
                return;
            }
        }

        // Fallback di chuyển trực tiếp Transform nếu không có NavMesh trong Scene
        float speed = chaseSpeed > 0.1f ? chaseSpeed : 3.5f;
        Vector3 moveDir = (dest - transform.position);
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 720f * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, transform.position + moveDir.normalized, speed * Time.deltaTime);
        }
    }

    private bool IsShooting()
    {
        if (attackType != EnemyAttackType.Ranged) return false;
        if (chaseTarget == null || !chaseTarget.gameObject.activeInHierarchy) return false;

        IDamageable damageable = chaseTarget.GetComponentInParent<IDamageable>();
        bool isAlive = damageable == null || damageable.CurrentHealth > 0f;
        if (!isAlive) return false;

        float distToTarget = GetDistanceToCollider(chaseTarget.gameObject);
        if (distToTarget > CurrentAttackRange) return false;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped) return false;

        return true;
    }

    private bool IsInShootState()
    {
        if (animator == null) return false;

        // Check if currently playing "Shoot" state
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Shoot"))
        {
            // If transitioning away from "Shoot" state, we are leaving it
            if (animator.IsInTransition(0))
            {
                var nextState = animator.GetNextAnimatorStateInfo(0);
                if (!nextState.IsName("Shoot"))
                {
                    return false;
                }
            }
            return true;
        }

        // Check if transitioning into "Shoot" state
        if (animator.IsInTransition(0))
        {
            var nextState = animator.GetNextAnimatorStateInfo(0);
            if (nextState.IsName("Shoot"))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        bool isMoving = false;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            bool hasMeaningfulPath = !agent.isStopped && agent.hasPath && !agent.pathPending && agent.remainingDistance > agent.stoppingDistance + moveThreshold;
            bool hasMeaningfulVelocity = !agent.isStopped && agent.velocity.sqrMagnitude > moveThreshold * moveThreshold;
            isMoving = hasMeaningfulPath || hasMeaningfulVelocity;
        }
        else
        {
            isMoving = false;
        }

        animator.SetBool(moveBoolParam, isMoving);

        if (!string.IsNullOrWhiteSpace(shootBoolParam))
        {
            bool isShooting = IsShooting();
            if (isShooting)
            {
                // Force the animator to stay in the "Shoot" state by looping it manually
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Shoot"))
                {
                    if (stateInfo.normalizedTime >= 0.9f && !animator.IsInTransition(0))
                    {
                        animator.Play("Shoot", 0, 0f);
                    }
                }
                else
                {
                    if (!animator.IsInTransition(0) || !animator.GetNextAnimatorStateInfo(0).IsName("Shoot"))
                    {
                        animator.Play("Shoot", 0, 0f);
                    }
                }

                // Set/Keep trigger or bool state active
                if (HasAnimatorParameter(animator, shootBoolParam, AnimatorControllerParameterType.Trigger))
                {
                    animator.SetTrigger(shootBoolParam);
                }
                else if (HasAnimatorParameter(animator, shootBoolParam, AnimatorControllerParameterType.Bool))
                {
                    animator.SetBool(shootBoolParam, true);
                }
            }
            else
            {
                // Reset trigger or bool state when not shooting
                if (HasAnimatorParameter(animator, shootBoolParam, AnimatorControllerParameterType.Trigger))
                {
                    animator.ResetTrigger(shootBoolParam);
                }
                else if (HasAnimatorParameter(animator, shootBoolParam, AnimatorControllerParameterType.Bool))
                {
                    animator.SetBool(shootBoolParam, false);
                }
            }
        }

        if (debugLogs && Time.time >= nextDebugLogTime)
        {
            nextDebugLogTime = Time.time + debugLogInterval;
            bool agentValid = agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
            Debug.LogFormat(
                "[EnemyAI] move={0} chaseTarget={1} hasPath={2} pathPending={3} remainingDistance={4:F2} velocity={5:F2} desiredVelocity={6:F2}",
                isMoving,
                chaseTarget != null ? chaseTarget.name : "none",
                agentValid && agent.hasPath,
                agentValid && agent.pathPending,
                agentValid ? agent.remainingDistance : -1f,
                agentValid ? agent.velocity.magnitude : -1f,
                agentValid ? agent.desiredVelocity.magnitude : -1f
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

    public bool IsMarching()
    {
        if (chaseTarget == null) return false;
        bool isMainTarget = chaseTarget == villageCenter || SafeCompareTag(chaseTarget.gameObject, "Main") || IsMainHouse(chaseTarget.gameObject);
        if (isMainTarget)
        {
            // If we are close to the main target (e.g. within 8m), we break formation and attack individually
            float distToTarget = GetDistanceToCollider(chaseTarget.gameObject);
            if (distToTarget <= 8.0f)
            {
                return false;
            }
            return true;
        }
        return false;
    }

    private EnemyAI GetMarchLeader()
    {
        foreach (var enemy in ActiveEnemiesList)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.IsMarching())
            {
                return enemy;
            }
        }
        return null;
    }

    private int GetMarchingIndex()
    {
        int index = 0;
        foreach (var enemy in ActiveEnemiesList)
        {
            if (enemy == this) return index;
            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.IsMarching())
            {
                index++;
            }
        }
        return index;
    }

    private Vector3 GetMarchFormationPosition(EnemyAI leader)
    {
        if (leader == null) return transform.position;

        Vector3 leaderPos = leader.transform.position;
        Vector3 leaderForward = leader.transform.forward;
        Vector3 leaderRight = leader.transform.right;

        int marchIndex = GetMarchingIndex();
        int row = marchIndex / marchColumns;
        int col = marchIndex % marchColumns;

        float offsetX = (col - (marchColumns - 1) * 0.5f) * marchSpacingX;
        float offsetZ = -row * marchSpacingZ;

        Vector3 targetPos = leaderPos + leaderRight * offsetX + leaderForward * offsetZ;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return targetPos;
    }
}

