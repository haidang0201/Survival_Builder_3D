using UnityEngine;

/// <summary>
/// Cục đá to "vô hạn": worker vẫn đập ra StonePickup để mang về như đá thường,
/// nhưng bản thân cục đá KHÔNG bao giờ biến mất (không SetActive(false), không bị hủy).
///
/// Kế thừa từ Stone để tận dụng nguyên vẹn: TryClaim/Release, đăng ký Registry,
/// DropStone (spawn StonePickup từ ObjectPool). WorkerFindStone.cs không cần sửa gì
/// vì nó tìm/đập theo type "Stone" và nhận diện StoneInfinite qua đa hình.
///
/// Chỉ override lại phần "hết máu thì làm gì":
/// - Stone gốc: hết máu -> DropStone() rồi ẩn/hủy object.
/// - StoneInfinite: hết máu -> DropStone() rồi HỒI MÁU về maxHealth, giữ nguyên active.
/// </summary>
public class StoneInfinite : Stone
{
    [Header("Infinite Stone Settings")]
    [Tooltip("Sau khi bị đập hết máu và ra đá, đợi bao lâu trước khi có thể bị đập tiếp (giây). 0 = đập liên tục ngay.")]
    public float regenDelay = 0f;

    private bool isRegenerating = false;
    private Collider stoneCollider;
    private Coroutine hitEffectRoutine;

    protected virtual void Start()
    {
        stoneCollider = GetComponent<Collider>();
        if (stoneCollider == null) stoneCollider = GetComponentInChildren<Collider>();

        // ClosestPoint chỉ hợp lệ với collider convex (Box/Sphere/Capsule, hoặc MeshCollider có Convex = true).
        if (stoneCollider is MeshCollider mc && !mc.convex)
        {
            Debug.LogWarning($"[StoneInfinite] '{name}' dùng MeshCollider không Convex — Collider.ClosestPoint sẽ lỗi. " +
                              $"Hãy bật Convex hoặc thêm 1 BoxCollider/CapsuleCollider bao quanh đá làm điểm mine.");
            stoneCollider = null; // fallback về transform.position, tránh crash
        }
    }

    /// <summary>
    /// Cục đá to thường có mesh lồi lõm và pivot nằm sâu bên trong,
    /// nên dùng điểm gần nhất trên bề mặt Collider thay vì tâm object,
    /// tránh việc worker cứ nhắm vào một chỗ lõm sâu bên trong khối đá.
    /// Yêu cầu: Collider phải convex (Box/Sphere/Capsule Collider, hoặc MeshCollider với Convex = true).
    /// </summary>
    public override Vector3 GetMinePoint(Vector3 fromPosition)
    {
        if (stoneCollider == null) return transform.position;
        return stoneCollider.ClosestPoint(fromPosition);
    }

    public override StonePickup[] TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            return DestroyStone();
        }

        // KHÔNG dùng ChippingEffect() của Stone gốc: hiệu ứng đó dựa vào healthPercent,
        // mà đá vô hạn luôn hồi máu về full nên healthPercent gần như không đổi.
        // Thay bằng 1 hiệu ứng "nảy nhẹ" độc lập, luôn hủy coroutine cũ trước khi chạy
        // cái mới để tránh nhiều coroutine chồng lên nhau gây giật/nhấp nháy khi đập liên tục.
        if (hitEffectRoutine != null) StopCoroutine(hitEffectRoutine);
        hitEffectRoutine = StartCoroutine(HitBounceEffect());
        return null;
    }

    private System.Collections.IEnumerator HitBounceEffect()
    {
        const float squashScale = 0.9f;
        const float duration = 0.15f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            // Nảy 1 nhịp mượt: lún xuống rồi trở lại nguyên kích thước, không giật cục
            float bounce = Mathf.Sin(progress * Mathf.PI);
            float scale = Mathf.Lerp(1f, squashScale, bounce);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
        hitEffectRoutine = null;
    }

    protected override StonePickup[] DestroyStone()
    {
        // Dừng hiệu ứng nảy đang chạy dở (nếu có) để tránh xung đột set scale
        if (hitEffectRoutine != null)
        {
            StopCoroutine(hitEffectRoutine);
            hitEffectRoutine = null;
        }
        transform.localScale = originalScale;

        // Vẫn ra đá y hệt logic gốc (DropStone dùng chung ObjectPool/StonePickup)
        StonePickup[] drops = DropStone();

        // Khác biệt duy nhất: KHÔNG SetActive(false), KHÔNG mất isOccupied vĩnh viễn theo kiểu bị hủy.
        isOccupied = false;

        if (regenDelay > 0f)
        {
            if (!isRegenerating) StartCoroutine(RegenAfterDelay());
        }
        else
        {
            currentHealth = maxHealth;
        }

        return drops;
    }

    private System.Collections.IEnumerator RegenAfterDelay()
    {
        isRegenerating = true;
        yield return new WaitForSeconds(regenDelay);
        currentHealth = maxHealth;
        isRegenerating = false;
    }
}