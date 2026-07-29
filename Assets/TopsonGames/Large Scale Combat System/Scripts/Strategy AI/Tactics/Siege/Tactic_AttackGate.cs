namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Tactic_AttackGate", menuName = "TopsonGames/AI/Tactic/Attack Gate")]
    public class Tactic_AttackGate : AITacticSO
    {
        [Tooltip("Which unit types should attack the gate? (Melee, Ram)")]
        public UnitTypeSO[] GateBreakerTypes;
        [Tooltip("Maximum number of zones per tick.")]
        public int MaxZonesToAssignPerTick = 2;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (!commander.IsInSiege() || commander.IsSiegeDefender()) return 0f;
            if (SiegeManager.instance == null) return 0f;

            DestructibleTarget bestGate = SiegeManager.instance.GetBestTargetGate(commander.transform.position);
            if (bestGate == null) return 0f;

            bool hasBreakers = availableFormations.Any(f => f != null && f.UnitData != null && GateBreakerTypes.Contains(f.UnitData.unitType));
            bool zonesAvailable = SiegeManager.instance.GetUnoccupiedAttackerZones(GateBreakerTypes.ToList(), bestGate).Count > 0;

            return (hasBreakers && zonesAvailable) ? 100f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (SiegeManager.instance == null) return;

            DestructibleTarget targetGate = SiegeManager.instance.GetBestTargetGate(commander.transform.position);
            if (targetGate == null) return;

            var breakers = availableFormations
                .Where(f => f != null && f.UnitData != null && GateBreakerTypes.Contains(f.UnitData.unitType))
                .OrderByDescending(f => f.armyData.Troops) 
                .ToList();

            if (breakers.Count == 0) return;

            var sortedZones = SiegeManager.instance.GetUnoccupiedAttackerZones(GateBreakerTypes.ToList(), targetGate)
                .OrderBy(z => Vector3.Distance(z.transform.position, commander.transform.position))
                .ToList();

            List<Formation> assignedFormations = new List<Formation>();
            int assignedCount = 0;

            int safety = 0;

            while (assignedCount < MaxZonesToAssignPerTick && sortedZones.Count > 0 && breakers.Count > 0)
            {
                safety++;
                if (safety > 100) break;

                Formation meleeFormation = breakers.First();
                AttackerZone bestZone = sortedZones.First();

                if (meleeFormation == null || bestZone == null)
                {
                    if (meleeFormation == null) breakers.RemoveAt(0);
                    if (bestZone == null) sortedZones.RemoveAt(0);
                    continue;
                }

                bestZone.SetOccupier(meleeFormation);
                 Debug.Log($"[Tactic_AttackGate] Attacker {meleeFormation.name} to gate {targetGate.name} (zone: {bestZone.name})");

                meleeFormation.WaypointCenter.position = bestZone.transform.position;
                meleeFormation.WaypointCenter.rotation = bestZone.transform.rotation;

                if (bestZone.zoneWidth > 0)
                {
                    float unitSpacing = Mathf.Max(meleeFormation.UnitData.formationSpacing, 0.1f);
                    int targetWidth = Mathf.FloorToInt(bestZone.zoneWidth / unitSpacing) + 1;
                    int numUnits = meleeFormation.numberOfUnits;
                    int minW = FormationController.instance.minFormationWidth;
                    int maxW = FormationController.instance.allowSingleRowFormations ? numUnits : Mathf.Max(1, numUnits - 1);

                    if (numUnits < minW) minW = numUnits;
                    if (maxW < minW) maxW = minW;
                    if (targetWidth < 1) targetWidth = 1;

                    meleeFormation.formationWidth = Mathf.Clamp(targetWidth, minW, maxW);
                }

                meleeFormation.SetMoveOrder();
                assignedFormations.Add(meleeFormation);

                sortedZones.Remove(bestZone);
                breakers.Remove(meleeFormation);

                assignedCount++;
            }

            commander.CommitFormationsToAction(assignedFormations);
        }
    }
}