namespace TopsonGames
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;

    [BurstCompile]
    public struct FindClosestEnemyJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> OwnUnitPositions;
        [ReadOnly] public NativeArray<Vector3> EnemyUnitPositions;

        public NativeArray<int> ClosestEnemyIndices;
        public NativeArray<float> ClosestEnemySqrDistances;

        public void Execute(int index)
        {
            float minDistanceSq = float.MaxValue;
            int bestTargetIndex = -1;
            Vector3 ownPosition = OwnUnitPositions[index];
            if (EnemyUnitPositions.Length == 0)
            {
                ClosestEnemyIndices[index] = -1;
                ClosestEnemySqrDistances[index] = float.MaxValue;
                return;
            }

            for (int i = 0; i < EnemyUnitPositions.Length; i++)
            {
                float distanceSq = Vector3.SqrMagnitude(ownPosition - EnemyUnitPositions[i]);
                if (distanceSq < minDistanceSq)
                {
                    minDistanceSq = distanceSq;
                    bestTargetIndex = i;
                }
            }
            ClosestEnemyIndices[index] = bestTargetIndex;
            ClosestEnemySqrDistances[index] = minDistanceSq;
        }
    }
}