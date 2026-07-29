using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TopsonGames.AI
{

    [CreateAssetMenu(fileName = "Tactic_ReserveFlankGuard", menuName = "TopsonGames/AI/Tactic/Reserve & Flank Guard")]
    public class Tactic_ReserveFlankGuard : AITacticSO
    {
        [Header("Reserve")]
        [Tooltip("Which of your own units can be used as reserves (usually close-quarters fighters)?")]
        public UnitTypeSO[] reserveEligibleTypes;
        [Range(0f, 1f)]
        [Tooltip("Proportion of suitable units held in reserve.")]
        public float reservePercentage = 0.25f;
        [Tooltip("How far behind the army headquarters the reserve is gathering.")]
        public float rallyDistanceBehindCenter = 25f;

        [Header("Flank Detection")]
        [Tooltip("The army’s front cone. Enemies OUTSIDE this cone are considered to be attacking from the flanks or the rear.")]
        [Range(20f, 180f)] public float frontArcDegrees = 100f;
        [Tooltip("Only enemies within this distance of the army centre are considered an immediate threat.")]
        public float flankThreatRange = 65f;
        [Tooltip("Threats that are closer than that are critical (a response is triggered even without a reserve).")]
        public float criticalThreatRange = 35f;

        public override float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            if (reserveEligibleTypes == null || reserveEligibleTypes.Length == 0) return 0f;

            bool hasEligible = availableFormations.Any(f => f != null && reserveEligibleTypes.Contains(f.UnitData.unitType));
            if (!hasEligible) return 0f;

            bool hasEnemies = allEnemies.Any(e => e != null && e.CurrentState != Formation.FormationState.Fleeing);
            if (!hasEnemies) return 0f;
            return 90f;
        }

        public override void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations)
        {
            var eligible = availableFormations.Where(f => f != null && reserveEligibleTypes.Contains(f.UnitData.unitType)).ToList();
            if (eligible.Count == 0) return;

            var enemies = allEnemies.Where(e => e != null && e.CurrentState != Formation.FormationState.Fleeing).ToList();
            if (enemies.Count == 0) return;
            var allMine = commander.GetAllMyFormations().Where(f => f != null).ToList();
            if (allMine.Count == 0) allMine = eligible;

            Vector3 armyCenter = Average(allMine.Select(f => f.CalculateUnitCenter()));
            Vector3 enemyCenter = Average(enemies.Select(f => f.CalculateUnitCenter()));

            Vector3 frontDir = enemyCenter - armyCenter; frontDir.y = 0f;
            if (frontDir.sqrMagnitude < 0.01f) frontDir = Vector3.forward;
            frontDir.Normalize();

            float halfArcCos = Mathf.Cos(frontArcDegrees * 0.5f * Mathf.Deg2Rad);

            var threats = new List<Formation>();
            foreach (var e in enemies)
            {
                Vector3 toEnemy = e.CalculateUnitCenter() - armyCenter; toEnemy.y = 0f;
                float dist = toEnemy.magnitude;
                if (dist > flankThreatRange) continue;

                Vector3 dir = dist > 0.01f ? toEnemy / dist : frontDir;
                float dot = Vector3.Dot(frontDir, dir);
                if (dot < halfArcCos) threats.Add(e); 
            }
            threats = threats.OrderBy(e => (e.CalculateUnitCenter() - armyCenter).sqrMagnitude).ToList();

            int reserveCount = Mathf.Clamp(Mathf.CeilToInt(eligible.Count * reservePercentage), 0, eligible.Count);
            if (reserveCount == 0 && threats.Count > 0) reserveCount = 1; 

            var reserve = eligible
                .OrderByDescending(f => Vector3.Dot((f.CalculateUnitCenter() - armyCenter), frontDir) * -1f) 
                .Take(reserveCount)
                .ToList();

            if (reserve.Count == 0) return;

            Vector3 rallyPoint = armyCenter - frontDir * rallyDistanceBehindCenter;
            var committed = new List<Formation>();
            int t = 0;
            foreach (var guard in reserve)
            {
                if (t < threats.Count)
                {
                    var threat = threats[t];
                    float threatDist = Vector3.Distance(guard.CalculateUnitCenter(), threat.CalculateUnitCenter());

                    guard.useGridPath = false;
                    guard.gridWaypoints.Clear();
                    guard.SetCustomTarget(threat);
                    guard.SetMoveOrder(true);

                    committed.Add(guard);
                    t++;
                    continue;
                }

                guard.useGridPath = false;
                guard.gridWaypoints.Clear();
                guard.SetCustomTarget(null);

                float lateral = (committed.Count - (reserve.Count - 1) * 0.5f) * 18f;
                Vector3 right = Vector3.Cross(Vector3.up, frontDir);
                guard.WaypointCenter.position = rallyPoint + right * lateral;
                guard.WaypointCenter.rotation = Quaternion.LookRotation(frontDir);
                guard.SetMoveOrder(false);

                committed.Add(guard);
            }

            for (; t < threats.Count; t++)
            {
                var threat = threats[t];
                if ((threat.CalculateUnitCenter() - armyCenter).magnitude > criticalThreatRange) break;

                var responder = eligible
                    .Where(f => !committed.Contains(f))
                    .OrderBy(f => (f.CalculateUnitCenter() - threat.CalculateUnitCenter()).sqrMagnitude)
                    .FirstOrDefault();
                if (responder == null) break;

                responder.useGridPath = false;
                responder.gridWaypoints.Clear();
                responder.SetCustomTarget(threat);
                responder.SetMoveOrder(true);
                committed.Add(responder);
            }

            commander.CommitFormationsToAction(committed);
        }

        private static Vector3 Average(IEnumerable<Vector3> pts)
        {
            Vector3 sum = Vector3.zero; int c = 0;
            foreach (var p in pts) { sum += p; c++; }
            return c > 0 ? sum / c : Vector3.zero;
        }
    }
}
