namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.AI;

    [CreateAssetMenu(fileName = "Tactic_CapturePoints", menuName = "TopsonGames/AI/Tactic/Capture Points")]
    public class Tactic_CapturePoints : AITacticSO
    {
        public UnitTypeSO[] CaptureUnitTypes;

        [Header("Unit Type Specifics")]
        public UnitTypeSO[] ArcherTypes;

        [Header("Weights")]
        public float assignmentPenaltyWeight = 50f;
        public float priorityWeight = 100f;
        public float enemyThreatWeight = 200f;

        [Header("Archer Logic")]
        public LayerMask sightBlockerLayers;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (commander == null || !commander.IsInSiege() || commander.IsSiegeDefender()) return 0f;
            if (SiegeManager.instance == null) return 0f;

            if (!SiegeManager.instance.AreAnyGatesDestroyed())
            {
                return 0f;
            }


            if (CaptureUnitTypes == null || CaptureUnitTypes.Length == 0)
            {
                return 0f;
            }

            List<SiegeCaptureZone> victoryPoints = SiegeManager.instance.GetAttackerVictoryPoints();
            if (victoryPoints == null || victoryPoints.Count == 0)
            {
                return 0f;
            }

            bool hasUnits = availableFormations.Any(f => f != null && f.UnitData != null && CaptureUnitTypes.Contains(f.UnitData.unitType));
            if (!hasUnits)
            {
                return 0f;
            }
            return 150f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (commander == null || SiegeManager.instance == null) return;
            if (availableFormations == null || CaptureUnitTypes == null) return;

            var captureUnits = availableFormations
                .Where(f => f != null && f.UnitData != null && CaptureUnitTypes.Contains(f.UnitData.unitType))
                .ToList();

            if (captureUnits.Count == 0)
            {
                return;
            }

            List<SiegeCaptureZone> victoryPoints = SiegeManager.instance.GetAttackerVictoryPoints();
            if (victoryPoints == null || victoryPoints.Count == 0) return;

            foreach (var zone in victoryPoints)
            {
                if (zone != null) zone.ResetChokePointUsage();
            }

            List<Formation> assignedFormations = new List<Formation>();
            Dictionary<SiegeCaptureZone, int> assignmentsInThisTick = new Dictionary<SiegeCaptureZone, int>();

            foreach (var formation in captureUnits)
            {
                if (formation == null || formation.UnitData == null) continue;

                SiegeCaptureZone bestZone = null;
                float bestScore = float.MaxValue;

                foreach (var zone in victoryPoints)
                {
                    if (zone == null) continue;

                    float distanceScore = Vector3.Distance(formation.CalculateUnitCenter(), zone.GetZoneCenter());
                    int assignedCount = assignmentsInThisTick.GetValueOrDefault(zone, 0);
                    float assignmentScore = assignedCount * assignmentPenaltyWeight;
                    float priorityScore = -zone.GetPriority() * priorityWeight;
                    int defenderCount = zone.GetDefenderCount();
                    float threatScore = defenderCount * enemyThreatWeight;

                    float finalScore = distanceScore + assignmentScore + priorityScore + threatScore;

                    if (finalScore < bestScore)
                    {
                        bestScore = finalScore;
                        bestZone = zone;
                    }
                }

                if (bestZone != null)
                {
                    Vector3 targetPos = bestZone.GetZoneCenter();
                    Quaternion targetRot = formation.transform.rotation;

                    bool isArcher = false;
                    if (ArcherTypes != null && ArcherTypes.Length > 0 && formation.UnitData.unitType != null)
                    {
                        isArcher = ArcherTypes.Contains(formation.UnitData.unitType);
                    }

                    bool useChokePoint = true;

                    if (isArcher)
                    {
                        float range = 20f;
                        if (formation.UnitData.combatBehaviour is ArcherCombatSO ac && ac != null)
                        {
                            range = ac.attackRange * 0.85f;
                        }

                        float distToZone = Vector3.Distance(formation.CalculateUnitCenter(), bestZone.GetZoneCenter());

                        if (distToZone <= range * 0.9f)
                        {
                            useChokePoint = false;
                            targetPos = CalculateBestArcherPosition(formation, bestZone, captureUnits);
                            Vector3 lookDir = (bestZone.GetZoneCenter() - targetPos).normalized;
                            if (lookDir != Vector3.zero)
                                targetRot = Quaternion.LookRotation(lookDir);
                        }
                    }

                    if (useChokePoint)
                    {
                        if (bestZone.chokePoints != null && bestZone.chokePoints.Count > 0)
                        {
                            var validChokes = bestZone.chokePoints
                                .Where(cp => cp != null && cp.isAttackerEntry && cp.HasCapacity())
                                .ToList();

                            if (validChokes.Count > 0)
                            {
                                SiegeChokePoint bestChoke = validChokes
                                    .OrderBy(cp => Vector3.Distance(formation.CalculateUnitCenter(), cp.transform.position))
                                    .First();

                                targetPos = bestChoke.transform.position;
                                targetRot = bestChoke.transform.rotation;
                                bestChoke.RegisterUser();
                            }
                            else
                            {
                                targetPos = bestZone.GetZoneCenter();
                            }
                        }
                        else
                        {
                            targetPos = bestZone.GetZoneCenter();
                        }
                    }

                    if (formation.WaypointCenter != null)
                    {
                        formation.WaypointCenter.position = targetPos;
                        formation.WaypointCenter.rotation = targetRot;

                        formation.SetMoveOrder();
                        assignedFormations.Add(formation);
                        assignmentsInThisTick[bestZone] = assignmentsInThisTick.GetValueOrDefault(bestZone, 0) + 1;
                        Debug.Log($"[Tactic_CapturePoints] Send {formation.name} to the capture zone {bestZone.name}");
                    }
                }
            }
            commander.CommitFormationsToAction(assignedFormations);
        }

        private Vector3 CalculateBestArcherPosition(Formation archer, SiegeCaptureZone zone, List<Formation> allMyUnits)
        {
            if (archer == null || zone == null) return Vector3.zero;

            float range = 20f;
            if (archer.UnitData != null && archer.UnitData.combatBehaviour is ArcherCombatSO ac && ac != null)
                range = ac.attackRange * 0.85f;

            Vector3 zoneCenter = zone.GetZoneCenter();
            Vector3 basePos;

            Formation nearbyMelee = null;
            if (allMyUnits != null && ArcherTypes != null)
            {
                nearbyMelee = allMyUnits
                    .Where(f => f != null && f != archer && f.UnitData != null &&
                           !ArcherTypes.Contains(f.UnitData.unitType) &&
                           Vector3.Distance(f.transform.position, zoneCenter) < range * 1.5f)
                    .OrderBy(f => Vector3.Distance(f.transform.position, archer.transform.position))
                    .FirstOrDefault();
            }

            if (nearbyMelee != null)
            {
                Vector3 dirToZone = (zoneCenter - nearbyMelee.transform.position).normalized;
                basePos = nearbyMelee.transform.position - dirToZone * 10f;
            }
            else
            {
                Vector3 dirToArcher = (archer.transform.position - zoneCenter).normalized;
                if (dirToArcher == Vector3.zero) dirToArcher = Vector3.forward;
                basePos = zoneCenter + dirToArcher * range;
            }

            for (int i = 0; i < 5; i++)
            {
                float angle = (i - 2) * 15f;
                Vector3 testPos = RotatePointAroundPivot(basePos, zoneCenter, Quaternion.Euler(0, angle, 0));

                if (NavMesh.SamplePosition(testPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    Vector3 origin = hit.position + Vector3.up * 1.7f;
                    Vector3 target = zoneCenter + Vector3.up * 1.0f;
                    Vector3 dir = target - origin;

                    if (dir.magnitude > 0.1f)
                    {
                        if (!Physics.Raycast(origin, dir.normalized, out RaycastHit rayHit, dir.magnitude, sightBlockerLayers))
                            return hit.position;
                    }
                }
            }
            return basePos;
        }

        private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            return rotation * (point - pivot) + pivot;
        }
    }
}