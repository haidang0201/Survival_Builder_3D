namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.AI;
    using static TopsonGames.Formation;
    using static TopsonGames.Unit;

    [CreateAssetMenu(fileName = "HorseArcherCombatSO", menuName = "TopsonGames/Combat Behaviours/Horse Archer")]
    public class HorseArcherCombatSO : CombatBehaviourSO
    {
        public float wanderRadius = 1.5f;

        [Header("Archer Combat Settings")]
        public float archerDetectionRange = 40f;
        public float archerDetectionTimer = 1f;
        public float timeBetweenShots = 3f;

        [Header("Melee settings (when enemy is close)")]
        public float knockbackForce = 1.5f;
        public float knockbackDuration = 2f;

        public override void InitializeUnit(Unit unit, Formation formation)
        {
            unit.attackTimer = cooldown;
            unit.switchAnimationTimer = Random.Range(0, switchAnimationTimer);

            if (unit.animatorLink is AnimatorLinkMeshCavalry cavLink)
            {
                cavLink.SetHorseAnimatorSpeed(Random.Range(0.85f, 1.15f));
            }
            unit.animatorLink.SetAnimatorSpeed(Random.Range(0.85f, 1.15f));
        }

        public override void InitializeFormation(Formation formation)
        {
            formation.currentArrows = formation.UnitData.Arrows;
            formation.currentTimeBetweenShots = timeBetweenShots;
        }

        public override void TickCombat(Unit unit, float deltaTime, Coroutine movementRoutine)
        {
            var target = unit.GetCurrentTarget();

            if (target == null || target.currentHealth <= 0)
            {
                unit.ResetTarget();
                unit.animatorLink.SetAttack(false);

                unit.animatorLink.SetAttackRanged(false);

                if (unit.animatorLink is AnimatorLinkMeshHorseArchers link) link.StopAiming();

                unit.currentState = UnitState.Idle;
                return;
            }

            float distanceToTarget = Vector3.Distance(unit.transform.position, target.transform.position);

            if (distanceToTarget <= attackRange)
            {
                unit.CancelArrowRoutine();
                unit.attackTimer -= Time.deltaTime;

                if (unit.attackTimer <= 0 && IsFacingTarget(unit, target.transform))
                {
                    unit.animatorLink.SetAttackRandomizer(Random.Range(1, unit.GetFormation().UnitData.attackAnimations + 1));
                    unit.animatorLink.SetAttack(true);
                    unit.attackTimer = cooldown;

                    if (!unit.hasAppliedKnockback)
                    {
                        unit.hasAppliedKnockback = true;
                        Unit targetUnit = target.GetOwner<Unit>();
                        if (targetUnit != null)
                        {
                            targetUnit.ApplyKnockback(unit.transform.forward, knockbackForce, 2f, knockbackDuration);
                            targetUnit.ProcessDamage(GameManager.instance.damageModifier.GetTotalDamage(unit.GetFormation().UnitData, targetUnit.GetFormation().UnitData), unit, false);
                        }
                    }
                }
            }
            else if (distanceToTarget <= archerDetectionRange)
            {
                if (unit.RangedWeapon != null && unit.RangedWeapon.gameObject.activeInHierarchy)
                {
                    Vector3 directionToTarget = (target.transform.position - unit.RangedWeapon.transform.position).normalized;
                    unit.RangedWeapon.transform.rotation = Quaternion.LookRotation(directionToTarget);
                }

                if (unit.animatorLink is AnimatorLinkMeshHorseArchers haLink)
                {
                    haLink.SetAimTarget(target.transform.position);
                }
            }

            unit.ManageMovementRoutine();
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
                            unit.StartCoroutine(unit.SwitchRangedMelee(false, 2f));
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
                            unit.StartCoroutine(unit.SwitchRangedMelee(true, 2f));
                        }
                        formation.CurrentCombatState = Formation.CombatState.Archer;
                        return;
                    }

                    formation.ArcherTarget = FindHorseArcherTarget(formation);

                    if (!formation.ArcherTarget)
                    {
                        formation.currentTimeBetweenShots = timeBetweenShots;
                        return;
                    }

                    formation.HandleEffects(EffectType.Shooting, false);

                    if (formation.currentTimeBetweenShots < 0)
                    {
                        formation.currentTimeBetweenShots = timeBetweenShots;
                        formation.currentArrows -= 1;
                        foreach (var unit in formation.GetUnits())
                        {
                            FireDirectionalArrow(unit, formation.ArcherTarget);
                        }
                    }
                }
                else
                {
                    formation.HandleEffects(EffectType.Shooting, true);
                }
            }
        }

        private void FireDirectionalArrow(Unit unit, Formation targetFormation)
        {
            if (!unit || targetFormation == null || unit.RangedWeapon == null || unit.RangedWeapon.Weapon == null) return;

            Unit closestEnemy = null;
            float distance = Mathf.Infinity;
            foreach (var eunit in targetFormation.GetUnits())
            {
                if (eunit == null) continue;
                float dist = Vector3.Distance(unit.transform.position, eunit.transform.position);
                if (dist < distance)
                {
                    distance = dist;
                    closestEnemy = eunit;
                }
            }

            if (closestEnemy != null)
            {
                DestructibleTarget destTarget = closestEnemy.GetComponent<DestructibleTarget>();
                if (destTarget != null) unit.SetCurrentTarget(destTarget);

                Vector3 dirToTarget = (closestEnemy.transform.position - unit.RangedWeapon.transform.position).normalized;
                unit.RangedWeapon.transform.rotation = Quaternion.LookRotation(dirToTarget);

                Vector3 flatDirToTarget = dirToTarget;
                flatDirToTarget.y = 0;
                Vector3 unitForward = unit.transform.forward;
                unitForward.y = 0;

                float angle = Vector3.SignedAngle(unitForward, flatDirToTarget, Vector3.up);
                int directionIndex = GetDirectionIndexFromAngle(angle);

                if (unit.animatorLink is AnimatorLinkMeshHorseArchers horseArcherLink)
                {
                    horseArcherLink.SetAttackRangedDirectional(true, directionIndex);
                    horseArcherLink.SetAimTarget(closestEnemy.transform.position);
                }
                else
                {
                    unit.animatorLink.SetAttackRanged(true);
                }

                if (unit.RangedWeapon.animator)
                {
                    unit.RangedWeapon.animator.SetTrigger("AttackRanged");
                }
            }
        }

        private int GetDirectionIndexFromAngle(float angle)
        {
            if (angle >= -22.5f && angle < 22.5f) return 0;
            if (angle >= 22.5f && angle < 67.5f) return 1;
            if (angle >= 67.5f && angle < 112.5f) return 2;
            if (angle >= 112.5f && angle < 157.5f) return 3;
            if (angle >= 157.5f || angle < -157.5f) return 4;
            if (angle >= -157.5f && angle < -112.5f) return 5;
            if (angle >= -112.5f && angle < -67.5f) return 6;
            if (angle >= -67.5f && angle < -22.5f) return 7;
            return 0;
        }

        private Formation FindHorseArcherTarget(Formation formation)
        {
            if (formation.customEnemy != null)
            {
                float distanceToTarget = Vector3.Distance(formation.CalculateUnitCenter(), formation.customEnemy.CalculateUnitCenter());
                if (distanceToTarget <= archerDetectionRange)
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

                    float distanceToFormation = Vector3.Distance(formation.CalculateUnitCenter(), enemyFormation.CalculateUnitCenter());

                    if (distanceToFormation <= archerDetectionRange)
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

        public override bool ShouldEngage(Unit self, Unit potentialTarget)
        {
            return potentialTarget != null && Vector3.Distance(self.transform.position, potentialTarget.transform.position) <= archerDetectionRange;
        }

        public override void OnMovementTick(Unit unit)
        {
            var target = unit.GetCurrentTarget();
            if (target != null && unit.GetFormation() != null && unit.GetFormation().CurrentCombatState == CombatState.Melee)
            {
                Vector3 desiredPosition = target.transform.position - (target.transform.right * attackRange);

                if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    unit.MoveTo(hit.position);
                }
            }
        }

        public override void TickIdle(Unit unit, NavMeshAgent agent, Formation formation, Coroutine movementRoutine)
        {
            if (agent.enabled && agent.isOnNavMesh)
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
                        if (unit.GetCurrentTarget() == null && unit.animatorLink is AnimatorLinkMeshHorseArchers link)
                        {
                            link.StopAiming();
                        }
                    }
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

        public override void OnUpdateUnit(Unit unit, Formation formation)
        {
            unit.animatorLink.SetBlend(unit.agent.velocity.magnitude);
            if (unit.animatorLink is AnimatorLinkMeshCavalry cavLink)
            {
                cavLink.SetHorseBlend(unit.agent.velocity.magnitude);
            }

            unit.animatorLink.SetEngaged(formation.CurrentState == Formation.FormationState.Engaged);

            if (formation.UnitData.idleAnimations > 1)
            {
                unit.switchAnimationTimer -= Time.deltaTime;
                if (unit.switchAnimationTimer < 0)
                {
                    unit.switchAnimationTimer = switchAnimationTimer;
                    unit.currentIdleAnimation = (Random.Range(0f, 1f) <= switchAnimationProbability)
                        ? Random.Range(1, formation.UnitData.idleAnimations + 1) : 1;
                }
                unit.animatorLink.SetIdle(unit.currentIdleAnimation);
            }
        }

        public override void DrawGizmosOnBehaviour(Formation formation)
        {
            if (formation.GetUnits() != null)
            {
                foreach (var unit in formation.GetUnits())
                {
                    if (unit != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(unit.transform.position, archerDetectionRange);
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(unit.transform.position, attackRange);
                    }
                }
            }
        }

        public override Unit OnFindClosestEnemy(Unit unit, Formation formation) { return null; }

        public override bool IsFacingTarget(Unit unit, Transform target, float angleThreshold = 30)
        {
            if (unit == null || target == null) return false;
            Vector3 directionToTarget = (target.position - unit.transform.position).normalized;
            directionToTarget.y = 0;
            Vector3 unitForward = unit.transform.forward;
            unitForward.y = 0;
            if (directionToTarget == Vector3.zero || unitForward == Vector3.zero) return false;
            return Vector3.Angle(unitForward, directionToTarget) <= angleThreshold;
        }

        public override void OnReportArrows(Formation formation)
        {
            foreach (var unit in formation.GetUnits())
            {
                if (unit != null) unit.ReportArrows();
            }
        }

        public override void OnReportArrowsUnit(Unit unit, Coroutine arrowRoutine)
        {
            if (unit.currentState == UnitState.Fighting) return;
            unit.ManageArrowRoutine();
        }

        public override void OnDeath(Unit unit, Unit attacker, Formation formation)
        {
            if (unit.animatorLink is AnimatorLinkMeshCavalry cavLink)
            {
                cavLink.SetHorseDeath();
            }
        }

        public override void OnTakeDamage(float Damage, Unit unit, Unit attacker, bool shieldHit = false) { }
    }
}