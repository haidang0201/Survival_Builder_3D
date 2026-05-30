using UnityEngine;

public enum AttackTowerType { Archer, Cannon }

public class AttackTowerAI : MonoBehaviour
{
    [Header("Cấu hình Loại Tháp")]
    public AttackTowerType towerType;
    public float fireRate = 1f;          // Tốc độ bắn (số phát / giây)
    public Transform firePoint;          // Kéo Object trống ở đầu nòng/họng pháo vào đây
    public GameObject projectilePrefab;  // Prefab Mũi tên (Arrow) hoặc Quả bom (Bomb)

    private Transform currentTarget;
    private float nextFireTime;

    // Hàm nhận lệnh tấn công do Tháp Canh truyền mục tiêu sang
    public void CommandAttack(Transform target)
    {
        currentTarget = target;
    }

    private void Update()
    {
        // Nếu không có mục tiêu được chỉ định từ tháp canh -> Bỏ qua không bắn
        if (currentTarget == null) return;

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
        if (currentTarget == null) return;

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

        // Reset mục tiêu sau mỗi phát bắn để bắt buộc Tháp Canh phải liên tục cập nhật quái mới ở Frame tiếp theo
        currentTarget = null;
    }

    private void SpawnArrow()
    {
        if (projectilePrefab == null || firePoint == null) return;

        // Tạo mũi tên tại vị trí đầu nòng cung
        GameObject arrow = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Hướng mũi tên quay thẳng về phía quái
        arrow.transform.LookAt(currentTarget);

        // Gợi ý: Bạn nên gắn script di chuyển thẳng tịnh tiến (MoveForward) lên Prefab mũi tên để nó tự lao đi.
    }

    private void SpawnAoEBomb()
    {
        if (projectilePrefab == null || firePoint == null) return;

        // Tạo quả bom dội từ trên cao xuống ngay đỉnh đầu của quái (Vị trí Y của quái cộng thêm 12 mét)
        Vector3 spawnPos = currentTarget.position + Vector3.up * 12f;

        GameObject bomb = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Gợi ý: Bạn nên gắn một script xử lý Rơi tự do (Rigidbody) hoặc Di chuyển xuống dưới 
        // lên Prefab quả bom để khi chạm đất nó tạo sát thương lan (AoE Explosion).
    }
}