using UnityEngine;

public class DayNightTest : MonoBehaviour
{
    private DayNightManager manager;
    private float logTimer = 0f; // Đếm log mỗi giây

    void Start()
    {
        // Lấy instance từ Singleton
        manager = DayNightManager.Ins;

        // Subscribe event
        manager.OnDayStart += () => Debug.Log("[Test] Day Start Event triggered");
        manager.OnNightStart += () => Debug.Log("[Test] Night Start Event triggered");

        Debug.Log($"[Test] DayNightManager started. Current Mode: {manager.CurrentMode}");
    }

    void Update()
    {
        // Giảm log spam: chỉ log mỗi 1 giây
        logTimer -= Time.deltaTime;
        if (logTimer <= 0f)
        {
            logTimer = 1f; // reset 1s
            if (manager.IsDay())
                Debug.Log($"[Test] DayTime running... Timer: {manager.DayDuration:F1}s");
            else
                Debug.Log($"[Test] NightTime running... Timer: {manager.NightDuration:F1}s");
        }
    }
}