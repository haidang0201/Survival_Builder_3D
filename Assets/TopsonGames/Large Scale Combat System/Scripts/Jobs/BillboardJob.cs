using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace TopsonGames
{
    [BurstCompile]
    public struct BillboardJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> FlagPositions;
        [ReadOnly] public Vector3 CameraPosition;

        [WriteOnly] public NativeArray<Quaternion> OutputRotations;

        public void Execute(int index)
        {
            Vector3 direction = CameraPosition - FlagPositions[index];
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                OutputRotations[index] = Quaternion.LookRotation(direction);
            }
            else
            {
                OutputRotations[index] = Quaternion.identity;
            }
        }
    }
}