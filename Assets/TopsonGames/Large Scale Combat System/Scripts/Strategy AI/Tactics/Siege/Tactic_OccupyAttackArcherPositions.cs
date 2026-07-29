namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Tactic_OccupyArcherPositions", menuName = "TopsonGames/AI/Tactic/Occupy Archer Positions")]
    public class Tactic_OccupyArcherPositions : AITacticSO
    {
        [Tooltip("Which unit types should occupy these positions?")]
        public UnitTypeSO[] ArcherUnitTypes;
        [Tooltip("Maximum number of zones per tick.")]
        public int MaxZonesToAssignPerTick = 2;
        [Tooltip("Maximum attack range of archers (for zone selection).")]
        public float MaxArcherRange = 60f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (!commander.IsInSiege() || commander.IsSiegeDefender()) return 0f;
            if (SiegeManager.instance == null) return 0f;

            DestructibleTarget bestGate = SiegeManager.instance.GetBestTargetGate(commander.transform.position);
            if (bestGate == null) return 0f;

            bool hasArchers = availableFormations.Any(f => f != null && f.UnitData != null && ArcherUnitTypes.Contains(f.UnitData.unitType));
            bool zonesAvailable = SiegeManager.instance.GetUnoccupiedAttackerZones(ArcherUnitTypes.ToList(), bestGate).Count > 0;

            return (hasArchers && zonesAvailable) ? 75f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (SiegeManager.instance == null) return;

            DestructibleTarget targetGate = SiegeManager.instance.GetBestTargetGate(commander.transform.position);
            if (targetGate == null) return;

            var archers = availableFormations
                .Where(f => f != null && f.UnitData != null && ArcherUnitTypes.Contains(f.UnitData.unitType))
                .ToList();

            if (archers.Count == 0) return;

            var sortedZones = SiegeManager.instance.GetUnoccupiedAttackerZones(ArcherUnitTypes.ToList(), targetGate)
                .OrderBy(z => Vector3.Distance(z.transform.position, commander.transform.position))
                .ToList();

            List<Formation> assignedFormations = new List<Formation>();
            int assignedCount = 0;

            int safety = 0;

            while (assignedCount < MaxZonesToAssignPerTick && sortedZones.Count > 0 && archers.Count > 0)
            {
                safety++;
                if (safety > 100) break;

                Formation archerFormation = archers.First();
                AttackerZone bestZone = sortedZones.First();

                if (archerFormation == null || bestZone == null)
                {
                    if (archerFormation == null) archers.RemoveAt(0);
                    if (bestZone == null) sortedZones.RemoveAt(0);
                    continue;
                }

                bestZone.SetOccupier(archerFormation);
                 Debug.Log($"[Tactic_OccupyArcherPositions] Archer {archerFormation.name} to Gate {targetGate.name} (Zone: {bestZone.name})");

                archerFormation.WaypointCenter.position = bestZone.transform.position;
                archerFormation.WaypointCenter.rotation = bestZone.transform.rotation;

                if (bestZone.zoneWidth > 0)
                {
                    float unitSpacing = Mathf.Max(archerFormation.UnitData.formationSpacing, 0.1f);
                    int targetWidth = Mathf.FloorToInt(bestZone.zoneWidth / unitSpacing) + 1;
                    int numUnits = archerFormation.numberOfUnits;
                    int minW = FormationController.instance.minFormationWidth;
                    int maxW = FormationController.instance.allowSingleRowFormations ? numUnits : Mathf.Max(1, numUnits - 1);

                    if (numUnits < minW) minW = numUnits;
                    if (maxW < minW) maxW = minW;
                    if (targetWidth < 1) targetWidth = 1;

                    archerFormation.formationWidth = Mathf.Clamp(targetWidth, minW, maxW);
                }

                archerFormation.SetMoveOrder();
                assignedFormations.Add(archerFormation);

                sortedZones.Remove(bestZone);
                archers.Remove(archerFormation);

                assignedCount++;
            }
            commander.CommitFormationsToAction(assignedFormations);
        }
    }
}