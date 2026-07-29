namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.AI;

    public class AICommander : MonoBehaviour
    {
        [Header("Strategies")]
        public AIStrategySO defaultStrategy;
        public AIStrategySO siegeAttackerStrategy;
        public AIStrategySO siegeDefenderStrategy;

        [Header("Initial Advance Phase")]
        [Tooltip("Should the AI stubbornly march forward at the start of the battle ? ")]
        public bool enableInitialAdvance = true;
        [Tooltip("Which unit types should advance?")]
        public List<UnitTypeSO> initialAdvanceUnitTypes;
        [Tooltip("How long does the AI wait after the start of the round before advancing ? (in seconds)")]
        public float initialDelay = 2.0f;
        [Tooltip("How long do the units stubbornly march forward before the regular AI takes over ? (in seconds)")]
        public float advanceDuration = 17f;
        [Tooltip("How far forward should they be sent as a target in this phase?")]
        public float advanceDistance = 40.0f;

        [Header("General Settings")]
        public float thinkInterval = 3.0f;
        public int teamID = 1;

        private float thinkTimer = 8f;
        private List<Formation> allMyFormations = new List<Formation>();
        private List<Formation> allEnemyFormations = new List<Formation>();
        private List<Formation> availableFormations = new List<Formation>();

        private bool isInSiege = false;
        private bool isSiegeDefender = false;
        private AIStrategySO currentStrategy;

        private enum CommanderPhase { InitialDelay, Advancing, Normal }
        private CommanderPhase currentPhase;
        private float phaseTimer = 0f;

        void Start()
        {
            if (SiegeManager.instance != null)
            {
                isInSiege = true;
                isSiegeDefender = (teamID == SiegeManager.instance.defenderTeamID);
                Debug.Log($"[AICommander Team {teamID}] Siege detected. Role: {(isSiegeDefender ? "Defender" : "Attacker")}");

                if (isSiegeDefender && siegeDefenderStrategy != null)
                {
                    currentStrategy = siegeDefenderStrategy;
                }
                else if (!isSiegeDefender && siegeAttackerStrategy != null)
                {
                    currentStrategy = siegeAttackerStrategy;
                }
                else
                {
                    Debug.LogWarning($"[AICommander Team {teamID}] In Siege but no specific Siege Strategy found. Using default.", this);
                    currentStrategy = defaultStrategy;
                }
            }
            else
            {
                isInSiege = false;
                Debug.Log($"[AICommander Team {teamID}] No Siege detected. Using default field strategy.");
                currentStrategy = defaultStrategy;
            }

            if (currentStrategy == null)
            {
                Debug.LogError($"[AICommander Team {teamID}] No strategy assigned! Falling back to defaultStrategy.", this);
                currentStrategy = defaultStrategy;
                if (currentStrategy == null)
                {
                    Debug.LogError($"[AICommander Team {teamID}] Default strategy is also null! AI will not function.", this);
                    this.enabled = false;
                }
            }

            if (enableInitialAdvance)
            {
                currentPhase = CommanderPhase.InitialDelay;
                phaseTimer = initialDelay;
                Debug.Log($"[AICommander Team {teamID}] Starting Initial Delay Phase for {initialDelay} seconds.");
            }
            else
            {
                currentPhase = CommanderPhase.Normal;
            }

            RefreshFormations();
        }

        void Update()
        {
            if (currentPhase == CommanderPhase.InitialDelay)
            {
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                {
                    StartInitialAdvance();
                    currentPhase = CommanderPhase.Advancing;
                    phaseTimer = advanceDuration;
                }
            }
            else if (currentPhase == CommanderPhase.Advancing)
            {
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                {
                    Debug.Log($"[AICommander Team {teamID}] Advance Phase finished. Handing over to Strategy System.");
                    currentPhase = CommanderPhase.Normal;
                    thinkTimer = 0f;
                }
            }
            else if (currentPhase == CommanderPhase.Normal)
            {
                thinkTimer -= Time.deltaTime;
                if (thinkTimer <= 0)
                {
                    thinkTimer = thinkInterval;
                    ExecuteAIThinkCycle();
                }
            }
        }

        private void StartInitialAdvance()
        {
            Debug.Log($"[AICommander Team {teamID}] Starting Initial Advance Phase!");
            RefreshFormations();

            if (initialAdvanceUnitTypes == null || initialAdvanceUnitTypes.Count == 0) return;

            foreach (var formation in allMyFormations)
            {
                if (initialAdvanceUnitTypes.Contains(formation.UnitData.unitType))
                {
                    Vector3 forwardDirection = formation.transform.forward;
                    Vector3 targetPos = formation.CalculateUnitCenter() + (forwardDirection * advanceDistance);

                    formation.WaypointCenter.position = targetPos;
                    formation.WaypointCenter.rotation = formation.transform.rotation;

                    formation.SetMoveOrder(false);
                }
            }
        }

        void ExecuteAIThinkCycle()
        {
            if (currentStrategy == null || currentStrategy.Tactics == null || currentStrategy.Tactics.Count == 0)
            {
                return;
            }

            RefreshFormations();
            if (allMyFormations.Count == 0 || allEnemyFormations.Count == 0) return;

            if (isInSiege && SiegeManager.instance != null)
            {
                SiegeManager.instance.ValidateOccupiedZonesAndPositions(allMyFormations);
            }

            availableFormations = allMyFormations.Where(f => f != null && f.CurrentState == Formation.FormationState.Idle).ToList();
            if (availableFormations.Count == 0) return;

            List<KeyValuePair<AITacticSO, float>> evaluatedTactics = new List<KeyValuePair<AITacticSO, float>>();

            foreach (var tactic in currentStrategy.Tactics)
            {
                if (tactic == null) continue;
                float currentScore = tactic.Evaluate(this, allEnemyFormations, availableFormations);

                if (currentScore > 0f)
                {
                    evaluatedTactics.Add(new KeyValuePair<AITacticSO, float>(tactic, currentScore));
                }
            }

            if (evaluatedTactics.Count > 0)
            {
                evaluatedTactics.Sort((a, b) => b.Value.CompareTo(a.Value));

                foreach (var tacticPair in evaluatedTactics)
                {
                    if (availableFormations.Count == 0) break;

               //     Debug.Log($"[AICommander Team {teamID}] Executing Tactic: {tacticPair.Key.name} (Score: {tacticPair.Value})", this);
                    tacticPair.Key.Execute(this, allEnemyFormations, availableFormations);
                }
            }
            else
            {
                Debug.Log($"[AICommander Team {teamID}] No suitable tactic found.", this);
            }
        }

        void RefreshFormations()
        {
            allMyFormations.Clear();
            allEnemyFormations.Clear();
            if (FormationController.instance == null) return;

            foreach (var f in FormationController.instance.GetFormations())
            {
                if (f == null || !f.gameObject.activeInHierarchy) continue;
                if (f.TeamID == this.teamID) allMyFormations.Add(f);
                else allEnemyFormations.Add(f);
            }
        }

        public void CommitFormationsToAction(List<Formation> formations)
        {
            if (formations == null) return;
            foreach (var f in formations)
            {
                if (f != null)
                {
                    availableFormations.Remove(f);
                }
            }
        }

        public static bool IsPathObstructed(Formation movingFormation, Vector3 targetPosition)
        {
            if (movingFormation == null) return true;

            NavMeshPath path = new NavMeshPath();

            if (NavMesh.SamplePosition(movingFormation.CalculateUnitCenter(), out NavMeshHit _, 1.0f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(movingFormation.CalculateUnitCenter(), targetPosition, NavMesh.AllAreas, path))
            {
                if (path.status != NavMeshPathStatus.PathComplete) return true;

                float pathLength = 0f;
                for (int i = 1; i < path.corners.Length; i++)
                {
                    pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                }

                float straightLineDistance = Vector3.Distance(movingFormation.CalculateUnitCenter(), targetPosition);


                if (pathLength > straightLineDistance * 1.5f)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        public List<Formation> GetAllMyFormations()
        {
            allMyFormations.RemoveAll(item => item == null);
            return allMyFormations;
        }

        public bool IsInSiege() => isInSiege;
        public bool IsSiegeDefender() => isSiegeDefender;
    }
}