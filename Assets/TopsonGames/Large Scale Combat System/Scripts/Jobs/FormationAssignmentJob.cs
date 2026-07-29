using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace TopsonGames
{
    [BurstCompile]
    public struct FormationAssignmentJob : IJob
    {
        [ReadOnly] public NativeArray<Vector3> UnitPositions;
        [ReadOnly] public NativeArray<Vector3> SlotPositions;


        public NativeArray<int> ResultAssignedUnitIndices;

        private struct AssignmentPair : System.IComparable<AssignmentPair>
        {
            public int unitIndex;
            public int slotIndex;
            public float distanceSqr;

            public int CompareTo(AssignmentPair other)
            {
                return distanceSqr.CompareTo(other.distanceSqr);
            }
        }

        public void Execute()
        {
            int uCount = UnitPositions.Length;
            int sCount = SlotPositions.Length;
            int pairCount = uCount * sCount;

            NativeArray<AssignmentPair> pairs = new NativeArray<AssignmentPair>(pairCount, Allocator.Temp);

            int index = 0;
            for (int u = 0; u < uCount; u++)
            {
                Vector3 uPos = UnitPositions[u];
                for (int s = 0; s < sCount; s++)
                {
                    float distSq = (uPos - SlotPositions[s]).sqrMagnitude;
                    pairs[index++] = new AssignmentPair { unitIndex = u, slotIndex = s, distanceSqr = distSq };
                }
            }

            pairs.Sort();

            NativeArray<bool> unitAssigned = new NativeArray<bool>(uCount, Allocator.Temp);
            NativeArray<bool> slotAssigned = new NativeArray<bool>(sCount, Allocator.Temp);

            int assignedCount = 0;
            int targetAssignCount = uCount < sCount ? uCount : sCount;

            for (int i = 0; i < pairs.Length; i++)
            {
                int uIdx = pairs[i].unitIndex;
                int sIdx = pairs[i].slotIndex;

                if (!unitAssigned[uIdx] && !slotAssigned[sIdx])
                {
                    ResultAssignedUnitIndices[sIdx] = uIdx;
                    unitAssigned[uIdx] = true;
                    slotAssigned[sIdx] = true;
                    assignedCount++;

                    if (assignedCount >= targetAssignCount)
                        break;
                }
            }

            pairs.Dispose();
            unitAssigned.Dispose();
            slotAssigned.Dispose();
        }
    }
}