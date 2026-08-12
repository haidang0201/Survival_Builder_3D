using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TopsonGames.AI.Grid;

namespace TopsonGames.AI
{
    [CreateAssetMenu(fileName = "Tactic_HorseArcherHarass", menuName = "TopsonGames/AI/Tactic/Horse Archer Harass")]
    public class Tactic_HorseArcherHarass : AITacticSO
    {
        [Header("Tactic Settings")]
        public UnitTypeSO[] HorseArcherTypes;
        public UnitTypeSO[] VulnerableTargetTypes;

        [Header("Harassment Settings (Kiting)")]
        [Tooltip("Multiplier for the maximum firing range (e.g. 0.8 = fires at 80% of the maximum range).")]
        public float shootDistanceVariable = 0.8f;
        [Tooltip("If the enemy gets closer than this distance, the Horse Archer flees (kiting).")]
        public float skirmishDistance = 25f;

        [Header("Grid Pathing Fine Tuning")]
        public float maxRiskTolerance = 15f;
        public float skipWaypointDistance = 5.0f;
        [Tooltip("Can Horse archers ride into a danger zone.")]
        public float ignoreTargetDangerDistance = 2f;
        [Tooltip("Allows them to break out if they are surrounded.")]
        public float ignoreStartDangerDistance = 15f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (HorseArcherTypes == null || HorseArcherTypes.Length == 0) return 0f;

            bool hasHorseArchers = availableFormations.Any(f => HorseArcherTypes.Contains(f.UnitData.unitType) && f.currentArrows > 0);
            if (!hasHorseArchers) return 0f;

            return 85f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            var horseArcherFormations = availableFormations.Where(f => HorseArcherTypes.Contains(f.UnitData.unitType) && f.currentArrows > 0).ToList();
            if (horseArcherFormations.Count == 0) return;

            var activeEnemies = allEnemies.Where(e => e.CurrentState != Formation.FormationState.Fleeing).ToList();
            if (activeEnemies.Count == 0) return;

            foreach (var haFormation in horseArcherFormations)
            {
                float detectionRange = 40f;
                if (haFormation.UnitData.combatBehaviour is HorseArcherSingleCombatSO haBehaviour)
                {
                    detectionRange = haBehaviour.archerDetectionRange;
                }
                else if (haFormation.UnitData.combatBehaviour is HorseArcherCombatSO archerBehaviour)
                {
                    detectionRange = archerBehaviour.archerDetectionRange;
                }

                float shootRange = detectionRange * shootDistanceVariable;

                var kite = haFormation.GetComponent<SkirmisherKiteAgent>();
                if (kite == null) kite = haFormation.gameObject.AddComponent<SkirmisherKiteAgent>();
                kite.Configure(
                    fleeDistance: skirmishDistance,
                    standoffFactor: shootDistanceVariable,
                    shootRange: detectionRange,
                    reactInterval: 0.7f,
                    useDangerMap: true
                );

                Formation bestTarget = activeEnemies
                    .OrderByDescending(e =>
                    {
                        float dmgModifier = GameManager.instance.damageModifier.GetUnitModifier(haFormation.UnitData.unitType, e.UnitData.unitType);
                        float distance = Vector3.Distance(haFormation.CalculateUnitCenter(), e.CalculateUnitCenter());
                        float vulBonus = (VulnerableTargetTypes != null && VulnerableTargetTypes.Contains(e.UnitData.unitType)) ? 500f : 0f;

                        return (dmgModifier * 1000f) + vulBonus - distance;
                    })
                    .FirstOrDefault();

                if (bestTarget == null) continue;

                Vector3 myCenter = haFormation.CalculateUnitCenter();
                Vector3 targetCenter = bestTarget.CalculateUnitCenter();
                float distanceToTarget = Vector3.Distance(myCenter, targetCenter);
                Vector3 dirFromTarget = (myCenter - targetCenter).normalized;

                Vector3 idealDestination;

                if (distanceToTarget < skirmishDistance)
                {
                    idealDestination = targetCenter + dirFromTarget * shootRange;
                }
                else if (distanceToTarget > shootRange)
                {
                    idealDestination = targetCenter + dirFromTarget * shootRange;
                }
                else
                {
                    idealDestination = myCenter;
                }

                bool useFallback = true;

                if (BattleGridManager.instance != null)
                {
                    BattleGrid dangerMap = BattleGridManager.instance.GetDangerMap(commander.teamID, haFormation.UnitData.unitType);

                    if (dangerMap != null)
                    {
                        int clearance = BattleGridManager.instance.pathfindingClearance;

                        Vector3 safeDestination = dangerMap.GetNearestSafePosition(idealDestination, maxRiskTolerance, clearance);

                        if (safeDestination == myCenter && distanceToTarget >= skirmishDistance)
                        {
                            haFormation.useGridPath = false;
                            haFormation.gridWaypoints.Clear();
                            haFormation.SetCustomTarget(bestTarget);
                            haFormation.CurrentState = Formation.FormationState.Idle;
                            useFallback = false;
                        }
                        else
                        {
                            List<Vector3> safePath = dangerMap.FindSafePath(
                                myCenter, safeDestination, maxRiskTolerance, ignoreTargetDangerDistance, ignoreStartDangerDistance, clearance
                            );

                            if (safePath != null && safePath.Count > 0)
                            {
                                haFormation.gridWaypoints = safePath;
                                haFormation.currentGridWaypointIndex = 0;

                                haFormation.SetCustomTarget(null);

                                haFormation.currentGridRiskTolerance = maxRiskTolerance;
                                haFormation.currentGridFlankDistance = shootRange;
                                haFormation.gridPathTargetsEnemyRear = false;

                                haFormation.currentGridSkipWaypointDistance = skipWaypointDistance;
                                haFormation.currentGridIgnoreTargetDangerDist = ignoreTargetDangerDistance;
                                haFormation.currentGridIgnoreStartDangerDist = ignoreStartDangerDistance;

                                haFormation.useGridPath = true;

                                while (haFormation.currentGridWaypointIndex < safePath.Count - 1 &&
                                       Vector3.Distance(myCenter, safePath[haFormation.currentGridWaypointIndex]) < skipWaypointDistance)
                                {
                                    haFormation.currentGridWaypointIndex++;
                                }

                                haFormation.finalDestination = safePath[haFormation.currentGridWaypointIndex];
                                haFormation.CurrentState = Formation.FormationState.MovingToWaypoint;
                                useFallback = false;
                            }
                        }
                    }
                }

                if (useFallback)
                {
                    haFormation.useGridPath = false;
                    haFormation.gridWaypoints.Clear();

                    if (distanceToTarget >= skirmishDistance && distanceToTarget <= shootRange)
                    {
                        haFormation.SetCustomTarget(bestTarget);
                        haFormation.CurrentState = Formation.FormationState.Idle;
                    }
                    else
                    {
                        haFormation.SetCustomTarget(null);
                        haFormation.WaypointCenter.position = idealDestination;

                        Vector3 faceDir = targetCenter - idealDestination;
                        faceDir.y = 0f;
                        if (faceDir.sqrMagnitude > 0.001f)
                        {
                            haFormation.WaypointCenter.rotation = Quaternion.LookRotation(faceDir.normalized);
                        }

                        haFormation.SetMoveOrder(false);
                    }
                }
            }

            commander.CommitFormationsToAction(horseArcherFormations);
        }
    }
}