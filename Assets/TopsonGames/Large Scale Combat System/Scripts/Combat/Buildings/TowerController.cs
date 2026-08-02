namespace TopsonGames
{
    using UnityEngine;
    using TopsonGames.Minimap;

    [RequireComponent(typeof(DestructibleTarget))]
    [RequireComponent(typeof(EngagementTrigger))]
    public class TowerController : MonoBehaviour, IMinimapTrackable
    {
        [Tooltip("The team that should attack this tower.")]
        public int enemyTeamID = 0;

        [Header("Attack Settings")]
        [Tooltip("The range of the attack.")]
        public float attackRange = 15f;
        [Tooltip("The damage caused by each attack.")]
        public float attackDamage = 5f;
        [Tooltip("The time in seconds between attacks.")]
        public float attackCooldown = 5f;
        public enum AttackType
        {
            Damage, Healing
        }
        public AttackType attackType = AttackType.Damage;

        [Header("Visuals")]
        [Tooltip("(Optional) A particle effect that plays when attacking.")]
        public GameObject attackEffectPrefab;
        [Tooltip("The point at which the attack effect occurs.")]
        public Transform attackOrigin;
        public Sprite minimapIcon;

        private DestructibleTarget destructibleTarget;
        private EngagementTrigger engagementTrigger;
        private float attackTimer;

        private void Awake()
        {
            destructibleTarget = GetComponent<DestructibleTarget>();
            engagementTrigger = GetComponent<EngagementTrigger>();
        }

        private void Start()
        {
            destructibleTarget.Initialize(destructibleTarget.TeamID, destructibleTarget.maxHealth, this);
            engagementTrigger.Initialize(destructibleTarget.TeamID);

            if (MinimapController.instance != null)
                MinimapController.instance.RegisterTrackable(this);

            attackTimer = Random.Range(0f, attackCooldown);
        }

        private void Update()
        {
            if (destructibleTarget.currentHealth <= 0)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                PerformPulseAttack();
                attackTimer = attackCooldown;
            }
        }

        private void PerformPulseAttack()
        {

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, FormationController.instance.unitMask);

            if(hitColliders.Length > 0)
            {
                if (attackEffectPrefab != null && attackOrigin != null)
                {
                    Instantiate(attackEffectPrefab, attackOrigin.position, attackOrigin.rotation);
                }
            }
            foreach (var hitCollider in hitColliders)
            {
                DestructibleTarget target = hitCollider.GetComponent<DestructibleTarget>();

                if (target != null && target.TeamID == enemyTeamID)
                {
                    if (attackType == AttackType.Damage)
                        target.TakeDamage(attackDamage, null);
                    else
                        target.currentHealth += attackDamage;
                }
            }
        }
        private void OnDisable()
        {
            if (MinimapController.instance != null)
                MinimapController.instance.UnregisterTrackable(this);
        }
        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }

        public float GetWorldRotationY()
        {
            return transform.eulerAngles.y;
        }

        public bool IsVisibleOnMap()
        {
            return gameObject.activeInHierarchy;
        }

        public int GetTeamID()
        {
            return destructibleTarget.TeamID;
        }

        public Sprite GetMinimapIcon()
        {
            return minimapIcon;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

    }
}