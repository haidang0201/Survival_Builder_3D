using UnityEngine;
using TopsonGames.AI.Grid;

namespace TopsonGames.AI
{
    [DisallowMultipleComponent]
    public class SkirmisherKiteAgent : MonoBehaviour
    {
        private Formation formation;

        [Header("Runtime configuration (set by the Tactic)")]
        [Tooltip("If an enemy comes closer than this -> ride away immediately. It MUST be larger than the engagement collider!")]
        public float fleeDistance = 24f;
        [Tooltip("Target distance when riding away as a proportion of the firing range.")]
        [Range(0.4f, 1f)] public float standoffFactor = 0.85f;
        [Tooltip("Unit’s actual firing range (archerDetectionRange).")]
        public float shootRange = 40f;
        [Tooltip("Recalculation of the escape response every X seconds.")]
        public float reactInterval = 0.25f;
        [Tooltip("Optimise escape route using the danger map (optional).")]
        public bool useDangerMap = true;

        private float timer;

        private void Awake()
        {
            formation = GetComponent<Formation>();
            if (formation == null)
            {
                Debug.LogError("[SkirmisherKiteAgent] No Formation component found on the same GameObject.", this);
                enabled = false;
            }
        }

        public void Configure(float fleeDistance, float standoffFactor, float shootRange, float reactInterval, bool useDangerMap)
        {
            this.fleeDistance = fleeDistance;
            this.standoffFactor = standoffFactor;
            this.shootRange = shootRange;
            this.reactInterval = reactInterval;
            this.useDangerMap = useDangerMap;
        }

        private void Update()
        {
            if (formation == null) { enabled = false; return; }

            if (formation.CurrentState == Formation.FormationState.Fleeing) return;
            if (formation.currentArrows <= 0) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = reactInterval;

            Formation nearest = FindNearestEnemy(out float dist);
            if (nearest == null || dist > fleeDistance) return; 

            if (formation.CurrentState == Formation.FormationState.Engaged)
                formation.Disengage();

            Vector3 myPos = formation.CalculateUnitCenter();
            Vector3 away = myPos - nearest.CalculateUnitCenter(); away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -formation.transform.forward;
            away.Normalize();

            Vector3 fleeTarget = nearest.CalculateUnitCenter() + away * (shootRange * standoffFactor);

            if (useDangerMap && BattleGridManager.instance != null)
            {
                var dangerMap = BattleGridManager.instance.GetDangerMap(formation.TeamID, formation.UnitData.unitType);
                if (dangerMap != null)
                {
                    int clearance = BattleGridManager.instance.pathfindingClearance;
                    fleeTarget = dangerMap.GetNearestSafePosition(fleeTarget, formation.currentGridRiskTolerance, clearance);
                }
            }

            formation.useGridPath = false;
            formation.gridWaypoints.Clear();
            formation.WaypointCenter.position = fleeTarget;
            formation.WaypointCenter.rotation = Quaternion.LookRotation(-away);
            formation.SetMoveOrder(false);
        }

        private Formation FindNearestEnemy(out float dist)
        {
            dist = float.MaxValue;
            Formation nearest = null;
            if (FormationController.instance == null) return null;

            Vector3 myPos = formation.CalculateUnitCenter();
            foreach (var e in FormationController.instance.GetFormations())
            {
                if (e == null || e.TeamID == formation.TeamID) continue;
                if (e.CurrentState == Formation.FormationState.Fleeing) continue;
                float d = Vector3.Distance(myPos, e.CalculateUnitCenter());
                if (d < dist) { dist = d; nearest = e; }
            }
            return nearest;
        }
    }
}