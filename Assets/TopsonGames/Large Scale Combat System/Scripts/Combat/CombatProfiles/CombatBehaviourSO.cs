namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.AI;

    public abstract class CombatBehaviourSO : ScriptableObject, ICombatBehaviour
    {  
        [Header("Enemy Detection Rate")]
        public float unitDetectionTimer = 1f;
        public float formationDetectionTimer = 0.5f;
        public float engagementBreakDistance = 10f;

        [Header("Melee Combat Settings")]
        public float attackRange = 2f;
        public float cooldown = 1.5f;

        [Header("Movement Settings")]
        public float raiseShieldTime = 2f;
        public float switchAnimationTimer = 4f;
        [Range(0f, 1f)]
        public float switchAnimationProbability = 0.5f;


        public abstract void InitializeUnit(Unit unit, Formation formation);
        public abstract void TickCombat(Unit unit, float deltaTime, Coroutine movementRoutine);
        public abstract void TickIdle(Unit unit, NavMeshAgent agent, Formation formation, Coroutine movementRoutine);
        public abstract void TickMovement(Unit unit, NavMeshAgent agent, Formation formation, Coroutine movementRoutine);
        public abstract bool ShouldEngage(Unit self, Unit potentialTarget); 
        public abstract void OnMovementTick(Unit unit);
        public abstract Unit OnFindClosestEnemy(Unit unit, Formation formation);
        public abstract void DrawGizmosOnBehaviour(Formation formation);
        public abstract void OnUpdateUnit(Unit unit, Formation formation);
        public abstract void OnUpdateFormation(Formation formation);
        public abstract void InitializeFormation(Formation formation);
        public abstract void OnReportArrows(Formation formation);
        public abstract void OnReportArrowsUnit(Unit unit, Coroutine arrowRoutine);
        public abstract bool IsFacingTarget(Unit unit, Transform target, float angleThreshold = 30f);
        public abstract void OnTakeDamage(float Damage, Unit unit, Unit attacker, bool shieldHit = false);
        public abstract void OnDeath(Unit unit, Unit attacker, Formation formation);
    }
}
