using UnityEngine;
using System.Collections;

public class EnvironmentVisualController : MonoBehaviour
{
    [Header("Skybox Settings")]
    public Material skyDay;
    public Material skyNight;
    public float transitionDuration = 5.0f; // Thời gian chuyển đổi (giây)

    [Header("Light Settings")]
    public Light sunLight;
    public Color dayLightColor = Color.white;
    public Color nightLightColor = new Color(0.15f, 0.15f, 0.4f);
    public float dayIntensity = 1.2f;
    public float nightIntensity = 0.2f;

    private Coroutine transitionCoroutine;

    // SỬ DỤNG START THAY VÌ ONENABLE ĐỂ TRÁNH LỖI KHỞI TẠO SINGLETON TỪ MANAGER
    private void Start()
    {
        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnDayStart += SetDayMode;
            DayNightManager.Ins.OnNightStart += SetNightMode;

            // Thiết lập màu sắc tức thì khi vừa vào game
            if (DayNightManager.Ins.IsDay()) SetDayModeInstant();
            else SetNightModeInstant();

            Debug.Log("[EnvironmentVisual] Đã kết nối thành công với DayNightManager!");
        }
        else
        {
            Debug.LogError("[EnvironmentVisual] BÁO LỖI: Không tìm thấy DayNightManager trong Scene!");
        }
    }

    // DÙNG ONDESTROY SẼ AN TOÀN HƠN ONDISABLE
    private void OnDestroy()
    {
        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnDayStart -= SetDayMode;
            DayNightManager.Ins.OnNightStart -= SetNightMode;
        }
    }

    // ================= LOGIC CHUYỂN ĐỔI MƯỢT MÀ =================

    private void SetDayMode() => StartTransition(skyDay, dayLightColor, dayIntensity, 1.3f);
    private void SetNightMode() => StartTransition(skyNight, nightLightColor, nightIntensity, 0.3f);

    private void StartTransition(Material sky, Color color, float intensity, float exposure)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionRoutine(sky, color, intensity, exposure));
    }

    private IEnumerator TransitionRoutine(Material targetSky, Color targetColor, float targetIntensity, float targetExposure)
    {
        float elapsedTime = 0f;
        Color startColor = sunLight.color;
        float startIntensity = sunLight.intensity;
        float startExposure = RenderSettings.skybox.GetFloat("_Exposure");

        RenderSettings.skybox = targetSky;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / transitionDuration);

            sunLight.color = Color.Lerp(startColor, targetColor, t);
            sunLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(startExposure, targetExposure, t));

            DynamicGI.UpdateEnvironment();
            yield return null;
        }

        sunLight.color = targetColor;
        sunLight.intensity = targetIntensity;
        RenderSettings.skybox.SetFloat("_Exposure", targetExposure);
    }

    // Thiết lập tức thì khi khởi động game
    private void SetDayModeInstant() { RenderSettings.skybox = skyDay; sunLight.color = dayLightColor; sunLight.intensity = dayIntensity; }
    private void SetNightModeInstant() { RenderSettings.skybox = skyNight; sunLight.color = nightLightColor; sunLight.intensity = nightIntensity; }
}