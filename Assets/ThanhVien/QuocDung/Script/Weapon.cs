using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Weapon : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private float damage = 20f;
    [SerializeField, Tooltip("Duration (seconds) to consider the attack active. Matches your attack animation length if you don't use Animation Events.")]
    private float attackDuration = 0.6f;

    private Collider weaponCollider;
    private bool isAttacking;
    private readonly HashSet<int> hitTargets = new HashSet<int>();

    private void Awake()
    {
        weaponCollider = GetComponent<Collider>();
        weaponCollider.isTrigger = true;
        weaponCollider.enabled = false;

        if (animator == null)
            animator = GetComponentInParent<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartAttack();
        }
    }

    public void StartAttack()
    {
        isAttacking = true;
        hitTargets.Clear();
        Debug.Log($"[Weapon] StartAttack called");
        if (animator != null)
            animator.SetTrigger(attackTriggerName);

        // Automatically enable hitbox right away (no need for Animation Event)
        StopAllCoroutines();
        EnableHitbox();
        StartCoroutine(AttackTimeout());
    }

    private System.Collections.IEnumerator AttackTimeout()
    {
        // If the hitbox was enabled via animation event, this will simply
        // ensure it is disabled after the duration.
        yield return new WaitForSeconds(attackDuration);
        DisableHitbox();
    }

    public void EnableHitbox()
    {
        hitTargets.Clear();
        weaponCollider.enabled = true;
        Debug.Log($"[Weapon] EnableHitbox called - Collider enabled: {weaponCollider.enabled}");
    }

    public void DisableHitbox()
    {
        weaponCollider.enabled = false;
        isAttacking = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Weapon] OnTriggerEnter: {other.name}, isAttacking={isAttacking}, colliderEnabled={weaponCollider.enabled}");
        
        if (!isAttacking || !weaponCollider.enabled)
        {
            Debug.Log($"[Weapon] Early return: isAttacking={isAttacking}, colliderEnabled={weaponCollider.enabled}");
            return;
        }

        if (!other.CompareTag("Enemy"))
        {
            Debug.Log($"[Weapon] Not Enemy tag: {other.tag}");
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            Debug.Log($"[Weapon] No IDamageable found on {other.name}");
            return;
        }

        int targetId = other.transform.root.GetInstanceID();
        if (hitTargets.Contains(targetId))
        {
            Debug.Log($"[Weapon] Already hit target {other.name}");
            return;
        }

        hitTargets.Add(targetId);
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Debug.Log($"[Weapon] Dealing {damage} damage to {other.name}");
        damageable.TakeDamage(damage, hitPoint);
    }
}
