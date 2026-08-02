using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TopsonGames.AI.Grid;

namespace TopsonGames.AI
{
    [CreateAssetMenu(fileName = "Tactic_EstablishFrontLine", menuName = "TopsonGames/AI/Tactic/Establish Front Line")]
    public class Tactic_EstablishFrontLine : AITacticSO
    {
        [Header("Tactic Settings")]
        public float directOpponentBonus = 50f;
        public float assignmentPenalty = 75f;

        [Header("Grid Pathing Fine Tuning")]
        public float maxRiskTolerance = 30f;
        public float skipWaypointDistance = 5.0f;
        [Tooltip("Melee fighters ignore the danger just before reaching their target in order to trigger the final clash.")]
        public float ignoreTargetDangerDistance = 35f;
        public float ignoreStartDangerDistance = 15f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            return availableFormations.Any(f => f.CurrentCombatState == Formation.CombatState.Melee) ? 80f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            var myMeleeFormations = availableFormations.Where(f => f.CurrentCombatState == Formation.CombatState.Melee).ToList();
            if (myMeleeFormations.Count == 0) return;

            var activeEnemies = allEnemies.Where(e => e.CurrentState != Formation.FormationState.Fleeing).ToList();
            if (activeEnemies.Count == 0) return;

            var enemyFrontline = activeEnemies.Where(f => f.CurrentCombatState == Formation.CombatState.Melee).ToList();
            if (enemyFrontline.Count == 0)
            {
                Vector3 myCenterFallback = myMeleeFormations[0].CalculateUnitCenter();
                enemyFrontline = activeEnemies.OrderBy(e => Vector3.Distance(myCenterFallback, e.CalculateUnitCenter())).Take(myMeleeFormations.Count).ToList();
            }
            if (enemyFrontline.Count == 0) return;

            Vector3 myCenter = Vector3.zero;
            foreach (var f in myMeleeFormations) myCenter += f.CalculateUnitCenter();
            myCenter /= myMeleeFormations.Count;

            Vector3 enemyCenter = Vector3.zero;
            Vector3 enemyForwardSum = Vector3.zero;
            foreach (var f in enemyFrontline)
            {
                enemyCenter += f.CalculateUnitCenter();
                enemyForwardSum += f.transform.forward;
            }
            enemyCenter /= enemyFrontline.Count;

            Vector3 avgEnemyForward = (enemyForwardSum / enemyFrontline.Count).normalized;

            Vector3 battleForward = -avgEnemyForward;
            if (battleForward == Vector3.zero) battleForward = (enemyCenter - myCenter).normalized;

            Vector3 battleRight = Quaternion.Euler(0, 90, 0) * battleForward;

            var sortedMyMelee = myMeleeFormations.OrderBy(f => Vector3.Dot(f.transform.position, battleRight)).ToList();
            var sortedEnemyMelee = enemyFrontline.OrderBy(f => Vector3.Dot(f.transform.position, battleRight)).ToList();

            var targetAssignments = new Dictionary<Formation, int>();
            foreach (var enemy in activeEnemies) { targetAssignments[enemy] = 0; }

            for (int i = 0; i < sortedMyMelee.Count; i++)
            {
                var myFormation = sortedMyMelee[i];
                Formation bestTarget = null;
                float bestScore = -Mathf.Infinity;

                for (int j = 0; j < sortedEnemyMelee.Count; j++)
                {
                    var enemyTarget = sortedEnemyMelee[j];
                    float damageScore = GameManager.instance.damageModifier.GetUnitModifier(myFormation.UnitData.unitType, enemyTarget.UnitData.unitType) * 100f;
                    float positionalBonus = (i == j) ? directOpponentBonus : -Mathf.Abs(i - j) * 20f;
                    float assignmentScore = -targetAssignments[enemyTarget] * assignmentPenalty;

                    float finalScore = damageScore + positionalBonus + assignmentScore;

                    if (finalScore > bestScore)
                    {
                        bestScore = finalScore;
                        bestTarget = enemyTarget;
                    }
                }

                if (bestTarget != null)
                {
                    bool useFallback = true;
                    targetAssignments[bestTarget]++;

                    if (BattleGridManager.instance != null)
                    {
                        BattleGrid dangerMap = BattleGridManager.instance.GetDangerMap(commander.teamID, myFormation.UnitData.unitType);
                        if (dangerMap != null)
                        {
                            Vector3 startPos = myFormation.CalculateUnitCenter();
                            Vector3 endPos = bestTarget.CalculateUnitCenter();
                            int clearance = BattleGridManager.instance.pathfindingClearance;

                            List<Vector3> safePath = dangerMap.FindSafePath(
                                startPos, endPos, maxRiskTolerance, ignoreTargetDangerDistance, ignoreStartDangerDistance, clearance
                            );

                            if (safePath != null && safePath.Count > 0)
                            {
                                myFormation.gridWaypoints = safePath;
                                myFormation.currentGridWaypointIndex = 0;
                                myFormation.customEnemy = bestTarget;
                                myFormation.currentGridRiskTolerance = maxRiskTolerance;
                                myFormation.currentGridFlankDistance = 0f;

                                myFormation.currentGridSkipWaypointDistance = skipWaypointDistance;
                                myFormation.currentGridIgnoreTargetDangerDist = ignoreTargetDangerDistance;
                                myFormation.currentGridIgnoreStartDangerDist = ignoreStartDangerDistance;

                                myFormation.useGridPath = true;

                                while (myFormation.currentGridWaypointIndex < safePath.Count - 1 &&
                                       Vector3.Distance(startPos, safePath[myFormation.currentGridWaypointIndex]) < skipWaypointDistance)
                                {
                                    myFormation.currentGridWaypointIndex++;
                                }

                                myFormation.finalDestination = safePath[myFormation.currentGridWaypointIndex];
                                myFormation.CurrentState = Formation.FormationState.MovingToWaypoint;
                                useFallback = false;
                            }
                        }
                    }

                    if (useFallback)
                    {
                        myFormation.useGridPath = false;
                        myFormation.gridWaypoints.Clear();
                        myFormation.SetCustomTarget(bestTarget);
                    }
                }
            }
            commander.CommitFormationsToAction(myMeleeFormations);
        }
    }
}