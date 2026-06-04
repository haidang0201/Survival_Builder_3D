using UnityEngine;

public class Shield : MonoBehaviour, IDamageable
{
    [Header("Cấu hình Giảm sát thương")]
    [Tooltip("Phần trăm sát thương giảm đi (ví dụ: 0.2 nghĩa là giảm 20% sát thương)")]
    [Range(0f, 1f)]
    public float damageReductionPercent = 0.2f;

    [Header("Cấu hình Máu độc lập (Nếu không tìm thấy máu của công trình mẹ)")]
    [SerializeField] private float defaultMaxHealth = 100f;
    private float currentHealth;
    private float maxHealth;

    private IDamageable parentDamageable;

    private void Awake()
    {
        // Tìm component IDamageable ở công trình/đối tượng cha (loại trừ chính nó)
        parentDamageable = FindParentDamageable();
        if (parentDamageable == null)
        {
            maxHealth = defaultMaxHealth;
            currentHealth = maxHealth;
            Debug.Log($"[Shield] {name}: Không tìm thấy IDamageable của công trình cha. Khởi tạo máu độc lập.");
        }
    }

    private IDamageable FindParentDamageable()
    {
        Transform p = transform.parent;
        while (p != null)
        {
            IDamageable dmg = p.GetComponent<IDamageable>();
            if (dmg != null && dmg != (IDamageable)this)
            {
                return dmg;
            }
            p = p.parent;
        }
        return null;
    }

    public float CurrentHealth
    {
        get
        {
            if (parentDamageable != null) return parentDamageable.CurrentHealth;
            return currentHealth;
        }
        set
        {
            if (parentDamageable != null) parentDamageable.CurrentHealth = value;
            else currentHealth = value;
        }
    }

    public float MaxHealth
    {
        get
        {
            if (parentDamageable != null) return parentDamageable.MaxHealth;
            return maxHealth;
        }
        set
        {
            if (parentDamageable != null) parentDamageable.MaxHealth = value;
            else maxHealth = value;
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        float reducedAmount = amount * (1f - damageReductionPercent);
        Debug.Log($"[Shield] {name}: Khiên đã đỡ đòn! Sát thương gốc: {amount}, Sát thương sau giảm (giảm {(damageReductionPercent * 100f)}%): {reducedAmount}");

        if (parentDamageable != null)
        {
            parentDamageable.TakeDamage(reducedAmount, hitPoint);
        }
        else
        {
            currentHealth -= reducedAmount;
            Debug.Log($"[Shield] {name} nhận {reducedAmount} sát thương (độc lập). Máu còn lại: {currentHealth}");
            if (currentHealth <= 0f)
            {
                OnDeath();
            }
        }
    }

    public void OnDeath()
    {
        if (parentDamageable != null)
        {
            parentDamageable.OnDeath();
        }
        else
        {
            Debug.Log($"[Shield] {name} đã bị phá hủy (độc lập).");
            gameObject.SetActive(false);
        }
    }
}
