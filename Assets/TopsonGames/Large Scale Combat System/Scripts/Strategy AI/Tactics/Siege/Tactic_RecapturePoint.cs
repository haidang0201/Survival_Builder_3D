
namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.AI;

    [CreateAssetMenu(fileName = "Tactic_RecapturePoints", menuName = "TopsonGames/AI/Tactic/Recapture Points")]
    public class Tactic_RecapturePoints : AITacticSO
    {
        [Tooltip("Which unit types should recapture points?")]
        public UnitTypeSO[] RecaptureUnitTypes;

        [Header("Unit Type Specifics")]
        [Tooltip("Define here which of the above types are ARCHERS.")]
        public UnitTypeSO[] ArcherTypes;

        [Header("Weights")]
        [Tooltip("How severely is the number of units already assigned penalized?")]
        public float assignmentPenaltyWeight = 50f;
        [Tooltip("How much is priority rewarded?")]
        public float priorityWeight = 100f;
        [Tooltip("How much is the number of attackers in the zone rewarded?")]
        public float enemyThreatWeight = 200f;
        [Tooltip("How severely is it penalized if multiple formations use the same approach point?")]
        public float approachPointCrowdingPenalty = 30f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (commander == null) return 0f;
            if (!commander.IsInSiege() || !commander.IsSiegeDefender()) return 0f;
            if (SiegeManager.instance == null) return 0f;

            if (RecaptureUnitTypes == null || RecaptureUnitTypes.Length == 0) return 0f;

            List<SiegeCaptureZone> lostPoints = SiegeManager.instance.GetDefenderVictoryPoints();
            if (lostPoints == null || lostPoints.Count == 0) return 0f;

            if (availableFormations == null) return 0f;

            bool hasUnits = availableFormations.Any(f => f != null && f.UnitData != null && RecaptureUnitTypes.Contains(f.UnitData.unitType));
            return hasUnits ? 100f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (commander == null || SiegeManager.instance == null) return;
            if (availableFormations == null) return;
            if (RecaptureUnitTypes == null) return;

            var recaptureUnits = availableFormations
                .Where(f => f != null && f.UnitData != null && RecaptureUnitTypes.Contains(f.UnitData.unitType))
                .ToList();

            if (recaptureUnits.Count == 0) return;

            List<SiegeCaptureZone> lostPoints = SiegeManager.instance.GetDefenderVictoryPoints();
            if (lostPoints == null || lostPoints.Count == 0) return;

            List<Formation> assignedFormations = new List<Formation>();
            Dictionary<SiegeCaptureZone, int> assignmentsInThisTick = new Dictionary<SiegeCaptureZone, int>();
            Dictionary<Transform, int> approachPointUsage = new Dictionary<Transform, int>();

            foreach (var formation in recaptureUnits)
            {
                if (formation == null || formation.UnitData == null) continue;

                SiegeCaptureZone bestZone = null;
                float bestScore = float.MaxValue;

                foreach (var zone in lostPoints)
                {
                    if (zone == null) continue;

                    float distanceScore = Vector3.Distance(formation.CalculateUnitCenter(), zone.GetZoneCenter());
                    int assignedCount = assignmentsInThisTick.GetValueOrDefault(zone, 0);
                    float assignmentScore = assignedCount * assignmentPenaltyWeight;
                    float priorityScore = -zone.GetPriority() * priorityWeight;

                    int attackerCount = zone.GetAttackerCount();
                    float threatScore = -attackerCount * enemyThreatWeight;

                    float finalScore = distanceScore + assignmentScore + priorityScore + threatScore;

                    if (finalScore < bestScore)
                    {
                        bestScore = finalScore;
                        bestZone = zone;
                    }
                }

                if (bestZone != null)
                {
                    Vector3 targetPos;
                    Quaternion targetRot;

                    bool isArcher = false;
                    if (ArcherTypes != null && ArcherTypes.Length > 0 && formation.UnitData.unitType != null)
                    {
                        isArcher = ArcherTypes.Contains(formation.UnitData.unitType);
                    }

                    if (isArcher)
                    {
                        float range = 0f;
                        if (formation.UnitData.combatBehaviour is ArcherCombatSO archerCombat && archerCombat != null)
                        {
                            range = archerCombat.attackRange * 0.85f;
                        }
                        else range = 20f;

                        Vector3 zoneCenter = bestZone.GetZoneCenter();
                        Vector3 dirFromZoneToUnit = (formation.CalculateUnitCenter() - zoneCenter).normalized;

                        Vector3 idealPos = zoneCenter + dirFromZoneToUnit * range;

                        if (NavMesh.SamplePosition(idealPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                        {
                            targetPos = hit.position;
                        }
                        else
                        {
                            targetPos = idealPos;
                        }

                        Vector3 lookDir = (zoneCenter - targetPos).normalized;
                        if (lookDir != Vector3.zero)
                            targetRot = Quaternion.LookRotation(lookDir);
                        else
                            targetRot = formation.transform.rotation;
                    }
                    else
                    {
                        if (bestZone.tacticalApproachPoints != null && bestZone.tacticalApproachPoints.Count > 0)
                        {
                            Transform bestApproach = null;
                            float bestApproachScore = float.MaxValue;

                            foreach (var point in bestZone.tacticalApproachPoints)
                            {
                                if (point == null) continue;

                                float dist = Vector3.Distance(formation.CalculateUnitCenter(), point.position);
                                int usage = approachPointUsage.GetValueOrDefault(point, 0);

                                float score = dist + (usage * approachPointCrowdingPenalty);

                                if (score < bestApproachScore)
                                {
                                    bestApproachScore = score;
                                    bestApproach = point;
                                }
                            }

                            if (bestApproach != null)
                            {
                                targetPos = bestApproach.position;
                                Vector3 lookDir = (bestZone.GetZoneCenter() - targetPos).normalized;
                                if (lookDir != Vector3.zero)
                                    targetRot = Quaternion.LookRotation(lookDir);
                                else
                                    targetRot = formation.transform.rotation;

                                approachPointUsage[bestApproach] = approachPointUsage.GetValueOrDefault(bestApproach, 0) + 1;
                            }
                            else
                            {
                                targetPos = bestZone.GetZoneCenter();
                                targetRot = formation.transform.rotation;
                            }
                        }
                        else
                        {
                            targetPos = bestZone.GetZoneCenter();
                            targetRot = formation.transform.rotation;
                        }
                    }

                    if (formation.WaypointCenter != null)
                    {
                        formation.WaypointCenter.position = targetPos;
                        formation.WaypointCenter.rotation = targetRot;

                        formation.SetMoveOrder();
                        assignedFormations.Add(formation);
                        assignmentsInThisTick[bestZone] = assignmentsInThisTick.GetValueOrDefault(bestZone, 0) + 1;
                    }
                }
            }

            Debug.Log($"[Tactic_RecapturePoints] {assignedFormations.Count} formations sent to recapture {assignmentsInThisTick.Count} points.");
            commander.CommitFormationsToAction(assignedFormations);
        }
    }
}
