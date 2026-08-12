using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("Day / Night Music")]
    [SerializeField] private AudioClip dayMusic;
    [SerializeField] private AudioClip nightMusic;
    [SerializeField] private AudioClip roosterSound;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float effectVolume = 1f;

    [Header("Options")]
    [SerializeField] private bool playRoosterWhenDayStarts = true;
    [SerializeField] private bool playCurrentMusicOnStart = true;

    private AudioSource musicSource;
    private AudioSource effectSource;
    private DayNightManager dayNightManager;
    private bool isBound;

    private void Awake()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

        effectSource = gameObject.AddComponent<AudioSource>();
        effectSource.playOnAwake = false;
        effectSource.loop = false;
        effectSource.spatialBlend = 0f;
        effectSource.volume = effectVolume;
    }

    private void OnEnable()
    {
        TryBindDayNightManager();
    }

    private void Start()
    {
        TryBindDayNightManager();

        if (playCurrentMusicOnStart && dayNightManager != null)
        {
            ApplyMusicForCurrentMode(false);
        }
    }

    private void OnDisable()
    {
        UnbindDayNightManager();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBindDayNightManager();
        }
    }

    private void TryBindDayNightManager()
    {
        if (isBound)
        {
            return;
        }

        dayNightManager = DayNightManager.Ins;
        if (dayNightManager == null)
        {
            return;
        }

        dayNightManager.OnDayStart += HandleDayStart;
        dayNightManager.OnNightStart += HandleNightStart;
        isBound = true;
    }

    private void UnbindDayNightManager()
    {
        if (!isBound || dayNightManager == null)
        {
            return;
        }

        dayNightManager.OnDayStart -= HandleDayStart;
        dayNightManager.OnNightStart -= HandleNightStart;
        isBound = false;
    }

    private void HandleDayStart()
    {
        if (playRoosterWhenDayStarts && roosterSound != null)
        {
            effectSource.PlayOneShot(roosterSound, effectVolume);
        }

        PlayMusic(dayMusic);
    }

    private void HandleNightStart()
    {
        PlayMusic(nightMusic);
    }

    private void ApplyMusicForCurrentMode(bool restartIfSameClip)
    {
        if (dayNightManager.IsDay())
        {
            PlayMusic(dayMusic, restartIfSameClip);
        }
        else
        {
            PlayMusic(nightMusic, restartIfSameClip);
        }
    }

    private void PlayMusic(AudioClip clip, bool restartIfSameClip = false)
    {
        if (clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        musicSource.volume = musicVolume;

        if (!restartIfSameClip && musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }
}
