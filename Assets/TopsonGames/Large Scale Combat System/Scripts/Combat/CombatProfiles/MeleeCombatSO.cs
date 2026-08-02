namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.AI;
    using static TopsonGames.Unit;

    [CreateAssetMenu(fileName = "MeleeCombat", menuName = "TopsonGames/Combat Behaviours/Melee")]
    public class MeleeCombatSO : CombatBehaviourSO
    {
          public float wanderRadius = 1.5f; 

        public override void InitializeUnit(Unit unit, Formation formation)
        {
            unit.attackTimer = cooldown;
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

        public override Unit OnFindClosestEnemy(Unit unit, Formation formation)
        {
            return null;
        }

        public override void TickMovement(Unit unit, NavMeshAgent agent, Formation formation, Coroutine movementRoutine)
        {
            unit.MoveTo(unit.Waypoint.transform.position);
            if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance + 0.1f)
            {
                unit.currentState = UnitState.Idle;
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

        public override void OnUpdateFormation(Formation formation) { }
        public override void InitializeFormation(Formation formation) { }
        public override void OnReportArrows(Formation formation)
        {
            foreach (var unit in formation.GetUnits())
            {
                if (unit) unit.ReportArrows();
            }
        }
        public override void OnReportArrowsUnit(Unit unit, Coroutine arrowRoutine)
        {
            if (unit.currentState == UnitState.Fighting) return;
            unit.ManageArrowRoutine();
        }
        public override void OnTakeDamage(float Dammage, Unit unit, Unit attacker, bool shieldHit = false) { }
        public override void OnDeath(Unit unit, Unit attacker, Formation formation) { }

    }
}