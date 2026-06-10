using UnityEngine;
using System.Collections;

public class LightningEffect : MonoBehaviour
{
    private Light myLight;
    public float minTime = 5f;
    public float maxTime = 15f;

    void Start()
    {
        myLight = GetComponent<Light>();
        StartCoroutine(Flashing());
    }

    IEnumerator Flashing()
    {
        while (true)
        {
            // Chờ một khoảng thời gian ngẫu nhiên giữa các lần chớp
            yield return new WaitForSeconds(Random.Range(minTime, maxTime));
            
            // Bật sáng rực lên
            myLight.intensity = 1.5f;
            yield return new WaitForSeconds(0.1f);
            
            // Tắt/Giảm độ sáng về lại mức trời mưa u ám
            myLight.intensity = 0.2f;
            yield return new WaitForSeconds(0.05f);
            
            // Nháy thêm một phát phụ cho chân thực
            myLight.intensity = 1.0f;
            yield return new WaitForSeconds(0.05f);
            myLight.intensity = 0.2f;
        }
    }
}