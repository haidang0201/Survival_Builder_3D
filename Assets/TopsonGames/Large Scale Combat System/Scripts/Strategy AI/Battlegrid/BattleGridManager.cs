namespace TopsonGames.AI.Grid
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Jobs;
    using Unity.Collections;
    using Unity.Burst;

    public struct GridShape
    {
        public int type; // 0 = Circle, 1 = Cone, 2 = Box
        public Vector3 center;
        public Vector3 forwardDir;
        public Vector3 boxHalfSize;
        public Matrix4x4 worldToLocal;
        public float value;
        public float range;
        public float angleThreshold;
    }

    [BurstCompile]
    public struct GridFillJob : IJobParallelFor
    {
        public int width;
        public int height;
        public float cellSize;
        public Vector3 originPosition;

        [ReadOnly] public NativeArray<GridShape> shapes;
        public NativeArray<float> gridArray;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;
            Vector3 cellWorldPos = new Vector3(x * cellSize, originPosition.y, y * cellSize) + originPosition;

            float cellValue = 0f;

            for (int i = 0; i < shapes.Length; i++)
            {
                GridShape shape = shapes[i];

                if (shape.type == 0) // Circle
                {
                    float dist = Vector3.Distance(new Vector3(shape.center.x, 0, shape.center.z), new Vector3(cellWorldPos.x, 0, cellWorldPos.z));
                    if (dist <= shape.range)
                    {
                        float falloff = 1f - (dist / shape.range);
                        cellValue += shape.value * falloff;
                    }
                }
                else if (shape.type == 1) // Cone
                {
                    float dist = Vector3.Distance(shape.center, cellWorldPos);
                    if (dist <= shape.range)
                    {
                        Vector3 dirToCell = (cellWorldPos - shape.center).normalized;
                        float dot = Vector3.Dot(shape.forwardDir, dirToCell);
                        if (dot >= shape.angleThreshold)
                        {
                            float falloff = 1f - (dist / shape.range);
                            cellValue += shape.value * falloff;
                        }
                    }
                }
                else if (shape.type == 2) // Box
                {
                    Vector3 localPos = shape.worldToLocal.MultiplyPoint3x4(cellWorldPos);
                    if (Mathf.Abs(localPos.x) <= shape.boxHalfSize.x && Mathf.Abs(localPos.z) <= shape.boxHalfSize.z)
                    {
                        cellValue += shape.value;
                    }
                }
            }

            gridArray[index] = cellValue;
        }
    }

    public class BattleGridManager : MonoBehaviour
    {
        public static BattleGridManager instance;
        [TextArea(10, 7)]
        public string Explanation =
            "Optional for better AI.\n\n" +
            "Currently only for Field Battles, Sieges will come in the next update\n";

        [Header("Grid Configuration")]
        public int width = 100;
        public int height = 100;
        public float cellSize = 2.5f;
        public bool centerGrid = true;
        public Vector3 originOffset = new Vector3(0, 0, 0);
        public float updateInterval = 0.5f;

        [Header("Pathfinding Settings")]
        public int pathfindingClearance = 2;

        [Header("AI Intelligence Settings")]
        public BattleGridUnitSettingsSO unitGridSettings;
        public List<UnitTypeSO> trackedUnitTypes;

        private Dictionary<int, Dictionary<UnitTypeSO, BattleGrid>> dangerMaps = new Dictionary<int, Dictionary<UnitTypeSO, BattleGrid>>();
        private Dictionary<int, Dictionary<UnitTypeSO, BattleGrid>> targetMaps = new Dictionary<int, Dictionary<UnitTypeSO, BattleGrid>>();

        private Dictionary<BattleGrid, List<GridShape>> pendingShapes = new Dictionary<BattleGrid, List<GridShape>>();

        [Header("Debug")]
        public bool showGizmos = true;
        public enum DebugMode { None, Danger, Targets }
        public DebugMode currentDebugMode = DebugMode.Danger;
        public UnitTypeSO debugUnitType;
        [Range(0, 1)] public int debugTeamID = 0;

        private List<List<Vector3>> activeDebugPaths = new List<List<Vector3>>();
        private List<Color> activeDebugColors = new List<Color>();

        private void Awake()
        {
            instance = this;
            InitializeGrids();
        }

        private Vector3 CalculateOrigin()
        {
            if (centerGrid)
                return transform.position + originOffset - new Vector3(width * cellSize / 2f, 0, height * cellSize / 2f);
            return transform.position + originOffset;
        }

        void InitializeGrids()
        {
            dangerMaps.Clear();
            targetMaps.Clear();
            pendingShapes.Clear();
        }

        private void Start()
        {
            StartCoroutine(UpdateRoutine());
        }

        IEnumerator UpdateRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(updateInterval);
            while (true)
            {
                // PERFORMANCETRICK: Coroutine warten lassen, bis Jobs im Hintergrund fertig sind!
                yield return StartCoroutine(UpdateInfluenceMapsWithJobsRoutine());

                if (activeDebugPaths.Count > 0)
                {
                    activeDebugPaths.Clear();
                    activeDebugColors.Clear();
                }
                yield return wait;
            }
        }

        void EnsureGridExists(Dictionary<int, Dictionary<UnitTypeSO, BattleGrid>> dict, int team, UnitTypeSO type, Vector3 origin)
        {
            if (!dict.ContainsKey(team)) dict[team] = new Dictionary<UnitTypeSO, BattleGrid>();
            if (!dict[team].ContainsKey(type))
            {
                BattleGrid newGrid = new BattleGrid(width, height, cellSize, origin);
                dict[team][type] = newGrid;
                pendingShapes[newGrid] = new List<GridShape>();
            }
        }

        IEnumerator UpdateInfluenceMapsWithJobsRoutine()
        {
            if (trackedUnitTypes == null || trackedUnitTypes.Count == 0) yield break;

            Vector3 realOrigin = CalculateOrigin();
            var formations = FormationController.instance.GetFormations();
            DamageModifierSO dmgMod = GameManager.instance.damageModifier;

            foreach (var list in pendingShapes.Values)
            {
                list.Clear();
            }

            foreach (var type in trackedUnitTypes)
            {
                if (type == null) continue;
                for (int team = 0; team < 2; team++)
                {
                    EnsureGridExists(dangerMaps, team, type, realOrigin);
                    EnsureGridExists(targetMaps, team, type, realOrigin);

                    dangerMaps[team][type].originPosition = realOrigin;
                    targetMaps[team][type].originPosition = realOrigin;
                }
            }

            foreach (var f in formations)
            {
                if (f == null || f.UnitData == null || f.CurrentState == Formation.FormationState.Fleeing) continue;
                int ownerTeam = f.TeamID;
                if (ownerTeam < 0 || ownerTeam > 1) continue;

                float basePower = f.numberOfUnits * f.UnitData.baseDamage;
                if (basePower < 1f) basePower = 1f;

                bool isArcher = (f.UnitData.combatBehaviour is ArcherCombatSO);
                Vector3 boxCenter = f.CalculateUnitCenter();
                Vector3 baseBoxSize = Vector3.one;
                Quaternion boxRot = f.transform.rotation;
                bool useBox = false;

                if (!isArcher && f.engagementTrigger != null)
                {
                    BoxCollider box = f.engagementTrigger.GetComponent<BoxCollider>();
                    if (box != null)
                    {
                        useBox = true;
                        boxCenter = f.engagementTrigger.transform.TransformPoint(box.center);
                        baseBoxSize = Vector3.Scale(box.size, f.engagementTrigger.transform.lossyScale);
                        boxRot = f.engagementTrigger.transform.rotation;
                        baseBoxSize += new Vector3(1f, 0, 1f);
                    }
                }

                float baseCoreRadius = 15f + (f.numberOfUnits * 0.1f);
                float coneAngle = 360f;
                Vector3 lookDir = f.transform.forward;
                if (f.engagementTrigger != null) lookDir = f.engagementTrigger.transform.forward;

                if (isArcher)
                {
                    var ac = f.UnitData.combatBehaviour as ArcherCombatSO;
                    baseCoreRadius = ac.archerDetectionRange;
                    coneAngle = ac.archerOuterAngle;
                }

                foreach (var trackedType in trackedUnitTypes)
                {
                    if (trackedType == null) continue;

                    float rMult = unitGridSettings != null ? unitGridSettings.GetRadiusMultiplier(trackedType, f.UnitData.unitType) : 1.0f;
                    float vMult = unitGridSettings != null ? unitGridSettings.GetValueMultiplier(trackedType, f.UnitData.unitType) : 1.0f;

                    float coreRadius = baseCoreRadius * rMult;
                    Vector3 boxSize = baseBoxSize * rMult;

                    float dmgModToTracked = dmgMod.GetUnitModifier(f.UnitData.unitType, trackedType);
                    float threatVal = basePower * dmgModToTracked * vMult;

                    var dangerGrid = dangerMaps[ownerTeam][trackedType];
                    var targetGrid = targetMaps[ownerTeam][trackedType];

                    if (!isArcher)
                    {
                        if (dmgModToTracked > 1.2f) threatVal *= 3.0f;
                        else threatVal *= 1.5f;

                        if (useBox)
                        {
                            Matrix4x4 wtl = Matrix4x4.TRS(boxCenter, boxRot, Vector3.one).inverse;
                            pendingShapes[dangerGrid].Add(new GridShape { type = 2, center = boxCenter, boxHalfSize = boxSize * 0.5f, worldToLocal = wtl, value = threatVal });
                        }
                        else
                        {
                            pendingShapes[dangerGrid].Add(new GridShape { type = 0, center = f.CalculateUnitCenter(), range = coreRadius, value = threatVal });
                        }

                        float bufferIntensity = threatVal * 0.15f;

                        if (useBox)
                        {
                            Vector3 bufferBoxSize = boxSize + new Vector3(15f * rMult, 0, 15f * rMult);
                            Matrix4x4 wtl = Matrix4x4.TRS(boxCenter, boxRot, Vector3.one).inverse;
                            pendingShapes[dangerGrid].Add(new GridShape { type = 2, center = boxCenter, boxHalfSize = bufferBoxSize * 0.5f, worldToLocal = wtl, value = bufferIntensity });
                        }
                        else
                        {
                            pendingShapes[dangerGrid].Add(new GridShape { type = 0, center = f.CalculateUnitCenter(), range = coreRadius + (15f * rMult), value = bufferIntensity });
                        }
                    }
                    else
                    {
                        threatVal *= 0.8f;
                        float angleThreshold = Mathf.Cos((coneAngle * 0.5f) * Mathf.Deg2Rad);
                        float angleThresholdBuffer = Mathf.Cos(((coneAngle + 20f) * 0.5f) * Mathf.Deg2Rad);

                        pendingShapes[dangerGrid].Add(new GridShape { type = 1, center = f.CalculateUnitCenter(), forwardDir = lookDir, range = coreRadius, angleThreshold = angleThreshold, value = threatVal });
                        pendingShapes[dangerGrid].Add(new GridShape { type = 1, center = f.CalculateUnitCenter(), forwardDir = lookDir, range = coreRadius + (10f * rMult), angleThreshold = angleThresholdBuffer, value = threatVal * 0.1f });
                    }

                    float dmgModFromTracked = dmgMod.GetUnitModifier(trackedType, f.UnitData.unitType);
                    float targetVal = basePower * dmgModFromTracked;

                    if (dmgModFromTracked > 1.2f) targetVal *= 2.0f;
                    if (isArcher) targetVal *= 1.5f;

                    pendingShapes[targetGrid].Add(new GridShape { type = 0, center = f.CalculateUnitCenter(), range = 15f, value = targetVal });

                    if (f.transform.forward != Vector3.zero)
                    {
                        Vector3 rearPos = f.CalculateUnitCenter() - (f.transform.forward * 12f);
                        pendingShapes[targetGrid].Add(new GridShape { type = 0, center = rearPos, range = 12f, value = targetVal * 1.5f });
                    }
                }
            }

            int gridCount = pendingShapes.Count;
            if (gridCount == 0) yield break;

            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(gridCount, Allocator.TempJob);
            List<NativeArray<GridShape>> allocatedShapes = new List<NativeArray<GridShape>>(gridCount);
            List<NativeArray<float>> allocatedGrids = new List<NativeArray<float>>(gridCount);
            List<BattleGrid> processedGrids = new List<BattleGrid>(gridCount);

            int jobIndex = 0;
            foreach (var kvp in pendingShapes)
            {
                BattleGrid bg = kvp.Key;
                List<GridShape> shapes = kvp.Value;

                NativeArray<GridShape> nativeShapes = new NativeArray<GridShape>(shapes.ToArray(), Allocator.TempJob);
                NativeArray<float> nativeGrid = new NativeArray<float>(bg.width * bg.height, Allocator.TempJob);

                GridFillJob job = new GridFillJob
                {
                    width = bg.width,
                    height = bg.height,
                    cellSize = bg.cellSize,
                    originPosition = bg.originPosition,
                    shapes = nativeShapes,
                    gridArray = nativeGrid
                };

                handles[jobIndex] = job.Schedule(bg.width * bg.height, 64);

                allocatedShapes.Add(nativeShapes);
                allocatedGrids.Add(nativeGrid);
                processedGrids.Add(bg);

                jobIndex++;
            }

            // PERFORMANCETRICK: Wir lassen das Spiel ein Frame weiterlaufen, während die Jobs rechnen!
            yield return null;

            JobHandle.CompleteAll(handles);

            for (int i = 0; i < processedGrids.Count; i++)
            {
                processedGrids[i].SetRawData(allocatedGrids[i].ToArray());

                allocatedShapes[i].Dispose();
                allocatedGrids[i].Dispose();
            }

            handles.Dispose();
        }

        public BattleGrid GetDangerMap(int myTeamID, UnitTypeSO myUnitType)
        {
            int enemyTeam = (myTeamID == 0) ? 1 : 0;
            if (dangerMaps.ContainsKey(enemyTeam) && dangerMaps[enemyTeam].ContainsKey(myUnitType)) return dangerMaps[enemyTeam][myUnitType];
            return null;
        }

        public BattleGrid GetTargetMap(int myTeamID, UnitTypeSO myUnitType)
        {
            int enemyTeam = (myTeamID == 0) ? 1 : 0;
            if (targetMaps.ContainsKey(enemyTeam) && targetMaps[enemyTeam].ContainsKey(myUnitType)) return targetMaps[enemyTeam][myUnitType];
            return null;
        }

        public void RegisterDebugPath(List<Vector3> pathPoints, Color color)
        {
            if (!showGizmos) return;
            activeDebugPaths.Add(new List<Vector3>(pathPoints));
            activeDebugColors.Add(color);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Vector3 realOrigin = CalculateOrigin();
            Vector3 center = realOrigin + new Vector3(width * cellSize / 2, 0, height * cellSize / 2);
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(center, new Vector3(width * cellSize, 2f, height * cellSize));

            if (debugUnitType != null)
            {
                int enemyTeam = (debugTeamID == 0) ? 1 : 0;
                if (currentDebugMode == DebugMode.Danger)
                {
                    if (dangerMaps.ContainsKey(enemyTeam) && dangerMaps[enemyTeam].ContainsKey(debugUnitType))
                        dangerMaps[enemyTeam][debugUnitType].DrawGizmos(Color.red, 0.1f);
                }
                else if (currentDebugMode == DebugMode.Targets)
                {
                    if (targetMaps.ContainsKey(enemyTeam) && targetMaps[enemyTeam].ContainsKey(debugUnitType))
                        targetMaps[enemyTeam][debugUnitType].DrawGizmos(Color.green, 0.1f);
                }
            }

            for (int i = 0; i < activeDebugPaths.Count; i++)
            {
                if (i >= activeDebugColors.Count) break;
                Gizmos.color = activeDebugColors[i];
                var path = activeDebugPaths[i];
                for (int j = 0; j < path.Count - 1; j++)
                {
                    Gizmos.DrawLine(path[j] + Vector3.up * 0.5f, path[j + 1] + Vector3.up * 0.5f);
                    Gizmos.DrawSphere(path[j + 1] + Vector3.up * 0.5f, 0.3f);
                }
            }
        }
    }
}