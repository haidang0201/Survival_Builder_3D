namespace TopsonGames
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;

    [BurstCompile]
    public struct RotationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> UnitPositions;
        [ReadOnly] public NativeArray<Quaternion> CurrentRotations;
        [ReadOnly] public NativeArray<Vector3> TargetPositions;
        [ReadOnly] public NativeArray<Quaternion> WaypointRotations;
        [ReadOnly] public NativeArray<Vector3> ClosestEnemyPositions;

        [ReadOnly] public NativeArray<Vector3> Velocities;

        [ReadOnly] public NativeArray<int> UnitStates;
        [ReadOnly] public NativeArray<float> RotationSpeeds;
        [ReadOnly] public float DeltaTime;

        public NativeArray<Quaternion> NewRotations;

        public void Execute(int index)
        {
            Quaternion currentRotation = CurrentRotations[index];
            Quaternion finalRotation;

            float rotationSpeed = RotationSpeeds[index];
            int state = UnitStates[index];

            if (state == 1) // Fighting
            {
                Vector3 direction = TargetPositions[index] - UnitPositions[index];
                if (direction.sqrMagnitude > 0.01f)
                {
                    finalRotation = Quaternion.Slerp(currentRotation, Quaternion.LookRotation(direction), DeltaTime * rotationSpeed);
                }
                else
                {
                    finalRotation = currentRotation;
                }
            }
            else if (state == 2) // Moving
            {
                Vector3 velocity = Velocities[index];
                if (velocity.sqrMagnitude > 0.01f)
                {
                    finalRotation = Quaternion.Slerp(currentRotation, Quaternion.LookRotation(velocity), DeltaTime * rotationSpeed);
                }
                else
                {
                    finalRotation = Quaternion.Slerp(currentRotation, WaypointRotations[index], DeltaTime * rotationSpeed);
                }
            }
            else // 0 = Idle
            {
                Vector3 closestEnemyPos = ClosestEnemyPositions[index];

                if (closestEnemyPos.y != float.MaxValue)
                {
                    Vector3 direction = closestEnemyPos - UnitPositions[index];
                    if (direction.sqrMagnitude > 0.01f)
                    {
                        finalRotation = Quaternion.Slerp(currentRotation, Quaternion.LookRotation(direction), DeltaTime * rotationSpeed);
                    }
                    else
                    {
                        finalRotation = currentRotation;
                    }
                }
                else
                {
                    finalRotation = Quaternion.Slerp(currentRotation, WaypointRotations[index], DeltaTime * rotationSpeed);
                }
            }

            NewRotations[index] = finalRotation;
        }
    }
}