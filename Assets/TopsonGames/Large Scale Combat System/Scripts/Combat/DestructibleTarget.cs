namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.Events;

    public class DestructibleTarget : MonoBehaviour
    {
        public int TeamID = 0;
        public float maxHealth = 100f;
        public float currentHealth;
        [Tooltip("Which Type has this object")]
        public TargetType targetType = TargetType.Unit;

        public UnityEvent<DestructibleTarget> OnDeath;

        private Component ownerScript;
        private IDamageProcessor damageProcessor;

        private void Awake()
        {
            currentHealth = maxHealth;
            damageProcessor = GetComponent<IDamageProcessor>();
        }

        private void OnDisable()
        {
            if (TargetManager.instance != null)
            {
                TargetManager.instance.Unregister(this);
            }
        }

        public void Initialize(int teamID, float health, Component owner)
        {
            this.TeamID = teamID;
            this.maxHealth = health;
            this.currentHealth = health;
            this.ownerScript = owner;

            if (TargetManager.instance != null)
            {
                TargetManager.instance.Register(this);
            }
        }

        public void TakeDamage(float rawDamage, Unit attacker)
        {
            if (currentHealth <= 0) return;

            float finalDamage = rawDamage;

            if (damageProcessor != null)
            {
                finalDamage = damageProcessor.ProcessDamage(rawDamage, attacker);
            }

            if (finalDamage > 0)
            {
                currentHealth -= finalDamage;
                if (ownerScript is Unit unit)
                {
                    unit.OnRevieveDamage.Invoke();
                }
            }

            if (currentHealth <= 0)
            {
                OnDeath.Invoke(this);

                if (ownerScript is Unit deadUnit)
                {
                    deadUnit.HandleDeath(attacker);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public T GetOwner<T>() where T : Component
        {
            return ownerScript as T;
        }
    }
}