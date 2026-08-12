namespace TopsonGames
{
    using System.Collections.Generic;
    using UnityEngine;

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Source Pool")]
        [SerializeField] private int initialPoolSize = 20;
        private List<AudioSource> audioSourcePool;

        private Dictionary<AudioDefinitionSO, int> currentlyPlayingCounts = new Dictionary<AudioDefinitionSO, int>();
        private Dictionary<AudioDefinitionSO, float> lastPlayedTimes = new Dictionary<AudioDefinitionSO, float>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                InitializeAudioSourcePool();
            }
        }

        private void InitializeAudioSourcePool()
        {
            audioSourcePool = new List<AudioSource>();
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreatePooledAudioSource();
            }
        }

        private AudioSource CreatePooledAudioSource()
        {
            GameObject sourceGO = new GameObject($"PooledAudioSource_{audioSourcePool.Count}");
            sourceGO.transform.SetParent(this.transform);
            AudioSource source = sourceGO.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSourcePool.Add(source);
            return source;
        }

        private AudioSource GetAvailableAudioSource()
        {
            foreach (var source in audioSourcePool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }
            return CreatePooledAudioSource();
        }

        public void PlaySound(AudioDefinitionSO soundDef, Vector3 position)
        {
            if (soundDef == null) return;
            if (lastPlayedTimes.ContainsKey(soundDef) && Time.time < lastPlayedTimes[soundDef] + soundDef.cooldown)
            {
                return;
            }

            int currentPlays = currentlyPlayingCounts.GetValueOrDefault(soundDef, 0);
            if (currentPlays >= soundDef.maxConcurrentPlays)
            {
                return;
            }

            AudioSource source = GetAvailableAudioSource();
            if (source == null) return;

            source.transform.position = position;
            source.spatialBlend = 0.7f;                     // 1 = full 3D
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;                    
            source.maxDistance = soundDef.playbackRadius;                     
            source.dopplerLevel = 0f;            
            source.clip = soundDef.GetRandomClip();
            source.volume = soundDef.volume + Random.Range(-soundDef.randomVolumeVariance, soundDef.randomVolumeVariance);
            source.pitch = soundDef.pitch + Random.Range(-soundDef.randomPitchVariance, soundDef.randomPitchVariance);

            source.Play();

            lastPlayedTimes[soundDef] = Time.time;
            currentlyPlayingCounts[soundDef] = currentPlays + 1;

            StartCoroutine(DecrementPlayCountAfter(soundDef, source.clip.length));
        }

        private System.Collections.IEnumerator DecrementPlayCountAfter(AudioDefinitionSO soundDef, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (currentlyPlayingCounts.ContainsKey(soundDef))
            {
                currentlyPlayingCounts[soundDef]--;
            }
        }
    }
}