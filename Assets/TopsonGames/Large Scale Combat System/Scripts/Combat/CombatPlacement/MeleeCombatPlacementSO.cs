namespace TopsonGames
{
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;
    using UnityEngine.AI;

    [CreateAssetMenu(fileName = "MeleeCombatPlacementSO", menuName = "TopsonGames/Combat Placement/Melee")]
    public class MeleeCombatPlacementSO : CombatPlacementSO
    {
        [Header("Boids Settings")]
        public float neighborRadius = 3.0f;
        public float separationRadius = 1.5f;
        public float cohesionForce = 1.0f;
        public float separationForce = 3.0f;
        public float alignmentForce = 1.0f;
        public float formationCohesionForce = 2.0f;

        [Header("Enemy Interaction")]
        public float enemyAttractionForce = 15.0f;
        public float enemySeparationRadius = 1.7f;
        public float enemySeparationForce = 200.0f;
        public float engagementDistance = 15.0f;
        public float arrivalDistance = 5.0f;
        public float stopDistanceToEnemy = 1.0f;

        [Header("Building Interaction")]
        [Tooltip("The desired distance from the surface of a building/gate.")]
        public float buildingAttackDistance = 1.5f;

        [Header("Advanced Enemy Interaction")]
        [Tooltip("How many of the closest enemies should be used to calculate the 'front'? 2-3 is a good starting point.")]
        public int enemyFrontlineSampleSize = 3;

        private NativeArray<Vector3> _ownWaypointPositions;
        private NativeArray<Quaternion> _ownWaypointRotations;
        private NativeArray<Vector3> _enemyWaypointPositions;
        private NativeArray<Vector3> _idealPositions;
        private NativeArray<Vector3> _forceVectors;

        private readonly List<Vector3> _idealPositionsBuffer = new List<Vector3>(64);
        private readonly List<Quaternion> _unusedRotationsBuffer = new List<Quaternion>(64);

        private void EnsureOwnCapacity(int count)
        {
            if (_ownWaypointPositions.IsCreated && _ownWaypointPositions.Length == count) return;
            if (_ownWaypointPositions.IsCreated)
            {
                _ownWaypointPositions.Dispose();
                _ownWaypointRotations.Dispose();
                _idealPositions.Dispose();
                _forceVectors.Dispose();
            }
            _ownWaypointPositions = new NativeArray<Vector3>(count, Allocator.Persistent);
            _ownWaypointRotations = new NativeArray<Quaternion>(count, Allocator.Persistent);
            _idealPositions = new NativeArray<Vector3>(count, Allocator.Persistent);
            _forceVectors = new NativeArray<Vector3>(count, Allocator.Persistent);
        }

        private void EnsureEnemyCapacity(int count)
        {
            if (_enemyWaypointPositions.IsCreated && _enemyWaypointPositions.Length == count) return;
            if (_enemyWaypointPositions.IsCreated) _enemyWaypointPositions.Dispose();
            _enemyWaypointPositions = new NativeArray<Vector3>(count, Allocator.Persistent);
        }
        private void OnDisable()
        {
            if (_ownWaypointPositions.IsCreated) _ownWaypointPositions.Dispose();
            if (_ownWaypointRotations.IsCreated) _ownWaypointRotations.Dispose();
            if (_enemyWaypointPositions.IsCreated) _enemyWaypointPositions.Dispose();
            if (_idealPositions.IsCreated) _idealPositions.Dispose();
            if (_forceVectors.IsCreated) _forceVectors.Dispose();
        }

        public override void TickUpdateEngagement(Formation formation, Formation engagedEnemy)
        {
            if (formation == null || formation.GetCachedUnits().Count == 0) return;

            if (engagedEnemy != null && !formation.isDefender)
            {
                List<Unit> ownUnits = formation.GetCachedUnits();
                List<Unit> enemyUnits = engagedEnemy.GetCachedUnits();

                if (ownUnits.Count == 0 || enemyUnits.Count == 0) return;

                EnsureOwnCapacity(ownUnits.Count);
                EnsureEnemyCapacity(enemyUnits.Count);

                NativeArray<Vector3> ownWaypointPositions = _ownWaypointPositions;
                NativeArray<Quaternion> ownWaypointRotations = _ownWaypointRotations;
                for (int i = 0; i < ownUnits.Count; i++)
                {
                    if (ownUnits[i] != null && ownUnits[i].Waypoint != null)
                    {
                        ownWaypointPositions[i] = ownUnits[i].Waypoint.transform.position;
                        ownWaypointRotations[i] = ownUnits[i].Waypoint.transform.rotation;
                    }
                    else
                    {
                        ownWaypointPositions[i] = Vector3.zero;
                        ownWaypointRotations[i] = Quaternion.identity;
                    }
                }

                NativeArray<Vector3> enemyWaypointPositions = _enemyWaypointPositions;
                for (int i = 0; i < enemyUnits.Count; i++)
                {
                    if (enemyUnits[i] != null && enemyUnits[i].Waypoint != null)
                    {
                        enemyWaypointPositions[i] = enemyUnits[i].Waypoint.transform.position;
                    }
                    else
                    {
                        enemyWaypointPositions[i] = Vector3.zero;
                    }
                }

                _idealPositionsBuffer.Clear();
                _unusedRotationsBuffer.Clear();
                formation.GetIdealFormationPoints(formation.WaypointCenter.position, formation.WaypointCenter.rotation, _idealPositionsBuffer, _unusedRotationsBuffer);
                NativeArray<Vector3> idealPositions = _idealPositions;
                for (int i = 0; i < idealPositions.Length; i++)
                {
                    idealPositions[i] = (i < _idealPositionsBuffer.Count) ? _idealPositionsBuffer[i] : Vector3.zero;
                }
                NativeArray<Vector3> forceVectors = _forceVectors;

                var job = new BoidsJob
                {
                    OwnWaypointPositions = ownWaypointPositions,
                    OwnWaypointRotations = ownWaypointRotations,
                    IdealFormationPositions = idealPositions,
                    EnemyWaypointPositions = enemyWaypointPositions,
                    neighborRadius = this.neighborRadius,
                    separationRadius = this.separationRadius,
                    cohesionForce = this.cohesionForce,
                    separationForce = this.separationForce,
                    alignmentForce = this.alignmentForce,
                    formationCohesionForce = this.formationCohesionForce,
                    enemyAttractionForce = this.enemyAttractionForce,
                    enemySeparationRadius = this.enemySeparationRadius,
                    enemySeparationForce = this.enemySeparationForce,
                    engagementDistance = this.engagementDistance,
                    stopDistanceToEnemy = this.stopDistanceToEnemy,
                    enemyFrontlineSampleSize = this.enemyFrontlineSampleSize,
                    ForceVectors = forceVectors
                };

                JobHandle handle = job.Schedule(ownUnits.Count, 32);
                handle.Complete();

                for (int i = 0; i < ownUnits.Count; i++)
                {
                    var unit = ownUnits[i];
                    if (unit == null || unit.Waypoint == null) continue;

                    Vector3 force = Vector3.ClampMagnitude(forceVectors[i], formation.UnitData.formationMoveSpeed * 2f);
                    unit.Waypoint.transform.position += force * Time.deltaTime;

                    Vector3 currentWaypointPos = unit.Waypoint.transform.position;
                    if (NavMesh.SamplePosition(currentWaypointPos, out NavMeshHit hitY, 1.0f, NavMesh.AllAreas))
                    {
                        currentWaypointPos.y = hitY.position.y;
                    }
                    else
                    {
                        currentWaypointPos.y = unit.transform.position.y;
                    }
                    unit.Waypoint.transform.position = currentWaypointPos;
                }
            }

            else if (engagedEnemy == null)
            {
                var units = formation.GetCachedUnits();
                DestructibleTarget buildingTarget = units.FirstOrDefault(u => u != null && u.GetCurrentTarget() != null && (u.GetCurrentTarget().targetType == TargetType.Building || u.GetCurrentTarget().targetType == TargetType.Gate))?.GetCurrentTarget();

                if (buildingTarget == null) return;

                Collider buildingCollider = buildingTarget.GetComponent<Collider>();
                if (buildingCollider == null) return;

                _idealPositionsBuffer.Clear();
                _unusedRotationsBuffer.Clear();
                formation.GetIdealFormationPoints(formation.WaypointCenter.position, formation.WaypointCenter.rotation, _idealPositionsBuffer, _unusedRotationsBuffer);
                List<Vector3> idealPositionsList = _idealPositionsBuffer;

                for (int i = 0; i < units.Count; i++)
                {
                    var unit = units[i];
                    if (unit == null || unit.Waypoint == null) continue;

                    Vector3 currentWP = unit.Waypoint.transform.position;
                    Vector3 idealPos = idealPositionsList[i];
                    Vector3 force = Vector3.zero;

                    Vector3 closestPoint = buildingCollider.ClosestPoint(currentWP);
                    Vector3 toBuilding = closestPoint - currentWP;
                    toBuilding.y = 0;
                    float distToBuilding = toBuilding.magnitude;

                    if (distToBuilding > buildingAttackDistance)
                    {
                        force += toBuilding.normalized * enemyAttractionForce;
                    }
                    else if (distToBuilding < buildingAttackDistance - 0.5f)
                    {
                        force -= toBuilding.normalized * enemySeparationForce;
                    }
                    Vector3 toIdeal = idealPos - currentWP;
                    toIdeal.y = 0;
                    force += toIdeal.normalized * formationCohesionForce;


                    force = Vector3.ClampMagnitude(force, formation.UnitData.formationMoveSpeed * 2f);
                    unit.Waypoint.transform.position += force * Time.deltaTime;

                    if (toBuilding.sqrMagnitude > 0.01f)
                    {
                        unit.Waypoint.transform.rotation = Quaternion.LookRotation(toBuilding.normalized);
                    }
                    Vector3 pos = unit.Waypoint.transform.position;
                    if (NavMesh.SamplePosition(pos, out NavMeshHit hitY, 1.0f, NavMesh.AllAreas))
                    {
                        pos.y = hitY.position.y;
                    }
                    unit.Waypoint.transform.position = pos;
                }
            }
        }
    }
}