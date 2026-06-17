using UnityEngine;

public class HPTower : MonoBehaviour, IDamageable
{
    [Header("Cấu hình máu (HP)")]
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private GameObject destroyVFXPrefab; // Hiệu ứng khi công trình bị phá hủy (nếu có)

    public float CurrentHealth { get; set; }
    public float MaxHealth { get; set; }

    private bool isDestroyed = false;

    private void Awake()
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (isDestroyed) return;

        CurrentHealth -= amount;
        Debug.Log($"[HPTower] {gameObject.name} nhận {amount} sát thương tại {hitPoint}. HP còn lại: {CurrentHealth}/{MaxHealth}");

        // Kích hoạt hiệu ứng rung lắc hoặc va chạm nhẹ ở đây nếu cần thiết

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            OnDeath();
        }
    }

    public void OnDeath()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"[HPTower] {gameObject.name} đã bị phá hủy hoàn toàn!");

        // Tạo hiệu ứng phá hủy nếu có gán prefab
        if (destroyVFXPrefab != null)
        {
            GameObject vfx = Instantiate(destroyVFXPrefab, transform.position, transform.rotation);
            Destroy(vfx, 2f);
        }

        // Hủy đối tượng công trình
        Destroy(gameObject);
    }
}

