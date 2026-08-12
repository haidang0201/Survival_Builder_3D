namespace TopsonGames
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.AI;
    using Unity.Collections;
    using Unity.Jobs;
    using TopsonGames.Minimap;
    using TopsonGames.AI;
    using TopsonGames.AI.Grid;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class Formation : MonoBehaviour, IMinimapTrackable
    {
        public int TeamID = 0;
        public UnitDataSO UnitData;

        [SerializeField] List<Unit> Units;
        public Visualizer[] visualizers;
        [SerializeField] private FormationCollider formationCollider;

        [Header("Reference to the trigger")]
        public EngagementTrigger engagementTrigger;

        [Header("Effects on other Formations")]
        [Tooltip("How often checks if toher formations nearby are performed per second to add effects (performance).")]
        public float checkInterval = 0.5f;
        public enum FormationState { Idle, MovingToWaypoint, Engaged, Fleeing }
        [Space]
        [Header("Debugging")]
        public FormationState CurrentState = FormationState.Idle;
        public enum CombatState { Melee, Archer }
        public CombatState CurrentCombatState = CombatState.Melee;
        public List<EffectSO> Effects;

        [Header("Morale System")]
        public float currentMorale;
        public float maxMorale;
        private bool generalIsDead = false;
        private float moraleCheckTimer = 1.0f;

        private float disengageCooldownTimer = 0f;

        [HideInInspector]
        public bool showline;
        public Formation currentEnemy;
        public Formation customEnemy;
        public Formation ArcherTarget;

        private NavMeshPath formationPath;
        private int currentPathCorner;
        private float detectionRefreshTimer = 1f;

        private float chargeBonusTimer = 0f;
        public bool IsChargeBonusActive => chargeBonusTimer > 0f;

        [HideInInspector] public int numberOfUnits;
        [HideInInspector] public Transform WaypointCenter;
        [HideInInspector] public Transform WaypointIndicator;
        [HideInInspector] public int currentArrows = 0;
        [HideInInspector] public float archerDetectionTimer = 1f;
        [HideInInspector] public float currentTimeBetweenShots = 5f;
        [HideInInspector] public float engagementRecalculationTimer = 0f;
        [HideInInspector] public Vector3 lockedEngagementCenter;

        [HideInInspector] public int formationWidth;
        [HideInInspector] public int targetFormationWidth;

        [HideInInspector] public ArmyData armyData;
        [HideInInspector] public bool isFreeShooting = true;
        [HideInInspector] public Vector3 finalDestination;
        [HideInInspector] public Quaternion finalRotation;
        [HideInInspector] public bool isDefender = false;

        [HideInInspector] public List<Vector3> gridWaypoints = new List<Vector3>();
        [HideInInspector] public bool useGridPath = false;
        [HideInInspector] public int currentGridWaypointIndex = 0;
        private float gridRecalculateTimer = 0f;
        [HideInInspector] public float currentGridRiskTolerance = 20f;
        [HideInInspector] public float currentGridFlankDistance = 15f;
        [HideInInspector] public bool gridPathTargetsEnemyRear = true;

        [HideInInspector] public float currentGridSkipWaypointDistance = 5f;
        [HideInInspector] public float currentGridIgnoreTargetDangerDist = 35f;
        [HideInInspector] public float currentGridIgnoreStartDangerDist = 15f;

        private int initialTroopCount;

        private List<Unit> _cachedValidUnits = new List<Unit>(50);
        private List<Vector3> _cachedGridPositions = new List<Vector3>(50);
        private List<Quaternion> _cachedGridRotations = new List<Quaternion>(50);

        private Coroutine underFire;
        private List<Formation> affectedFormations = new List<Formation>();
        private Coroutine checkRoutine;

        private NativeArray<Vector3> cohesionUnitPositions;
        private NativeArray<Vector3> cohesionWaypointPositions;
        private NativeArray<Vector3> cohesionFormationVelocities;
        private NativeArray<float> cohesionTargetAgentSpeeds;

        void Start()
        {
            formationPath = new NavMeshPath();
            gridRecalculateTimer = Random.Range(0f, 1.5f);

            if (FormationController.instance != null)
                FormationController.instance.RegisterFormation(this);

            if (MinimapController.instance != null)
                MinimapController.instance.RegisterTrackable(this);

            if (WaypointCenter == null)
                WaypointCenter = transform;

            formationCollider.SetUp(this);

            if (engagementTrigger != null)
            {
                engagementTrigger.Initialize(this.TeamID, this);
            }

            UpdateCachedValidUnits();
            WaypointCenter.SetPositionAndRotation(CalculateUnitCenter(), CalculateAverageRotation());

            detectionRefreshTimer = UnitData.combatBehaviour.formationDetectionTimer;
            this.numberOfUnits = _cachedValidUnits.Count;
            initialTroopCount = this.numberOfUnits;

            formationWidth = UnitData.formationWidth;
            targetFormationWidth = formationWidth;

            if (UnitData != null)
            {
                maxMorale = UnitData.baseMorale;
                currentMorale = maxMorale;
            }

            foreach (Unit unit in Units)
            {
                if (unit != null)
                {
                    unit.OnStart(WaypointIndicator, this);
                    if (unit.WaypointMarker != null) unit.WaypointMarker.SetActive(false);
                }
            }
            ArrangeFormation(false, true);
            UnitData.combatBehaviour.InitializeFormation(this);

            GameManager.OnGeneralDied += HandleGeneralDeath;

            if (UnitData.effectsOnOtherFormations != null && UnitData.effectsOnOtherFormations.Length > 0)
                checkRoutine = StartCoroutine(AuraRoutine());
        }

        private void OnDestroy()
        {
            GameManager.OnGeneralDied -= HandleGeneralDeath;
            DisposeCohesionArrays();
        }

        private void HandleGeneralDeath(int deadTeamID)
        {
            if (this.TeamID == deadTeamID)
            {
                generalIsDead = true;
            }
        }

        private void ReallocateCohesionArrays(int capacity)
        {
            DisposeCohesionArrays();
            if (capacity <= 0) return;

            cohesionUnitPositions = new NativeArray<Vector3>(capacity, Allocator.Persistent);
            cohesionWaypointPositions = new NativeArray<Vector3>(capacity, Allocator.Persistent);
            cohesionFormationVelocities = new NativeArray<Vector3>(capacity, Allocator.Persistent);
            cohesionTargetAgentSpeeds = new NativeArray<float>(capacity, Allocator.Persistent);
        }

        private void DisposeCohesionArrays()
        {
            if (cohesionUnitPositions.IsCreated) cohesionUnitPositions.Dispose();
            if (cohesionWaypointPositions.IsCreated) cohesionWaypointPositions.Dispose();
            if (cohesionFormationVelocities.IsCreated) cohesionFormationVelocities.Dispose();
            if (cohesionTargetAgentSpeeds.IsCreated) cohesionTargetAgentSpeeds.Dispose();
        }
        private void UpdateCachedValidUnits()
        {
            if (Units == null)
            {
                if (_cachedValidUnits.Count > 0)
                {
                    _cachedValidUnits.Clear();
                    ReallocateCohesionArrays(0);
                }
                return;
            }

            bool changed = Units.RemoveAll(u => u == null) > 0;

            if (_cachedValidUnits.Count != Units.Count || changed)
            {
                _cachedValidUnits.Clear();
                _cachedValidUnits.AddRange(Units);
                ReallocateCohesionArrays(_cachedValidUnits.Count);
                numberOfUnits = _cachedValidUnits.Count;
            }
        }

        private void Update()
        {
            if (disengageCooldownTimer > 0)
            {
                disengageCooldownTimer -= Time.deltaTime;
            }
            if (chargeBonusTimer > 0f)
            {
                chargeBonusTimer -= Time.deltaTime;
            }

            if (_cachedValidUnits.RemoveAll(u => u == null) > 0)
            {
                Units.RemoveAll(u => u == null);
                ReallocateCohesionArrays(_cachedValidUnits.Count);
                numberOfUnits = _cachedValidUnits.Count;
            }

            if (CurrentState != FormationState.Fleeing)
            {
                moraleCheckTimer -= Time.deltaTime;
                if (moraleCheckTimer <= 0)
                {
                    CalculateMorale();
                    moraleCheckTimer = 0.5f;
                }
            }

            if (useGridPath && CurrentState == FormationState.MovingToWaypoint && customEnemy != null)
            {
                gridRecalculateTimer += Time.deltaTime;
                if (gridRecalculateTimer >= 1.5f)
                {
                    gridRecalculateTimer = 0f;
                    RecalculateGridPathInternal();
                }
            }

            if (CurrentState == FormationState.Fleeing)
            {
                UpdateFleeingLogic();
            }
            else if (CurrentState != FormationState.Engaged && disengageCooldownTimer <= 0)
            {
                CheckForEngagement();
            }

            switch (CurrentState)
            {
                case FormationState.Idle:
                    UpdateCustomTargetEngagement();
                    HandleEffects(EffectType.Engaged, true);
                    HandleEffects(EffectType.Charging, true);
                    HandleEffects(EffectType.Flanked, true);
                    HandleEffects(EffectType.Walking, true);
                    break;
                case FormationState.MovingToWaypoint:
                    UpdateFormationMovement();
                    if (customEnemy && customEnemy.CurrentState != FormationState.Fleeing)
                        UpdateCustomTargetMovement();

                    HandleEffects(EffectType.Engaged, true);
                    HandleEffects(EffectType.Charging, true);
                    HandleEffects(EffectType.Flanked, true);
                    HandleEffects(EffectType.Walking, false);
                    HandleEffects(EffectType.Shooting, true);
                    break;
                case FormationState.Engaged:
                    if (!HasNearbyEnemies() && disengageCooldownTimer <= 0)
                    {
                        Disengage();
                    }
                    else
                    {
                        UpdateEngagement();
                        bool targetIsBuilding = (currentEnemy == null);
                        foreach (var unit in _cachedValidUnits)
                        {
                            if (unit != null && (unit.currentState != Unit.UnitState.Fighting || targetIsBuilding))
                            {
                                unit.MoveTo(unit.Waypoint.transform.position);
                            }
                        }

                        HandleEffects(EffectType.Engaged, false);

                        if (IsChargeBonusActive)
                            HandleEffects(EffectType.Charging, false);
                        else
                            HandleEffects(EffectType.Charging, true);

                        var enemys = formationCollider.GetDetectedTriggers();
                        bool isFlanked = false;

                        if (enemys.Count >= 2)
                        {
                            isFlanked = true;
                        }
                        else if (enemys.Count == 1)
                        {
                            var enemyTrigger = enemys[0];

                            if (enemyTrigger)
                            {
                                Vector3 myForward = (formationCollider != null && formationCollider.detectionCollider != null)
                                                    ? formationCollider.detectionCollider.transform.forward
                                                    : transform.forward;

                                Vector3 myPosition = (formationCollider != null && formationCollider.detectionCollider != null)
                                                    ? formationCollider.detectionCollider.transform.position
                                                    : CalculateUnitCenter();

                                Vector3 directionToEnemy = (enemyTrigger.transform.position - myPosition).normalized;

                                float dot = Vector3.Dot(myForward, directionToEnemy);
                                if (dot < 0.5f)
                                {
                                    isFlanked = true;
                                }
                            }
                        }

                        if (isFlanked)
                            HandleEffects(EffectType.Flanked, false);
                        else
                            HandleEffects(EffectType.Flanked, true);
                        HandleEffects(EffectType.Walking, true);
                    }
                    break;
                case FormationState.Fleeing:
                    HandleEffects(EffectType.Engaged, true);
                    HandleEffects(EffectType.Charging, true);
                    HandleEffects(EffectType.Flanked, true);
                    break;
            }

            if (showline)
                UpdateVisualizers();

            foreach (var unit in _cachedValidUnits)
            {
                if (unit != null) unit.OnUpdate();
            }
            foreach (var effect in Effects)
                if (effect != null) effect.OnUpdateFormation(this);
            UpdateColliderPositionAndSize();
            UnitData.combatBehaviour.OnUpdateFormation(this);
        }

        private void FixedUpdate()
        {
            detectionRefreshTimer -= Time.fixedDeltaTime;
            if (detectionRefreshTimer <= 0)
            {
                if (formationCollider != null) formationCollider.RefreshDetectedTriggers();
                detectionRefreshTimer = UnitData.combatBehaviour.formationDetectionTimer;
            }
        }

        private void CalculateMorale()
        {
            if (UnitData == null) return;

            float calcMorale = UnitData.baseMorale;

            int currentTroops = _cachedValidUnits.Count;
            if (initialTroopCount > 0)
            {
                float lossRatio = 1f - ((float)currentTroops / (float)initialTroopCount);

                float lossPenalty = (lossRatio * 10f) * UnitData.moralePenaltyPerHealthLoss;
                calcMorale -= lossPenalty;
            }

            if (generalIsDead)
            {
                calcMorale -= UnitData.generalDeathImpact;
            }

            foreach (var effect in Effects)
            {
                if (effect != null) calcMorale += effect.moralePenalty;
            }

            currentMorale = Mathf.Clamp(calcMorale, -100f, maxMorale);

            if (currentMorale <= 0f)
            {
                StartFleeing();
            }
        }

        public void StartFleeing()
        {
            if (CurrentState == FormationState.Fleeing) return;
#if UNITY_EDITOR
            Debug.Log($"Formation {name} is fleeing!");
#endif
            CurrentState = FormationState.Fleeing;

            Disengage();
            currentEnemy = null;
            customEnemy = null;
            useGridPath = false;

            ShowVisualizers(false);
            HideWaypointIndicators();
            HideWaypoints();

            Vector3 fleeTarget = FindClosestMapExit();

            foreach (var unit in _cachedValidUnits)
            {
                if (unit != null && unit.agent != null)
                {
                    unit.agent.speed = UnitData.formationMoveSpeed * 1.4f;
                    if (unit.agent.isOnNavMesh)
                        unit.agent.isStopped = false;
                    unit.ResetTarget();
                    unit.currentState = Unit.UnitState.Moving;
                    unit.Waypoint.transform.position = fleeTarget;

                    Vector3 fleeDir = fleeTarget - unit.transform.position;
                    fleeDir.y = 0f;
                    if (fleeDir.sqrMagnitude > 0.001f)
                        unit.Waypoint.transform.rotation = Quaternion.LookRotation(fleeDir.normalized);

                    unit.MoveTo(fleeTarget);
                }
            }
        }

        private void UpdateFleeingLogic()
        {
            Vector3 fleeTarget = FindClosestMapExit();
            foreach (var unit in _cachedValidUnits)
            {
                if (unit != null && unit.agent != null && unit.agent.enabled)
                {
                    unit.Waypoint.transform.position = fleeTarget;

                    Vector3 fleeDir = fleeTarget - unit.transform.position;
                    fleeDir.y = 0f;
                    if (fleeDir.sqrMagnitude > 0.001f)
                        unit.Waypoint.transform.rotation = Quaternion.LookRotation(fleeDir.normalized);

                    unit.MoveTo(fleeTarget);
                }
            }
        }

        private Vector3 FindClosestMapExit()
        {
            GameObject[] exits = GameObject.FindGameObjectsWithTag("MapExit");
            if (exits.Length == 0) return transform.position - transform.forward * 100f;

            Vector3 myPos = CalculateUnitCenter();
            Vector3 bestPos = exits[0].transform.position;
            float minSqr = (bestPos - myPos).sqrMagnitude;

            for (int i = 1; i < exits.Length; i++)
            {
                float sqr = (exits[i].transform.position - myPos).sqrMagnitude;
                if (sqr < minSqr)
                {
                    minSqr = sqr;
                    bestPos = exits[i].transform.position;
                }
            }
            return bestPos;
        }

        private void CheckForEngagement()
        {
            if (CurrentState == FormationState.Fleeing) return;

            if (formationCollider == null || disengageCooldownTimer > 0) return;

            var triggers = formationCollider.GetDetectedTriggers();
            if (triggers != null && triggers.Count > 0)
            {
                Formation enemyFormation = triggers[0].GetComponentInParent<Formation>();
                EngageWithEnemy(enemyFormation);
            }
        }

        public void EngageWithEnemy(Formation enemyFormation = null)
        {
            if (CurrentState == FormationState.Fleeing) return;

            if (CurrentState == FormationState.Engaged || disengageCooldownTimer > 0) return;

            if (CurrentState == FormationState.Idle) isDefender = true;
            else if (CurrentState == FormationState.MovingToWaypoint) isDefender = false;

            CurrentState = FormationState.Engaged;
            currentEnemy = enemyFormation;
            useGridPath = false;
            lockedEngagementCenter = CalculateWaypointCenter();
            ShowVisualizers(false);
            HideWaypointIndicators();
            HideWaypoints();

            engagementRecalculationTimer = 0f;
            MoveUnitsToWaypoints();

            if (!isDefender && UnitData.chargeDamageDuration > 0f)
            {
                chargeBonusTimer = UnitData.chargeDamageDuration;
            }

        }

        public void Disengage()
        {
            if (CurrentState != FormationState.Engaged) return;

            CurrentState = FormationState.Idle;
            currentEnemy = null;

            if (FormationController.instance != null)
                disengageCooldownTimer = FormationController.instance.disengageCooldownDuration;
            else
                disengageCooldownTimer = 2f;

            foreach (var unit in _cachedValidUnits)
            {
                if (unit != null)
                {
                    unit.ResetTarget();
                    if (unit.agent != null && unit.agent.isOnNavMesh && !unit.agent.isStopped)
                    {
                        unit.StopMovement();
                    }
                    unit.currentState = Unit.UnitState.Idle;
                }
            }
        }

        private bool HasNearbyEnemies()
        {
            if (formationCollider == null) return false;
            var triggers = formationCollider.GetDetectedTriggers();
            return triggers != null && triggers.Count > 0;
        }

        public void UpdateEngagement()
        {
            UnitData.placementBehaviour.TickUpdateEngagement(this, currentEnemy);
        }

        private void UpdateCustomTargetEngagement()
        {
            if (CurrentState == FormationState.Fleeing) return;

            if (customEnemy == null || disengageCooldownTimer > 0) return;

            Vector3 ownCenter = CalculateUnitCenter();
            Vector3 enemyCenter = customEnemy.CalculateUnitCenter();
            float distance = Vector3.Distance(ownCenter, enemyCenter);

            if (CurrentCombatState == CombatState.Archer)
            {
                var archerBehaviour = UnitData.combatBehaviour as ArcherCombatSO;
                if (archerBehaviour != null)
                {
                    if (distance > archerBehaviour.archerDetectionRange * 0.95f)
                    {
                        Vector3 directionToEnemy = (enemyCenter - ownCenter).normalized;
                        Vector3 targetPosition = enemyCenter - directionToEnemy * (archerBehaviour.archerDetectionRange * 0.8f);
                        WaypointCenter.position = targetPosition;
                        WaypointCenter.rotation = Quaternion.LookRotation(directionToEnemy);
                        SetMoveOrder(true);
                    }
                }
            }
            else
            {
                if (distance > UnitData.combatBehaviour.attackRange + 2f)
                {
                    Vector3 directionToEnemy = (enemyCenter - ownCenter).normalized;
                    WaypointCenter.position = enemyCenter;
                    WaypointCenter.rotation = Quaternion.LookRotation(directionToEnemy);
                    SetMoveOrder(true);
                }
                else if (CurrentState == FormationState.Idle)
                {
                    EngageWithEnemy(customEnemy);
                }
            }
        }

        private void UpdateCustomTargetMovement()
        {
            if (customEnemy == null || useGridPath) return;

            Vector3 ownCenter = CalculateUnitCenter();
            Vector3 enemyCenter = customEnemy.CalculateUnitCenter();
            float distance = Vector3.Distance(ownCenter, enemyCenter);

            if (CurrentCombatState == CombatState.Archer)
            {
                var archerBehaviour = UnitData.combatBehaviour as ArcherCombatSO;
                if (archerBehaviour != null && distance > archerBehaviour.archerDetectionRange * 0.95f)
                {
                    Vector3 directionToEnemy = (enemyCenter - ownCenter).normalized;
                    Vector3 targetPosition = enemyCenter - directionToEnemy * (archerBehaviour.archerDetectionRange * 0.8f);
                    finalDestination = targetPosition;
                    finalRotation = Quaternion.LookRotation(directionToEnemy);
                }
            }
            else
            {
                if (distance > UnitData.combatBehaviour.attackRange + 2f)
                {
                    Vector3 directionToEnemy = (enemyCenter - ownCenter).normalized;
                    finalDestination = enemyCenter;
                    finalRotation = Quaternion.LookRotation(directionToEnemy);
                }
            }
        }

        private void UpdateFormationMovement()
        {
            Vector3 targetPosition = finalDestination;
            bool isLastCorner = true;

            if (useGridPath && gridWaypoints != null && gridWaypoints.Count > 0 && currentGridWaypointIndex < gridWaypoints.Count)
            {
                targetPosition = gridWaypoints[currentGridWaypointIndex];
                isLastCorner = (currentGridWaypointIndex >= gridWaypoints.Count - 1);
            }
            else if (formationPath != null && formationPath.status == NavMeshPathStatus.PathComplete && currentPathCorner < formationPath.corners.Length)
            {
                targetPosition = formationPath.corners[currentPathCorner];
                isLastCorner = (currentPathCorner >= formationPath.corners.Length - 1);
            }

            Vector3 oldWaypointCenterPos = WaypointCenter.position;
            float currentSpeed = UnitData.formationMoveSpeed;
            bool isSiegeActive = (SiegeManager.instance != null);

            if (isSiegeActive)
            {
                float lagDist = Vector3.Distance(oldWaypointCenterPos, CalculateUnitCenter());
                float maxLag = UnitData.formationSpacing * 2.0f;

                if (lagDist > maxLag)
                {
                    float lagFactor = Mathf.Clamp01((lagDist - maxLag) / (UnitData.formationSpacing * 4f));
                    currentSpeed = Mathf.Lerp(currentSpeed, currentSpeed * 0.15f, lagFactor);
                }
            }

            Vector3 newWaypointCenterPos = Vector3.MoveTowards(oldWaypointCenterPos, targetPosition, currentSpeed * Time.deltaTime);

            if (NavMesh.SamplePosition(newWaypointCenterPos, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
            {
                newWaypointCenterPos = hit.position;
            }
            else
            {
                newWaypointCenterPos = oldWaypointCenterPos;
            }

            Vector3 formationVelocity = (newWaypointCenterPos - oldWaypointCenterPos) / (Time.deltaTime > 0 ? Time.deltaTime : 0.01f);
            WaypointCenter.position = newWaypointCenterPos;

            float distToTarget = Vector3.Distance(WaypointCenter.position, targetPosition);
            float dist = BattleGridManager.instance ? 4 : 0.5f;
            if (distToTarget < dist)
            {
                if (useGridPath)
                {
                    currentGridWaypointIndex++;
                }
                else if (currentPathCorner < formationPath.corners.Length)
                {
                    currentPathCorner++;
                }
            }

            if (!isSiegeActive)
            {
                WaypointCenter.rotation = Quaternion.Slerp(WaypointCenter.rotation, finalRotation, Time.deltaTime * UnitData.formationTurnSpeed);
            }
            else
            {
                if (isLastCorner)
                {
                    Vector3 finalForward = finalRotation * Vector3.forward;
                    Vector3 currentMoveDir = formationVelocity.normalized;
                    currentMoveDir.y = 0;

                    bool isTacticalAdjustment = distToTarget < 20.0f && Vector3.Angle(finalForward, currentMoveDir) > 45f;

                    if (distToTarget < 2.0f || isTacticalAdjustment)
                    {
                        WaypointCenter.rotation = Quaternion.Slerp(WaypointCenter.rotation, finalRotation, Time.deltaTime * UnitData.formationTurnSpeed);
                    }
                    else if (formationVelocity.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetLookRotation = Quaternion.LookRotation(currentMoveDir);
                        WaypointCenter.rotation = Quaternion.Slerp(WaypointCenter.rotation, targetLookRotation, Time.deltaTime * UnitData.formationTurnSpeed);
                    }
                }
                else if (formationVelocity.sqrMagnitude > 0.01f)
                {
                    Vector3 moveDir = formationVelocity.normalized;
                    moveDir.y = 0;

                    if (moveDir != Vector3.zero)
                    {
                        Quaternion targetLookRotation = Quaternion.LookRotation(moveDir);
                        WaypointCenter.rotation = Quaternion.Slerp(WaypointCenter.rotation, targetLookRotation, Time.deltaTime * UnitData.formationTurnSpeed);
                    }
                }
            }

            ArrangeFormation(false);

            int unitCount = _cachedValidUnits.Count;
            if (unitCount == 0 || !cohesionUnitPositions.IsCreated) return;

            for (int i = 0; i < unitCount; i++)
            {
                if (_cachedValidUnits[i] != null)
                {
                    cohesionUnitPositions[i] = _cachedValidUnits[i].transform.position;
                    cohesionWaypointPositions[i] = _cachedValidUnits[i].Waypoint.transform.position;
                    cohesionFormationVelocities[i] = formationVelocity;
                }
            }

            var job = new CohesionJob
            {
                UnitPositions = cohesionUnitPositions,
                WaypointPositions = cohesionWaypointPositions,
                FormationVelocities = cohesionFormationVelocities,
                BaseSpeed = this.UnitData.formationMoveSpeed,
                CatchUpFactor = this.UnitData.unitCatchUpFactor,
                SlowDownFactor = this.UnitData.unitSlowDownFactor,
                MaxSpeedMultiplier = this.UnitData.maxSpeedMultiplier,
                NewAgentSpeeds = cohesionTargetAgentSpeeds
            };
            JobHandle handle = job.Schedule(unitCount, 32);
            handle.Complete();

            float finalDist = Vector3.Distance(WaypointCenter.position, finalDestination);

            bool isAtFinalDestination = false;
            if (useGridPath)
            {
                isAtFinalDestination = (currentGridWaypointIndex >= gridWaypoints.Count);
            }
            else
            {
                isAtFinalDestination = finalDist < 0.5f || (isLastCorner && finalDist < 3.0f && formationVelocity.sqrMagnitude < 0.05f);
            }

            for (int i = 0; i < unitCount; i++)
            {
                var unit = _cachedValidUnits[i];
                if (unit != null && unit.agent != null)
                {
                    if (isAtFinalDestination)
                    {
                        unit.StopMovement();
                        unit.agent.speed = UnitData.formationMoveSpeed;
                    }
                    else
                    {
                        float targetSpeed = cohesionTargetAgentSpeeds[i];
                        unit.agent.speed = Mathf.Lerp(unit.agent.speed, targetSpeed, Time.deltaTime * 5f);
                        unit.MoveTo(unit.Waypoint.transform.position);
                    }
                }
            }

            if (isAtFinalDestination)
            {
                if (useGridPath)
                {
                    useGridPath = false;
                    gridWaypoints.Clear();
                    if (customEnemy != null)
                    {
                        SetCustomTarget(customEnemy);
                        SetMoveOrder(true);
                        return;
                    }
                }
                CurrentState = FormationState.Idle;
            }
        }

        private void RecalculateGridPathInternal()
        {
            if (customEnemy == null || BattleGridManager.instance == null) return;
            BattleGrid dangerMap = BattleGridManager.instance.GetDangerMap(TeamID, UnitData.unitType);
            if (dangerMap != null)
            {
                Vector3 myCenter = CalculateUnitCenter();
                Vector3 enemyCenter = customEnemy.CalculateUnitCenter();

                Vector3 flankDestination;
                if (gridPathTargetsEnemyRear)
                {
                    Vector3 targetForward = customEnemy.transform.forward;
                    flankDestination = enemyCenter - (targetForward * currentGridFlankDistance);
                }
                else
                {
                    Vector3 dirEnemyToMe = myCenter - enemyCenter;
                    dirEnemyToMe.y = 0f;
                    if (dirEnemyToMe.sqrMagnitude < 0.001f)
                        dirEnemyToMe = -customEnemy.transform.forward;
                    flankDestination = enemyCenter + dirEnemyToMe.normalized * currentGridFlankDistance;
                }

                int clearance = BattleGridManager.instance.pathfindingClearance;

                List<Vector3> newPath = dangerMap.FindSafePath(
                    myCenter,
                    flankDestination,
                    currentGridRiskTolerance,
                    currentGridIgnoreTargetDangerDist,
                    currentGridIgnoreStartDangerDist,
                    clearance
                );

                if (newPath != null && newPath.Count > 0)
                {
                    gridWaypoints = newPath;
                    currentGridWaypointIndex = 0;

                    while (currentGridWaypointIndex < gridWaypoints.Count - 1 &&
                           Vector3.Distance(myCenter, gridWaypoints[currentGridWaypointIndex]) < currentGridSkipWaypointDistance)
                    {
                        currentGridWaypointIndex++;
                    }

                    finalDestination = gridWaypoints[currentGridWaypointIndex];
                }
            }
        }

        public void SetMoveOrder(bool movetocustomenemy = false)
        {
            if (CurrentState == FormationState.Fleeing) return;

            if (movetocustomenemy == false)
                customEnemy = null;

            Vector3 potentialFinalDestination = WaypointCenter.position;
            Quaternion potentialFinalRotation = WaypointCenter.rotation;
            Vector3 currentUnitCenter = CalculateUnitCenter();

            if (CurrentState == FormationState.Engaged)
            {
                Disengage();
            }

            finalDestination = potentialFinalDestination;
            finalRotation = potentialFinalRotation;

            WaypointCenter.position = currentUnitCenter;
            WaypointCenter.rotation = CalculateAverageRotation();

            AssignAndLockFormationSlots(finalDestination, finalRotation);

            List<Vector3> finalGridPositions = new List<Vector3>();
            List<Quaternion> finalGridRotations = new List<Quaternion>();

            GetIdealFormationPoints(finalDestination, finalRotation, finalGridPositions, finalGridRotations);

            for (int i = 0; i < _cachedValidUnits.Count; i++)
            {
                if (i < finalGridPositions.Count && _cachedValidUnits[i] != null && _cachedValidUnits[i].WaypointMarker != null)
                {
                    _cachedValidUnits[i].WaypointMarker.transform.SetPositionAndRotation(finalGridPositions[i], finalGridRotations[i]);
                }
            }

            bool pathFound = NavMesh.CalculatePath(currentUnitCenter, finalDestination, NavMesh.AllAreas, formationPath);
            currentPathCorner = pathFound ? 0 : int.MaxValue;
            CurrentState = FormationState.MovingToWaypoint;
        }

        public void HaltAndBecomeTarget()
        {
            if (CurrentState == FormationState.MovingToWaypoint)
            {
                CurrentState = FormationState.Idle;
                useGridPath = false;
                foreach (var unit in _cachedValidUnits)
                {
                    if (unit != null) unit.StopMovement();
                }
            }
        }

        public void ShowWaypointMarkers()
        {
            foreach (var unit in _cachedValidUnits)
            {
                if (unit != null && unit.WaypointMarker != null) unit.WaypointMarker.SetActive(true);
            }
        }

        public void HideWaypointMarkers()
        {
            foreach (var unit in _cachedValidUnits)
            {
                if (unit != null && unit.WaypointMarker != null) unit.WaypointMarker.SetActive(false);
            }
        }

        private void AssignAndLockFormationSlots(Vector3 targetPos, Quaternion targetRot)
        {
            if (_cachedValidUnits.Count == 0) return;
            GetIdealFormationPoints(targetPos, targetRot, _cachedGridPositions, _cachedGridRotations);
            int finalWidthAtTarget = formationWidth;

            int backupTargetWidth = targetFormationWidth;
            targetFormationWidth = finalWidthAtTarget;

            Vector3 currentCenter = CalculateUnitCenter();
            List<Vector3> phantomPositions = new List<Vector3>();
            List<Quaternion> phantomRotations = new List<Quaternion>();
            GetIdealFormationPoints(currentCenter, targetRot, phantomPositions, phantomRotations);

            targetFormationWidth = backupTargetWidth;
            formationWidth = finalWidthAtTarget;

            List<Unit> availableUnits = new List<Unit>(_cachedValidUnits);
            int unitCount = availableUnits.Count;
            int slotCount = phantomPositions.Count;
            NativeArray<Vector3> unitPosArray = new NativeArray<Vector3>(unitCount, Allocator.TempJob);
            NativeArray<Vector3> slotPosArray = new NativeArray<Vector3>(slotCount, Allocator.TempJob);
            NativeArray<int> resultIndicesArray = new NativeArray<int>(slotCount, Allocator.TempJob);

            for (int i = 0; i < unitCount; i++)
            {
                unitPosArray[i] = availableUnits[i].transform.position;
            }

            for (int i = 0; i < slotCount; i++)
            {
                slotPosArray[i] = phantomPositions[i];
                resultIndicesArray[i] = -1;
            }
            FormationAssignmentJob assignmentJob = new FormationAssignmentJob
            {
                UnitPositions = unitPosArray,
                SlotPositions = slotPosArray,
                ResultAssignedUnitIndices = resultIndicesArray
            };

            JobHandle handle = assignmentJob.Schedule();
            handle.Complete();

            Unit[] assignedUnits = new Unit[slotCount];
            bool[] unitWasAssigned = new bool[unitCount];

            for (int s = 0; s < slotCount; s++)
            {
                int assignedUnitIndex = resultIndicesArray[s];
                if (assignedUnitIndex != -1)
                {
                    assignedUnits[s] = availableUnits[assignedUnitIndex];
                    unitWasAssigned[assignedUnitIndex] = true;
                }
            }

            unitPosArray.Dispose();
            slotPosArray.Dispose();
            resultIndicesArray.Dispose();

            Units.Clear();
            for (int i = 0; i < assignedUnits.Length; i++)
            {
                if (assignedUnits[i] != null)
                {
                    Units.Add(assignedUnits[i]);
                }
            }

            for (int u = 0; u < unitCount; u++)
            {
                if (!unitWasAssigned[u])
                {
                    Units.Add(availableUnits[u]);
                }
            }

            UpdateCachedValidUnits();
        }

        private struct AssignmentPair : System.IComparable<AssignmentPair>
        {
            public int unitIndex;
            public int slotIndex;
            public float distanceSqr;

            public int CompareTo(AssignmentPair other)
            {
                return distanceSqr.CompareTo(other.distanceSqr);
            }
        }

        public void ArrangeFormation(bool useWaypointIndicators, bool setWaypointsToUnitPositions = false)
        {
            numberOfUnits = _cachedValidUnits.Count;
            if (targetFormationWidth <= 0) targetFormationWidth = UnitData.formationWidth;
            if (numberOfUnits <= 0) return;

            if (setWaypointsToUnitPositions)
            {
                foreach (var unit in _cachedValidUnits)
                {
                    if (unit != null)
                    {
                        unit.Waypoint.transform.position = unit.transform.position;
                        unit.Waypoint.transform.rotation = unit.transform.rotation;
                    }
                }
                return;
            }

            GetIdealFormationPoints(WaypointCenter.position, WaypointCenter.rotation, _cachedGridPositions, _cachedGridRotations);

            for (int i = 0; i < _cachedValidUnits.Count; i++)
            {
                if (i >= _cachedGridPositions.Count) continue;
                var unit = _cachedValidUnits[i];
                if (unit == null) continue;

                var targetTransform = useWaypointIndicators ? unit.WaypointIndicator.transform : unit.Waypoint.transform;
                targetTransform.SetPositionAndRotation(_cachedGridPositions[i], _cachedGridRotations[i]);

                int row = (i / formationWidth) + 1;
                unit.rowInFormation = row;
            }
        }

        public void UpdateDragVisuals(Vector3 dragCenter, Quaternion dragRotation, int newWidth)
        {
            targetFormationWidth = newWidth;

            GetIdealFormationPoints(dragCenter, dragRotation, _cachedGridPositions, _cachedGridRotations);

            for (int i = 0; i < _cachedValidUnits.Count; i++)
            {
                if (i >= _cachedGridPositions.Count) continue;
                var unit = _cachedValidUnits[i];
                if (unit == null || unit.WaypointIndicator == null) continue;
                unit.WaypointIndicator.transform.SetPositionAndRotation(_cachedGridPositions[i], _cachedGridRotations[i]);
            }
        }

        void UpdateColliderPositionAndSize()
        {
            if (formationCollider == null || formationCollider.detectionCollider == null || _cachedValidUnits.Count == 0)
            {
                if (formationCollider != null && formationCollider.detectionCollider != null) formationCollider.detectionCollider.enabled = false;
                return;
            }
            formationCollider.detectionCollider.enabled = true;
            formationCollider.detectionCollider.transform.position = Vector3.Lerp(formationCollider.detectionCollider.transform.position, CalculateUnitCenter(), Time.deltaTime * UnitData.centerSmoothingSpeed);
            formationCollider.detectionCollider.transform.rotation = Quaternion.Slerp(formationCollider.detectionCollider.transform.rotation, CalculateAverageRotation(), Time.deltaTime * UnitData.centerSmoothingSpeed);

            var firstUnit = _cachedValidUnits.FirstOrDefault(u => u != null);
            if (firstUnit == null) return;

            var firstPoint = formationCollider.detectionCollider.transform.InverseTransformPoint(firstUnit.transform.position);
            Bounds localBounds = new Bounds(firstPoint, Vector3.zero);

            for (int i = 1; i < _cachedValidUnits.Count; i++)
            {
                if (_cachedValidUnits[i] != null)
                {
                    var localPoint = formationCollider.detectionCollider.transform.InverseTransformPoint(_cachedValidUnits[i].transform.position);
                    localBounds.Encapsulate(localPoint);
                }
            }

            formationCollider.detectionCollider.center = localBounds.center;
            Vector3 newSize = localBounds.size + new Vector3(UnitData.colliderOverhang, 0, UnitData.colliderOverhang);
            newSize.x = Mathf.Min(newSize.x, UnitData.maxColliderSize.x);
            newSize.z = Mathf.Min(newSize.z, UnitData.maxColliderSize.y);
            newSize.y = UnitData.colliderHeight;
            formationCollider.detectionCollider.size = newSize;
        }

        private Vector3 GetAdjustedCenterAndWidth(Vector3 center, Quaternion rotation, int targetWidth, out int actualWidth)
        {
            if (NavMesh.SamplePosition(center, out NavMeshHit centerHit, 3.0f, NavMesh.AllAreas))
            {
                center = centerHit.position;
            }

            Vector3 right = rotation * Vector3.right;
            right.y = 0;
            right.Normalize();

            float requiredHalfWidth = ((targetWidth - 1) * UnitData.formationSpacing) / 2.0f;
            float buffer = UnitData.formationSpacing * 0.5f;

            Vector3 leftEdge, rightEdge;
            NavMeshHit hit;

            bool hitRight = NavMesh.Raycast(center, center + right * (requiredHalfWidth + buffer), out hit, NavMesh.AllAreas);
            if (hitRight) rightEdge = hit.position - (right * buffer);
            else rightEdge = center + right * requiredHalfWidth;

            bool hitLeft = NavMesh.Raycast(center, center - right * (requiredHalfWidth + buffer), out hit, NavMesh.AllAreas);
            if (hitLeft) leftEdge = hit.position + (right * buffer);
            else leftEdge = center - right * requiredHalfWidth;

            float availableDist = Vector3.Distance(leftEdge, rightEdge);

            actualWidth = Mathf.FloorToInt((availableDist + 0.05f) / UnitData.formationSpacing) + 1;

            int minWidth = FormationController.instance != null ? FormationController.instance.minFormationWidth : 2;
            actualWidth = Mathf.Clamp(actualWidth, minWidth, targetWidth);

            if (hitLeft || hitRight)
            {
                return Vector3.Lerp(leftEdge, rightEdge, 0.5f);
            }
            return center;
        }

        public void GetIdealFormationPoints(Vector3 inputCenter, Quaternion rotation, List<Vector3> positions, List<Quaternion> rotations)
        {
            positions.Clear();
            rotations.Clear();
            var validUnitCount = _cachedValidUnits.Count;
            if (validUnitCount <= 0) return;

            Vector3 safeCenter = GetAdjustedCenterAndWidth(inputCenter, rotation, targetFormationWidth, out formationWidth);

            int columns = Mathf.Min(formationWidth, validUnitCount);
            if (validUnitCount > columns)
            {
                columns = formationWidth;
            }

            int rows = Mathf.CeilToInt((float)validUnitCount / (float)columns);
            float startX = -((columns - 1) * UnitData.formationSpacing) / 2.0f;
            float startZ = ((rows - 1) * UnitData.formationSpacing) / 2.0f;

            int unitsInLastRow = validUnitCount - (rows - 1) * columns;
            int startColForLastRow = (columns - unitsInLastRow) / 2;

            for (int i = 0; i < validUnitCount; i++)
            {
                int row = i / columns;
                int unitIndexInRow = i % columns;
                int col;

                bool isLastRow = (row == rows - 1);

                if (isLastRow)
                {
                    col = startColForLastRow + unitIndexInRow;
                }
                else
                {
                    col = unitIndexInRow;
                }

                float x = startX + col * UnitData.formationSpacing;
                float z = startZ - row * UnitData.formationSpacing;
                Vector3 localPos = new Vector3(x, 0, z);

                Vector3 finalPos = safeCenter + rotation * localPos;

                if (NavMesh.SamplePosition(finalPos, out NavMeshHit slotHit, 3.0f, NavMesh.AllAreas))
                {
                    finalPos.y = slotHit.position.y;
                }

                positions.Add(finalPos);
                rotations.Add(rotation);
            }
        }

        public void MoveUnitsToWaypoints()
        {
            foreach (var unit in _cachedValidUnits)
            {
                if (unit != null) unit.MoveTo(unit.Waypoint.transform.position);
            }
        }

        public void ShowVisualizers(bool show)
        {
            showline = show;
            if (visualizers == null || visualizers.Length == 0) return;
            foreach (var visualizer in visualizers)
            {
                visualizer.ShowLine(show);
                if (show) visualizer.OnUpdateVisualizer(UnitData.combatBehaviour, this);
            }
        }

        public void UpdateVisualizers()
        {
            if (visualizers == null || visualizers.Length == 0) return;
            foreach (var visualizer in visualizers)
            {
                visualizer.OnUpdateVisualizer(UnitData.combatBehaviour, this);
            }
        }

        public Vector3 CalculateWaypointMarkerCenter()
        {
            var markers = _cachedValidUnits
                .Where(u => u != null && u.WaypointMarker != null && u.WaypointMarker.activeSelf)
                .Select(u => u.WaypointMarker.transform.position)
                .ToList();
            if (markers.Count == 0) return CalculateUnitCenter();
            Vector3 sum = Vector3.zero;
            foreach (Vector3 pos in markers) sum += pos;
            return sum / markers.Count;
        }

#if UNITY_EDITOR
        [ContextMenu("Find and Arrange Units from Children")]
#endif
        public void FindAndArrangeUnitsFromChildren()
        {
            if (WaypointCenter == null) { WaypointCenter = transform; }
            WaypointCenter.position = transform.position;
            WaypointCenter.rotation = transform.rotation;

            if (Units == null) Units = new List<Unit>();
            Units.Clear();
            Units.AddRange(GetComponentsInChildren<Unit>());

            UpdateCachedValidUnits();

            if (_cachedValidUnits.Count == 0)
            {
                if (UnitData != null) UnitData.Troops = 0;
                return;
            }

            if (UnitData != null)
            {
                UnitData.Troops = _cachedValidUnits.Count;
#if UNITY_EDITOR
                EditorUtility.SetDirty(UnitData);
#endif
            }

            numberOfUnits = _cachedValidUnits.Count;
            targetFormationWidth = UnitData != null ? UnitData.formationWidth : 8;
            formationWidth = targetFormationWidth;
            if (formationWidth <= 0) formationWidth = 1;

            List<Vector3> targetPositions = new List<Vector3>();
            List<Quaternion> targetRotations = new List<Quaternion>();

            GetIdealFormationPoints(transform.position, transform.rotation, targetPositions, targetRotations);

            for (int i = 0; i < _cachedValidUnits.Count; i++)
            {
                if (i < targetPositions.Count)
                {
                    Unit unit = _cachedValidUnits[i];
                    if (unit == null) continue;

#if UNITY_EDITOR
                    Undo.RecordObject(unit.transform, "Arrange Unit");
#endif
                    unit.transform.position = targetPositions[i];
                    unit.transform.rotation = targetRotations[i];

                    if (unit.Waypoint != null)
                    {
#if UNITY_EDITOR
                        Undo.RecordObject(unit.Waypoint.transform, "Arrange Waypoint");
#endif
                        unit.Waypoint.transform.SetPositionAndRotation(targetPositions[i], targetRotations[i]);
                    }
                }
            }
        }

        public void BakePositionsToUnits()
        {
            foreach (var unit in Units)
            {
                if (unit == null || unit.Waypoint == null) continue;
                unit.transform.position = unit.Waypoint.transform.position;
                unit.transform.rotation = unit.Waypoint.transform.rotation;
#if UNITY_EDITOR
                EditorUtility.SetDirty(unit);
                EditorUtility.SetDirty(unit.transform);
#endif
            }
        }
        IEnumerator AuraRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(checkInterval);

            while (true)
            {
                ApplyAuraLogic();
                yield return wait;
            }
        }

        void ApplyAuraLogic()
        {
            if (FormationController.instance == null || UnitData.effectsOnOtherFormations == null || UnitData.effectsOnOtherFormations.Length == 0) return;

            List<Formation> allFormations = FormationController.instance.GetFormations();

            List<Formation> currentlyInRadius = new List<Formation>();

            Vector3 myPos = GetWorldPosition();
            float radiusSqr = UnitData.effectRadius * UnitData.effectRadius;

            foreach (var formation in allFormations)
            {
                if (formation == null || formation == this) continue;

                if (formation.TeamID != this.TeamID) continue;

                if ((formation.GetWorldPosition() - myPos).sqrMagnitude <= radiusSqr)
                {
                    currentlyInRadius.Add(formation);

                    if (!affectedFormations.Contains(formation))
                    {
                        foreach (var effect in UnitData.effectsOnOtherFormations)
                        {
                            if (effect == null) continue;
                            formation.AddSpecificEffect(effect);
                            affectedFormations.Add(formation);
                        }
                    }
                }
            }

            for (int i = affectedFormations.Count - 1; i >= 0; i--)
            {
                Formation f = affectedFormations[i];
                if (f == null || !currentlyInRadius.Contains(f))
                {
                    if (f != null)
                    {
                        foreach (var effect in UnitData.effectsOnOtherFormations)
                        {
                            if (effect == null) continue;
                            f.RemoveSpecificEffect(effect);
                        }
                    }
                    affectedFormations.RemoveAt(i);
                }
            }
        }

        void RemoveAuraFromAll()
        {
            foreach (var f in affectedFormations)
            {
                if (f != null)
                {
                    foreach (var effect in UnitData.effectsOnOtherFormations)
                    {
                        if (effect == null) continue;
                        f.RemoveSpecificEffect(effect);
                    }
                }
            }
            affectedFormations.Clear();
        }
        public void ShowSelectionIndicators()
        {
            foreach (var u in _cachedValidUnits)
            {
                if (u != null && u.PositionIndicator != null) u.PositionIndicator.SetActive(true);
            }
        }

        public void HideSelectionIndicators()
        {
            foreach (var u in _cachedValidUnits)
            {
                if (u != null && u.PositionIndicator != null) u.PositionIndicator.SetActive(false);
            }
        }

        public void ShowWaypointIndicators()
        {
            foreach (var u in _cachedValidUnits)
            {
                if (u != null && u.WaypointIndicator != null) u.WaypointIndicator.SetActive(true);
            }
        }

        public void HideWaypointIndicators()
        {
            foreach (var u in _cachedValidUnits)
            {
                if (u != null && u.WaypointIndicator != null) u.WaypointIndicator.SetActive(false);
            }
        }

        public void ShowWaypoints()
        {
            foreach (var u in _cachedValidUnits)
            {
                if (u != null && u.Waypoint != null) u.Waypoint.SetActive(true);
            }
        }

        public void HideWaypoints()
        {
            foreach (var u in _cachedValidUnits)
            {
                if (u != null && u.Waypoint != null) u.Waypoint.SetActive(false);
            }
        }

        public void FlashWaypointIndicators()
        {
            StartCoroutine(FlashWaypointIndicatorsRoutine());
        }

        public void ChangeFreeShooting(bool freeshooting)
        {
            isFreeShooting = freeshooting;
        }

        public void ReportArrows()
        {
            UnitData.combatBehaviour.OnReportArrows(this);
            if (underFire != null)
                StopCoroutine(underFire);
            underFire = StartCoroutine(OnReportArrows());
        }
        IEnumerator OnReportArrows()
        {
            HandleEffects(EffectType.UnderFire, false);
            yield return new WaitForSeconds(3f);
            HandleEffects(EffectType.UnderFire, true);
        }
        public void SetCustomTarget(Formation formation)
        {
            customEnemy = formation;
            UpdateCustomTargetEngagement();
        }

        public void UnitDeath(Unit unit)
        {
            if (Units.Contains(unit))
            {
                Units.Remove(unit);
            }
            UpdateCachedValidUnits();

            if (GameManager.instance != null) GameManager.instance.OnUnitDeath(this);

            if (unit != null) unit.transform.SetParent(null);

            if (_cachedValidUnits.Count == 0)
            {
                if (UnitData.isGeneral)
                {
                    GameManager.OnGeneralDied?.Invoke(this.TeamID);
                }

                if (FormationController.instance != null && FormationController.instance.GetFormations().Contains(this))
                    FormationController.instance.RemoveFormation(this);

                if (gameObject != null) Destroy(gameObject);
                return;
            }

            numberOfUnits = _cachedValidUnits.Count;
            if (CurrentState == FormationState.Engaged)
            {
                engagementRecalculationTimer = 0f;
            }
            else
            {
                ArrangeFormation(false);
            }
            MoveUnitsToWaypoints();
        }

        public void CullUnitsToStrength(int targetStrength)
        {
            if (Units.Count <= targetStrength)
            {
                UpdateCachedValidUnits();
                return;
            }
            int unitsToRemove = Units.Count - targetStrength;
            Units = Units.OrderBy(u => u != null ? transform.InverseTransformPoint(u.transform.position).z : float.MaxValue)
                         .ThenBy(u => u != null ? transform.InverseTransformPoint(u.transform.position).x : float.MaxValue)
                         .ToList();
            List<Unit> unitsToKeep = new List<Unit>();
            for (int i = 0; i < Units.Count; i++)
            {
                if (i < unitsToRemove)
                {
                    if (Units[i] != null)
                    {
                        Units[i].gameObject.SetActive(false);
                        Destroy(Units[i].gameObject);
                    }

                }
                else
                {
                    unitsToKeep.Add(Units[i]);
                }
            }
            Units = unitsToKeep;
            UpdateCachedValidUnits();
        }
        public void HandleEffects(EffectType effectType, bool remove = false)
        {
            if (remove)
            {
                for (int i = 0; i < Effects.Count; i++)
                {
                    if (Effects[i] == null)
                    {
                        Effects.RemoveAt(i);
                        i--;
                    }
                    else if (Effects[i].effectType == effectType)
                    {
                        Effects[i].OnRemoveEffect(this);
                        Effects.Remove(Effects[i]);
                        i--;
                    }
                }
                return;
            }
            var Effect = UiManager.Instance.GetEffect(effectType);
            if (Effect == null || Effects.Contains(Effect)) return;
            Effects.Add(Effect);
            Effect.OnAddEffect(this);
        }

        private IEnumerator FlashWaypointIndicatorsRoutine()
        {
            ArrangeFormation(true);
            ShowWaypointIndicators();
            yield return new WaitForSeconds(1.0f);
            HideWaypointIndicators();
        }

        public List<Unit> GetCachedUnits()
        {
            return _cachedValidUnits;
        }

        public Vector3 CalculateUnitCenter()
        {
            if (_cachedValidUnits.Count == 0) return transform.position;
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (Unit unit in _cachedValidUnits)
            {
                if (unit != null)
                {
                    sum += unit.transform.position;
                    count++;
                }
            }
            return count > 0 ? sum / count : transform.position;
        }

        public Vector3 CalculateWaypointCenter()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var unit in _cachedValidUnits)
            {
                if (unit != null && unit.Waypoint != null)
                {
                    sum += unit.Waypoint.transform.position;
                    count++;
                }
            }
            if (count == 0) return transform.position;
            return sum / count;
        }

        public void AddSpecificEffect(EffectSO effect)
        {
            if (effect == null) return;
            if (!Effects.Contains(effect))
            {
                Effects.Add(effect);
                effect.OnAddEffect(this);
            }
        }

        public void RemoveSpecificEffect(EffectSO effect)
        {
            if (effect == null) return;
            if (Effects.Contains(effect))
            {
                effect.OnRemoveEffect(this);
                Effects.Remove(effect);
            }
        }

        public List<Unit> GetUnits()
        {
            return Units;
        }

        public BoxCollider GetUnitCenterCollider()
        {
            return formationCollider.detectionCollider;
        }

        public Quaternion CalculateAverageRotation()
        {
            if (_cachedValidUnits.Count == 0) return transform.rotation;
            Vector3 averageForward = Vector3.zero;
            foreach (Unit unit in _cachedValidUnits)
            {
                if (unit != null) averageForward += unit.transform.forward;
            }
            if (averageForward.sqrMagnitude > 0.01f)
                return Quaternion.LookRotation(averageForward.normalized);
            return WaypointCenter != null ? WaypointCenter.rotation : transform.rotation;
        }
        private void OnDisable()
        {
            if (MinimapController.instance != null)
                MinimapController.instance.UnregisterTrackable(this);
            RemoveAuraFromAll();
        }

        private void OnDrawGizmosSelected()
        {
            if (UnitData && UnitData.combatBehaviour != null)
                UnitData.combatBehaviour.DrawGizmosOnBehaviour(this);
        }

        private void OnDrawGizmos()
        {
            if (useGridPath && gridWaypoints != null && gridWaypoints.Count > 0)
            {
                if (BattleGridManager.instance != null && BattleGridManager.instance.showGizmos)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 currentPos = CalculateUnitCenter();

                    if (currentGridWaypointIndex < gridWaypoints.Count)
                    {
                        Gizmos.DrawLine(currentPos + Vector3.up * 0.5f, gridWaypoints[currentGridWaypointIndex] + Vector3.up * 0.5f);
                    }

                    for (int i = currentGridWaypointIndex; i < gridWaypoints.Count - 1; i++)
                    {
                        Gizmos.DrawLine(gridWaypoints[i] + Vector3.up * 0.5f, gridWaypoints[i + 1] + Vector3.up * 0.5f);
                        Gizmos.DrawSphere(gridWaypoints[i + 1] + Vector3.up * 0.5f, 0.3f);
                    }
                }
            }
        }

        public Vector3 GetWorldPosition()
        {
            if (engagementTrigger != null)
                return engagementTrigger.transform.position;
            return transform.position;
        }

        public float GetWorldRotationY()
        {
            if (engagementTrigger != null)
                return engagementTrigger.transform.eulerAngles.y;
            return transform.eulerAngles.y;
        }

        public bool IsVisibleOnMap()
        {
            return gameObject.activeInHierarchy;
        }

        public int GetTeamID()
        {
            return TeamID;
        }
        public Sprite GetMinimapIcon()
        {
            return null;
        }
    }
}