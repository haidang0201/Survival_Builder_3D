using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private GameObject bloodVFXPrefab;
    [SerializeField] private Transform visualChild;

    public float CurrentHealth { get; set; }
    public float MaxHealth { get; set; }

    private void Awake()
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;

        // Tự động tìm object con đầu tiên nếu chưa gán trong Inspector
        if (visualChild == null && transform.childCount > 0)
        {
            visualChild = transform.GetChild(0);
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        CurrentHealth -= amount;

        Debug.Log($"{name} took {amount} damage at {hitPoint}. Current HP: {CurrentHealth}");

        if (bloodVFXPrefab != null)
        {
            // Lấy hướng và làm cha theo object con (visualChild) hoặc bản thân object này
            Quaternion vfxRotation = visualChild != null ? visualChild.rotation : transform.rotation;
            Transform parentTransform = visualChild != null ? visualChild : transform;

            GameObject vfx = Instantiate(bloodVFXPrefab, hitPoint, vfxRotation, parentTransform);
            Destroy(vfx, 1f);
        }

        if (CurrentHealth <= 0f)
        {
            OnDeath();
        }
    }

    public void OnDeath()
    {
        Debug.Log($"{name} died");
        Destroy(gameObject);
    }
}