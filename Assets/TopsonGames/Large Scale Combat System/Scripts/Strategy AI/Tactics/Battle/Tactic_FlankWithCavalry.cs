using System.Collections.Generic;
using System.Linq;
using TopsonGames.AI.Grid;
using UnityEngine;

namespace TopsonGames.AI
{
    [CreateAssetMenu(fileName = "Tactic_FlankWithCavalry", menuName = "TopsonGames/AI/Tactic/Flank With Cavalry")]
    public class Tactic_FlankWithCavalry : AITacticSO
    {
        [Header("Tactic Settings")]
        [Range(0f, 1f)] public float chanceForEarlyAttack = 0.1f;
        public UnitTypeSO[] CavalryTypes;
        public UnitTypeSO[] VulnerableTargetTypes;

        [Header("Fallback Settings")]
        public float fallbackFlankDistance = 40f;

        [Header("Grid Settings")]
        public float maxRiskTolerance = 20f;
        public float rearFlankDistance = 15f;

        [Header("Grid Pathing Fine Tuning")]
        [Tooltip("Control points closer than this value are skipped (to prevent stuttering at start-up).")]
        public float skipWaypointDistance = 5.0f;
        [Tooltip("In the final X metres to the finish line, danger is ignored in order to ensure the charge through the red lines.")]
        public float ignoreTargetDangerDistance = 35f;
        [Tooltip("For the first X metres, danger is ignored so that the unit can break out of encirclement.")]
        public float ignoreStartDangerDistance = 15f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (CavalryTypes == null || CavalryTypes.Length == 0) return 0f;
            bool hasAvailableCavalry = availableFormations.Any(f => CavalryTypes.Contains(f.UnitData.unitType));
            if (!hasAvailableCavalry) return 0f;

            var activeEnemies = allEnemies.Where(e => e.CurrentState != Formation.FormationState.Fleeing).ToList();
            bool hasVulnerableTargets = activeEnemies.Any(f => VulnerableTargetTypes.Contains(f.UnitData.unitType));
            if (!hasVulnerableTargets) return 0f;

            bool frontLinesEngaged = commander.GetAllMyFormations().Any(f =>
                f.CurrentCombatState == Formation.CombatState.Melee && f.customEnemy != null
            );

            bool shouldAttack = frontLinesEngaged || Random.value < chanceForEarlyAttack;

            return shouldAttack ? 95f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            var cavalryFormations = availableFormations.Where(f => CavalryTypes.Contains(f.UnitData.unitType)).ToList();
            if (cavalryFormations.Count == 0) return;

            var activeEnemies = allEnemies.Where(e => e.CurrentState != Formation.FormationState.Fleeing).ToList();

            foreach (var cav in cavalryFormations)
            {
                bool useFallback = true;
                Formation bestTarget = null;
                Vector3 flankDestination = Vector3.zero;

                bestTarget = activeEnemies
                    .OrderByDescending(e =>
                    {
                        float dmgModifier = GameManager.instance.damageModifier.GetUnitModifier(cav.UnitData.unitType, e.UnitData.unitType);
                        float distance = Vector3.Distance(cav.CalculateUnitCenter(), e.CalculateUnitCenter());
                        return (dmgModifier * 1000f) - distance;
                    })
                    .FirstOrDefault();

                if (bestTarget == null) continue;

                if (BattleGridManager.instance != null)
                {
                    BattleGrid dangerMap = BattleGridManager.instance.GetDangerMap(commander.teamID, cav.UnitData.unitType);

                    if (dangerMap != null)
                    {
                        Vector3 myCenter = cav.CalculateUnitCenter();
                        Vector3 targetForward = bestTarget.transform.forward;

                        flankDestination = bestTarget.CalculateUnitCenter() - (targetForward * rearFlankDistance);
                        int clearance = BattleGridManager.instance.pathfindingClearance;

                        List<Vector3> safePath = dangerMap.FindSafePath(
                            myCenter,
                            flankDestination,
                            maxRiskTolerance,
                            ignoreTargetDangerDistance,
                            ignoreStartDangerDistance,
                            clearance
                        );

                        if (safePath != null && safePath.Count > 0)
                        {
                            cav.gridWaypoints = safePath;
                            cav.currentGridWaypointIndex = 0;
                            cav.customEnemy = bestTarget;
                            cav.currentGridRiskTolerance = maxRiskTolerance;
                            cav.currentGridFlankDistance = rearFlankDistance;

                            cav.currentGridSkipWaypointDistance = skipWaypointDistance;
                            cav.currentGridIgnoreTargetDangerDist = ignoreTargetDangerDistance;
                            cav.currentGridIgnoreStartDangerDist = ignoreStartDangerDistance;

                            cav.useGridPath = true;

                            while (cav.currentGridWaypointIndex < safePath.Count - 1 &&
                                   Vector3.Distance(myCenter, safePath[cav.currentGridWaypointIndex]) < skipWaypointDistance)
                            {
                                cav.currentGridWaypointIndex++;
                            }

                            cav.finalDestination = safePath[cav.currentGridWaypointIndex];
                            cav.CurrentState = Formation.FormationState.MovingToWaypoint;

                            useFallback = false;
                        }
                    }
                }

                if (useFallback)
                {
                    cav.useGridPath = false;
                    cav.gridWaypoints.Clear();

                    Vector3 targetCenter = bestTarget.CalculateUnitCenter();
                    Vector3 myCavCenter = cav.CalculateUnitCenter();
                    Vector3 targetRight = bestTarget.transform.right;

                    Vector3 flankDirection = (Vector3.Dot(myCavCenter - targetCenter, targetRight) > 0) ? targetRight : -targetRight;
                    flankDestination = targetCenter + flankDirection * fallbackFlankDistance;
                    Quaternion targetRotation = Quaternion.LookRotation(targetCenter - flankDestination);

                    cav.SetCustomTarget(bestTarget);
                    cav.WaypointCenter.position = flankDestination;
                    cav.WaypointCenter.rotation = targetRotation;
                    cav.SetMoveOrder(true);
                }
            }

            commander.CommitFormationsToAction(cavalryFormations);
        }
    }
}