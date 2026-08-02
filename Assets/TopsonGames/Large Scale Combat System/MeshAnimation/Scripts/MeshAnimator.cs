using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TopsonGames.MeshAnimationSystem
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class LODLevel
        {
            public List<MeshAnimation> animations = new List<MeshAnimation>();
        }

        [Tooltip("The MeshFilter whose mesh is updated internally.")]
        public MeshFilter targetMeshFilter;

        [Tooltip("LOD levels (0 = highest quality)")]
        public List<LODLevel> lodLevels = new List<LODLevel>();

        [Header("Playback")]
        [Tooltip("Automatically plays a clip at startup.")]
        public bool playAutomatically = true;
        [Tooltip("Optional: Name of the clip to be started automatically (otherwise first clip in LOD0).")]
        public MeshAnimation autoPlayClip = null;

        [SerializeField, Tooltip("Dynamic playback speed for this specific unit. Use this for slow-motion or haste buffs.")]
        private float m_playbackSpeed = 1.0f;
        public float playbackSpeed
        {
            get => m_playbackSpeed;
            set { if (m_playbackSpeed != value) { m_playbackSpeed = value; needToSyncToJob = true; } }
        }

        [Header("Performance")]
        [SerializeField, Tooltip("If true, looping animations (Walk, Idle) are snapped to global buckets to maximize instancing performance. Non-looping animations (Attacks) play normally to preserve precise logic.")]
        private bool m_bucketLoopingAnimations = true;
        public bool bucketLoopingAnimations
        {
            get => m_bucketLoopingAnimations;
            set { if (m_bucketLoopingAnimations != value) { m_bucketLoopingAnimations = value; needToSyncToJob = true; } }
        }

        [Header("Debug / Compatibility")]
        [Tooltip("Additional multiplier on the baked bone positions. Should normally be 1.")]
        public float boneScale = 1f;

        // Runtime Properties
        private MeshRenderer meshRenderer;
        private List<Dictionary<string, MeshAnimation>> lodAnimationDicts;


        private MeshAnimation[] currentLODClips;

        public Material CharacterMaterial { get; private set; }
        public bool IsVisible { get; private set; }
        public Matrix4x4 LocalToWorldMatrix { get; set; }
        public Mesh CurrentMesh { get; private set; }

        private int currentBuckets = 1;
        public int CurrentBuckets
        {
            get => currentBuckets;
            set { if (currentBuckets != value) { currentBuckets = value; needToSyncToJob = true; } }
        }

        public bool IsPlaying => isPlaying;

        // Internal State
        private MeshAnimation currentClip;
        private int currentFrame;
        private int prevFrameForEvents;
        private bool isPlaying;
        private bool isFrozen;
        private float localAnimationTime;
        private int randomBucketSeed;

        public bool needToSyncToJob = true;

        private Dictionary<string, BoneAnimation> currentBoneAnims = new Dictionary<string, BoneAnimation>();
        private Dictionary<string, BoneAnimation> currentBoneAnimsNormalized = new Dictionary<string, BoneAnimation>();
        private HashSet<string> warnedBoneNames = new HashSet<string>();

        private static readonly Dictionary<string, string> normalizedBoneKeyCache = new Dictionary<string, string>();

        private bool isInitialized = false;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            isInitialized = true;

            if (targetMeshFilter == null) targetMeshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (meshRenderer != null)
                CharacterMaterial = meshRenderer.sharedMaterial;

            randomBucketSeed = Random.Range(0, 10000);

            lodAnimationDicts = new List<Dictionary<string, MeshAnimation>>();
            currentLODClips = new MeshAnimation[lodLevels.Count];

            foreach (var level in lodLevels)
            {
                var list = (level?.animations) ?? new List<MeshAnimation>();
                var dict = list.Where(a => a != null)
                               .GroupBy(a => a.clipName)
                               .ToDictionary(g => g.Key, g => g.First());
                lodAnimationDicts.Add(dict);
            }
        }

        void Start()
        {
            EnsureInitialized();

            if (meshRenderer != null)
                meshRenderer.enabled = false;

            IsVisible = true;

            if (MeshInstancingManager.Instance != null)
                MeshInstancingManager.Instance.RegisterAnimator(this);

            if (playAutomatically && lodAnimationDicts.Count > 0)
            {
                if (autoPlayClip == null && lodAnimationDicts[0].Count > 0)
                    autoPlayClip = lodAnimationDicts[0].Values.FirstOrDefault();
                if (autoPlayClip != null) Play(autoPlayClip);
            }
        }

        void OnDestroy()
        {
            if (MeshInstancingManager.Instance != null)
                MeshInstancingManager.Instance.UnregisterAnimator(this);
        }

        public void SetActiveForInstancing(bool isActive) { IsVisible = isActive; }
        void OnBecameVisible() { IsVisible = true; }
        void OnBecameInvisible() { IsVisible = false; }

        public Mesh GetMeshForLOD(int lodIndex)
        {
            EnsureInitialized();
            if (currentClip == null || lodIndex < 0 || lodIndex >= currentLODClips.Length) return null;

            var lodClip = currentLODClips[lodIndex];
            if (lodClip != null && lodClip.frames != null && lodClip.frames.Length > 0)
            {
                int i = Mathf.Clamp(currentFrame, 0, lodClip.frames.Length - 1);
                return lodClip.frames[i];
            }
            return null;
        }

        public bool GetBoneTransform(string boneName, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (GetBoneLocal(boneName, out Vector3 localPos, out Quaternion localRot))
            {
                position = transform.TransformPoint(localPos);
                rotation = transform.rotation * localRot;
                return true;
            }
            return false;
        }

        public bool GetBoneLocal(string boneName, out Vector3 localPos, out Quaternion localRot)
        {
            EnsureInitialized();
            localPos = Vector3.zero;
            localRot = Quaternion.identity;

            if (currentClip == null || currentBoneAnims == null) return false;

            if (!string.IsNullOrEmpty(boneName) && currentBoneAnims.TryGetValue(boneName, out var ba))
                return ReadBoneFrameLocal(ba, out localPos, out localRot);

            string norm = NormalizeBoneKey(boneName);
            if (currentBoneAnimsNormalized != null && currentBoneAnimsNormalized.TryGetValue(norm, out ba))
                return ReadBoneFrameLocal(ba, out localPos, out localRot);

            if (!string.IsNullOrEmpty(boneName) && !warnedBoneNames.Contains(boneName))
            {
                warnedBoneNames.Add(boneName);
            }
            return false;
        }

        private bool ReadBoneFrameLocal(BoneAnimation boneAnim, out Vector3 pos, out Quaternion rot)
        {
            int idx = Mathf.Clamp(currentFrame, 0, boneAnim.frames.Length - 1);
            var f = boneAnim.frames[idx];
            pos = f.position * Mathf.Max(1e-6f, boneScale);
            rot = f.rotation;
            return true;
        }

        public void Play(string clipName)
        {
            EnsureInitialized();
            if (lodAnimationDicts.Count == 0) return;
            if (lodAnimationDicts[0].TryGetValue(clipName, out MeshAnimation clip)) Play(clip);
        }

        public void Play(MeshAnimation clip)
        {
            EnsureInitialized();
            isFrozen = false;
            if (clip == null || (clip == currentClip && isPlaying)) return;

            currentClip = clip;
            localAnimationTime = 0f;
            currentFrame = 0;
            prevFrameForEvents = -1;
            isPlaying = true;

            for (int i = 0; i < lodLevels.Count; i++)
            {
                if (lodAnimationDicts.Count > i && lodAnimationDicts[i] != null)
                {
                    lodAnimationDicts[i].TryGetValue(clip.clipName, out currentLODClips[i]);
                }
                else
                {
                    currentLODClips[i] = null;
                }
            }

            currentBoneAnims.Clear();
            currentBoneAnimsNormalized.Clear();

            if (currentClip.boneAnimations != null)
            {
                foreach (var ba in currentClip.boneAnimations)
                {
                    if (ba == null || string.IsNullOrEmpty(ba.boneName)) continue;
                    currentBoneAnims[ba.boneName] = ba;
                    string norm = NormalizeBoneKey(ba.boneName);
                    if (!currentBoneAnimsNormalized.ContainsKey(norm)) currentBoneAnimsNormalized.Add(norm, ba);
                }
            }

            warnedBoneNames.Clear();
            if (currentClip.frames != null && currentClip.frames.Length > 0)
            {
                CurrentMesh = currentClip.frames[0];
                if (targetMeshFilter != null) targetMeshFilter.sharedMesh = CurrentMesh;
            }

            needToSyncToJob = true;
        }

        public MeshAnimation GetCurrentAnimation() => currentClip;

        public void Stop()
        {
            isPlaying = false;
            currentClip = null;
            needToSyncToJob = true;
        }

        public void ForceUpdateMatrix()
        {
            LocalToWorldMatrix = transform.localToWorldMatrix;
        }

        public void SnapToFrame(MeshAnimation clip, int frameIndex)
        {
            EnsureInitialized();
            if (clip == null) return;
            Play(clip);

            if (currentClip.frames != null && currentClip.frames.Length > 0)
            {
                currentFrame = Mathf.Clamp(frameIndex, 0, currentClip.frames.Length - 1);
                CurrentMesh = currentClip.frames[currentFrame];
                if (targetMeshFilter != null) targetMeshFilter.sharedMesh = CurrentMesh;
            }

            isFrozen = true;
            needToSyncToJob = true;
        }

        public void SyncToJob(int index, Unity.Collections.NativeArray<float> localTimes, Unity.Collections.NativeArray<int> playings,
            Unity.Collections.NativeArray<float> frameRates, Unity.Collections.NativeArray<int> totals, Unity.Collections.NativeArray<int> loopings,
            Unity.Collections.NativeArray<int> bucketLoopings, Unity.Collections.NativeArray<int> buckets, Unity.Collections.NativeArray<int> randomSeeds,
            Unity.Collections.NativeArray<int> currentFrames)
        {
            localTimes[index] = localAnimationTime;
            playings[index] = isPlaying && currentClip != null && currentClip.frames != null && currentClip.frames.Length > 0 && !isFrozen ? 1 : 0;

            if (currentClip != null)
            {
                frameRates[index] = currentClip.frameRate * currentClip.speedMultiplier * m_playbackSpeed;
                totals[index] = currentClip.frames != null ? currentClip.frames.Length : 0;
                loopings[index] = currentClip.isLooping ? 1 : 0;
            }
            else
            {
                frameRates[index] = 0;
                totals[index] = 0;
                loopings[index] = 0;
            }

            bucketLoopings[index] = m_bucketLoopingAnimations ? 1 : 0;
            buckets[index] = CurrentBuckets;
            randomSeeds[index] = randomBucketSeed;
            currentFrames[index] = currentFrame;

            needToSyncToJob = false;
        }

        public void ApplyJobState(int prevFrame, int curFrame, float newLocalTime, bool newIsPlaying, Matrix4x4 newMatrix)
        {
            LocalToWorldMatrix = newMatrix;

            if (needToSyncToJob || isFrozen) return;

            localAnimationTime = newLocalTime;
            isPlaying = newIsPlaying;

            if (currentClip != null && currentClip.frames != null && currentClip.frames.Length > 0)
            {
                curFrame = Mathf.Clamp(curFrame, 0, currentClip.frames.Length - 1);

                if (prevFrame != -1 && curFrame != prevFrame)
                {
                    if (currentClip.isLooping && curFrame < prevFrame)
                    {
                        CheckForEventsLooped(prevFrame, currentClip.frames.Length - 1);
                        CheckForEvents(-1, curFrame);
                    }
                    else
                    {
                        CheckForEvents(prevFrame, curFrame);
                    }
                }

                currentFrame = curFrame;
                prevFrameForEvents = currentFrame;

                CurrentMesh = currentClip.frames[currentFrame];
                if (targetMeshFilter != null) targetMeshFilter.sharedMesh = CurrentMesh;
            }
        }

        private void CheckForEvents(int prevFrame, int curFrame)
        {
            if (currentClip?.events == null) return;
            foreach (var ev in currentClip.events)
            {
                int eventFrame = (int)(ev.normalizedTime * (currentClip.frames.Length - 1));
                if (eventFrame > prevFrame && eventFrame <= curFrame)
                    gameObject.SendMessage(ev.functionName, ev.intValue, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void CheckForEventsLooped(int prevFrame, int lastFrame)
        {
            if (currentClip?.events == null) return;
            foreach (var ev in currentClip.events)
            {
                int eventFrame = (int)(ev.normalizedTime * (currentClip.frames.Length - 1));
                if (eventFrame > prevFrame && eventFrame <= lastFrame)
                    gameObject.SendMessage(ev.functionName, ev.intValue, SendMessageOptions.DontRequireReceiver);
            }
        }

        private static string NormalizeBoneKey(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            if (normalizedBoneKeyCache.TryGetValue(name, out string cached)) return cached;
            string key = name.Trim().Replace("\\", "/");
            if (key.Contains("/")) key = key.Substring(key.LastIndexOf('/') + 1);
            if (key.Contains(":")) key = key.Substring(key.LastIndexOf(':') + 1);
            normalizedBoneKeyCache[name] = key;
            return key;
        }
    }
}