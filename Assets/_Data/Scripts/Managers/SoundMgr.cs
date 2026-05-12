using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum SoundChannel
{
    BGM,
    SFX
}

[System.Serializable]
public class SoundData
{
    public string id;
    public AudioClip clip;
}

public class SoundMgr : Singleton<SoundMgr>
{
    [Header("Startup")]
    public bool playBGMOnStart = true;
    public string startupBGMId;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public List<SoundData> bgmClips = new List<SoundData>();
    public List<SoundData> sfxClips = new List<SoundData>();

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();

    public event Action<SoundChannel, string, AudioClip> OnSoundPlayed;
    public event Action<SoundChannel, string> OnSoundMissing;
    public event Action<float, float, float> OnVolumeChanged;
    public event Action OnBGMStopped;

    protected override void Awake()
    {
        MakeSingleton(false);

        if (SoundMgr.Ins != this)
        {
            enabled = false;
            return;
        }

        SetupAudioSources();
        CacheAudioClips();
        ApplyVolume();
    }

    private void Start()
    {
        PlayStartupBGM();
    }

    private void OnValidate()
    {
        CacheAudioClips();
        ApplyVolume();
    }

    private void SetupAudioSources()
    {
        bgmSource = EnsureAudioSource(bgmSource, "BGM_Source", true);
        sfxSource = EnsureAudioSource(sfxSource, "SFX_Source", false);
        ConfigureAudioSource(bgmSource, true);
        ConfigureAudioSource(sfxSource, false);
    }

    private AudioSource EnsureAudioSource(AudioSource source, string sourceName, bool shouldLoop)
    {
        if (source != null)
        {
            return source;
        }

        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource newSource = sourceObject.AddComponent<AudioSource>();
        newSource.loop = shouldLoop;
        newSource.playOnAwake = false;
        return newSource;
    }

    private void ConfigureAudioSource(AudioSource source, bool shouldLoop)
    {
        if (source == null)
        {
            return;
        }

        source.loop = shouldLoop;
        source.playOnAwake = false;
    }

    private void CacheAudioClips()
    {
        bgmDict.Clear();
        sfxDict.Clear();

        foreach (SoundData data in bgmClips)
        {
            if (data == null || string.IsNullOrEmpty(data.id) || data.clip == null) continue;
            bgmDict[data.id] = data.clip;
        }

        foreach (SoundData data in sfxClips)
        {
            if (data == null || string.IsNullOrEmpty(data.id) || data.clip == null) continue;
            sfxDict[data.id] = data.clip;
        }
    }

    public void RefreshAudioClips()
    {
        CacheAudioClips();
    }

    private void PlayStartupBGM()
    {
        if (!playBGMOnStart)
        {
            return;
        }

        if (!string.IsNullOrEmpty(startupBGMId))
        {
            PlayBGM(startupBGMId);
            return;
        }

        if (bgmClips.Count == 0)
        {
            Debug.LogWarning("SoundMgr: Không có BGM nào để tự phát khi Start.");
            return;
        }

        SoundData firstClip = bgmClips[0];
        if (firstClip == null || string.IsNullOrEmpty(firstClip.id))
        {
            Debug.LogWarning("SoundMgr: BGM đầu tiên không hợp lệ để tự phát khi Start.");
            return;
        }

        PlayBGM(firstClip.id);
    }

    public void PlayBGM(string id)
    {
        if (!TryGetClip(bgmDict, id, SoundChannel.BGM, out AudioClip clip))
        {
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
        OnSoundPlayed?.Invoke(SoundChannel.BGM, id, clip);
    }

    public void StopBGM()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
        OnBGMStopped?.Invoke();
    }

    public void PlaySFX(string id)
    {
        if (!TryGetClip(sfxDict, id, SoundChannel.SFX, out AudioClip clip))
        {
            return;
        }

        if (sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, sfxSource.volume);
        OnSoundPlayed?.Invoke(SoundChannel.SFX, id, clip);
    }

    public void PlaySFXAtPosition(string id, Vector3 position)
    {
        if (!TryGetClip(sfxDict, id, SoundChannel.SFX, out AudioClip clip))
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, GetEffectiveVolume(sfxVolume));
        OnSoundPlayed?.Invoke(SoundChannel.SFX, id, clip);
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public float GetEffectiveMasterVolume()
    {
        return masterVolume;
    }

    public float GetEffectiveBGMVolume()
    {
        return GetEffectiveVolume(bgmVolume);
    }

    public float GetEffectiveSFXVolume()
    {
        return GetEffectiveVolume(sfxVolume);
    }

    private bool TryGetClip(Dictionary<string, AudioClip> library, string id, SoundChannel channel, out AudioClip clip)
    {
        if (library != null && library.TryGetValue(id, out clip))
        {
            return true;
        }

        clip = null;
        Debug.LogWarning("Không tìm thấy " + channel + ": " + id);
        OnSoundMissing?.Invoke(channel, id);
        return false;
    }

    private float GetEffectiveVolume(float channelVolume)
    {
        return Mathf.Clamp01(channelVolume) * Mathf.Clamp01(masterVolume);
    }

    private void ApplyVolume()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = GetEffectiveVolume(bgmVolume);
        }

        if (sfxSource != null)
        {
            sfxSource.volume = GetEffectiveVolume(sfxVolume);
        }

        OnVolumeChanged?.Invoke(masterVolume, bgmVolume, sfxVolume);
    }
}