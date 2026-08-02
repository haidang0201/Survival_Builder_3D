namespace TopsonGames
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;
    using UnityEngine.Events;

    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;
        public DamageModifierSO damageModifier;
        [Header("Army")]
        public ArmySO playerArmy;
        public ArmySpawner playerArmySpawner;
        public ArmySO enemyArmy;
        public ArmySpawner enemyArmySpawner;
        [Header("Events")]
        public UnityEvent OnFinishSetup;
        public UnityEvent OnPlayerWin;
        public UnityEvent OnEnemyWin;
        public UnityEvent OnPlayerGeneralDied;
        public UnityEvent OnEnemyGeneralDied;
        [Header("Debugging")]
        [Tooltip("Should army sizes be reset after the game? False = Save progress.")]
        public bool ResetArmyAfterPlaying = true;
        [Tooltip("Additional tolerance for attacks on buildings to compensate for NavMesh gaps.")]
        public float buildingAttackRangeBuffer = 1.5f;

        [HideInInspector] public bool isGameOver = false;
        private List<Unit> allActiveUnits = new List<Unit>();
        [Tooltip("In Game Armies (RUNTIME)")]
        public InGameArmy INPlayerArmy, INEnemyArmy;

        private List<DestructibleTarget> potentialTargetsCache = new List<DestructibleTarget>(1000);
        private List<DestructibleTarget> validTargetsForJob = new List<DestructibleTarget>(1000);

        public static System.Action<int> OnGeneralDied;

        private Dictionary<Formation, float> formationTargetTimers = new Dictionary<Formation, float>();
        private readonly List<Formation> _staleTimerKeysBuffer = new List<Formation>(32);
        private float timerCleanupTimer = 0f;

        private NativeArray<Vector3> engOwnPositions;
        private NativeArray<Vector3> engEnemyPositions;
        private NativeArray<int> engResultIndices;
        private NativeArray<float> engResultSqrDistances;

        private NativeArray<Vector3> rotUnitPositions;
        private NativeArray<Quaternion> rotCurrentRotations;
        private NativeArray<Vector3> rotTargetPositions;
        private NativeArray<Quaternion> rotWaypointRotations;
        private NativeArray<int> rotUnitStates;
        private NativeArray<Quaternion> rotNewRotations;
        private NativeArray<float> rotRotationSpeeds;
        private NativeArray<Vector3> rotVelocities;
        private NativeArray<Vector3> rotClosestEnemyPositions;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(this);
        }

        private IEnumerator Start()
        {
            INPlayerArmy = new InGameArmy();
            INEnemyArmy = new InGameArmy();

            if (playerArmy && playerArmySpawner)
            {
                INPlayerArmy.SetUpArmyData(playerArmy);
                playerArmySpawner.SpawnArmy(INPlayerArmy, true);
            }
            if (enemyArmy && enemyArmySpawner)
            {
                INEnemyArmy.SetUpArmyData(enemyArmy);
                enemyArmySpawner.SpawnArmy(INEnemyArmy);
            }

            yield return new WaitForEndOfFrame();

            if (UiManager.Instance != null)
                UiManager.Instance.SetUp();

            OnFinishSetup.Invoke();
        }

        void Update()
        {
            if (isGameOver) return;

            List<Formation> allFormations = FormationController.instance.GetFormations();

            if (allFormations == null)
            {
                CheckForVictory();
                return;
            }

            foreach (var formation in allFormations)
            {
                if (formation != null && formation.CurrentState == Formation.FormationState.Engaged)
                {
                    float nextUpdate = formationTargetTimers.GetValueOrDefault(formation, 0f);
                    if (Time.time >= nextUpdate)
                    {
                        formationTargetTimers[formation] = Time.time + 0.25f;
                        ProcessEngagedFormation(formation);
                    }
                }
            }

            ProcessUnitRotations();
            CheckForVictory();

            timerCleanupTimer += Time.deltaTime;
            if (timerCleanupTimer >= 10f)
            {
                timerCleanupTimer = 0f;
                CleanupStaleFormationTimers();
            }
        }

        private void CleanupStaleFormationTimers()
        {
            if (formationTargetTimers.Count == 0) return;
            _staleTimerKeysBuffer.Clear();
            foreach (var kvp in formationTargetTimers)
            {
                if (kvp.Key == null) _staleTimerKeysBuffer.Add(kvp.Key);
            }
            for (int i = 0; i < _staleTimerKeysBuffer.Count; i++)
            {
                formationTargetTimers.Remove(_staleTimerKeysBuffer[i]);
            }
        }

        void CheckForVictory()
        {
            if (isGameOver) return;

            bool playerHasUnits = GetPlayerStrength() > 0;
            bool enemyHasUnits = GetEnemyStrength() > 0;

            if (playerHasUnits && !enemyHasUnits)
            {
                isGameOver = true;
                OnPlayerWin.Invoke();
            }
            else if (enemyHasUnits && !playerHasUnits)
            {
                isGameOver = true;
                OnEnemyWin.Invoke();
            }

            if (isGameOver && !ResetArmyAfterPlaying)
            {
                INEnemyArmy.SetArmyDataToArmy();
                INPlayerArmy.SetArmyDataToArmy();
            }
        }

        public void OnUnitDeath(Formation formation)
        {
            if (formation == null) return;
            if (formation.TeamID == FormationController.instance.TeamID)
                INPlayerArmy.OnUnitDeath(formation.armyData);
            else
                INEnemyArmy.OnUnitDeath(formation.armyData);
        }

        public int GetEnemyStrength()
        {
            int strength = 0;
            if (INEnemyArmy == null || INEnemyArmy.armyData == null) return 0;
            foreach (var army in INEnemyArmy.armyData)
            {
                if (army != null) strength += army.Troops;
            }
            return strength;
        }

        public int GetPlayerStrength()
        {
            int strength = 0;
            if (INPlayerArmy == null || INPlayerArmy.armyData == null) return 0;
            foreach (var army in INPlayerArmy.armyData)
            {
                if (army != null) strength += army.Troops;
            }
            return strength;
        }

        private void ProcessEngagedFormation(Formation ownFormation)
        {
            if (ownFormation == null || ownFormation.UnitData == null) return;

            List<Unit> ownUnits = ownFormation.GetCachedUnits();
            if (ownUnits.Count == 0) return;

            int enemyTeamID = (ownFormation.TeamID == 0) ? 1 : 0;
            List<TargetType> attackableTypes = ownFormation.UnitData.attackableTargetTypes;
            if (attackableTypes == null || attackableTypes.Count == 0) return;

            if (TargetManager.instance == null) return;
            TargetManager.instance.GetPotentialTargets(enemyTeamID, attackableTypes, potentialTargetsCache);

            validTargetsForJob.Clear();
            foreach (var target in potentialTargetsCache)
            {
                if (target != null && target.gameObject != null && target.currentHealth > 0)
                {
                    validTargetsForJob.Add(target);
                }
            }

            if (validTargetsForJob.Count == 0) return;

            List<Unit> validOwnUnits = ownUnits;
            if (validOwnUnits.Count == 0) return;
            EnsureEngagedOwnArraysCapacity(validOwnUnits.Count);
            EnsureEngagedEnemyArrayCapacity(validTargetsForJob.Count);

            for (int i = 0; i < validOwnUnits.Count; i++)
            {
                engOwnPositions[i] = validOwnUnits[i].transform.position;
            }

            for (int i = 0; i < validTargetsForJob.Count; i++)
            {
                engEnemyPositions[i] = validTargetsForJob[i].transform.position;
            }

            var job = new FindClosestTargetJob
            {
                OwnUnitPositions = engOwnPositions,
                EnemyTargetPositions = engEnemyPositions,
                ClosestTargetIndices = engResultIndices,
                ClosestTargetSqrDistances = engResultSqrDistances
            };

            JobHandle handle = job.Schedule(validOwnUnits.Count, 32);
            handle.Complete();

            for (int i = 0; i < validOwnUnits.Count; i++)
            {
                Unit currentUnit = validOwnUnits[i];
                int closestEnemyIndex = engResultIndices[i];

                if (closestEnemyIndex == -1) continue;

                DestructibleTarget target = validTargetsForJob[closestEnemyIndex];
                currentUnit.SetClosestEnemy(target);

                if (ownFormation.UnitData.combatBehaviour == null) continue;

                float attackRange = ownFormation.UnitData.combatBehaviour.attackRange;
                float effectiveRangeSqr;
                float currentSqrDist;

                bool isStructure = (target.targetType == TargetType.Building || target.targetType == TargetType.Gate);

                if (isStructure)
                {
                    Collider targetCollider = target.GetComponent<Collider>();

                    if (targetCollider == null && target.GetOwner<Component>() != null)
                    {
                        targetCollider = target.GetOwner<Component>().GetComponent<Collider>();
                    }

                    if (targetCollider != null)
                    {

                        Vector3 closestPoint = targetCollider.ClosestPoint(currentUnit.transform.position);
                        currentSqrDist = (closestPoint - currentUnit.transform.position).sqrMagnitude;

                        float totalRange = attackRange + buildingAttackRangeBuffer;
                        effectiveRangeSqr = totalRange * totalRange;
                    }
                    else
                    {
                        currentSqrDist = engResultSqrDistances[i];
                        effectiveRangeSqr = attackRange * attackRange;
                    }
                }
                else
                {
                    currentSqrDist = engResultSqrDistances[i];
                    effectiveRangeSqr = attackRange * attackRange;
                }

                if (currentSqrDist <= effectiveRangeSqr)
                {
                    currentUnit.SetCurrentTarget(target);
                    if (currentUnit.currentState != Unit.UnitState.Fighting)
                    {
                        currentUnit.StopMovement();
                        currentUnit.currentState = Unit.UnitState.Fighting;
                        currentUnit.attackTimer = 0;
                    }
                }
                else if (currentUnit.currentState == Unit.UnitState.Idle)
                {
                    currentUnit.SetCurrentTarget(target);
                    currentUnit.currentState = Unit.UnitState.Moving;
                }
                else if (currentUnit.currentState == Unit.UnitState.Moving)
                {
                    currentUnit.SetCurrentTarget(target);
                }
            }
        }
        private void EnsureEngagedOwnArraysCapacity(int count)
        {
            if (engOwnPositions.IsCreated && engOwnPositions.Length == count) return;
            if (engOwnPositions.IsCreated) { engOwnPositions.Dispose(); engResultIndices.Dispose(); engResultSqrDistances.Dispose(); }
            engOwnPositions = new NativeArray<Vector3>(count, Allocator.Persistent);
            engResultIndices = new NativeArray<int>(count, Allocator.Persistent);
            engResultSqrDistances = new NativeArray<float>(count, Allocator.Persistent);
        }

        private void EnsureEngagedEnemyArrayCapacity(int count)
        {
            if (engEnemyPositions.IsCreated && engEnemyPositions.Length == count) return;
            if (engEnemyPositions.IsCreated) engEnemyPositions.Dispose();
            engEnemyPositions = new NativeArray<Vector3>(count, Allocator.Persistent);
        }

        private void DisposeEngagedArrays()
        {
            if (engOwnPositions.IsCreated) engOwnPositions.Dispose();
            if (engEnemyPositions.IsCreated) engEnemyPositions.Dispose();
            if (engResultIndices.IsCreated) engResultIndices.Dispose();
            if (engResultSqrDistances.IsCreated) engResultSqrDistances.Dispose();
        }

        private void ReallocateRotationArrays(int capacity)
        {
            DisposeRotationArrays();
            if (capacity <= 0) return;

            rotUnitPositions = new NativeArray<Vector3>(capacity, Allocator.Persistent);
            rotCurrentRotations = new NativeArray<Quaternion>(capacity, Allocator.Persistent);
            rotTargetPositions = new NativeArray<Vector3>(capacity, Allocator.Persistent);
            rotWaypointRotations = new NativeArray<Quaternion>(capacity, Allocator.Persistent);
            rotUnitStates = new NativeArray<int>(capacity, Allocator.Persistent);
            rotNewRotations = new NativeArray<Quaternion>(capacity, Allocator.Persistent);
            rotRotationSpeeds = new NativeArray<float>(capacity, Allocator.Persistent);
            rotVelocities = new NativeArray<Vector3>(capacity, Allocator.Persistent);
            rotClosestEnemyPositions = new NativeArray<Vector3>(capacity, Allocator.Persistent);
        }

        private void DisposeRotationArrays()
        {
            if (rotUnitPositions.IsCreated) rotUnitPositions.Dispose();
            if (rotCurrentRotations.IsCreated) rotCurrentRotations.Dispose();
            if (rotTargetPositions.IsCreated) rotTargetPositions.Dispose();
            if (rotWaypointRotations.IsCreated) rotWaypointRotations.Dispose();
            if (rotUnitStates.IsCreated) rotUnitStates.Dispose();
            if (rotNewRotations.IsCreated) rotNewRotations.Dispose();
            if (rotRotationSpeeds.IsCreated) rotRotationSpeeds.Dispose();
            if (rotVelocities.IsCreated) rotVelocities.Dispose();
            if (rotClosestEnemyPositions.IsCreated) rotClosestEnemyPositions.Dispose();
        }

        void ProcessUnitRotations()
        {
            allActiveUnits.Clear();
            var allFormations = FormationController.instance.GetFormations();
            if (allFormations == null) return;
            foreach (var formation in allFormations)
            {
                if (formation != null) { allActiveUnits.AddRange(formation.GetCachedUnits()); }
            }

            allActiveUnits.RemoveAll(item => item == null);
            if (allActiveUnits.Count == 0) return;

            int unitCount = allActiveUnits.Count;

            if (!rotUnitPositions.IsCreated || rotUnitPositions.Length != unitCount)
            {
                ReallocateRotationArrays(unitCount);
            }

            for (int i = 0; i < unitCount; i++)
            {
                var unit = allActiveUnits[i];
                if (unit == null || unit.agent == null || unit.GetFormation() == null || unit.GetFormation().UnitData == null) continue;

                rotUnitPositions[i] = unit.transform.position;
                rotCurrentRotations[i] = unit.transform.rotation;
                rotWaypointRotations[i] = (unit.Waypoint != null) ? unit.Waypoint.transform.rotation : unit.transform.rotation;
                rotRotationSpeeds[i] = unit.GetFormation().UnitData.unitRotationSpeed;
                rotVelocities[i] = unit.agent.velocity;

                var closestEnemy = unit.GetClosestEnemy();
                if (closestEnemy != null)
                {
                    rotClosestEnemyPositions[i] = closestEnemy.transform.position;
                }
                else
                {
                    rotClosestEnemyPositions[i] = new Vector3(0, float.MaxValue, 0);
                }

                var currentTarget = unit.GetCurrentTarget();
                if (unit.currentState == Unit.UnitState.Fighting && currentTarget != null)
                {
                    rotUnitStates[i] = 1; rotTargetPositions[i] = currentTarget.transform.position;
                }
                else if (unit.currentState == Unit.UnitState.Moving)
                {
                    rotUnitStates[i] = 2; rotTargetPositions[i] = Vector3.zero;
                }
                else
                {
                    rotUnitStates[i] = 0; rotTargetPositions[i] = Vector3.zero;
                }
            }

            var job = new RotationJob
            {
                UnitPositions = rotUnitPositions,
                CurrentRotations = rotCurrentRotations,
                TargetPositions = rotTargetPositions,
                WaypointRotations = rotWaypointRotations,
                Velocities = rotVelocities,
                UnitStates = rotUnitStates,
                RotationSpeeds = rotRotationSpeeds,
                ClosestEnemyPositions = rotClosestEnemyPositions,
                DeltaTime = Time.deltaTime,
                NewRotations = rotNewRotations
            };

            JobHandle handle = job.Schedule(unitCount, 32);
            handle.Complete();

            for (int i = 0; i < unitCount; i++)
            {
                var unit = allActiveUnits[i];
                if (unit != null)
                {
                    unit.transform.rotation = rotNewRotations[i];
                    if (unit.visualsTransform != null && unit.visualsTransform != unit.transform)
                    {
                        unit.visualsTransform.localRotation = Quaternion.identity;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            DisposeRotationArrays();
            DisposeEngagedArrays();
        }
    }

    [System.Serializable]
    public class InGameArmy
    {
        public List<ArmyData> armyData;
        public ArmySO currentArmy;

        public void SetUpArmyData(ArmySO army)
        {
            currentArmy = army;
            armyData = new List<ArmyData>();

            if (army == null || army.armyData == null) return;
            foreach (var data in army.armyData)
            {
                if (data != null && data.Unit != null)
                {
                    armyData.Add(new ArmyData { Unit = data.Unit, Troops = data.Troops });
                }
            }
        }

        public void SetArmyDataToArmy()
        {
            if (currentArmy == null) return;
            currentArmy.armyData = this.armyData;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(currentArmy);
#endif
        }

        public void OnCheckSizes()
        {
            if (armyData == null) return;
            foreach (var army in armyData)
            {
                if (army != null && army.Troops > army.Unit.Troops)
                {
                    army.Troops = army.Unit.Troops;
                }
            }
        }

        public void OnUnitDeath(ArmyData data)
        {
            if (armyData == null || armyData.Count == 0) return;

            for (int i = 0; i < armyData.Count; i++)
            {
                if (armyData[i].Unit == data.Unit)
                {
                    if (armyData[i].Troops > 0)
                    {
                        armyData[i].Troops--;
                    }
                    if (armyData[i].Troops <= 0)
                    {

                        OnFormationDeath(armyData[i]);

                        i--;
                    }
                    break;
                }
            }
        }
        void OnFormationDeath(ArmyData data)
        {
            if (armyData.Contains(data))
                armyData.Remove(data);
        }
    }
}