namespace TopsonGames
{
    using TopsonGames;
    using UnityEngine;
    using UnityEngine.AI;

    [CreateAssetMenu(fileName = "IBowSO", menuName = "TopsonGames/Ranged Weapon/IBowSO")]
    public class IBowSO : RangedWeaponSO
    {
        public float defaultLaunchAngle = 70f;
        [Tooltip("Random Target Offset. 0 for perfect shooting")]
        public Vector3 RandomOffset = new Vector3(0.5f, 0.5f, 0.5f);

        public override void Attack(RangedWeapon rangedWeapon, Unit currentTarget, Formation formation, Formation parentFormation)
        {
            Formation targetFormation = formation;
            Vector3 formationCenter = targetFormation.CalculateUnitCenter();
            Quaternion formationRotation = targetFormation.CalculateAverageRotation();

            float formationWidth = targetFormation.UnitData.formationSpacing * Mathf.Min(targetFormation.GetUnits().Count, targetFormation.UnitData.formationWidth);
            float formationDepth = targetFormation.UnitData.formationSpacing * Mathf.CeilToInt((float)targetFormation.GetUnits().Count / targetFormation.UnitData.formationWidth);

            Vector3 shooterPosition = rangedWeapon.ShootPoint.position;


            float offsetX = Random.Range(-formationWidth / 2f, formationWidth / 2f);
            float offsetZ = Random.Range(-formationDepth / 2f, formationDepth / 2f); 

            Vector3 localOffset = new Vector3(offsetX, 0, offsetZ); 
            Vector3 randomizedTarget = formationCenter + (formationRotation * localOffset) + Vector3.up * 2f;

            randomizedTarget += new Vector3(
                Random.Range(-RandomOffset.x, RandomOffset.x),
                Random.Range(-RandomOffset.y, RandomOffset.y),
                Random.Range(-RandomOffset.z, RandomOffset.z)
            );

            Vector3 targetVelocity = Vector3.zero;
            NavMeshAgent targetAgent = currentTarget.GetComponent<NavMeshAgent>();
            if (targetAgent != null)
            {
                targetVelocity = targetAgent.velocity;
            }

            float launchAngle = GetLaunchAngleFromForward(rangedWeapon.ShootPoint.forward);
            Vector3 dir = randomizedTarget - rangedWeapon.ShootPoint.position;
            float flatDistance = new Vector3(dir.x, 0, dir.z).magnitude;
            float estTime = flatDistance / 15f;
            Vector3 predictedTarget = randomizedTarget + targetVelocity * estTime;

            float? speed = CalculateRequiredSpeed(rangedWeapon.ShootPoint.position, predictedTarget, launchAngle);
            if (speed == null)
            {
                FireProjectileAtStaticTarget(rangedWeapon, randomizedTarget, parentFormation);
                return;
            }

            Vector3? velocity = CalculateBallisticVelocity(rangedWeapon.ShootPoint.position, predictedTarget, speed.Value, launchAngle);
            if (velocity.HasValue)
            {
                Projectile arrow = rangedWeapon.GetNextArrow();
                if (arrow == null) return;

                arrow.rigidBody.linearVelocity = velocity.Value;
                arrow.transform.forward = velocity.Value.normalized;
                arrow.parentFormation = parentFormation;
            }
            else
            {
                FireProjectileAtStaticTarget(rangedWeapon, randomizedTarget, parentFormation);
            }
        }
        private void FireProjectileAtStaticTarget(RangedWeapon rangedWeapon,Vector3 targetPos, Formation parentFormation)
        {
            Projectile arrow = rangedWeapon.GetNextArrow();
            if (arrow == null) return;

            float launchAngle = GetLaunchAngleFromForward(rangedWeapon.ShootPoint.forward);
            float? speed = CalculateRequiredSpeed(rangedWeapon.ShootPoint.position, targetPos, launchAngle);
            if (speed == null) return;

            Vector3? velocity = CalculateBallisticVelocity(rangedWeapon.ShootPoint.position, targetPos, speed.Value, launchAngle);
            if (velocity == null) return;

            arrow.rigidBody.linearVelocity = velocity.Value;
            arrow.transform.forward = velocity.Value.normalized;
            arrow.parentFormation = parentFormation;
        }

        private void FireProjectileAtStaticDirection(RangedWeapon rangedWeapon,Vector3 direction, Formation parentFormation)
        {
            Projectile arrow = rangedWeapon.GetNextArrow();
            if (arrow == null) return;

            float launchAngle = GetLaunchAngleFromForward(direction);
            Vector3 flatDir = new Vector3(direction.x, 0, direction.z).normalized;
            float horizontalSpeed = 15f * Mathf.Cos(launchAngle * Mathf.Deg2Rad);
            float verticalSpeed = 15f * Mathf.Sin(launchAngle * Mathf.Deg2Rad);
            Vector3 velocity = flatDir * horizontalSpeed + Vector3.up * verticalSpeed;

            arrow.rigidBody.linearVelocity = velocity;
            arrow.transform.forward = velocity.normalized;
            arrow.parentFormation = parentFormation;
        }

        private float? CalculateRequiredSpeed(Vector3 origin, Vector3 target, float angleDegrees)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float g = -Physics.gravity.y;

            Vector3 dir = target - origin;
            float x = new Vector3(dir.x, 0, dir.z).magnitude;
            float y = dir.y;

            float cosTheta = Mathf.Cos(angleRad);
            float tanTheta = Mathf.Tan(angleRad);

            float denominator = 2 * (x * tanTheta - y) * cosTheta * cosTheta;

            if (denominator <= 0.001f)
                return null;

            float vSquared = (g * x * x) / denominator;
            if (vSquared <= 0) return null;

            return Mathf.Sqrt(vSquared);
        }

        private Vector3? CalculateBallisticVelocity(Vector3 origin, Vector3 target, float speed, float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = target - origin;
            Vector3 dirXZ = new Vector3(dir.x, 0, dir.z);
            float x = dirXZ.magnitude;
            float y = dir.y;

            float velocityX = speed * Mathf.Cos(rad);
            float velocityY = speed * Mathf.Sin(rad);
            Vector3 result = dirXZ.normalized * velocityX + Vector3.up * velocityY;
            return result;
        }

        private float GetLaunchAngleFromForward(Vector3 forward)
        {
            return Vector3.Angle(new Vector3(forward.x, 0, forward.z), forward);
        }
    }

}