using UnityEngine;
using UnityEngine.AI;

namespace TopsonGames
{
    public interface ICombatBehaviour
    {
        void InitializeUnit(Unit unit, Formation formation);
        void InitializeFormation(Formation formation);
        void TickCombat(Unit unit, float deltaTime, Coroutine movementRoutine);
        void TickIdle(Unit unit, NavMeshAgent agent, Formation formation, Coroutine movementRoutine);
        void TickMovement(Unit unit, NavMeshAgent agent, Formation formation, Coroutine movementRoutine);
        bool ShouldEngage(Unit self, Unit potentialTarget); 
        void OnMovementTick(Unit unit);
        Unit OnFindClosestEnemy(Unit unit, Formation formation);
        void DrawGizmosOnBehaviour(Formation formation);
        void OnUpdateUnit(Unit unit, Formation formation);
        void OnUpdateFormation(Formation formation);
        void OnReportArrows(Formation formation);
        void OnReportArrowsUnit(Unit unit, Coroutine arrowRoutine);
        void OnTakeDamage(float Dammage, Unit unit, Unit attacker, bool shieldHit = false);
        void OnDeath(Unit unit, Unit attacker, Formation formation);
    }

}