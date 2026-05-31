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
        GameObject arrow = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Debug.Log($"[AttackTowerAI] Spawned arrow '{projectilePrefab.name}' at {firePoint.position}");

        // Try to pass target to Arrow component so it moves smoothly
        var arrowComp = arrow.GetComponent<Arrow>();
        if (arrowComp != null && currentTarget != null)
        {
            arrowComp.SetTarget(currentTarget, projectileSpeed);
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

        // Tạo quả bom dội từ trên cao xuống ngay đỉnh đầu của quái (Vị trí Y của quái cộng thêm 12 mét)
        Vector3 spawnPos = currentTarget.position + Vector3.up * 12f;

        GameObject bomb = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[AttackTowerAI] Spawned AoE bomb '{projectilePrefab.name}' at {spawnPos}");

        // Gợi ý: Bạn nên gắn một script xử lý Rơi tự do (Rigidbody) hoặc Di chuyển xuống dưới 
        // lên Prefab quả bom để khi chạm đất nó tạo sát thương lan (AoE Explosion).
    }
}