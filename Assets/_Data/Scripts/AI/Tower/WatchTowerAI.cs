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
        // 1. Quét tìm quái vật xung quanh tháp canh
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectRadius, enemyLayer);
        if (enemies.Length == 0) return; // Không có quái thì bỏ qua

        // Lấy con quái đầu tiên làm mục tiêu ưu tiên để chỉ định cho đồng đội
        Transform targetEnemy = enemies[0].transform;

        // 2. Tìm các tháp tấn công nằm trong tầm báo động của tháp canh này
        Collider[] nearbyTowers = Physics.OverlapSphere(transform.position, alertRadius, towerLayer);

        foreach (var towerCollider in nearbyTowers)
        {
            AttackTowerAI attackTower = towerCollider.GetComponent<AttackTowerAI>();
            if (attackTower != null)
            {
                // Truyền mục tiêu trực tiếp cho tháp cung hoặc pháo
                attackTower.CommandAttack(targetEnemy);
            }
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