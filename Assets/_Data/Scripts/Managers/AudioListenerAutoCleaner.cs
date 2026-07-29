using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * AudioListenerAutoCleaner.cs
 * Folder: Scripts/Managers/
 * Dự án: KHẨN HOANG
 *
 * NHIỆM VỤ: Tự động phát hiện và gỡ bỏ các AudioListener dư thừa trong Scene khi khởi chạy game
 *           hoặc khi load Scene mới, giúp ngăn chặn triệt để cảnh báo:
 *           "There are 2 audio listeners in the scene..."
 */

public static class AudioListenerAutoCleaner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCleanOnInit()
    {
        CleanDuplicateListeners();

        // Đăng ký sự kiện mỗi khi load Scene mới
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanDuplicateListeners();
    }

    public static void CleanDuplicateListeners()
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length <= 1) return;

        // Ưu tiên giữ lại AudioListener nằm trên Main Camera
        AudioListener keepListener = null;
        if (Camera.main != null)
        {
            keepListener = Camera.main.GetComponent<AudioListener>();
        }

        if (keepListener == null)
        {
            keepListener = listeners[0];
        }

        int removedCount = 0;
        foreach (AudioListener listener in listeners)
        {
            if (listener == keepListener) continue;

            // Xoá hoặc disable component AudioListener dư thừa
            Object.Destroy(listener);
            removedCount++;
            Debug.Log($"[AudioListenerCleaner] 🧹 Đã tự động xoá AudioListener dư thừa trên: {listener.gameObject.name}");
        }

        if (removedCount > 0)
        {
            Debug.Log($"[AudioListenerCleaner] ✅ Đã xử lý xong: Giữ lại AudioListener trên '{keepListener.gameObject.name}', xoá {removedCount} AudioListener dư thừa.");
        }
    }
}
