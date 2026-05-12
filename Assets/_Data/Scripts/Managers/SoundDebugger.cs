using UnityEngine;

/// <summary>
/// Script debug để test SoundMgr - gắn vào bất kỳ GameObject nào trong scene
/// </summary>
public class SoundDebugger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[SoundDebugger] Scene đã load.");
        
        // Kiểm tra AudioListener
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener == null)
        {
            Debug.LogError("[SoundDebugger] LỖI: Không có AudioListener trong scene! Âm thanh không thể nghe được.");
            Debug.LogWarning("[SoundDebugger] Thêm Camera vào scene (nó có sẵn AudioListener) hoặc tạo GameObject mới với AudioListener component.");
            return;
        }
        Debug.Log("[SoundDebugger] ✓ AudioListener found: " + listener.gameObject.name);

        // Kiểm tra SoundMgr
        SoundMgr soundMgr = SoundMgr.Ins;
        if (soundMgr == null)
        {
            Debug.LogError("[SoundDebugger] LỖI: SoundMgr singleton không tìm thấy!");
            return;
        }
        Debug.Log("[SoundDebugger] ✓ SoundMgr found");

        // Kiểm tra AudioSource
        if (soundMgr.bgmSource == null)
        {
            Debug.LogError("[SoundDebugger] LỖI: bgmSource null!");
            return;
        }
        Debug.Log("[SoundDebugger] ✓ bgmSource found: " + soundMgr.bgmSource.gameObject.name);

        if (soundMgr.sfxSource == null)
        {
            Debug.LogError("[SoundDebugger] LỖI: sfxSource null!");
            return;
        }
        Debug.Log("[SoundDebugger] ✓ sfxSource found: " + soundMgr.sfxSource.gameObject.name);

        // Subscribe to events để debug
        soundMgr.OnSoundPlayed += OnSoundPlayed;
        soundMgr.OnSoundMissing += OnSoundMissing;
        soundMgr.OnVolumeChanged += OnVolumeChanged;
        soundMgr.OnBGMStopped += OnBGMStopped;

        Debug.Log("[SoundDebugger] ✓ Subscribed to all events");
        Debug.Log("[SoundDebugger] Setup hoàn tất! Bây giờ test PlayBGM...");

        // Test: Phát nhạc tự động
        if (soundMgr.bgmClips.Count > 0)
        {
            string firstBGMId = soundMgr.bgmClips[0].id;
            Debug.Log("[SoundDebugger] Phát BGM: " + firstBGMId);
            soundMgr.PlayBGM(firstBGMId);
        }
        else
        {
            Debug.LogWarning("[SoundDebugger] Không có BGM clips nào! Thêm vài clips vào Inspector.");
        }
    }

    private void OnSoundPlayed(SoundChannel channel, string id, AudioClip clip)
    {
        Debug.Log($"[SoundDebugger] 🔊 Phát {channel}: {id} ({clip.name}, duration: {clip.length:F2}s)");
    }

    private void OnSoundMissing(SoundChannel channel, string id)
    {
        Debug.LogWarning($"[SoundDebugger] ⚠️ Clip không tìm thấy - {channel}: {id}");
    }

    private void OnVolumeChanged(float master, float bgm, float sfx)
    {
        Debug.Log($"[SoundDebugger] 🔈 Volume thay đổi - Master: {master:F2}, BGM: {bgm:F2}, SFX: {sfx:F2}");
    }

    private void OnBGMStopped()
    {
        Debug.Log("[SoundDebugger] ⏹️ BGM dừng");
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("SoundMgr Test", new GUIStyle(GUI.skin.label) { fontSize = 16 });

        if (GUILayout.Button("Play BGM", GUILayout.Height(30)))
        {
            if (SoundMgr.Ins.bgmClips.Count > 0)
            {
                SoundMgr.Ins.PlayBGM(SoundMgr.Ins.bgmClips[0].id);
            }
        }

        if (GUILayout.Button("Stop BGM", GUILayout.Height(30)))
        {
            SoundMgr.Ins.StopBGM();
        }

        if (GUILayout.Button("Play SFX", GUILayout.Height(30)))
        {
            if (SoundMgr.Ins.sfxClips.Count > 0)
            {
                SoundMgr.Ins.PlaySFX(SoundMgr.Ins.sfxClips[0].id);
            }
        }

        if (GUILayout.Button("Master Volume -0.1", GUILayout.Height(30)))
        {
            SoundMgr.Ins.SetMasterVolume(SoundMgr.Ins.GetEffectiveMasterVolume() - 0.1f);
        }

        if (GUILayout.Button("Master Volume +0.1", GUILayout.Height(30)))
        {
            SoundMgr.Ins.SetMasterVolume(SoundMgr.Ins.GetEffectiveMasterVolume() + 0.1f);
        }

        GUILayout.EndArea();
    }
}
