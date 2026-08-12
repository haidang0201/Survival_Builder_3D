using UnityEngine;

namespace TopsonGames
{
    public class Projectile : MonoBehaviour
    {
        public float lifetime = 20f;
        public float ColliderTime = 1f;
        public Collider damageCollider;
        public Rigidbody rigidBody;
        public Formation parentFormation;

        float currentTime = 0f;
        void OnEnable()
        {
            transform.SetParent(null);
            CancelInvoke();
            Invoke("Deactivate", lifetime);
        }

        void Deactivate()
        {
            gameObject.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other == parentFormation.GetUnitCenterCollider())
                return;

            if (other.TryGetComponent<FormationCollider>(out FormationCollider collider))
            {
                if (collider.parentFormation.TeamID != parentFormation.TeamID)
                    collider.parentFormation.ReportArrows();
                return;
            }

            if (other.CompareTag("Unit"))
            {
                Unit hitUnit = other.GetComponent<Unit>();
                if (hitUnit)
                {
                    DestructibleTarget target = hitUnit.GetComponent<DestructibleTarget>();
                    if (target != null)
                        target.TakeDamage(GameManager.instance.damageModifier.GetTotalDamage(parentFormation.UnitData, hitUnit.GetFormation().UnitData), null);
                }
                gameObject.SetActive(false);
            }
            else if (other.CompareTag("Shield"))
            {
                Unit hitUnit = other.GetComponentInParent<Unit>();
                if (hitUnit && Random.Range(0f, 1f) >= hitUnit.GetFormation().UnitData.shieldArrowBlockChance)
                {
                    DestructibleTarget target = hitUnit.GetComponent<DestructibleTarget>();
                    if (target != null)
                        target.TakeDamage(GameManager.instance.damageModifier.GetTotalDamage(parentFormation.UnitData, hitUnit.GetFormation().UnitData), null);
                }
                gameObject.SetActive(false);
            }
        }

        void FixedUpdate()
        {
            if (rigidBody.linearVelocity.sqrMagnitude > 0.1f)
                transform.forward = rigidBody.linearVelocity.normalized;
            currentTime += Time.deltaTime;
            if (currentTime > ColliderTime)
                damageCollider.enabled = true;
        }
    }
}