namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Tactic_DefendGate", menuName = "TopsonGames/AI/Tactic/Defend Gate")]
    public class Tactic_DefendGate : AITacticSO
    {
        public UnitTypeSO[] MeleeGateDefenders;
        public int MaxUnitsToAssignPerTick = 3;
        [Tooltip("How many attackers at the sensor are required for the gate to be considered 'threatened'?")]
        public int minAttackersForThreat = 1;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (!commander.IsInSiege() || !commander.IsSiegeDefender()) return 0f;
            if (SiegeManager.instance == null || SiegeManager.instance.mainGates == null) return 0f;

            bool hasDefenders = availableFormations.Any(f => f != null && f.UnitData != null && MeleeGateDefenders.Contains(f.UnitData.unitType));
            if (!hasDefenders) return 0f;

            bool gateUnderThreat = false;
            foreach (var gate in SiegeManager.instance.mainGates)
            {
                if (gate != null && gate.currentHealth > 0 && !SiegeManager.instance.IsGateDestroyed(gate))
                {
                    GateController gc = gate.GetComponent<GateController>(); 
                    if (gc == null) gc = gate.GetOwner<GateController>();

                    if (gc != null && gc.GetAttackerCount() >= minAttackersForThreat)
                    {
                        gateUnderThreat = true;
                        break;
                    }
                }
            }

            bool zonesAvailable = SiegeManager.instance.GetUnoccupiedGateZones().Count > 0;

            return (gateUnderThreat && zonesAvailable) ? 95f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            var availableDefenders = availableFormations
                .Where(f => f != null && f.UnitData != null && MeleeGateDefenders.Contains(f.UnitData.unitType))
                .OrderByDescending(f => f.armyData.Troops)
                .ToList();

            if (availableDefenders.Count == 0) return;

            var threatenedGates = SiegeManager.instance.mainGates
                .Where(g => g != null && !SiegeManager.instance.IsGateDestroyed(g) && g.GetComponent<GateController>().GetAttackerCount() >= minAttackersForThreat)
                .OrderByDescending(g => g.GetComponent<GateController>().GetAttackerCount())
                .ToList();

            if (threatenedGates.Count == 0) return;

            List<Formation> assignedFormations = new List<Formation>();
            int assignedCount = 0;
            int safety = 0;

            foreach (var gate in threatenedGates)
            {
                var availableZones = SiegeManager.instance.GetUnoccupiedGateZones(gate);

                while (assignedCount < MaxUnitsToAssignPerTick && availableZones.Count > 0 && availableDefenders.Count > 0)
                {
                    safety++;
                    if (safety > 100) break;

                    var bestZone = availableZones.First();
                    var unitFormation = availableDefenders.First();

                    if (bestZone == null || unitFormation == null)
                    {
                        if (bestZone == null) availableZones.RemoveAt(0);
                        if (unitFormation == null) availableDefenders.RemoveAt(0);
                        continue;
                    }

                    bestZone.SetOccupier(unitFormation);
                     Debug.Log($"[Tactic_DefendGate] Defender {unitFormation.name} at gate {gate.name}");

                    unitFormation.WaypointCenter.position = bestZone.transform.position;
                    unitFormation.WaypointCenter.rotation = bestZone.transform.rotation;

                    float unitSpacing = Mathf.Max(unitFormation.UnitData.formationSpacing, 0.1f);
                    int targetWidth = Mathf.FloorToInt(bestZone.zoneWidth / unitSpacing) + 1;
                    int numUnits = unitFormation.numberOfUnits;
                    int minW = FormationController.instance.minFormationWidth;
                    int maxW = FormationController.instance.allowSingleRowFormations ? numUnits : Mathf.Max(1, numUnits - 1);
                    if (numUnits < minW) minW = numUnits;
                    if (maxW < minW) maxW = minW;
                    if (targetWidth < 1) targetWidth = 1;
                    unitFormation.formationWidth = Mathf.Clamp(targetWidth, minW, maxW);

                    unitFormation.SetMoveOrder();
                    assignedFormations.Add(unitFormation);

                    availableZones.Remove(bestZone);
                    availableDefenders.Remove(unitFormation);

                    assignedCount++;
                }

                if (safety > 100) break; 
            }
            commander.CommitFormationsToAction(assignedFormations);
        }
    }
}