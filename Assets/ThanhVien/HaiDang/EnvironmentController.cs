using UnityEngine;

public class EnvironmentVisualController : MonoBehaviour
{
    [Header("Skybox Settings")]
    public Material skyDay;
    public Material skyNight;

    void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện từ DayNightManager
        DayNightManager.Ins.OnDayStart += SetDayMode;
        DayNightManager.Ins.OnNightStart += SetNightMode;

        // Thiết lập trạng thái ban đầu ngay khi game chạy
        if (DayNightManager.Ins.IsDay()) SetDayMode();
        else SetNightMode();
    }

    void OnDisable()
    {
        // Hủy đăng ký để tránh lỗi khi tắt Scene
        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnDayStart -= SetDayMode;
            DayNightManager.Ins.OnNightStart -= SetNightMode;
        }
    }

    private void SetDayMode()
    {
        RenderSettings.skybox = skyDay;
        DynamicGI.UpdateEnvironment(); // Cập nhật ánh sáng toàn cục
        Debug.Log("Đã đổi sang Skybox Ngày");
    }

    private void SetNightMode()
    {
        RenderSettings.skybox = skyNight;
        DynamicGI.UpdateEnvironment();
        Debug.Log("Đã đổi sang Skybox Đêm");
    }
}