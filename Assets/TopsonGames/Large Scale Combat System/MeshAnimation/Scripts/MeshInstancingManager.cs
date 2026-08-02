using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Jobs;

namespace TopsonGames.MeshAnimationSystem
{
    public class MeshInstancingManager : MonoBehaviour
    {
        public static MeshInstancingManager Instance { get; private set; }

        [Tooltip("The global LOD distances. Sort in ascending order! E.g. 20, 50, 100.")]
        public float[] lodDistances;

        [Tooltip("How many animation buckets to use per LOD. Sort to match lodDistances! E.g. LOD0=4 buckets, LOD1=2 buckets, LOD2=1 bucket.")]
        public int[] bucketsPerLOD = new int[] { 4, 2, 1 };

        private readonly struct InstanceKey : IEquatable<InstanceKey>
        {
            public readonly Mesh Mesh;
            public readonly Material Material;

            public InstanceKey(Mesh mesh, Material material) { Mesh = mesh; Material = material; }
            public bool Equals(InstanceKey other) { return Mesh == other.Mesh && Material == other.Material; }
            public override int GetHashCode() { return HashCode.Combine(Mesh, Material); }
        }

        // --- Mesh Animator Tracking ---
        private List<MeshAnimator> activeAnimators = new List<MeshAnimator>();
        private HashSet<MeshAnimator> pendingAdds = new HashSet<MeshAnimator>();
        private HashSet<MeshAnimator> pendingRemoves = new HashSet<MeshAnimator>();

        // --- Attachment Point Tracking ---
        private List<AttachmentPoint> activeAttachments = new List<AttachmentPoint>();
        private HashSet<AttachmentPoint> pendingAttachmentAdds = new HashSet<AttachmentPoint>();
        private HashSet<AttachmentPoint> pendingAttachmentRemoves = new HashSet<AttachmentPoint>();

        // --- Instancing Data ---
        private Dictionary<InstanceKey, List<Matrix4x4>> meshInstanceGroups = new Dictionary<InstanceKey, List<Matrix4x4>>();
        private List<List<Matrix4x4>> matrixPool = new List<List<Matrix4x4>>();
        private Matrix4x4[] matrices = new Matrix4x4[1023];

        // --- Job Data (Animators) ---
        private TransformAccessArray animatorTransforms;
        private NativeArray<Vector3> animatorPositions;
        private NativeArray<Matrix4x4> animatorMatrices;

        private NativeArray<float> animLocalTimes;
        private NativeArray<int> animIsPlayings;
        private NativeArray<float> animFrameRates;
        private NativeArray<int> animTotalFrames;
        private NativeArray<int> animIsLoopings;
        private NativeArray<int> animBucketLoopings;
        private NativeArray<int> animCurrentBuckets;
        private NativeArray<int> animRandomSeeds;
        private NativeArray<int> animCurrentFrames;
        private NativeArray<int> animPrevFrames;

        private NativeArray<int> outputLODIndices;
        private NativeArray<float> lodDistancesNative;
        private JobHandle animatorJobsHandle;
        private bool areAnimatorJobsScheduled = false;

        // --- Job Data (Attachments) ---
        private TransformAccessArray attachmentTransforms;
        private NativeArray<Vector3> attachmentTargetPositions;
        private NativeArray<Quaternion> attachmentTargetRotations;
        private JobHandle attachmentJobHandle;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            if (lodDistances != null && lodDistances.Length > 0)
            {
                lodDistancesNative = new NativeArray<float>(lodDistances, Allocator.Persistent);
            }
        }

        public void RegisterAnimator(MeshAnimator animator) { pendingRemoves.Remove(animator); pendingAdds.Add(animator); }
        public void UnregisterAnimator(MeshAnimator animator) { pendingAdds.Remove(animator); pendingRemoves.Add(animator); }
        public void RegisterAttachment(AttachmentPoint ap) { pendingAttachmentRemoves.Remove(ap); pendingAttachmentAdds.Add(ap); }
        public void UnregisterAttachment(AttachmentPoint ap) { pendingAttachmentAdds.Remove(ap); pendingAttachmentRemoves.Add(ap); }

        private void ReallocateAnimatorArrays(int capacity)
        {
            DisposeAnimatorArrays();
            if (capacity <= 0) return;

            Transform[] transforms = new Transform[capacity];
            for (int i = 0; i < capacity; i++) transforms[i] = activeAnimators[i].transform;
            animatorTransforms = new TransformAccessArray(transforms);

            animatorPositions = new NativeArray<Vector3>(capacity, Allocator.Persistent);
            animatorMatrices = new NativeArray<Matrix4x4>(capacity, Allocator.Persistent);

            animLocalTimes = new NativeArray<float>(capacity, Allocator.Persistent);
            animIsPlayings = new NativeArray<int>(capacity, Allocator.Persistent);
            animFrameRates = new NativeArray<float>(capacity, Allocator.Persistent);
            animTotalFrames = new NativeArray<int>(capacity, Allocator.Persistent);
            animIsLoopings = new NativeArray<int>(capacity, Allocator.Persistent);
            animBucketLoopings = new NativeArray<int>(capacity, Allocator.Persistent);
            animCurrentBuckets = new NativeArray<int>(capacity, Allocator.Persistent);
            animRandomSeeds = new NativeArray<int>(capacity, Allocator.Persistent);
            animCurrentFrames = new NativeArray<int>(capacity, Allocator.Persistent);
            animPrevFrames = new NativeArray<int>(capacity, Allocator.Persistent);
            outputLODIndices = new NativeArray<int>(capacity, Allocator.Persistent);
        }

        void Update()
        {
            if (areAnimatorJobsScheduled) animatorJobsHandle.Complete();

            // 1. Process Animator Adds/Removes
            bool animatorsChanged = false;
            if (pendingRemoves.Count > 0)
            {
                foreach (var a in pendingRemoves) activeAnimators.Remove(a);
                pendingRemoves.Clear();
                animatorsChanged = true;
            }
            if (pendingAdds.Count > 0)
            {
                foreach (var a in pendingAdds) if (!activeAnimators.Contains(a)) activeAnimators.Add(a);
                pendingAdds.Clear();
                animatorsChanged = true;
            }

            if (animatorsChanged || (activeAnimators.Count > 0 && !animatorTransforms.isCreated))
            {
                ReallocateAnimatorArrays(activeAnimators.Count);
                for (int i = 0; i < activeAnimators.Count; i++) activeAnimators[i].needToSyncToJob = true;
            }

            // 2. Process Attachment Adds/Removes
            bool attachmentsChanged = false;
            if (pendingAttachmentRemoves.Count > 0)
            {
                foreach (var a in pendingAttachmentRemoves) activeAttachments.Remove(a);
                pendingAttachmentRemoves.Clear();
                attachmentsChanged = true;
            }
            if (pendingAttachmentAdds.Count > 0)
            {
                foreach (var a in pendingAttachmentAdds) if (!activeAttachments.Contains(a)) activeAttachments.Add(a);
                pendingAttachmentAdds.Clear();
                attachmentsChanged = true;
            }

            if (attachmentsChanged || (activeAttachments.Count > 0 && !attachmentTransforms.isCreated))
            {
                if (attachmentTransforms.isCreated) attachmentTransforms.Dispose();
                if (attachmentTargetPositions.IsCreated) attachmentTargetPositions.Dispose();
                if (attachmentTargetRotations.IsCreated) attachmentTargetRotations.Dispose();

                if (activeAttachments.Count > 0)
                {
                    Transform[] transforms = new Transform[activeAttachments.Count];
                    for (int i = 0; i < activeAttachments.Count; i++) transforms[i] = activeAttachments[i].transform;

                    attachmentTransforms = new TransformAccessArray(transforms);
                    attachmentTargetPositions = new NativeArray<Vector3>(activeAttachments.Count, Allocator.Persistent);
                    attachmentTargetRotations = new NativeArray<Quaternion>(activeAttachments.Count, Allocator.Persistent);
                }
            }

            if (activeAnimators.Count == 0 || !animatorTransforms.isCreated) return;

            // 3. Sync C# data to Job Arrays only if needed
            for (int i = 0; i < activeAnimators.Count; i++)
            {
                if (activeAnimators[i].needToSyncToJob)
                {
                    activeAnimators[i].SyncToJob(i, animLocalTimes, animIsPlayings, animFrameRates, animTotalFrames,
                        animIsLoopings, animBucketLoopings, animCurrentBuckets, animRandomSeeds, animCurrentFrames);
                }
            }

            // 4. Schedule Jobs!
            var transformJob = new AnimatorTransformJob { positions = animatorPositions, matrices = animatorMatrices };
            JobHandle transformHandle = transformJob.Schedule(animatorTransforms);

            var lodJob = new LODUpdateJob
            {
                cameraPosition = Camera.main.transform.position,
                lodDistances = lodDistancesNative,
                animatorPositions = animatorPositions,
                outputLODIndices = outputLODIndices
            };
            JobHandle lodHandle = lodJob.Schedule(activeAnimators.Count, 32, transformHandle);

            var tickJob = new AnimationTickJob
            {
                deltaTime = Time.deltaTime,
                time = Time.time,
                localAnimationTimes = animLocalTimes,
                isPlayings = animIsPlayings,
                effectiveFrameRates = animFrameRates,
                totalFrames = animTotalFrames,
                isLoopings = animIsLoopings,
                bucketLoopingAnimations = animBucketLoopings,
                currentBuckets = animCurrentBuckets,
                randomBucketSeeds = animRandomSeeds,
                currentFrames = animCurrentFrames,
                prevFrames = animPrevFrames
            };
            JobHandle tickHandle = tickJob.Schedule(activeAnimators.Count, 32);

            animatorJobsHandle = JobHandle.CombineDependencies(lodHandle, tickHandle);
            areAnimatorJobsScheduled = true;
        }

        void LateUpdate()
        {
            if (!areAnimatorJobsScheduled) return;


            animatorJobsHandle.Complete();
            areAnimatorJobsScheduled = false;

            for (int i = 0; i < activeAnimators.Count; i++)
            {
                activeAnimators[i].ApplyJobState(
                    animPrevFrames[i],
                    animCurrentFrames[i],
                    animLocalTimes[i],
                    animIsPlayings[i] == 1,
                    animatorMatrices[i]
                );
            }

            if (activeAttachments.Count > 0 && attachmentTransforms.isCreated)
            {
                for (int i = 0; i < activeAttachments.Count; i++)
                {
                    var ap = activeAttachments[i];
                    if (ap != null && ap.targetAnimator != null &&
                        ap.targetAnimator.GetBoneLocal(ap.boneName, out Vector3 bonePos, out Quaternion boneRot))
                    {
                        attachmentTargetPositions[i] = bonePos + (boneRot * ap.positionOffset);
                        attachmentTargetRotations[i] = boneRot * Quaternion.Euler(ap.rotationOffset);
                    }
                    else
                    {
                        attachmentTargetPositions[i] = ap != null ? ap.transform.localPosition : Vector3.zero;
                        attachmentTargetRotations[i] = ap != null ? ap.transform.localRotation : Quaternion.identity;
                    }
                }

                if (activeAttachments.Count >= 32)
                {
                    var attachmentJob = new UpdateAttachmentsJob
                    {
                        targetLocalPositions = attachmentTargetPositions,
                        targetLocalRotations = attachmentTargetRotations
                    };
                    attachmentJobHandle = attachmentJob.Schedule(attachmentTransforms);
                }
                else
                {
                    for (int i = 0; i < activeAttachments.Count; i++)
                    {
                        if (activeAttachments[i] != null)
                        {
                            activeAttachments[i].transform.localPosition = attachmentTargetPositions[i];
                            activeAttachments[i].transform.localRotation = attachmentTargetRotations[i];
                        }
                    }
                }
            }


            GroupAndDrawInstances();

            if (activeAttachments.Count >= 32 && attachmentTransforms.isCreated)
            {
                attachmentJobHandle.Complete();
            }
        }

        private void GroupAndDrawInstances()
        {
            meshInstanceGroups.Clear();
            int poolIndex = 0;

            if (!outputLODIndices.IsCreated) return;

            for (int i = 0; i < activeAnimators.Count; i++)
            {
                var animator = activeAnimators[i];
                if (animator == null) continue;

                int lodIndex = outputLODIndices[i];
                int currentBuckets = (lodIndex < bucketsPerLOD.Length) ? bucketsPerLOD[lodIndex] : 1;
                animator.CurrentBuckets = currentBuckets;

                if (!animator.IsVisible || lodIndex >= lodDistances.Length) continue;

                Mesh currentMesh = animator.GetMeshForLOD(lodIndex);
                if (currentMesh == null || animator.CharacterMaterial == null) continue;

                var key = new InstanceKey(currentMesh, animator.CharacterMaterial);
                if (!meshInstanceGroups.TryGetValue(key, out List<Matrix4x4> matricesList))
                {
                    if (poolIndex >= matrixPool.Count) matrixPool.Add(new List<Matrix4x4>(500));
                    matricesList = matrixPool[poolIndex++];
                    matricesList.Clear();
                    meshInstanceGroups[key] = matricesList;
                }
                matricesList.Add(animator.LocalToWorldMatrix);
            }

            foreach (var group in meshInstanceGroups)
            {
                var key = group.Key;
                var matricesList = group.Value;
                int count = matricesList.Count;
                int renderedCount = 0;
                while (renderedCount < count)
                {
                    int batchSize = Mathf.Min(count - renderedCount, 1023);
                    matricesList.CopyTo(renderedCount, matrices, 0, batchSize);
                    Graphics.DrawMeshInstanced(key.Mesh, 0, key.Material, matrices, batchSize);
                    renderedCount += batchSize;
                }
            }
        }

        private void DisposeAnimatorArrays()
        {
            if (animatorTransforms.isCreated) animatorTransforms.Dispose();
            if (animatorPositions.IsCreated) animatorPositions.Dispose();
            if (animatorMatrices.IsCreated) animatorMatrices.Dispose();
            if (animLocalTimes.IsCreated) animLocalTimes.Dispose();
            if (animIsPlayings.IsCreated) animIsPlayings.Dispose();
            if (animFrameRates.IsCreated) animFrameRates.Dispose();
            if (animTotalFrames.IsCreated) animTotalFrames.Dispose();
            if (animIsLoopings.IsCreated) animIsLoopings.Dispose();
            if (animBucketLoopings.IsCreated) animBucketLoopings.Dispose();
            if (animCurrentBuckets.IsCreated) animCurrentBuckets.Dispose();
            if (animRandomSeeds.IsCreated) animRandomSeeds.Dispose();
            if (animCurrentFrames.IsCreated) animCurrentFrames.Dispose();
            if (animPrevFrames.IsCreated) animPrevFrames.Dispose();
            if (outputLODIndices.IsCreated) outputLODIndices.Dispose();
        }

        void OnDestroy()
        {
            if (areAnimatorJobsScheduled) animatorJobsHandle.Complete();
            if (lodDistancesNative.IsCreated) lodDistancesNative.Dispose();
            DisposeAnimatorArrays();

            if (attachmentTransforms.isCreated) attachmentTransforms.Dispose();
            if (attachmentTargetPositions.IsCreated) attachmentTargetPositions.Dispose();
            if (attachmentTargetRotations.IsCreated) attachmentTargetRotations.Dispose();
        }
    }

    // --- BURST JOBS ---

    [BurstCompile]
    public struct AnimatorTransformJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<Vector3> positions;
        [WriteOnly] public NativeArray<Matrix4x4> matrices;

        public void Execute(int index, TransformAccess transform)
        {
            positions[index] = transform.position;
            matrices[index] = transform.localToWorldMatrix;
        }
    }

    [BurstCompile]
    public struct LODUpdateJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> lodDistances;
        [ReadOnly] public NativeArray<Vector3> animatorPositions;
        [ReadOnly] public Vector3 cameraPosition;
        [WriteOnly] public NativeArray<int> outputLODIndices;

        public void Execute(int index)
        {
            float d = Vector3.Distance(animatorPositions[index], cameraPosition);
            int lod = -1;
            for (int i = 0; i < lodDistances.Length; i++) { if (d < lodDistances[i]) { lod = i; break; } }
            if (lod == -1) lod = lodDistances.Length;
            outputLODIndices[index] = lod;
        }
    }

    [BurstCompile]
    public struct AnimationTickJob : IJobParallelFor
    {
        public float deltaTime;
        public float time;

        public NativeArray<float> localAnimationTimes;
        public NativeArray<int> isPlayings;

        [ReadOnly] public NativeArray<float> effectiveFrameRates;
        [ReadOnly] public NativeArray<int> totalFrames;
        [ReadOnly] public NativeArray<int> isLoopings;
        [ReadOnly] public NativeArray<int> bucketLoopingAnimations;
        [ReadOnly] public NativeArray<int> currentBuckets;
        [ReadOnly] public NativeArray<int> randomBucketSeeds;

        public NativeArray<int> currentFrames;
        [WriteOnly] public NativeArray<int> prevFrames;

        public void Execute(int i)
        {
            if (isPlayings[i] == 0 || totalFrames[i] == 0)
            {
                prevFrames[i] = currentFrames[i];
                return;
            }

            int total = totalFrames[i];
            float effRate = effectiveFrameRates[i];
            int prevFrame = currentFrames[i];
            int calculatedNewFrame = 0;

            if (isLoopings[i] == 1 && bucketLoopingAnimations[i] == 1)
            {
                int globalFrameCounter = (int)(time * effRate);
                int activeBuckets = currentBuckets[i] < 1 ? 1 : currentBuckets[i];
                int myBucketIndex = randomBucketSeeds[i] % activeBuckets;
                int bucketOffset = (total / activeBuckets) * myBucketIndex;

                int calc = (globalFrameCounter + bucketOffset) % total;
                if (calc < 0) calc += total;
                calculatedNewFrame = calc;
            }
            else
            {
                float localTime = localAnimationTimes[i] + deltaTime * effRate;
                calculatedNewFrame = (int)localTime;

                if (calculatedNewFrame >= total)
                {
                    if (isLoopings[i] == 1)
                    {
                        localTime -= total;
                        calculatedNewFrame = (int)localTime;
                    }
                    else
                    {
                        calculatedNewFrame = total - 1;
                        isPlayings[i] = 0;
                    }
                }
                else if (calculatedNewFrame < 0)
                {
                    if (isLoopings[i] == 1)
                    {
                        localTime += total;
                        calculatedNewFrame = (int)localTime;
                    }
                    else
                    {
                        calculatedNewFrame = 0;
                        isPlayings[i] = 0;
                    }
                }
                localAnimationTimes[i] = localTime;
            }

            prevFrames[i] = prevFrame;
            currentFrames[i] = calculatedNewFrame;
        }
    }

    [BurstCompile]
    public struct UpdateAttachmentsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<Vector3> targetLocalPositions;
        [ReadOnly] public NativeArray<Quaternion> targetLocalRotations;

        public void Execute(int index, TransformAccess transform)
        {
            transform.localPosition = targetLocalPositions[index];
            transform.localRotation = targetLocalRotations[index];
        }
    }
}