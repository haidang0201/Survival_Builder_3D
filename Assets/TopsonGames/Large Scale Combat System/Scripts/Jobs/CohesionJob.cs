namespace TopsonGames
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;

    [BurstCompile]
    public struct CohesionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> UnitPositions;
        [ReadOnly] public NativeArray<Vector3> WaypointPositions;
        [ReadOnly] public NativeArray<Vector3> FormationVelocities;

        [ReadOnly] public float BaseSpeed;
        [ReadOnly] public float CatchUpFactor;
        [ReadOnly] public float SlowDownFactor;
        [ReadOnly] public float MaxSpeedMultiplier;

        public NativeArray<float> NewAgentSpeeds;

        public void Execute(int index)
        {
            float distanceToSlot = Vector3.Distance(UnitPositions[index], WaypointPositions[index]);
            float speedModifier = 1.0f;

            Vector3 positionError = WaypointPositions[index] - UnitPositions[index];

            if (distanceToSlot > 1.0f)
            {
                if (Vector3.Dot(positionError, FormationVelocities[index]) > 0)
                {
                    speedModifier = 1.0f + (distanceToSlot * CatchUpFactor);
                    speedModifier = Mathf.Min(speedModifier, MaxSpeedMultiplier);
                }
            }
            else if (distanceToSlot < 0.5f)
            {
                speedModifier = 1.0f - (distanceToSlot * SlowDownFactor);
                speedModifier = Mathf.Max(speedModifier, 0.5f);
            }

            NewAgentSpeeds[index] = BaseSpeed * speedModifier;
        }
    }
}