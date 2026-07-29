namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using TopsonGames;

    public class SiegeManager : MonoBehaviour
    {
        public static SiegeManager instance;

        [Header("Team Roles")]
        public int attackerTeamID = 0;
        public int defenderTeamID = 1;

        [Header("Siege Objectives")]
        public List<DestructibleTarget> mainGates;
        public List<SiegeCaptureZone> captureZones;

        [Header("Win Condition Settings")]
        public float pointsToWin = 2000f;
        public float battleTimeLimit = 1200f;

        [Header("Runtime Status")]
        [SerializeField] private float currentAttackerScore = 0f;
        [SerializeField] private float currentBattleTimer;
        [SerializeField] private float currentPointsPerSecond = 0f;

        [Header("Defensive Zones")]
        public List<DefensiveZone> wallZones;
        public List<DefensiveZone> gateZones;

        [Header("Attacker Zones")]
        public List<AttackerZone> attackerZones;

        [Header("Debugging")]
        public bool DrawZoneGizmos = true;

        private float zoneOccupationTimer = 0f;
        private const float ZoneOccupationCheckInterval = 0.2f;

        private readonly HashSet<Formation> _reusableFormationSet = new HashSet<Formation>();

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {

            if (wallZones != null) foreach (var zone in wallZones) if (zone != null) zone.SetOccupier(null);
            if (gateZones != null) foreach (var zone in gateZones) if (zone != null) zone.SetOccupier(null);
            if (attackerZones != null) foreach (var zone in attackerZones) if (zone != null) zone.SetOccupier(null);

            currentBattleTimer = battleTimeLimit;
            currentAttackerScore = 0f;
        }

        private void Update()
        {
            if (GameManager.instance.isGameOver) return;

            currentBattleTimer -= Time.deltaTime;
            if (currentBattleTimer <= 0)
            {
                Debug.Log("Time's up! The defender wins.");
                TriggerWinForTeam(defenderTeamID);
                return;
            }

            CalculateSiegeScore();

            zoneOccupationTimer -= Time.deltaTime;
            if (zoneOccupationTimer <= 0f)
            {
                zoneOccupationTimer = ZoneOccupationCheckInterval;
                UpdateZoneOccupation();
            }
        }

        private void CalculateSiegeScore()
        {
            if (captureZones == null) return;

            float scoreDelta = 0f;
            foreach (var zone in captureZones)
            {
                if (zone == null || !zone.ContributesToResult()) continue;

                if (zone.GetControllingTeam() == attackerTeamID && zone.GetStatus() == SiegeCaptureZone.CaptureStatus.ControlledAttacker)
                {
                    scoreDelta += zone.priority;
                }
                else if (zone.GetControllingTeam() == defenderTeamID && zone.GetStatus() == SiegeCaptureZone.CaptureStatus.ControlledDefender)
                {
                    scoreDelta -= zone.priority;
                }
            }

            currentPointsPerSecond = scoreDelta;
            currentAttackerScore += scoreDelta * Time.deltaTime;
            currentAttackerScore = Mathf.Clamp(currentAttackerScore, 0f, pointsToWin + 100f);

            if (currentAttackerScore >= pointsToWin)
            {
                Debug.Log("Attacker has collected enough victory points! Victory for the attacker.");
                TriggerWinForTeam(attackerTeamID);
            }
        }

        private void TriggerWinForTeam(int winningTeamID)
        {
            if (GameManager.instance.isGameOver) return;

            int playerTeamID = -1;
            if (FormationController.instance != null)
            {
                playerTeamID = FormationController.instance.TeamID;
            }

            if (playerTeamID == winningTeamID)
            {
                GameManager.instance.OnPlayerWin.Invoke();
            }
            else
            {
                GameManager.instance.OnEnemyWin.Invoke();
            }
            GameManager.instance.isGameOver = true;
        }

        private void UpdateZoneOccupation()
        {
            _reusableFormationSet.Clear();
            if (FormationController.instance != null)
            {
                var trackedFormations = FormationController.instance.GetFormations();
                if (trackedFormations != null)
                {
                    for (int i = 0; i < trackedFormations.Count; i++)
                    {
                        if (trackedFormations[i] != null) _reusableFormationSet.Add(trackedFormations[i]);
                    }
                }
            }
            HashSet<Formation> currentFormations = _reusableFormationSet;
            if (wallZones != null)
            {
                foreach (var zone in wallZones)
                {
                    if (zone == null) continue;

                    if (zone.IsOccupied())
                    {
                        Formation occupier = zone.GetOccupier();

                        if (occupier == null || !occupier.gameObject.activeInHierarchy || !currentFormations.Contains(occupier))
                        {
                            zone.SetOccupier(null);
                            continue;
                        }
                        if (occupier.CurrentState != Formation.FormationState.Idle &&
                            occupier.CurrentState != Formation.FormationState.MovingToWaypoint &&
                            occupier.CurrentState != Formation.FormationState.Engaged)
                        {
                            zone.SetOccupier(null);
                        }
                    }
                }
            }
            if (attackerZones != null)
            {
                foreach (var zone in attackerZones)
                {
                    if (zone == null) continue;

                    if (zone.IsOccupied())
                    {
                        Formation occupier = zone.GetOccupier();
                        if (occupier == null || !occupier.gameObject.activeInHierarchy || !currentFormations.Contains(occupier))
                        {
                            zone.SetOccupier(null);
                        }
                    }
                }
            }
            if (gateZones != null)
            {
                foreach (var zone in gateZones)
                {
                    if (zone == null) continue;

                    if (zone.IsOccupied())
                    {
                        Formation occupier = zone.GetOccupier();
                        if (occupier == null || !occupier.gameObject.activeInHierarchy || !currentFormations.Contains(occupier))
                        {
                            zone.SetOccupier(null);
                        }
                    }
                }
            }
        }


        public bool IsGateDestroyed(DestructibleTarget gate)
        {
            if (gate == null) return true;
            if (!gate.gameObject.activeInHierarchy) return true;

            GateController gc = gate.GetComponent<GateController>();
            if (gc != null && gc.isDestroyed) return true;

            if (gate.currentHealth <= 0) return true;

            return false;
        }

        public bool AreAnyGatesDestroyed()
        {
            if (mainGates == null || mainGates.Count == 0) return false;
            bool anyDestroyed = mainGates.Any(g => IsGateDestroyed(g));

            return anyDestroyed;
        }


        public DestructibleTarget GetBestTargetGate(Vector3 attackerPosition)
        {
            if (mainGates == null) return null;
            var intactGates = mainGates.Where(g => !IsGateDestroyed(g)).ToList();
            if (intactGates.Count == 0) return null;

            DestructibleTarget bestGate = null;
            float bestScore = float.MinValue;

            foreach (var gate in intactGates)
            {
                float distance = Vector3.Distance(attackerPosition, gate.transform.position);
                int defenders = 0;
                GateController controller = gate.GetComponent<GateController>();
                if (controller != null) defenders = controller.GetDefenderCount();
                else if (gate.GetOwner<GateController>() != null) defenders = gate.GetOwner<GateController>().GetDefenderCount();

                float defenderPenaltyWeight = 15.0f;
                float score = -(distance) - (defenders * defenderPenaltyWeight);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestGate = gate;
                }
            }
            return bestGate;
        }

        public List<DefensiveZone> GetUnoccupiedWallZones(DestructibleTarget linkedStructure = null)
        {
            var query = wallZones?.Where(z => z != null && !z.IsOccupied());
            if (linkedStructure != null) query = query?.Where(z => z.linkedStructure == linkedStructure);
            return query?.ToList() ?? new List<DefensiveZone>();
        }

        public List<DefensiveZone> GetUnoccupiedGateZones(DestructibleTarget linkedStructure = null)
        {
            var query = gateZones?.Where(z => z != null && !z.IsOccupied());
            if (linkedStructure != null) query = query?.Where(z => z.linkedStructure == linkedStructure);
            return query?.ToList() ?? new List<DefensiveZone>();
        }

        public List<AttackerZone> GetUnoccupiedAttackerZones(List<UnitTypeSO> preferredTypes = null, DestructibleTarget linkedStructure = null)
        {
            var query = attackerZones?.Where(z => z != null && !z.IsOccupied());
            if (linkedStructure != null) query = query?.Where(z => z.linkedStructure == linkedStructure);
            if (preferredTypes != null && preferredTypes.Count > 0) query = query?.Where(z => z.preferredUnitTypes == null || z.preferredUnitTypes.Count == 0 || z.preferredUnitTypes.Any(pt => preferredTypes.Contains(pt)));
            return query?.ToList() ?? new List<AttackerZone>();
        }

        public List<SiegeCaptureZone> GetAttackerVictoryPoints()
        {
            return captureZones?.Where(z => z != null && z.isVictoryPoint && z.GetControllingTeam() != attackerTeamID).ToList() ?? new List<SiegeCaptureZone>();
        }

        public List<SiegeCaptureZone> GetDefenderVictoryPoints()
        {
            return captureZones?.Where(z => z != null && z.isVictoryPoint && z.GetControllingTeam() != defenderTeamID).ToList() ?? new List<SiegeCaptureZone>();
        }

        public void ValidateOccupiedZonesAndPositions(List<Formation> currentFormations)
        {
            ValidateDefensiveZoneList(wallZones, currentFormations);
            ValidateDefensiveZoneList(gateZones, currentFormations);
            ValidateAttackerZoneList(attackerZones, currentFormations);
        }

        private void ValidateDefensiveZoneList(List<DefensiveZone> zones, List<Formation> currentFormations)
        {
            if (zones == null) return;
            foreach (var zone in zones)
            {
                if (zone != null && zone.IsOccupied())
                {
                    Formation occupier = zone.GetOccupier();
                    if (occupier == null || !currentFormations.Contains(occupier) ||
                        (occupier.CurrentState != Formation.FormationState.Idle && occupier.CurrentState != Formation.FormationState.MovingToWaypoint && occupier.CurrentState != Formation.FormationState.Engaged))
                    {
                        zone.SetOccupier(null);
                    }
                }
            }
        }

        private void ValidateAttackerZoneList(List<AttackerZone> zones, List<Formation> currentFormations)
        {
            if (zones == null) return;
            foreach (var zone in zones)
            {
                if (zone != null && zone.IsOccupied())
                {
                    Formation occupier = zone.GetOccupier();
                    if (occupier == null || !currentFormations.Contains(occupier) ||
                        (occupier.CurrentState != Formation.FormationState.Idle && occupier.CurrentState != Formation.FormationState.MovingToWaypoint && occupier.CurrentState != Formation.FormationState.Engaged))
                    {
                        zone.SetOccupier(null);
                    }
                }
            }
        }

        public Formation GetOccupyingFormation(Component zoneComponent)
        {
            if (zoneComponent is DefensiveZone dz) return dz.GetOccupier();
            if (zoneComponent is AttackerZone az) return az.GetOccupier();
            return null;
        }

        public bool IsFormationStationedOnWall(Formation formation)
        {
            if (wallZones == null) return false;

            foreach (var zone in wallZones)
            {
                if (zone != null && zone.GetOccupier() == formation)
                {
                    return true;
                }
            }
            return false;
        }

        public float GetCurrentAttackerScore() => currentAttackerScore;
        public float GetPointsToWin() => pointsToWin;
        public float GetBattleTimer() => currentBattleTimer;
        public float GetCurrentPointsPerSecond() => currentPointsPerSecond;
    }
}