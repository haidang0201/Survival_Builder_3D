using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundData
{
    public string id;
    public AudioClip clip;
}
public class SoundMgr : Singleton<SoundMgr>
{
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

    private void SetupAudioSources()
    {
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_Source");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }

        bgmSource.loop = true;
        sfxSource.loop = false;
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
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

    public void PlayBGM(string id)
    {
        if (!bgmDict.TryGetValue(id, out AudioClip clip))
        {
            Debug.LogWarning("Không tìm thấy BGM: " + id);
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlaySFX(string id)
    {
        if (!sfxDict.TryGetValue(id, out AudioClip clip))
        {
            Debug.LogWarning("Không tìm thấy SFX: " + id);
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }

    public void PlaySFXAtPosition(string id, Vector3 position)
    {
        if (!sfxDict.TryGetValue(id, out AudioClip clip))
        {
            Debug.LogWarning("Không tìm thấy SFX: " + id);
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume);
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

    private void ApplyVolume()
    {
        if (bgmSource != null)
            bgmSource.volume = bgmVolume * masterVolume;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume * masterVolume;
    }
}