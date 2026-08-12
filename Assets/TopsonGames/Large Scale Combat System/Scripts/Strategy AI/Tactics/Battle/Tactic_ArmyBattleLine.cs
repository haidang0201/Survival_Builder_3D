using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TopsonGames.AI.Grid;

namespace TopsonGames.AI
{

    [CreateAssetMenu(fileName = "Tactic_ArmyBattleLine", menuName = "TopsonGames/AI/Tactic/Army Battle Line")]
    public class Tactic_ArmyBattleLine : AITacticSO
    {
        [Header("Unit Selection")]
        [Tooltip("Empty = all formations in melee mode. Otherwise, ONLY these unit types.")]
        public UnitTypeSO[] onlyTheseTypes;
        [Tooltip("NEVER deploy these units in line formation (e.g. cavalry, elephants, musketeers).")]
        public UnitTypeSO[] excludeTypes;

        [Header("Line Geometry")]
        [Tooltip("Lateral distance between two formations on the line (metres). ~ Formation width + buffer.")]
        public float lineSpacing = 20f;
        [Tooltip("How far in front of the enemy line the deployment line is formed.")]
        public float standoffDistance = 22f;

        [Header("Phase Control")]
        [Tooltip("If the distance between the army and the enemy falls below this value, a close-quarters charge will be launched.")]
        public float chargeRange = 26f;
        [Tooltip("Once a formation is this close to its own slot, it is considered to be ‘deployed’ (triggering the previous charge).")]
        public float slotTolerance = 7f;

        [Header("Targeting")]
        [Tooltip("Is a formation allowed to switch to a direct neighbour if the matchup there is significantly better?")]
        public bool allowNeighbourSwap = true;
        [Tooltip("A significant matchup advantage for a neighbour swap (0.4 = +40% damage).")]
        public float neighbourSwapThreshold = 0.4f;

        [Header("Approach (Grid optional)")]
        [Tooltip("Only move around the zone in the event of EXTREME danger (e.g. large groups of archers). Default = head-on.")]
        public bool avoidExtremeDangerOnApproach = true;
        public float extremeDangerThreshold = 120f;
        public float maxRiskTolerance = 120f;
        public float skipWaypointDistance = 5f;
        public float ignoreTargetDangerDistance = 40f;
        public float ignoreStartDangerDistance = 15f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            bool hasMelee = availableFormations.Any(f => f != null && f.CurrentCombatState == Formation.CombatState.Melee);
            bool hasEnemies = allEnemies.Any(e => e != null && e.CurrentState != Formation.FormationState.Fleeing);
            return (hasMelee && hasEnemies) ? 82f : 0f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            var myMelee = availableFormations.Where(f =>
            f != null &&
            f.CurrentCombatState == Formation.CombatState.Melee &&
            (onlyTheseTypes == null || onlyTheseTypes.Length == 0 || onlyTheseTypes.Contains(f.UnitData.unitType)) &&
            (excludeTypes == null || !excludeTypes.Contains(f.UnitData.unitType))).ToList();
            if (myMelee.Count == 0) return;

            var enemies = allEnemies.Where(e => e != null && e.CurrentState != Formation.FormationState.Fleeing).ToList();
            if (enemies.Count == 0) return;

            Vector3 myCenter = Average(myMelee.Select(f => f.CalculateUnitCenter()));

            var enemyLine = enemies.Where(e => e.CurrentCombatState == Formation.CombatState.Melee).ToList();
            if (enemyLine.Count == 0) enemyLine = enemies;

            Vector3 enemyCenter = Average(enemyLine.Select(f => f.CalculateUnitCenter()));

            Vector3 battleForward = enemyCenter - myCenter;
            battleForward.y = 0f;
            if (battleForward.sqrMagnitude < 0.01f)
            {
                Vector3 fwdSum = Vector3.zero;
                foreach (var e in enemyLine) fwdSum += e.transform.forward;
                battleForward = -fwdSum;
                if (battleForward.sqrMagnitude < 0.01f) battleForward = Vector3.forward;
            }
            battleForward.Normalize();
            Vector3 battleRight = Vector3.Cross(Vector3.up, battleForward);

            float armyGap = Vector3.Distance(myCenter, enemyCenter);

            var sortedMine = myMelee.OrderBy(f => Vector3.Dot(f.CalculateUnitCenter(), battleRight)).ToList();
            var sortedEnemy = enemyLine.OrderBy(f => Vector3.Dot(f.CalculateUnitCenter(), battleRight)).ToList();

            int n = sortedMine.Count;
            int m = sortedEnemy.Count;
            Vector3 lineCenter = enemyCenter - battleForward * standoffDistance;
            Quaternion faceEnemy = Quaternion.LookRotation(battleForward);

            var slots = new Vector3[n];
            float maxSlotDist = 0f;
            for (int i = 0; i < n; i++)
            {
                float lateral = (i - (n - 1) * 0.5f) * lineSpacing;
                slots[i] = lineCenter + battleRight * lateral;
                maxSlotDist = Mathf.Max(maxSlotDist, Vector3.Distance(sortedMine[i].CalculateUnitCenter(), slots[i]));
            }

            bool formedUp = maxSlotDist <= slotTolerance;
            bool shouldCharge = armyGap <= chargeRange || formedUp;

            var assignCount = new Dictionary<Formation, int>();
            foreach (var e in sortedEnemy) assignCount[e] = 0;
            var assignment = new Formation[n];

            for (int i = 0; i < n; i++)
            {
                int baseIdx = (m == 1) ? 0 : Mathf.RoundToInt(i * (float)(m - 1) / Mathf.Max(1, n - 1));
                baseIdx = Mathf.Clamp(baseIdx, 0, m - 1);
                Formation chosen = sortedEnemy[baseIdx];

                if (allowNeighbourSwap && m > 1)
                {
                    float bestMod = Modifier(sortedMine[i], chosen);
                    for (int d = -1; d <= 1; d++)
                    {
                        int idx = Mathf.Clamp(baseIdx + d, 0, m - 1);
                        var cand = sortedEnemy[idx];
                        float mod = Modifier(sortedMine[i], cand);

                        if (mod >= bestMod + neighbourSwapThreshold && assignCount[cand] <= assignCount[chosen])
                        {
                            chosen = cand; bestMod = mod;
                        }
                    }
                }
                assignment[i] = chosen;
                assignCount[chosen]++;
            }
            for (int i = 0; i < n; i++)
            {
                var f = sortedMine[i];
                var target = assignment[i];

                if (shouldCharge)
                {

                    f.useGridPath = false;
                    f.gridWaypoints.Clear();
                    f.SetCustomTarget(target);

                    Vector3 chargeFrom = f.CalculateUnitCenter();
                    Vector3 chargeTo = target.CalculateUnitCenter();
                    Vector3 chargeDir = chargeTo - chargeFrom;
                    chargeDir.y = 0f;

                    f.WaypointCenter.position = chargeTo;
                    if (chargeDir.sqrMagnitude > 0.001f)
                        f.WaypointCenter.rotation = Quaternion.LookRotation(chargeDir.normalized);
                    else
                        f.WaypointCenter.rotation = faceEnemy;

                    f.SetMoveOrder(true);
                }
                else
                {
                    Vector3 slot = slots[i];
                    bool moved = false;

                    if (avoidExtremeDangerOnApproach && BattleGridManager.instance != null)
                    {
                        var dangerMap = BattleGridManager.instance.GetDangerMap(commander.teamID, f.UnitData.unitType);
                        if (dangerMap != null && dangerMap.GetValue(slot) > extremeDangerThreshold)
                            moved = TryGridApproach(commander, f, slot);
                    }

                    if (!moved)
                    {
                        f.useGridPath = false;
                        f.gridWaypoints.Clear();
                        f.SetCustomTarget(null);
                        f.WaypointCenter.position = slot;
                        f.WaypointCenter.rotation = faceEnemy;
                        f.SetMoveOrder(false);
                    }
                }
            }

            commander.CommitFormationsToAction(myMelee);
        }

        private bool TryGridApproach(AICommander commander, Formation f, Vector3 dest)
        {
            var dangerMap = BattleGridManager.instance.GetDangerMap(commander.teamID, f.UnitData.unitType);
            if (dangerMap == null) return false;

            int clearance = BattleGridManager.instance.pathfindingClearance;
            Vector3 start = f.CalculateUnitCenter();
            var path = dangerMap.FindSafePath(start, dest, maxRiskTolerance, ignoreTargetDangerDistance, ignoreStartDangerDistance, clearance);
            if (path == null || path.Count == 0) return false;

            f.gridWaypoints = path;
            f.currentGridWaypointIndex = 0;
            f.customEnemy = null;
            f.currentGridRiskTolerance = maxRiskTolerance;
            f.currentGridSkipWaypointDistance = skipWaypointDistance;
            f.currentGridIgnoreTargetDangerDist = ignoreTargetDangerDistance;
            f.currentGridIgnoreStartDangerDist = ignoreStartDangerDistance;
            f.useGridPath = true;

            while (f.currentGridWaypointIndex < path.Count - 1 &&
                   Vector3.Distance(start, path[f.currentGridWaypointIndex]) < skipWaypointDistance)
                f.currentGridWaypointIndex++;

            f.finalDestination = path[f.currentGridWaypointIndex];
            f.CurrentState = Formation.FormationState.MovingToWaypoint;
            return true;
        }

        private float Modifier(Formation attacker, Formation defender)
        {
            if (GameManager.instance == null || GameManager.instance.damageModifier == null) return 1f;
            return GameManager.instance.damageModifier.GetUnitModifier(attacker.UnitData.unitType, defender.UnitData.unitType);
        }

        private static Vector3 Average(IEnumerable<Vector3> pts)
        {
            Vector3 sum = Vector3.zero; int c = 0;
            foreach (var p in pts) { sum += p; c++; }
            return c > 0 ? sum / c : Vector3.zero;
        }
    }
}