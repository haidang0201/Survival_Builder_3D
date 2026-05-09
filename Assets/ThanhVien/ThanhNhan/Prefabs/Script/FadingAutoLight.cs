using UnityEngine;

public class FadingAutoLight : MonoBehaviour
{
    [Header("Cài đặt Đèn (Cường độ)")]
    [Tooltip("Kéo thả Point Light của cây đèn vào đây")]
    public Light lightToFade;

    [Tooltip("Kéo thả Directional Light (Mặt trời) của scene vào đây")]
    public Transform sunTransform;

    [Header("Cài đặt Ngưỡng Sáng Dần")]
    [Tooltip("Cường độ tối đa khi đêm tối hoàn toàn.")]
    public float maxIntensity = 10f; // Chỉnh giá trị này tùy ý (theo ví dụ trong hình)

    [Range(-1f, 1f)]
    [Tooltip("Ngưỡng bắt đầu chạng vạng. Đèn bắt đầu sáng mờ (giá trị âm). Ví dụ: -0.1.")]
    public float twilightThreshold = -0.1f;

    [Range(-1f, 1f)]
    [Tooltip("Ngưỡng đêm tối hoàn toàn. Đèn sáng tối đa (giá trị âm thấp hơn). Ví dụ: -0.4.")]
    public float nightThreshold = -0.4f;
    

    // Biến để theo dõi trạng thái hiện tại (để tối ưu và hiển thị)
    [SerializeField]
    private float currentFadeFactor = 0f; // Hiển thị tỷ lệ sáng (0.0 đến 1.0)
    [SerializeField ]
    private float currentIntensity = 0f;  // Hiển thị cường độ hiện tại

    void Start()
    {
        // Tự động tìm Point Light nếu chưa kéo thả (tìm trong các object con)
        if (lightToFade == null)
        {
            lightToFade = GetComponentInChildren<Light>();
        }

        // Tự động tìm Directional Light nếu chưa kéo thả
        if (sunTransform == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sunTransform = l.transform;
                    break;
                }
            }
        }

        // Thiết lập giá trị ban đầu (tránh đèn bật bất ngờ)
        if (lightToFade != null)
        {
            lightToFade.intensity = 0f;
            lightToFade.enabled = false;
        }
    }

    void Update()
    {
        if (sunTransform != null && lightToFade != null)
        {
            // Kiểm tra hướng mặt trời so với mặt đất (Dot < 0 là ban đêm)
            float sunDirection = Vector3.Dot(sunTransform.forward, Vector3.down);

            // LOGIC XỬ LÝ SÁNG DẦN
            
            // 1. Kiểm tra ban ngày hoặc chạng vạng sớm (tắt đèn)
            if (sunDirection > twilightThreshold)
            {
                if (lightToFade.enabled) lightToFade.enabled = false;
                lightToFade.intensity = 0f;
                currentFadeFactor = 0f;
                currentIntensity = 0f;
            }
            // 2. Kiểm tra đêm tối hoàn toàn (sáng tối đa)
            else if (sunDirection <= nightThreshold)
            {
                if (!lightToFade.enabled) lightToFade.enabled = true;
                if (lightToFade.intensity != maxIntensity)
                {
                    lightToFade.intensity = maxIntensity;
                }
                currentFadeFactor = 1f;
                currentIntensity = maxIntensity;
            }
            // 3. Giai đoạn chạng vạng (đèn sáng dần)
            else 
            {
                if (!lightToFade.enabled) lightToFade.enabled = true;

                // Tính toán tỷ lệ chạng vạng (từ 0.0 tại twilightThreshold đến 1.0 tại nightThreshold)
                // Chúng ta đảo ngược các giá trị vì twilightThreshold (-0.1) lớn hơn nightThreshold (-0.4)
                float normalizedFactor = Mathf.InverseLerp(twilightThreshold, nightThreshold, sunDirection);

                // Cường độ tăng từ 0 đến maxIntensity khi đêm về
                float targetIntensity = Mathf.Lerp(0f, maxIntensity, normalizedFactor);
                lightToFade.intensity = targetIntensity;

                // Cập nhật giá trị hiển thị trong Inspector
                currentFadeFactor = normalizedFactor;
                currentIntensity = targetIntensity;
            }
        }
    }
}