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

    // =====================================================
    // UI CẢNH BÁO KẺ ĐỊCH (RoKEnemyAlertUI)
    // Khi tháp canh quét thấy ít nhất 1 kẻ địch trong detectRadius, banner
    // cảnh báo (icon nhấp nháy) sẽ tự hiện lên đầu màn hình. Khi không còn
    // kẻ địch nào trong tầm quét, banner tự ẩn.
    // Không cần gán field "alertUI" thủ công — nếu để trống, script sẽ tự
    // tìm qua RoKEnemyAlertUI.Instance (chỉ cần có 1 RoKEnemyAlertUI trong scene).
    // =====================================================
    [Header("UI CẢNH BÁO KẺ ĐỊCH")]
    [Tooltip("Kéo RoKEnemyAlertUI vào đây nếu muốn chỉ định rõ. Để trống -> tự dùng RoKEnemyAlertUI.Instance.")]
    public RoKEnemyAlertUI alertUI;

    [Tooltip("Nội dung hiển thị trên banner cảnh báo khi phát hiện kẻ địch.")]
    public string enemyAlertMessage = "⚠ Kẻ địch xuất hiện! Chuẩn bị tấn công!";

    // Nhớ trạng thái lần quét trước để chỉ gọi Show/Hide khi trạng thái THAY ĐỔI,
    // tránh gọi lặp lại liên tục mỗi 0.2s không cần thiết.
    private bool wasEnemyDetectedLastScan = false;

    private bool CanScan()
    {
        UpgradeableBuilding ub = GetComponent<UpgradeableBuilding>();
        if (ub == null) ub = GetComponentInParent<UpgradeableBuilding>();
        if (ub != null && (ub.IsInitialBuildNeeded || ub.IsUpgrading || ub.IsRuined)) return false;

        BuildingCtrl ctrl = GetComponent<BuildingCtrl>();
        if (ctrl == null) ctrl = GetComponentInParent<BuildingCtrl>();
        if (ctrl != null && !ctrl.IsBuilt) return false;

        HPTower hp = GetComponent<HPTower>();
        if (hp == null) hp = GetComponentInParent<HPTower>();
        if (hp != null && (hp.IsDestroyed || hp.CurrentHealth <= 0)) return false;

        return true;
    }

    private void Update()
    {
        if (!CanScan())
        {
            UpdateEnemyAlertUI(false);
            return;
        }

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
        validEnemies.Sort((a, b) =>
        {
            float distA = (a.position - transform.position).sqrMagnitude;
            float distB = (b.position - transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        });

        // Cập nhật banner UI cảnh báo dựa trên việc CÓ hay KHÔNG có kẻ địch trong
        // tầm quét lần này. Đặt ngay sau khi validEnemies đã sẵn sàng để không bỏ
        // sót nhánh "return" bên dưới khi danh sách rỗng.
        UpdateEnemyAlertUI(validEnemies.Count > 0);

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
        activeTowers.Sort((a, b) =>
        {
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
            activeFallbackTowers.Sort((a, b) =>
            {
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

    /// <summary>
    /// Bật/tắt banner cảnh báo trên UI theo trạng thái phát hiện kẻ địch của
    /// lần quét vừa rồi. Chỉ gọi ShowAlert()/HideAlert() khi trạng thái THAY
    /// ĐỔI so với lần quét trước, để tránh gọi lặp mỗi 0.2 giây không cần thiết.
    /// </summary>
    private void UpdateEnemyAlertUI(bool enemyDetected)
    {
        if (enemyDetected == wasEnemyDetectedLastScan)
            return;

        wasEnemyDetectedLastScan = enemyDetected;

        RoKEnemyAlertUI ui = alertUI != null ? alertUI : RoKEnemyAlertUI.Instance;

        if (ui == null)
        {
            if (enemyDetected)
                Debug.LogWarning("[WatchTower] Không tìm thấy RoKEnemyAlertUI trong scene — không hiện được banner cảnh báo. Hãy add script RoKEnemyAlertUI vào 1 GameObject trong scene.");

            return;
        }

        if (enemyDetected)
            ui.ShowAlert(enemyAlertMessage);
        else
            ui.HideAlert();
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