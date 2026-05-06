using UnityEngine;

public class AutoLight : MonoBehaviour
{
    [Header("Cài đặt Đèn")]
    [Tooltip("Kéo thả Point Light của cây đèn vào đây")]
    public Light streetLight; 

    [Tooltip("Kéo thả Directional Light (Mặt trời) của scene vào đây")]
    public Transform sun;     

    [Header("Cài đặt Thời gian")]
    [Tooltip("Ngưỡng bật đèn. 0 là lúc mặt trời nằm ngang chân trời. Tăng lên một chút (ví dụ 0.1) để đèn bật sớm hơn lúc chạng vạng.")]
    public float twilightThreshold = 0.1f;

    void Start()
    {
        // Tự động tìm Point Light nếu bạn quên kéo thả (tìm trong các object con)
        if (streetLight == null)
        {
            streetLight = GetComponentInChildren<Light>();
        }

        // Tự động tìm Directional Light nếu bạn quên kéo thả
        if (sun == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sun = l.transform;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (sun != null && streetLight != null)
        {
            // Kiểm tra hướng chiếu của mặt trời so với mặt đất (hướng đi xuống)
            // Dot > 0: Mặt trời đang ở trên cao chiếu xuống (Ban ngày)
            // Dot < 0: Mặt trời đang ở dưới chân trời hắt lên (Ban đêm)
            float sunDirection = Vector3.Dot(sun.forward, Vector3.down);

            // Nếu mặt trời thấp hơn ngưỡng chạng vạng
            if (sunDirection < twilightThreshold)
            {
                // Ban đêm -> Bật đèn (chỉ bật nếu nó đang tắt để tối ưu hiệu suất)
                if (!streetLight.enabled)
                    streetLight.enabled = true;
            }
            else
            {
                // Ban ngày -> Tắt đèn
                if (streetLight.enabled)
                    streetLight.enabled = false;
            }
        }
    }
}