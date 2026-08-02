namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.AI;
    using static TopsonGames.Unit;

    [CreateAssetMenu(fileName = "ElephantCombatSO", menuName = "TopsonGames/Combat Behaviours/Elephant")]
    public class ElephantCombatSO : CombatBehaviourSO
    {
        public float wanderRadius = 1.5f;

        [Header("Knockback Settings")]
        public float knockbackForce = 3f;
        public float knockbackDuration = 3f;
        public float knockbackRadius = 2.5f;
        public float knockbackDamageMultiplier = 3f;
        public enum knockBackBehaviour { SingleUnit, Area }
        [Tooltip("Elephants should typically use Area knockback to trample multiple units.")]
        public knockBackBehaviour knockbackBehaviour = knockBackBehaviour.Area;

        public override void InitializeUnit(Unit unit, Formation formation)
        {
            unit.attackTimer = cooldown;
            unit.animatorLink.SetAnimatorSpeed(Random.Range(0.85f, 1.15f));
            unit.switchAnimationTimer = Random.Range(0, switchAnimationTimer);
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


                if (knockbackBehaviour == knockBackBehaviour.SingleUnit)
                {
                    Unit targetUnit = Target.GetOwner<Unit>();
                    if (targetUnit != null)
                    {
                        targetUnit.ApplyKnockback(unit.transform.forward, knockbackForce, 3f, knockbackDuration);
                        targetUnit.ProcessDamage(GameManager.instance.damageModifier.GetTotalDamage(unit.GetFormation().UnitData, targetUnit.GetFormation().UnitData) * knockbackDamageMultiplier, unit, false);
                    }
                }
                else if (knockbackBehaviour == knockBackBehaviour.Area)
                {
                    Collider[] hits = Physics.OverlapSphere(unit.transform.position, knockbackRadius, FormationController.instance.unitMask);

                    foreach (var hit in hits)
                    {
                        Unit enemyUnit = hit.GetComponentInParent<Unit>();

                        if (enemyUnit != null &&
                            enemyUnit != unit &&
                            enemyUnit.GetCurrentHealth() > 0 &&
                            enemyUnit.GetFormation().TeamID != unit.GetFormation().TeamID)
                        {
                            Vector3 knockbackDir = (enemyUnit.transform.position - unit.transform.position).normalized;

                            if (knockbackDir == Vector3.zero) knockbackDir = unit.transform.forward;

                            enemyUnit.ApplyKnockback(knockbackDir, knockbackForce, 3f, knockbackDuration);
                            enemyUnit.ProcessDamage(GameManager.instance.damageModifier.GetTotalDamage(unit.GetFormation().UnitData, enemyUnit.GetFormation().UnitData) * knockbackDamageMultiplier, unit, false);
                        }
                    }
                }
            }
            unit.ManageMovementRoutine();
            unit.animatorLink.SetBlend(unit.agent.velocity.magnitude);
        }

        public override bool ShouldEngage(Unit self, Unit potentialTarget)
        {
            return potentialTarget != null && Vector3.Distance(self.transform.position, potentialTarget.transform.position) <= attackRange;
        }

        public override void OnMovementTick(Unit unit)
        {
            var target = unit.GetCurrentTarget();
            if (target != null)
            {
                Vector3 directionToTarget = (target.transform.position - unit.transform.position).normalized;
                Vector3 perpendicularRight = Vector3.Cross(directionToTarget, Vector3.up).normalized;
                Vector3 desiredPosition = target.transform.position + perpendicularRight * attackRange * 0.8f + directionToTarget * attackRange * 0.2f;

                if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, wanderRadius * 2f, NavMesh.AllAreas))
                {
                    NavMesh.SamplePosition(target.transform.position - directionToTarget * attackRange, out hit, wanderRadius * 2f, NavMesh.AllAreas);
                }

                if (Vector3.SqrMagnitude(hit.position - unit.lastTargetPosition) > 0.2f)
                {
                    unit.MoveTo(hit.position);
                    unit.lastTargetPosition = hit.position;
                }
            }
            else
            {
                unit.StopMovement();
                unit.lastTargetPosition = Vector3.zero;
            }
        }

        public override void DrawGizmosOnBehaviour(Formation formation)
        {
            if (formation.GetUnits() != null && formation.GetUnits().Count != 0)
            {
                foreach (var unit in formation.GetUnits())
                {
                    if (unit != null)
                        Gizmos.DrawWireSphere(unit.transform.position, attackRange);
                }
            }
        }

        public override Unit OnFindClosestEnemy(Unit unit, Formation formation)
        {
            return null;
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

        public override void OnUpdateUnit(Unit unit, Formation formation)
        {
            float blendVal = unit.agent.velocity.magnitude;
            unit.animatorLink.SetBlend(blendVal);

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

        public override bool IsFacingTarget(Unit unit, UnityEngine.Transform target, float angleThreshold = 30)
        {
            if (unit == null || target == null) return false;
            Vector3 directionToTarget = (target.position - unit.transform.position).normalized;
            directionToTarget.y = 0;
            Vector3 unitForward = unit.transform.forward;
            unitForward.y = 0;
            if (directionToTarget == Vector3.zero || unitForward == Vector3.zero) return false;
            return Vector3.Angle(unitForward, directionToTarget) <= angleThreshold;
        }

        public override void OnDeath(Unit unit, Unit attacker, Formation formation) { }
        public override void OnUpdateFormation(Formation formation) { }
        public override void InitializeFormation(Formation formation) { }
        public override void OnReportArrows(Formation formation) { }
        public override void OnReportArrowsUnit(Unit unit, Coroutine arrowRoutine) { }
        public override void OnTakeDamage(float Dammage, Unit unit, Unit attacker, bool shieldHit = false) { }
    }
}