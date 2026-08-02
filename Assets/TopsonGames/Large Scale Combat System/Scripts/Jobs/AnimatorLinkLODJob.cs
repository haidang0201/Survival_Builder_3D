using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace TopsonGames
{
    [BurstCompile]
    public struct AnimatorDistanceJob : IJobParallelForTransform
    {
        [ReadOnly] public Vector3 CameraPosition;
        [ReadOnly] public float CullDistanceSqr;

        [WriteOnly] public NativeArray<bool> Results;

        public void Execute(int index, TransformAccess transform)
        {
            Results[index] = Vector3.SqrMagnitude(transform.position - CameraPosition) <= CullDistanceSqr;
        }
    }
}