using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; set; }
    public float MaxHealth { get; set; }

    private void Awake()
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        CurrentHealth -= amount;

        Debug.Log($"{name} took {amount} damage at {hitPoint}. Current HP: {CurrentHealth}");

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