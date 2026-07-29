using UnityEngine;
namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.AI;

    [CreateAssetMenu(fileName = "MusketSO", menuName = "TopsonGames/Ranged Weapon/MusketSO")]
    public class MusketSO : RangedWeaponSO
    {
        [Tooltip("The speed of the projectile in meters per second.")]
        public float projectileSpeed = 100f;

        [Tooltip("A random scatter for the shot. Set to (0,0,0) for perfect accuracy.")]
        public Vector3 inaccuracyOffset = new Vector3(0.5f, 0.5f, 0.5f);

        public override void Attack(RangedWeapon rangedWeapon, Unit currentTarget, Formation targetFormation, Formation parentFormation)
        {

            Projectile projectile = rangedWeapon.GetNextArrow();
            if (projectile == null) return;

            projectile.gameObject.SetActive(true);
            Vector3 targetPosition = currentTarget.transform.position + Vector3.up * 1.5f;

            targetPosition += new Vector3(
                Random.Range(-inaccuracyOffset.x, inaccuracyOffset.x),
                Random.Range(-inaccuracyOffset.y, inaccuracyOffset.y),
                Random.Range(-inaccuracyOffset.z, inaccuracyOffset.z)
            );

            Vector3 targetVelocity = Vector3.zero;
            NavMeshAgent targetAgent = currentTarget.GetComponent<NavMeshAgent>();
            if (targetAgent != null)
            {
                targetVelocity = targetAgent.velocity;
            }
            float distance = Vector3.Distance(rangedWeapon.ShootPoint.position, targetPosition);
            float travelTime = distance / projectileSpeed;

            Vector3 predictedTarget = targetPosition + targetVelocity * travelTime;

            Vector3 direction = (predictedTarget - rangedWeapon.ShootPoint.position).normalized;

            projectile.rigidBody.linearVelocity = direction * projectileSpeed;
            projectile.transform.forward = direction;
            projectile.parentFormation = parentFormation;
        }
    }
}