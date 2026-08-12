namespace TopsonGames.MeshAnimationSystem
{
    using UnityEngine;
    using System.Collections.Generic;

    [System.Serializable]
    public class AnimationEvent
    {
        public string functionName = "OnAnimationEvent";
        [Tooltip("The normalized time (0=start, 1=end) at which the event is triggered.")]
        [Range(0f, 1f)] public float normalizedTime = 0f;
        public int intValue = 0;
    }

    [System.Serializable]
    public struct BoneFrameData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class BoneAnimation
    {
        public string boneName;
        public BoneFrameData[] frames;
    }

    [CreateAssetMenu(fileName = "New MeshAnimation", menuName = "Animation/Mesh Animation")]
    public class MeshAnimation : ScriptableObject
    {
        public string clipName;
        public Mesh[] frames;
        public float frameRate = 30.0f;

        [Tooltip("General speed multiplier for this specific clip. Useful to fix slow/fast baked animations.")]
        public float speedMultiplier = 1.0f;

        public bool isLooping = true;
        public List<AnimationEvent> events;
        public List<BoneAnimation> boneAnimations;

        /// <summary>
        /// Scale that was applied to the mesh vertices during baking (e.g. 100 for cm->m).
        /// Is multiplied to the saved bone positions at runtime.
        /// </summary>
        public float bakedScale = 1f;
    }
}