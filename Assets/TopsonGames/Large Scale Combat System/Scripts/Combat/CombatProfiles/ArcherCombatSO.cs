namespace TopsonGames
{
    using System.Linq;
    using UnityEngine;
    using UnityEngine.AI;
    using static TopsonGames.Formation;
    using static TopsonGames.Unit;

    [CreateAssetMenu(fileName = "ArcherCombat", menuName = "TopsonGames/Combat Behaviours/ArcherCombat")]
    public class ArcherCombatSO : CombatBehaviourSO
    {
        public float wanderRadius = 1.5f;

        [Header("Archer Combat Settings")]
        public float archerDetectionRange = 30f;
        public float archerInnerAngle = 30f;
        public float archerOuterAngle = 60f;
        public float archerFrontOffset = 5f;
        public float archerDetectionTimer = 1f;
        public float timeBetweenShots = 5f;
        public float ProjectileSpeed = 10f;

        public override void InitializeUnit(Unit unit, Formation formation)
        {
            unit.attackTimer = cooldown;
            unit.switchAnimationTimer = Random.Range(0, switchAnimationTimer);
        }

        public override void InitializeFormation(Formation formation)
        {
            formation.currentArrows = formation.UnitData.Arrows;
            formation.currentTimeBetweenShots = timeBetweenShots;
        }

        public override void TickCombat(Unit unit, float deltaTime, Coroutine movementRoutine)
        {
            var Target = unit.GetCurrentTarget();

            if (Target == null || Target.currentHealth <= 0)
            {
                unit.ResetTarget();
                unit.animatorLink.SetAttack(false);
                unit.currentState = UnitState.Idle;
                return;
            }

            unit.CancelArrowRoutine();
            unit.attackTimer -= Time.deltaTime;
            if (unit.attackTimer <= 0 && IsFacingTarget(unit, Target.transform))
            {
                unit.animatorLink.SetAttackRandomizer(Random.Range(1, unit.GetFormation().UnitData.attackAnimations + 1));
                unit.animatorLink.SetAttack(true);
                unit.attackTimer = cooldown;
            }
            unit.ManageMovementRoutine();
        }

        public override bool ShouldEngage(Unit self, Unit potentialTarget)
        {
            return potentialTarget != null && Vector3.Distance(self.transform.position, potentialTarget.transform.position) <= attackRange;
        }

        public override void OnMovementTick(Unit unit)
        {
            if (unit.GetCurrentTarget() != null)
            {
                Vector3 randomOffset = Random.insideUnitSphere * wanderRadius;
                randomOffset.y = 0;
                Vector3 targetPosition = unit.Waypoint.transform.position + randomOffset;

                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, wanderRadius * 2f, NavMesh.AllAreas))
                {
                    if (Vector3.SqrMagnitude(hit.position - unit.lastTargetPosition) > 0.2f)
                    {
                        unit.MoveTo(hit.position);
                        unit.lastTargetPosition = hit.position;
                    }
                }
            }
            else
            {
                unit.StopMovement();
                unit.lastTargetPosition = Vector3.zero;
            }
        }

        public override Unit OnFindClosestEnemy(Unit unit, Formation formation)
        {
            return null;
        }

        Formation FindEnemyArcherFormation(Formation formation)
        {
            if (formation.customEnemy != null)
            {
                float distanceToTarget = Vector3.Distance(formation.CalculateUnitCenter(), formation.customEnemy.CalculateUnitCenter());
                Vector3 directionToTarget = (formation.customEnemy.CalculateUnitCenter() - formation.CalculateUnitCenter()).normalized;
                float angleToTarget = Vector3.Angle(formation.GetUnitCenterCollider().transform.forward, directionToTarget);

                if (distanceToTarget <= archerDetectionRange && angleToTarget <= archerOuterAngle / 2f)
                {
                    return formation.customEnemy;
                }
            }
            if (formation.isFreeShooting)
            {
                Formation closestEnemy = null;
                float closestDistanceSqr = Mathf.Infinity;

                foreach (Formation enemyFormation in FormationController.instance.GetFormations())
                {
                    if (enemyFormation.TeamID == formation.TeamID) continue;

                    Vector3 formationCenter = enemyFormation.CalculateUnitCenter();
                    Vector3 directionToFormation = (formationCenter - formation.CalculateUnitCenter()).normalized;
                    float angleToFormation = Vector3.Angle(formation.GetUnitCenterCollider().transform.forward, directionToFormation);
                    float distanceToFormation = Vector3.Distance(formation.CalculateUnitCenter(), formationCenter);

                    if (distanceToFormation <= archerDetectionRange && angleToFormation <= archerOuterAngle / 2f)
                    {
                        float distToEnemyUnitSqr = (formation.CalculateUnitCenter() - enemyFormation.CalculateUnitCenter()).sqrMagnitude;
                        if (distToEnemyUnitSqr < closestDistanceSqr)
                        {
                            closestDistanceSqr = distToEnemyUnitSqr;
                            closestEnemy = enemyFormation;
                        }
                    }
                }
                return closestEnemy;
            }

            return null;
        }

        public override void OnUpdateUnit(Unit unit, Formation formation)
        {
            unit.animatorLink.SetBlend(unit.agent.velocity.magnitude);
            if (formation.CurrentState == Formation.FormationState.Engaged)
                unit.animatorLink.SetEngaged(true);
            else
                unit.animatorLink.SetEngaged(false);

            if (formation.UnitData.idleAnimations > 1)
            {
                unit.switchAnimationTimer -= Time.deltaTime;
                if (unit.switchAnimationTimer < 0)
                {
                    unit.switchAnimationTimer = switchAnimationTimer;
                    if (Random.Range(0f, 1f) <= switchAnimationProbability)
                    {
                        unit.currentIdleAnimation = Random.Range(1, formation.UnitData.idleAnimations + 1);
                    }
                    else
                    {
                        unit.currentIdleAnimation = 1;
                    }
                }
                unit.animatorLink.SetIdle(unit.currentIdleAnimation);
            }
        }

        public override void OnUpdateFormation(Formation formation)
        {
            formation.archerDetectionTimer -= Time.deltaTime;

            if (formation.archerDetectionTimer < 0)
            {
                formation.archerDetectionTimer = archerDetectionTimer;

                if (formation.CurrentState == FormationState.Engaged || formation.currentArrows <= 0)
                {
                    if (formation.CurrentCombatState == Formation.CombatState.Archer)
                    {
                        foreach (var unit in formation.GetUnits())
                        {
                            unit.animatorLink.SetSwitchMelee(true);
                            unit.animatorLink.SetSwitchRanged(false);
                            unit.StartCoroutine(unit.SwitchRangedMelee(false, 4f));
                        }
                        formation.CurrentCombatState = Formation.CombatState.Melee;
                    }
                    formation.HandleEffects(EffectType.Shooting, true);
                    return;
                }

                if (formation.currentArrows > 0)
                {
                    formation.currentTimeBetweenShots -= Time.deltaTime + archerDetectionTimer;

                    if (formation.CurrentCombatState != Formation.CombatState.Archer)
                    {
                        foreach (var unit in formation.GetUnits())
                        {
                            unit.animatorLink.SetSwitchMelee(false);
                            unit.animatorLink.SetSwitchRanged(true);
                            unit.StartCoroutine(unit.SwitchRangedMelee(true, 4f));
                        }
                        formation.CurrentCombatState = Formation.CombatState.Archer;
                        return;
                    }

                    formation.ArcherTarget = FindEnemyArcherFormation(formation);

                    if (!formation.ArcherTarget)
                    {
                        formation.currentTimeBetweenShots = timeBetweenShots;
                        return;
                    }
                    formation.HandleEffects(EffectType.Shooting, false);
                    if (formation.currentTimeBetweenShots < 0 && formation.CurrentState == FormationState.Idle)
                    {
                        formation.currentTimeBetweenShots = timeBetweenShots;
                        formation.currentArrows -= 1;
                        foreach (var unit in formation.GetUnits())
                        {
                            ArcherAttack(unit);
                        }
                    }
                }
                else
                    formation.HandleEffects(EffectType.Shooting, true);
            }
        }

        public override void DrawGizmosOnBehaviour(Formation formation)
        {
            if (formation.GetUnits() != null && formation.GetUnits().Count != 0)
            {
                foreach (var unit in formation.GetUnits())
                {
                    if (unit != null) Gizmos.DrawWireSphere(unit.transform.position, attackRange);
                }
                if (formation.GetUnits().Any(u => u != null))
                {
                    Gizmos.color = Color.cyan;
                    Vector3 formationCenter = formation.CalculateUnitCenter();
                    Vector3 formationForward = formation.GetUnits()[0].transform.forward;
                    Vector3 offsetPoint = formationCenter + formationForward * archerFrontOffset;
                    Quaternion leftRotOuter = Quaternion.AngleAxis(-archerOuterAngle / 2f, Vector3.up);
                    Quaternion rightRotOuter = Quaternion.AngleAxis(archerOuterAngle / 2f, Vector3.up);
                    Vector3 outerLeftFromOffset = offsetPoint + (leftRotOuter * formationForward) * (archerDetectionRange - archerFrontOffset);
                    Vector3 outerRightFromOffset = offsetPoint + (rightRotOuter * formationForward) * (archerDetectionRange - archerFrontOffset);
                    Gizmos.DrawLine(offsetPoint + (Quaternion.AngleAxis(-archerInnerAngle / 2f, Vector3.up) * formationForward).normalized * (0.1f + archerFrontOffset), outerLeftFromOffset);
                    Gizmos.DrawLine(offsetPoint + (Quaternion.AngleAxis(archerInnerAngle / 2f, Vector3.up) * formationForward).normalized * (0.1f + archerFrontOffset), outerRightFromOffset);
                    Gizmos.DrawLine(offsetPoint + (Quaternion.AngleAxis(-archerInnerAngle / 2f, Vector3.up) * formationForward).normalized * (0.1f + archerFrontOffset), offsetPoint + (Quaternion.AngleAxis(archerInnerAngle / 2f, Vector3.up) * formationForward).normalized * (0.1f + archerFrontOffset));
                    Gizmos.DrawLine(outerLeftFromOffset, outerRightFromOffset);
                    Gizmos.DrawSphere(offsetPoint, 0.5f);
                }
            }
        }

        public override void TickIdle(Unit unit, NavMeshAgent agent, Formation formation, Coroutine movementRoutine)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                if (formation.CurrentState != Formation.FormationState.Engaged)
                {
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        if (Vector3.Distance(unit.transform.position, unit.Waypoint.transform.position) > agent.stoppingDistance + 0.1f)
                        {
                            unit.currentState = UnitState.Moving;
                            unit.MoveTo(unit.Waypoint.transform.position);
                        }
                        else
                        {
                            unit.StopMovement();
                            if (formation.CurrentCombatState == Formation.CombatState.Archer && formation.ArcherTarget != null)
                            {
                                Unit targetUnit = GetArcherTarget(unit, formation.ArcherTarget);
                                if (targetUnit != null)
                                {
                                    DestructibleTarget target = targetUnit.GetComponent<DestructibleTarget>();
                                    unit.SetCurrentTarget(target);
                                }
                            }
                        }
                    }
                }
                else if (unit.GetCurrentTarget() == null && Vector3.Distance(unit.transform.position, unit.Waypoint.transform.position) > 0.3f)
                {
                    unit.currentState = UnitState.Moving;
                    unit.MoveTo(unit.Waypoint.transform.position);
                }
            }
        }

        public override void TickMovement(Unit unit, NavMeshAgent agent, Formation formation, Coroutine movementRoutine)
        {
            unit.MoveTo(unit.Waypoint.transform.position);

            if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance + 0.1f)
            {
                unit.currentState = UnitState.Idle;
            }
        }

        public Unit GetArcherTarget(Unit unit, Formation formation)
        {
            Unit closestEnemy = null;
            float distance = Mathf.Infinity;
            foreach (var eunit in formation.GetUnits())
            {
                if (unit == null || eunit == null) continue;
                float dist = Vector3.Distance(unit.transform.position, eunit.transform.position);
                if (dist < distance)
                {
                    distance = dist;
                    closestEnemy = eunit;
                }
            }
            return closestEnemy;
        }

        public void ArcherAttack(Unit unit)
        {
            if (!unit || unit.GetCurrentTarget() == null || unit.RangedWeapon.Weapon == null) return;

            if (unit.currentState == UnitState.Idle && IsFacingTarget(unit, unit.GetCurrentTarget().transform, 60))
            {
                unit.animatorLink.SetAttackRanged(true);
                unit.animatorLink.SetSwitchRanged(false);
                if (unit.RangedWeapon.animator)
                {
                    unit.RangedWeapon.animator.SetTrigger("AttackRanged");
                }
            }
        }

        public override void OnReportArrows(Formation formation)
        {
            foreach (var unit in formation.GetUnits())
            {
                if (unit == null) continue;
                unit.ReportArrows();
            }
        }

        public override void OnReportArrowsUnit(Unit unit, Coroutine arrowRoutine)
        {
            if (unit.currentState == UnitState.Fighting) return;
            unit.ManageArrowRoutine();
        }

        public override bool IsFacingTarget(Unit unit, UnityEngine.Transform target, float angleThreshold = 30)
        {
            if (unit == null || target == null) return false;
            Vector3 directionToTarget = (target.position - unit.transform.position).normalized;
            directionToTarget.y = 0;
            Vector3 unitForward = unit.transform.forward;
            unitForward.y = 0;
            if (directionToTarget == Vector3.zero || unitForward == Vector3.zero) return false;
            float angle = Vector3.Angle(unitForward, directionToTarget);
            return angle <= angleThreshold;
        }

        public override void OnTakeDamage(float Dammage, Unit unit, Unit attacker, bool shieldHit = false) { }
        public override void OnDeath(Unit unit, Unit attacker, Formation formation) { }
    }
}