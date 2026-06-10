using UnityEngine;

public class WatchTowerAI : MonoBehaviour
{
    [Header("Bán kính Quét Quái (Layer: Enemy)")]
    public float detectRadius = 20f;
    public LayerMask enemyLayer;

    [Header("Bán kính Gọi Đồng Đội (Layer: DefenseTower)")]
    public float alertRadius = 15f;
    public LayerMask towerLayer;

    [Header("Tối ưu hiệu năng")]
    [Tooltip("Thời gian giãn cách giữa các lần quét (giây) để tránh nặng máy")]
    public float scanInterval = 0.2f;
    private float nextScanTime;

    private void Update()
    {
        // QUÉT KHÔNG PHÂN BIỆT NGÀY ĐÊM
        if (Time.time >= nextScanTime)
        {
            ScanAndAlert();
            nextScanTime = Time.time + scanInterval;
        }
    }

    private void ScanAndAlert()
    {
        Debug.Log($"[WatchTower] Scanning for enemies. detectRadius={detectRadius}, enemyLayer={enemyLayer.value}");
        // 1. Quét tìm quái vật xung quanh tháp canh
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectRadius, enemyLayer);
        Debug.Log($"[WatchTower] OverlapSphere returned {enemies.Length} colliders (layer)");

        System.Collections.Generic.List<Transform> validEnemies = new System.Collections.Generic.List<Transform>();
        foreach (var c in enemies)
        {
            if (c != null && c.gameObject.activeInHierarchy)
            {
                var health = c.GetComponentInParent<EnemyHealth>();
                Transform t = (health != null) ? health.transform : c.transform;
                if (!validEnemies.Contains(t))
                {
                    validEnemies.Add(t);
                }
            }
        }

        if (validEnemies.Count == 0)
        {
            // Fallback: nếu bạn chưa cấu hình LayerMask, thử tìm theo Tag "Enemy"
            Debug.Log("[WatchTower] No enemies via LayerMask — fallback to Tag 'Enemy'");
            GameObject[] tagged = null;
            try { tagged = GameObject.FindGameObjectsWithTag("Enemy"); } catch { tagged = null; }
            if (tagged != null && tagged.Length > 0)
            {
                foreach (var go in tagged)
                {
                    if (go != null && go.activeInHierarchy)
                    {
                        var health = go.GetComponentInParent<EnemyHealth>();
                        Transform t = (health != null) ? health.transform : go.transform;
                        float sqr = (t.position - transform.position).sqrMagnitude;
                        if (sqr <= detectRadius * detectRadius && !validEnemies.Contains(t))
                        {
                            validEnemies.Add(t);
                        }
                    }
                }
            }
        }

        // Sắp xếp các kẻ địch theo khoảng cách từ gần đến xa đối với tháp canh
        validEnemies.Sort((a, b) => {
            float distA = (a.position - transform.position).sqrMagnitude;
            float distB = (b.position - transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        });

        if (validEnemies.Count == 0) return; // Không có quái thì bỏ qua
        Debug.Log($"[WatchTower] Selected closest target: {validEnemies[0].name} out of {validEnemies.Count} valid enemies");

        // 2. Tìm các tháp tấn công nằm trong tầm báo động của tháp canh này
        Collider[] nearbyTowers = Physics.OverlapSphere(transform.position, alertRadius, towerLayer);
        Debug.Log($"[WatchTower] OverlapSphere for towers returned {nearbyTowers.Length} colliders (layer)");

        if (nearbyTowers.Length == 0)
        {
            Debug.Log("[WatchTower] No towers found via LayerMask — fallback to scanning all colliders in radius");
            // Fallback: tìm mọi collider trong bán kính rồi lọc những object có AttackTowerAI
            nearbyTowers = Physics.OverlapSphere(transform.position, alertRadius);
        }

        Debug.Log($"[WatchTower] Found {nearbyTowers.Length} nearby colliders to check for AttackTowerAI");
        System.Collections.Generic.List<AttackTowerAI> activeTowers = new System.Collections.Generic.List<AttackTowerAI>();
        foreach (var towerCollider in nearbyTowers)
        {
            AttackTowerAI attackTower = null;
            var parentTowers = towerCollider.GetComponentsInParent<AttackTowerAI>(true);
            foreach (var t in parentTowers)
            {
                if (t.enabled) { attackTower = t; break; }
            }

            if (attackTower == null)
            {
                var childTowers = towerCollider.GetComponentsInChildren<AttackTowerAI>(true);
                foreach (var t in childTowers)
                {
                    if (t.enabled) { attackTower = t; break; }
                }
            }

            if (attackTower != null && !activeTowers.Contains(attackTower))
            {
                activeTowers.Add(attackTower);
            }
        }

        // Sắp xếp các tháp theo khoảng cách tăng dần tới tháp canh để phân bổ mục tiêu ổn định
        activeTowers.Sort((a, b) => {
            float distA = (a.transform.position - transform.position).sqrMagnitude;
            float distB = (b.transform.position - transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        });

        int alerted = 0;
        System.Collections.Generic.List<Transform> availableEnemies = new System.Collections.Generic.List<Transform>(validEnemies);

        foreach (var attackTower in activeTowers)
        {
            if (validEnemies.Count > 0)
            {
                Transform assignedEnemy = null;
                if (availableEnemies.Count > 0)
                {
                    // Chọn kẻ địch gần tháp này nhất trong số các kẻ địch chưa bị tháp khác nhắm tới
                    float closestSqrDist = float.MaxValue;
                    int closestIndex = -1;
                    for (int i = 0; i < availableEnemies.Count; i++)
                    {
                        float dist = (availableEnemies[i].position - attackTower.transform.position).sqrMagnitude;
                        if (dist < closestSqrDist)
                        {
                            closestSqrDist = dist;
                            closestIndex = i;
                        }
                    }
                    if (closestIndex != -1)
                    {
                        assignedEnemy = availableEnemies[closestIndex];
                        availableEnemies.RemoveAt(closestIndex);
                    }
                }
                else
                {
                    // Nếu tất cả kẻ địch đều đã bị nhắm tới, tìm kẻ địch gần tháp này nhất trong toàn bộ danh sách
                    float closestSqrDist = float.MaxValue;
                    foreach (var enemy in validEnemies)
                    {
                        float dist = (enemy.position - attackTower.transform.position).sqrMagnitude;
                        if (dist < closestSqrDist)
                        {
                            closestSqrDist = dist;
                            assignedEnemy = enemy;
                        }
                    }
                }

                if (assignedEnemy != null)
                {
                    Debug.Log($"[WatchTower] Alerting tower '{attackTower.gameObject.name}' to attack closest enemy '{assignedEnemy.name}'");
                    attackTower.CommandAttack(assignedEnemy);
                    alerted++;
                }
            }
            else
            {
                attackTower.CommandAttack(null);
            }
        }

        if (alerted == 0)
        {
            // Last-resort fallback: tìm thẳng các AttackTowerAI trong scene (không cần Collider)
            Debug.Log("[WatchTower] No towers alerted via colliders — fallback to FindObjectsOfType<AttackTowerAI>()");
            var towers = FindObjectsOfType<AttackTowerAI>();
            System.Collections.Generic.List<AttackTowerAI> activeFallbackTowers = new System.Collections.Generic.List<AttackTowerAI>();
            foreach (var t in towers)
            {
                if (t != null && t.enabled)
                {
                    float sqr = (t.transform.position - transform.position).sqrMagnitude;
                    if (sqr <= alertRadius * alertRadius)
                    {
                        activeFallbackTowers.Add(t);
                    }
                }
            }

            // Sắp xếp các tháp theo khoảng cách tăng dần tới tháp canh
            activeFallbackTowers.Sort((a, b) => {
                float distA = (a.transform.position - transform.position).sqrMagnitude;
                float distB = (b.transform.position - transform.position).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            System.Collections.Generic.List<Transform> availableFallbackEnemies = new System.Collections.Generic.List<Transform>(validEnemies);

            foreach (var t in activeFallbackTowers)
            {
                if (validEnemies.Count > 0)
                {
                    Transform assignedEnemy = null;
                    if (availableFallbackEnemies.Count > 0)
                    {
                        float closestSqrDist = float.MaxValue;
                        int closestIndex = -1;
                        for (int i = 0; i < availableFallbackEnemies.Count; i++)
                        {
                            float dist = (availableFallbackEnemies[i].position - t.transform.position).sqrMagnitude;
                            if (dist < closestSqrDist)
                            {
                                closestSqrDist = dist;
                                closestIndex = i;
                            }
                        }
                        if (closestIndex != -1)
                        {
                            assignedEnemy = availableFallbackEnemies[closestIndex];
                            availableFallbackEnemies.RemoveAt(closestIndex);
                        }
                    }
                    else
                    {
                        float closestSqrDist = float.MaxValue;
                        foreach (var enemy in validEnemies)
                        {
                            float dist = (enemy.position - t.transform.position).sqrMagnitude;
                            if (dist < closestSqrDist)
                            {
                                closestSqrDist = dist;
                                assignedEnemy = enemy;
                            }
                        }
                    }

                    if (assignedEnemy != null)
                    {
                        Debug.Log($"[WatchTower] Fallback alerting active tower '{t.name}' to attack closest enemy '{assignedEnemy.name}'");
                        t.CommandAttack(assignedEnemy);
                        alerted++;
                    }
                }
                else
                {
                    t.CommandAttack(null);
                }
            }
            if (alerted == 0) Debug.Log("[WatchTower] Fallback found 0 active AttackTowerAI within alertRadius");
        }
    }

    // Vẽ vòng bán kính trong Editor để bạn dễ nhìn và chỉnh thông số
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius); // Vòng xanh: Tầm nhìn quét quái
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alertRadius);  // Vòng vàng: Tầm gọi tháp đồng đội
    }
}