namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.Audio;


    [CreateAssetMenu(fileName = "AudioDefinitionSO", menuName = "TopsonGames/Unit Audio/AudioDefinitionSO")]
    public class AudioDefinitionSO : ScriptableObject
    {
        [Header("Sound Clips")]
        public AudioClip[] clips;

        [Header("Playback Settings")]
        [Tooltip("What is the maximum number of instances of this sound that can play simultaneously?")]
        public int maxConcurrentPlays = 3;

        [Tooltip("Minimum time in seconds before this sound can be played again.")]
        public float cooldown = 2f;

        [Header("Volume & Pitch Variation")]
        [Range(0f, 2f)]
        public float volume = 1f;
        [Range(0f, 1f)]
        public float randomVolumeVariance = 0.1f;

        [Range(-3f, 3f)]
        public float pitch = 1f;
        [Range(0f, 1f)]
        public float randomPitchVariance = 0.1f;
        [Range(1f, 200f)]
        public float playbackRadius = 100f;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}