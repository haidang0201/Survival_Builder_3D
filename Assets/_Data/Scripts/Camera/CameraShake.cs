using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    public Transform cam;
    public float intensity = 0.2f;
    public float duration = 0.4f;

    Vector3 startPos;

    void Awake()
    {
        Instance = this;
        startPos = cam.localPosition;
    }

    public void Shake()
    {
        StartCoroutine(DoShake());
    }

    IEnumerator DoShake()
    {
        float t = duration;

        while (t > 0)
        {
            cam.localPosition = startPos + Random.insideUnitSphere * intensity;
            t -= Time.deltaTime;
            yield return null;
        }

        cam.localPosition = startPos;
    }
}