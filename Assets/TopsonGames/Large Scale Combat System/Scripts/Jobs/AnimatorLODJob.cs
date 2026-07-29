

namespace TopsonGames
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;

    public struct AnimatorLODResult
    {
        public bool IsRendererVisible;
        public int UpdateRate;
    }

    [BurstCompile]
    public struct AnimatorLODJob : IJobParallelFor
    {

        [ReadOnly] public Vector3 CameraPosition;
        [ReadOnly] public NativeArray<Plane> FrustumPlanes;
        [ReadOnly] public NativeArray<Vector3> UnitPositions;
        [ReadOnly] public NativeArray<Bounds> RendererBounds;

        [ReadOnly] public NativeArray<float> LodDistances;
        [ReadOnly] public NativeArray<int> LodUpdateRates;
        [ReadOnly] public float CullDistance;
        [ReadOnly] public int CulledUpdateRate;

        public NativeArray<AnimatorLODResult> Results;

        public void Execute(int index)
        {
            float distance = Vector3.Distance(UnitPositions[index], CameraPosition);
            bool isInFrustum = IsBoundsInFrustum(FrustumPlanes, RendererBounds[index]);

            bool isRendererVisible = isInFrustum && distance <= CullDistance;
            int updateRate = 1;

            if (!isInFrustum || distance > CullDistance)
            {
                updateRate = CulledUpdateRate;
            }
            else
            {
                for (int i = 0; i < LodDistances.Length; i++)
                {
                    if (distance > LodDistances[i])
                    {
                        updateRate = LodUpdateRates[i];
                    }
                    else
                    {
                        break; 
                    }
                }
            }

            Results[index] = new AnimatorLODResult
            {
                IsRendererVisible = isRendererVisible,
                UpdateRate = updateRate
            };
        }

        private bool IsBoundsInFrustum(NativeArray<Plane> planes, Bounds bounds)
        {
            for (int i = 0; i < 6; i++)
            {
                float distance = planes[i].GetDistanceToPoint(bounds.center);
                float radius = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(planes[i].normal.x), Mathf.Abs(planes[i].normal.y), Mathf.Abs(planes[i].normal.z)));
                if (distance + radius < 0)
                {
                    return false; 
                }
            }
            return true;
        }
    }
}