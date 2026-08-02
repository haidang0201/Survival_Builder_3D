namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Tactic_ManTheWalls", menuName = "TopsonGames/AI/Tactic/Man The Walls")]
    public class Tactic_ManTheWalls : AITacticSO
    {
        [Tooltip("Which unit types should be placed on the walls?")]
        public UnitTypeSO[] RangedWallUnits;
        [Tooltip("What is the maximum number of zones that should be reassigned per tactical run?")]
        public int MaxZonesToAssignPerTick = 2;

        [Header("Dynamik")]
        [Tooltip("How much higher must the priority of a new zone be for a unit to leave its current position? (Prevents jittering)")]
        public float repositionThreshold = 30f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (!commander.IsInSiege() || !commander.IsSiegeDefender()) return 0f;
            if (SiegeManager.instance == null) return 0f;

            bool hasRanged = availableFormations.Any(f =>
                f != null &&
                f.UnitData != null &&
                RangedWallUnits.Contains(f.UnitData.unitType) &&
                f.UnitData.unitType.canManWalls
            );

            return (hasRanged) ? 85f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            var availableRanged = availableFormations.Where(f =>
               f != null &&
               f.UnitData != null &&
               RangedWallUnits.Contains(f.UnitData.unitType) &&
               f.UnitData.unitType.canManWalls
           ).ToList();

            if (availableRanged.Count == 0) return;

            var unoccupiedZones = SiegeManager.instance.GetUnoccupiedWallZones()
                .OrderByDescending(z => z.GetDynamicPriority())
                .ToList();

            var allDefensiveZones = Object.FindObjectsByType<DefensiveZone>(FindObjectsSortMode.None);

            List<Formation> assignedFormations = new List<Formation>();
            int assignedCount = 0;

            foreach (var unitFormation in availableRanged)
            {
                if (unitFormation == null) continue;

                DefensiveZone currentZone = allDefensiveZones.FirstOrDefault(z => z.GetOccupier() == unitFormation);

                if (unoccupiedZones.Count == 0)
                {
                    if (currentZone != null)
                    {
                        assignedFormations.Add(unitFormation);
                    }
                    continue;
                }

                DefensiveZone bestFreeZone = unoccupiedZones.First();

                if (currentZone != null)
                {
                    float currentPrio = currentZone.GetDynamicPriority();
                    float bestFreePrio = bestFreeZone.GetDynamicPriority();

                    if (bestFreePrio <= currentPrio + repositionThreshold)
                    {
                        assignedFormations.Add(unitFormation); 
                        continue; 
                    }
                    else
                    {
                        currentZone.SetOccupier(null);
                    }
                }

                if (assignedCount < MaxZonesToAssignPerTick)
                {
                    bestFreeZone.SetOccupier(unitFormation);

                    unitFormation.WaypointCenter.position = bestFreeZone.transform.position;
                    unitFormation.WaypointCenter.rotation = bestFreeZone.transform.rotation;

                    float unitSpacing = Mathf.Max(unitFormation.UnitData.formationSpacing, 0.1f);
                    int targetWidth = Mathf.FloorToInt(bestFreeZone.zoneWidth / unitSpacing) + 1;
                    int numUnits = unitFormation.numberOfUnits;
                    int minW = FormationController.instance.minFormationWidth;
                    int maxW = FormationController.instance.allowSingleRowFormations ? numUnits : Mathf.Max(1, numUnits - 1);
                    if (numUnits < minW) minW = numUnits;
                    if (maxW < minW) maxW = minW;
                    if (targetWidth < 1) targetWidth = 1;
                    unitFormation.formationWidth = Mathf.Clamp(targetWidth, minW, maxW);

                    unitFormation.SetMoveOrder();

                    assignedFormations.Add(unitFormation);
                    unoccupiedZones.Remove(bestFreeZone); 
                    assignedCount++;
                }
                else if (currentZone != null)
                {
                    assignedFormations.Add(unitFormation);
                }
            }
            commander.CommitFormationsToAction(assignedFormations);
        }
    }
}