using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private GameObject bloodVFXPrefab;
    [SerializeField] private Transform visualChild;
    [SerializeField] private Transform bloodTransform;

    public float CurrentHealth { get; set; }
    public float MaxHealth { get; set; }

    private bool isDead = false;

    /// <summary>
    /// Sự kiện tĩnh: bắn ra mỗi khi 1 con quái bất kỳ chết, kèm theo chính nó.
    /// RoKFirstRaidManager (hoặc bất kỳ hệ thống raid nào khác) đăng ký sự kiện này
    /// để biết khi nào toàn bộ đợt raid đã bị đánh bại hết.
    /// </summary>
    public static event System.Action<EnemyHealth> OnEnemyDied;

    private void Awake()
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;

        // Tự động tìm object con đầu tiên nếu chưa gán trong Inspector
        if (visualChild == null && transform.childCount > 0)
        {
            visualChild = transform.GetChild(0);
        }

        // Tự động tìm object con có tên chứa "blood"
        if (bloodTransform == null)
        {
            bloodTransform = FindChildRecursive(transform, "blood");
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(targetName.ToLower()))
            {
                return child;
            }
            Transform found = FindChildRecursive(child, targetName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        // Đã chết rồi thì bỏ qua, tránh OnDeath() bị gọi nhiều lần nếu trúng
        // nhiều đòn đánh trong cùng 1 frame (multi-hit, AOE...).
        if (isDead)
            return;

        CurrentHealth -= amount;

        Debug.Log($"{name} took {amount} damage at {hitPoint}. Current HP: {CurrentHealth}");

        if (bloodVFXPrefab != null)
        {
            // Lấy hướng và làm cha theo object con (visualChild) hoặc bản thân object này
            Quaternion vfxRotation = visualChild != null ? visualChild.rotation : transform.rotation;
            Transform parentTransform = visualChild != null ? visualChild : transform;

            // Ưu tiên vị trí của bloodTransform nếu tìm thấy, ngược lại dùng hitPoint
            Vector3 spawnPosition = bloodTransform != null ? bloodTransform.position : hitPoint;

            GameObject vfx = Instantiate(bloodVFXPrefab, spawnPosition, vfxRotation, parentTransform);

            // QUAN TRỌNG: bỏ làm con của xác quái trước khi xác bị Destroy(),
            // nếu không VFX máu sẽ bị xoá theo xác NGAY LẬP TỨC thay vì tồn tại đủ 1s.
            vfx.transform.SetParent(null, true);

            Destroy(vfx, 1f);
        }

        if (CurrentHealth <= 0f)
        {
            OnDeath();
        }
    }

    public void OnDeath()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log($"{name} died");

        // Báo cho các hệ thống khác (vd RoKFirstRaidManager) biết con quái này đã chết,
        // TRƯỚC khi bị Destroy, để chúng còn kịp đọc thông tin/tham chiếu tới nó.
        OnEnemyDied?.Invoke(this);

        Destroy(gameObject);
    }
}