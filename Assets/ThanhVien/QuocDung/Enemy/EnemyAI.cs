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

    [Header("Move Animation")]
    public float moveThreshold = 0.1f;

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

    // Static variables for group coordination and target sharing
    private static List<EnemyAI> globalActiveEnemies = new List<EnemyAI>();
    public List<EnemyAI> squadEnemies;
    private List<EnemyAI> ActiveEnemiesList => squadEnemies != null ? squadEnemies : globalActiveEnemies;
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
            agent.angularSpeed = 360f;
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

        UpdateAnimationState();
    }

    private void Update()
    {
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

            int myIndex = ActiveEnemiesList.IndexOf(this);
            if (myIndex < 0) myIndex = 0;

            Vector3 dest = GetFormationPosition(target, myIndex);
            
            // Calculate distance to boundary of target collider
            float distToTarget = GetDistanceToCollider(target.gameObject);

            if (agent != null)
            {
                agent.speed = chaseSpeed;
                
                if (IsMarching())
                {
                    EnemyAI leader = GetMarchLeader();
                    if (leader != null && leader != this)
                    {
                        // Follow the march leader in formation
                        Vector3 marchDest = GetMarchFormationPosition(leader);
                        agent.isStopped = false;
                        SetDestination(marchDest);
                    }
                    else
                    {
                        // Leader behavior or far-away follower: move to target formation position independently
                        float actualAttackRange = CurrentAttackRange;
                        if (attackType == EnemyAttackType.Melee && 
                            (target.CompareTag("Main") || IsMainHouse(target.gameObject) || target.CompareTag("Tower") || IsTower(target.gameObject)))
                        {
                            actualAttackRange = 1.0f;
                        }

                        if (distToTarget <= actualAttackRange)
                        {
                            agent.isStopped = true;
                            
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
                    // Combat state: split up and attack individual targets
                    float actualAttackRange = CurrentAttackRange;

                    if (distToTarget <= actualAttackRange)
                    {
                        agent.isStopped = true;
                        
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
        }
        else
        {
            if (agent != null) agent.isStopped = true;
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
        if (newTarget != null && newTarget != villageCenter && !newTarget.CompareTag("Main"))
        {
            targetAssignments[newTarget] = this;
        }
    }

    private void FindIndividualTarget()
    {
        // 1. Giữ mục tiêu hiện tại (đánh đến cùng) cho đến khi mục tiêu chết hoặc biến mất
        // CHỈ giữ mục tiêu nếu đó là Soldier hoặc Tower hoặc nếu ta muốn test đánh trực tiếp Main.
        // Nếu đang target Main/villageCenter (đang hành quân) và không bật test đánh trực tiếp Main,
        // vẫn cho phép quét tìm Soldier hoặc Tower xung quanh để tách ra tấn công.
        if (chaseTarget != null && chaseTarget.gameObject.activeInHierarchy)
        {
            bool isCurrentTargetMain = chaseTarget.CompareTag("Main") || chaseTarget == villageCenter || IsMainHouse(chaseTarget.gameObject);
            
            if (!isCurrentTargetMain || attackMainDirectly)
            {
                IDamageable damageable = chaseTarget.GetComponentInParent<IDamageable>();
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

        float searchRadius = chaseTriggerRange * 3f;
        Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius);

        Transform selected = null;

        // Nếu người dùng bật tùy chọn test đánh thẳng Main
        if (attackMainDirectly)
        {
            Transform bestMain = null;
            float minMainDist = float.MaxValue;

            foreach (var col in colliders)
            {
                if (col == null || !col.gameObject.activeInHierarchy) continue;
                GameObject go = col.gameObject;

                if (IsMainHouse(go))
                {
                    IDamageable damageable = go.GetComponentInParent<IDamageable>();
                    if (damageable != null && damageable.CurrentHealth <= 0f) continue;

                    Transform t = GetEntityRoot(go, "Main");
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

        foreach (var col in colliders)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;
            GameObject go = col.gameObject;

            // Kiểm tra xem mục tiêu có còn sống không
            IDamageable damageable = go.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.CurrentHealth <= 0f) continue;

            if (IsSoldier(go))
            {
                Transform t = GetEntityRoot(go, "Soldier");
                if (t != null && !aliveSoldiers.Contains(t))
                {
                    aliveSoldiers.Add(t);
                }
            }
            else if (IsTower(go))
            {
                Transform t = GetEntityRoot(go, "Tower");
                if (t != null && !aliveTowers.Contains(t))
                {
                    aliveTowers.Add(t);
                }
            }
            else if (IsMainHouse(go))
            {
                Transform t = GetEntityRoot(go, "Main");
                if (t != null)
                {
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
            }
        }
        // C. Nếu không còn Soldier và Tower nào, tiến hành đánh thẳng vào Main
        else
        {
            if (bestMainInScan != null)
            {
                selected = bestMainInScan;
            }
            else
            {
                // Fallback cuối cùng về villageCenter
                if (villageCenter == null)
                {
                    GameObject mainObj = GameObject.FindGameObjectWithTag("Main");
                    if (mainObj != null)
                    {
                        villageCenter = mainObj.transform;
                    }
                }

                if (villageCenter != null)
                {
                    IDamageable damageable = villageCenter.GetComponentInParent<IDamageable>();
                    if (damageable == null || damageable.CurrentHealth > 0f)
                    {
                        selected = villageCenter;
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

    private bool IsSoldier(GameObject go)
    {
        if (go == null) return false;
        return GetEntityRoot(go, "Soldier") != null;
    }

    private bool IsTower(GameObject go)
    {
        if (go == null) return false;
        return GetEntityRoot(go, "Tower") != null;
    }

    private bool IsMainHouse(GameObject go)
    {
        if (go == null) return false;
        return GetEntityRoot(go, "Main") != null;
    }

    private Transform GetEntityRoot(GameObject go, string tag)
    {
        if (go == null) return null;
        Transform curr = go.transform;
        while (curr != null)
        {
            if (curr.CompareTag(tag))
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

    private bool IsShooting()
    {
        if (attackType != EnemyAttackType.Ranged) return false;
        if (chaseTarget == null || !chaseTarget.gameObject.activeInHierarchy) return false;

        IDamageable damageable = chaseTarget.GetComponentInParent<IDamageable>();
        bool isAlive = damageable == null || damageable.CurrentHealth > 0f;
        if (!isAlive) return false;

        float distToTarget = GetDistanceToCollider(chaseTarget.gameObject);
        if (distToTarget > CurrentAttackRange) return false;

        if (agent != null && !agent.isStopped) return false;

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

    public bool IsMarching()
    {
        if (chaseTarget == null) return false;
        bool isMainTarget = chaseTarget == villageCenter || chaseTarget.CompareTag("Main") || IsMainHouse(chaseTarget.gameObject);
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

