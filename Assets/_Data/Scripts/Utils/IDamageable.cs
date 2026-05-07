public interface IDamageable
{
    float CurrentHealth { get; set; }
    float MaxHealth { get; set; }
    
    // amount: lượng máu trừ, hitPoint: điểm va chạm (để Tiến tạo effect rung cây)
    void TakeDamage(float amount, UnityEngine.Vector3 hitPoint);
    void OnDeath();
}