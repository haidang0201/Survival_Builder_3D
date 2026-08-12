using System.Collections;
using UnityEngine;

/// <summary>
/// Xử lý toàn bộ hiệu ứng hình ảnh của cây:
/// - Rung khi bị chặt
/// - Đổ khi bị đốn xong
/// Gắn vào cùng GameObject với Tree.cs
/// </summary>
public class TreeVisual : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeAngle    = 8f;   // độ nghiêng tối đa khi rung (degree)
    public float shakeSpeed    = 12f;  // tốc độ rung
    public float shakeDuration = 0.4f; // thời gian rung (giây)

    [Header("Fall Settings")]
    public float fallAngle    = 90f;  // độ nghiêng khi đổ
    public float fallDuration = 0.8f; // thời gian đổ (giây)
    public float fallDelay    = 0.1f; // delay trước khi đổ (giây)

    [Header("Fall Direction")]
    [Tooltip("Hướng cây đổ. Nếu để (0,0,0) sẽ random mỗi lần.")]
    public Vector3 fallDirection = Vector3.zero;

    // ===== INTERNAL =====
    private Coroutine shakeCoroutine;
    private Coroutine fallCoroutine;
    private Quaternion originalRotation;
    private bool isFalling = false;

    void Awake()
    {
        originalRotation = transform.localRotation;
    }

    void OnEnable()
    {
        // Reset lại khi cây được tái sử dụng từ pool
        StopAllCoroutines();
        shakeCoroutine = null; // BUG FIX: xóa reference cũ sau StopAllCoroutines
        fallCoroutine  = null; // BUG FIX: xóa reference cũ sau StopAllCoroutines

        transform.localRotation = originalRotation;
        isFalling = false;
    }

    /// <summary>
    /// Reset về trạng thái ban đầu (gọi khi cây hồi sinh mà không qua SetActive).
    /// </summary>
    public void ResetState()
    {
        StopAllCoroutines();
        shakeCoroutine = null;
        fallCoroutine  = null;
        transform.localRotation = originalRotation;
        isFalling = false;
    }

    // ===== PUBLIC API =====

    /// <summary>Gọi khi cây bị chặt 1 lần (còn sống).</summary>
    public void PlayShake()
    {
        if (isFalling) return;

        // Nếu đang rung → restart
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    /// <summary>Gọi khi cây bị đốn xong (health <= 0).</summary>
    public void PlayFall(System.Action onFallComplete = null)
    {
        if (isFalling) return;
        isFalling = true;

        // Dừng shake và reset về thẳng trước khi đổ
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        // BUG FIX: reset rotation về original trước khi đổ
        // vì shake có thể đang ở giữa chừng, làm FallRoutine bắt đầu từ góc lệch
        transform.localRotation = originalRotation;

        fallCoroutine = StartCoroutine(FallRoutine(onFallComplete));
    }

    // ===== COROUTINES =====

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / shakeDuration;
            float fade     = 1f - progress; // biên độ giảm dần về cuối

            float angle = Mathf.Sin(elapsed * shakeSpeed) * shakeAngle * fade;

            transform.localRotation = originalRotation *
                Quaternion.AngleAxis(angle, Vector3.forward);

            yield return null;
        }

        transform.localRotation = originalRotation;
        shakeCoroutine = null;
    }

    IEnumerator FallRoutine(System.Action onFallComplete)
    {
        yield return new WaitForSeconds(fallDelay);

        Vector3    axis      = GetFallAxis();
        Quaternion startRot  = transform.localRotation; // đã là originalRotation nhờ fix bên trên
        Quaternion targetRot = originalRotation * Quaternion.AngleAxis(fallAngle, axis);

        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;

            // EaseIn: chậm lúc đầu, nhanh dần (giống cây bị trọng lực kéo)
            float t      = Mathf.Clamp01(elapsed / fallDuration);
            float smooth = t * t;

            transform.localRotation = Quaternion.Slerp(startRot, targetRot, smooth);

            yield return null;
        }

        transform.localRotation = targetRot;
        fallCoroutine           = null;

        Debug.Log($"[TreeVisual] '{name}' đã đổ xong.");

        onFallComplete?.Invoke();
    }

    Vector3 GetFallAxis()
    {
        if (fallDirection != Vector3.zero)
            return fallDirection.normalized;

        // Random hướng đổ trên mặt phẳng XZ
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }
}