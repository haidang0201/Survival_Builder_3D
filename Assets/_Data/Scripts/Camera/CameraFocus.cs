using System.Collections;
using UnityEngine;

/// <summary>
/// Di chuyển camera mượt đến target.
/// Gắn vào Main Camera.
/// </summary>
public class CameraFocus : MonoBehaviour
{
    public static CameraFocus Instance;

    [Tooltip("Kéo Main Camera vào đây")]
    public Camera cam;

    [Tooltip("Tốc độ di chuyển camera (2 = mượt, 4 = nhanh)")]
    public float moveSpeed = 2f;

    private Coroutine moveRoutine;

    void Awake() { Instance = this; }

    public void MoveTo(Transform target)
    {
        if (target == null) return;
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(Transform target)
    {
        Vector3 start = cam.transform.position;
        Vector3 end = target.position + new Vector3(0, 10, -10);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            // Ease In-Out Quad
            float e = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
            cam.transform.position = Vector3.Lerp(start, end, e);
            yield return null;
        }

        cam.transform.position = end;
        Vector3 dir = target.position - cam.transform.position;
        if (dir != Vector3.zero)
            cam.transform.rotation = Quaternion.LookRotation(dir);

        moveRoutine = null;
    }
}