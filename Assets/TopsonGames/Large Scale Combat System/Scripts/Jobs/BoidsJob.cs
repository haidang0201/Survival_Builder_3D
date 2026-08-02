namespace TopsonGames
{

    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;

    [BurstCompile]
    public struct BoidsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> OwnWaypointPositions;
        [ReadOnly] public NativeArray<Quaternion> OwnWaypointRotations;
        [ReadOnly] public NativeArray<Vector3> IdealFormationPositions;

        [ReadOnly] public NativeArray<Vector3> EnemyWaypointPositions;

        [ReadOnly] public float neighborRadius;
        [ReadOnly] public float separationRadius;
        [ReadOnly] public float cohesionForce;
        [ReadOnly] public float separationForce;
        [ReadOnly] public float alignmentForce;
        [ReadOnly] public float formationCohesionForce;
        [ReadOnly] public float enemyAttractionForce;
        [ReadOnly] public float enemySeparationRadius;
        [ReadOnly] public float enemySeparationForce;
        [ReadOnly] public float engagementDistance;
        [ReadOnly] public float stopDistanceToEnemy;

        [ReadOnly] public int enemyFrontlineSampleSize;

        public NativeArray<Vector3> ForceVectors;

        public void Execute(int index)
        {
            Vector3 currentPosition = OwnWaypointPositions[index];
            Vector3 finalForce = Vector3.zero;

            Vector3 separation = Vector3.zero;
            Vector3 cohesionCenter = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            int neighborCount = 0;

            for (int i = 0; i < OwnWaypointPositions.Length; i++)
            {
                if (i == index) continue;

                float distSqr = Vector3.SqrMagnitude(currentPosition - OwnWaypointPositions[i]);
                if (distSqr <= neighborRadius * neighborRadius)
                {
                    if (distSqr > 0 && distSqr < separationRadius * separationRadius)
                    {
                        separation += (currentPosition - OwnWaypointPositions[i]) / distSqr;
                    }
                    cohesionCenter += OwnWaypointPositions[i];
                    alignment += OwnWaypointRotations[i] * Vector3.forward;
                    neighborCount++;
                }
            }

            if (neighborCount > 0)
            {
                cohesionCenter /= neighborCount;
                Vector3 cohesion = (cohesionCenter - currentPosition).normalized;
                finalForce += separation.normalized * separationForce;
                finalForce += cohesion * cohesionForce;
                finalForce += alignment.normalized * alignmentForce;
            }

            if (IdealFormationPositions.Length > index)
            {
                Vector3 idealPosition = IdealFormationPositions[index];
                Vector3 cohesionToIdeal = (idealPosition - currentPosition).normalized;
                finalForce += cohesionToIdeal * formationCohesionForce;
            }


            if (EnemyWaypointPositions.Length > 0)
            {
                NativeList<float> closestDistances = new NativeList<float>(Allocator.Temp);
                for (int i = 0; i < EnemyWaypointPositions.Length; i++)
                {
                    closestDistances.Add(Vector3.SqrMagnitude(currentPosition - EnemyWaypointPositions[i]));
                }
                closestDistances.Sort();

                float distanceToFrontline = Mathf.Sqrt(closestDistances[0]);

                if (distanceToFrontline < engagementDistance)
                {
                    Vector3 localEnemyFrontCenter = Vector3.zero;
                    int sampleCount = Mathf.Min(enemyFrontlineSampleSize, EnemyWaypointPositions.Length);
                    NativeList<int> usedIndices = new NativeList<int>(Allocator.Temp);

                    for (int i = 0; i < sampleCount; i++)
                    {
                        float distToFind = closestDistances[i];
                        for (int j = 0; j < EnemyWaypointPositions.Length; j++)
                        {
                            if (usedIndices.Contains(j)) continue;

                            if (Mathf.Approximately(Vector3.SqrMagnitude(currentPosition - EnemyWaypointPositions[j]), distToFind))
                            {
                                localEnemyFrontCenter += EnemyWaypointPositions[j];
                                usedIndices.Add(j);
                                break;
                            }
                        }
                    }
                    localEnemyFrontCenter /= sampleCount;

                    Vector3 enemyInteractionForce = Vector3.zero;
                    if (distanceToFrontline > stopDistanceToEnemy)
                    {
                        float attractionFactor = Mathf.Clamp01((distanceToFrontline - stopDistanceToEnemy) / (engagementDistance - stopDistanceToEnemy));
                        Vector3 attractionDirection = (localEnemyFrontCenter - currentPosition).normalized;
                        enemyInteractionForce += attractionDirection * enemyAttractionForce * attractionFactor;
                    }

                    for (int i = 0; i < EnemyWaypointPositions.Length; i++)
                    {
                        Vector3 direction = currentPosition - EnemyWaypointPositions[i];
                        float distSqrToEnemy = direction.sqrMagnitude;
                        if (distSqrToEnemy > 0 && distSqrToEnemy < enemySeparationRadius * enemySeparationRadius)
                        {
                            float distance = Mathf.Sqrt(distSqrToEnemy);
                            float separationStrength = enemySeparationForce * (1.0f - (distance / enemySeparationRadius));
                            enemyInteractionForce += (direction / distance) * separationStrength;
                        }
                    }
                    finalForce += enemyInteractionForce;

                    usedIndices.Dispose();
                }
                closestDistances.Dispose();
            }

            finalForce.y = 0;
            ForceVectors[index] = finalForce;
        }
    }
}