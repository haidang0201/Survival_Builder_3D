namespace TopsonGames
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;

    [BurstCompile]
    public struct FindClosestTargetJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> OwnUnitPositions;
        [ReadOnly] public NativeArray<Vector3> EnemyTargetPositions;

        public NativeArray<int> ClosestTargetIndices;
        public NativeArray<float> ClosestTargetSqrDistances;

        public void Execute(int index)
        {
            float minDistanceSq = float.MaxValue;
            int bestTargetIndex = -1;
            Vector3 ownPosition = OwnUnitPositions[index];
            if (EnemyTargetPositions.Length == 0)
            {
                ClosestTargetIndices[index] = -1;
                ClosestTargetSqrDistances[index] = float.MaxValue;
                return;
            }

            for (int i = 0; i < EnemyTargetPositions.Length; i++)
            {
                float distanceSq = Vector3.SqrMagnitude(ownPosition - EnemyTargetPositions[i]);
                if (distanceSq < minDistanceSq)
                {
                    minDistanceSq = distanceSq;
                    bestTargetIndex = i;
                }
            }
            ClosestTargetIndices[index] = bestTargetIndex;
            ClosestTargetSqrDistances[index] = minDistanceSq;
        }
    }
}