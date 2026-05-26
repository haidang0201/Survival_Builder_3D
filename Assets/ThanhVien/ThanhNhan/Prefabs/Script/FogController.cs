using UnityEngine;
// Gọi namespace của Buto
using OccaSoftware.Buto.Runtime; 

[ExecuteAlways]
public class FogController : MonoBehaviour
{
    public Light fireLight; 
    public GameObject fogMaskObject; // Kéo object Fog_Clear_Area vào đây
    
    [Header("Settings")]
    public float maxClearRadius = 25f; 
    public float fireIntensityMultiplier = 0.5f; 

    void Update()
    {
        if (fogMaskObject != null && fireLight != null)
        {
            // Tính bán kính theo cường độ sáng của đèn
            float currentRadius = fireLight.intensity * fireIntensityMultiplier;
            currentRadius = Mathf.Clamp(currentRadius, 2f, maxClearRadius); 
            
            // Ép Scale của khối khoét sương mù thay đổi theo ngọn lửa
            fogMaskObject.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);
        }
    }
}