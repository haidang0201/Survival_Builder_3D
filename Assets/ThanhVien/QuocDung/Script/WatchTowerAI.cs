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

        Transform targetEnemy = null;
        if (enemies.Length > 0)
        {
            Debug.Log($"[WatchTower] Found {enemies.Length} enemy colliders via LayerMask; selecting first");
            targetEnemy = enemies[0].transform;
        }
        else
        {
            // Fallback: nếu bạn chưa cấu hình LayerMask, thử tìm theo Tag "Enemy"
            Debug.Log("[WatchTower] No enemies via LayerMask — fallback to Tag 'Enemy'");
            GameObject[] tagged = null;
            try { tagged = GameObject.FindGameObjectsWithTag("Enemy"); } catch { tagged = null; }
            if (tagged != null && tagged.Length > 0)
            {
                Debug.Log($"[WatchTower] Found {tagged.Length} objects with Tag 'Enemy' — checking distance");
                float bestSqr = float.MaxValue;
                foreach (var go in tagged)
                {
                    float sqr = (go.transform.position - transform.position).sqrMagnitude;
                    if (sqr <= detectRadius * detectRadius && sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        targetEnemy = go.transform;
                    }
                }
                if (targetEnemy != null) Debug.Log($"[WatchTower] Fallback selected target {targetEnemy.name}");
            }
        }

        if (targetEnemy == null) return; // Không có quái thì bỏ qua
        Debug.Log($"[WatchTower] Selected target: {targetEnemy.name} at distance {(targetEnemy.position - transform.position).magnitude}");

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
        int alerted = 0;
        foreach (var towerCollider in nearbyTowers)
        {
            AttackTowerAI attackTower = towerCollider.GetComponentInParent<AttackTowerAI>();
            if (attackTower == null)
            {
                attackTower = towerCollider.GetComponentInChildren<AttackTowerAI>();
            }
            if (attackTower != null)
            {
                Debug.Log($"[WatchTower] Alerting tower '{towerCollider.name}'");
                attackTower.CommandAttack(targetEnemy);
                alerted++;
            }
            else
            {
                Debug.Log($"[WatchTower] Collider '{towerCollider.name}' has no AttackTowerAI component (in parent or children)");
            }
        }

        if (alerted == 0)
        {
            // Last-resort fallback: tìm thẳng các AttackTowerAI trong scene (không cần Collider)
            Debug.Log("[WatchTower] No towers alerted via colliders — fallback to FindObjectsOfType<AttackTowerAI>()");
            var towers = FindObjectsOfType<AttackTowerAI>();
            foreach (var t in towers)
            {
                if (t == null) continue;
                float sqr = (t.transform.position - transform.position).sqrMagnitude;
                if (sqr <= alertRadius * alertRadius)
                {
                    Debug.Log($"[WatchTower] Fallback alerting tower component '{t.name}'");
                    t.CommandAttack(targetEnemy);
                    alerted++;
                }
            }
            if (alerted == 0) Debug.Log("[WatchTower] Fallback found 0 AttackTowerAI within alertRadius");
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