using UnityEngine;

[RequireComponent(typeof(Light))]
public class FireFlicker : MonoBehaviour
{
    [Header("Cấu hình nhấp nháy")]
    [Tooltip("Độ sáng tối thiểu khi lửa bập bùng xuống")]
    public float minIntensity = 1.0f;
    
    [Tooltip("Độ sáng tối đa khi lửa bùng lên")]
    public float maxIntensity = 3.5f;
    
    [Tooltip("Tốc độ gió thổi (Càng cao nháy càng nhanh)")]
    public float flickerSpeed = 3.0f;

    private Light fireLight;
    private float randomSeed;

    void Start()
    {
        // Lấy component Point Light
        fireLight = GetComponent<Light>();
        
        // Tạo một số ngẫu nhiên để nếu có nhiều đống lửa, chúng không nháy cùng nhịp với nhau
        randomSeed = Random.Range(0.0f, 100.0f); 
    }

    void Update()
    {
        // Dùng Perlin Noise để nội suy mượt mà giữa min và max
        float noise = Mathf.PerlinNoise(randomSeed, Time.time * flickerSpeed);
        fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}