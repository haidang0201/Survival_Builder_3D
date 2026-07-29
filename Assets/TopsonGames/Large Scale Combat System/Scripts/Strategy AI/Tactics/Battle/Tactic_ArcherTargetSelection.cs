using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TopsonGames.AI.Grid;

namespace TopsonGames.AI
{
    [CreateAssetMenu(fileName = "Tactic_ArcherTargetSelection", menuName = "TopsonGames/AI/Tactic/Archer Target Selection")]
    public class Tactic_ArcherTargetSelection : AITacticSO
    {
        [Header("Targeting Preferences")]
        public UnitTypeSO[] VulnerableTargetTypes;
        public float vulnerabilityBonus = 30f;
        public float stickinessBonus = 20f;

        [Header("Targeting Fine Tuning")]
        [Tooltip("How heavily is distance penalised? The higher the penalty, the more you should aim for nearby targets.")]
        public float distancePenaltyWeight = 0.5f;
        [Tooltip("How severely are targets penalised if their firing position is in the red zone? Higher = greater safety.")]
        public float dangerPenaltyWeight = 2.0f;

        [Header("Fallback Tactic Settings")]
        public LayerMask enemyBlockingLayer;
        public float archerShootDistanceVariable = 0.7f;
        public float archerDangerShootDistanceVariable = 0.8f;

        [Header("Grid Pathing Fine Tuning")]
        public float maxRiskTolerance = 15f;
        public float skipWaypointDistance = 5.0f;
        [Tooltip("It must be 0! Archers must stay strictly outside the danger zone.")]
        public float ignoreTargetDangerDistance = 0f;
        public float ignoreStartDangerDistance = 10f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            return availableFormations.Any(f => f.CurrentCombatState == Formation.CombatState.Archer) ? 60f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            var archerFormations = availableFormations.Where(f => f.CurrentCombatState == Formation.CombatState.Archer).ToList();
            if (archerFormations.Count == 0) return;

            var activeEnemies = allEnemies.Where(e => e.CurrentState != Formation.FormationState.Fleeing).ToList();
            if (activeEnemies.Count == 0) return;

            var enemyTargetCounts = new Dictionary<Formation, int>();
            foreach (var enemy in activeEnemies) { enemyTargetCounts[enemy] = 0; }

            foreach (var archerFormation in archerFormations)
            {
                var archerBehaviour = archerFormation.UnitData.combatBehaviour as ArcherCombatSO;
                if (archerBehaviour == null) continue;

                Vector3 myCenter = archerFormation.CalculateUnitCenter();
                BattleGrid dangerMap = null;

                if (BattleGridManager.instance != null)
                {
                    dangerMap = BattleGridManager.instance.GetDangerMap(commander.teamID, archerFormation.UnitData.unitType);
                }

                var bestTarget = activeEnemies
                    .Select(enemy => {
                        Vector3 targetCenter = enemy.CalculateUnitCenter();
                        Vector3 directionToEnemy = (targetCenter - myCenter).normalized;
                        Vector3 idealFiringPosition = targetCenter - directionToEnemy * (archerBehaviour.archerDetectionRange * archerShootDistanceVariable);

                        float damageScore = GameManager.instance.damageModifier.GetUnitModifier(archerFormation.UnitData.unitType, enemy.UnitData.unitType) * 10f;
                        float distance = Vector3.Distance(myCenter, targetCenter);
                        float distancePenalty = distance * distancePenaltyWeight;
                        float assignmentPenalty = enemyTargetCounts[enemy] * 2.0f;

                        float vulBonus = (VulnerableTargetTypes != null && VulnerableTargetTypes.Contains(enemy.UnitData.unitType)) ? vulnerabilityBonus : 0f;
                        float stickyBonus = (archerFormation.customEnemy == enemy) ? stickinessBonus : 0f;

                        float gridDangerPenalty = 0f;
                        float pathSafetyScore = 0f;

                        if (dangerMap != null)
                        {
                            float dangerAtShootPos = dangerMap.GetValue(idealFiringPosition);
                            gridDangerPenalty = dangerAtShootPos * dangerPenaltyWeight;
                        }
                        else
                        {
                            pathSafetyScore = CalculatePathSafetyFallback(archerFormation, enemy, activeEnemies, archerBehaviour);
                        }

                        float finalScore = damageScore + pathSafetyScore - distancePenalty - assignmentPenalty + vulBonus + stickyBonus - gridDangerPenalty;
                        return new { Target = enemy, Score = finalScore };
                    })
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault()?.Target;

                if (bestTarget != null)
                {
                    bool targetChanged = (archerFormation.customEnemy != bestTarget);
                    archerFormation.SetCustomTarget(bestTarget);
                    enemyTargetCounts[bestTarget]++;

                    Vector3 targetCenter = bestTarget.CalculateUnitCenter();
                    float currentDistance = Vector3.Distance(myCenter, targetCenter);

                    bool needsToMove = targetChanged || currentDistance > (archerBehaviour.archerDetectionRange * archerShootDistanceVariable);

                    if (needsToMove)
                    {
                        bool useFallback = true;
                        Vector3 directionToEnemy = (targetCenter - myCenter).normalized;
                        Vector3 idealFiringPosition = targetCenter - directionToEnemy * (archerBehaviour.archerDetectionRange * archerShootDistanceVariable);

                        if (dangerMap != null)
                        {
                            int clearance = BattleGridManager.instance.pathfindingClearance;

                            Vector3 safeFiringPos = dangerMap.GetNearestSafePosition(idealFiringPosition, maxRiskTolerance, clearance);

                            List<Vector3> safePath = dangerMap.FindSafePath(
                                myCenter, safeFiringPos, maxRiskTolerance, ignoreTargetDangerDistance, ignoreStartDangerDistance, clearance
                            );

                            if (safePath != null && safePath.Count > 0)
                            {
                                archerFormation.gridWaypoints = safePath;
                                archerFormation.currentGridWaypointIndex = 0;
                                archerFormation.customEnemy = bestTarget;
                                archerFormation.currentGridRiskTolerance = maxRiskTolerance;
                                archerFormation.currentGridFlankDistance = (archerBehaviour.archerDetectionRange * archerShootDistanceVariable);
                                archerFormation.gridPathTargetsEnemyRear = false;

                                archerFormation.currentGridSkipWaypointDistance = skipWaypointDistance;
                                archerFormation.currentGridIgnoreTargetDangerDist = ignoreTargetDangerDistance;
                                archerFormation.currentGridIgnoreStartDangerDist = ignoreStartDangerDistance;

                                archerFormation.useGridPath = true;

                                while (archerFormation.currentGridWaypointIndex < safePath.Count - 1 &&
                                       Vector3.Distance(myCenter, safePath[archerFormation.currentGridWaypointIndex]) < skipWaypointDistance)
                                {
                                    archerFormation.currentGridWaypointIndex++;
                                }

                                archerFormation.finalDestination = safePath[archerFormation.currentGridWaypointIndex];
                                archerFormation.CurrentState = Formation.FormationState.MovingToWaypoint;
                                useFallback = false;
                            }
                        }

                        if (useFallback)
                        {
                            archerFormation.useGridPath = false;
                            archerFormation.gridWaypoints.Clear();
                            archerFormation.WaypointCenter.position = idealFiringPosition;
                            archerFormation.WaypointCenter.rotation = Quaternion.LookRotation(directionToEnemy);
                            archerFormation.SetMoveOrder(true);
                        }
                    }
                }
            }
            commander.CommitFormationsToAction(archerFormations);
        }

        private float CalculatePathSafetyFallback(Formation archerFormation, Formation target, List<Formation> allEnemies, ArcherCombatSO archerBehaviour)
        {
            Vector3 myPos = archerFormation.CalculateUnitCenter();
            Vector3 targetPos = target.CalculateUnitCenter();
            Vector3 firingPos = targetPos - (targetPos - myPos).normalized * (archerBehaviour.archerDetectionRange * archerDangerShootDistanceVariable);

            float pathDistance = Vector3.Distance(myPos, firingPos);
            if (pathDistance < 1f) return 0;

            Vector3 pathDirection = (firingPos - myPos).normalized;

            if (Physics.BoxCast(myPos, archerFormation.GetUnitCenterCollider().bounds.extents, pathDirection, Quaternion.LookRotation(pathDirection), pathDistance, enemyBlockingLayer))
            {
                return -1000f;
            }

            return 0;
        }
    }
}