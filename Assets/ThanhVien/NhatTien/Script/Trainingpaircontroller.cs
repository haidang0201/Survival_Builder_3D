using UnityEngine;

/// <summary>
/// Gắn vào một GameObject cha (rỗng) chứa hai lính tập luyện.
/// Animator chỉ có 1 state Attack loop liên tục — không cần trigger/parameter.
/// Script chỉ lo: đặt vị trí đối mặt nhau + lệch pha animation để trông tự nhiên.
///
/// Cách dùng:
///   1. Tạo GameObject rỗng "TrainingPair".
///   2. Kéo prefab lính A và B vào làm con.
///   3. Gắn script này vào "TrainingPair".
///   4. Kéo soldierA / soldierB vào Inspector.
/// </summary>
public class TrainingPairController : MonoBehaviour
{
    [Header("References — kéo hai SoldierTraining vào đây")]
    public SoldierTraining soldierA;
    public SoldierTraining soldierB;

    [Header("Positioning")]
    [Tooltip("Khoảng cách giữa hai lính (m)")]
    public float faceDistance = 1.4f;

    [Header("Phase Offset")]
    [Tooltip(
        "Lệch pha animation của lính B so với A (0 = cùng lúc, 0.5 = ngược pha hoàn toàn).\n" +
        "Dùng ~0.5 để A chém lúc B đang kéo kiếm về — trông như đấu thật hơn.")]
    [Range(0f, 1f)]
    public float soldierBCycleOffset = 0.5f;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        SetupPositions();
        ApplyPhaseOffset();
    }

    /// <summary>Đặt hai lính đối xứng quanh GameObject cha, quay mặt vào nhau.</summary>
    void SetupPositions()
    {
        if (soldierA == null || soldierB == null) return;

        Vector3 center = transform.position;
        float half = faceDistance * 0.5f;

        soldierA.transform.position = center + transform.forward * half;
        soldierB.transform.position = center - transform.forward * half;

        soldierA.transform.LookAt(soldierB.transform.position);
        soldierB.transform.LookAt(soldierA.transform.position);
    }

    /// <summary>
    /// Set Cycle Offset trên state Attack của lính B để animation lệch pha.
    /// Cycle Offset trong Animator = normalizedTime bắt đầu của clip (0–1).
    /// </summary>
    void ApplyPhaseOffset()
    {
        if (soldierB == null) return;

        Animator anim = soldierB.animator != null
            ? soldierB.animator
            : soldierB.GetComponent<Animator>();

        if (anim == null) return;

        // Dùng AnimatorStateInfo để play ngay từ offset — cần Play() với offset
        // (SetFloat Cycle Offset chỉ hoạt động nếu bật Parameter trong state)
        // Cách đơn giản & chắc chắn nhất: Play state từ normalizedTime = offset
        anim.Play(soldierB.attackStateName, 0, soldierBCycleOffset);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;
        float half = faceDistance * 0.5f;

        Vector3 posA = center + transform.forward * half;
        Vector3 posB = center - transform.forward * half;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(posA, 0.08f);
        Gizmos.DrawSphere(posB, 0.08f);
        Gizmos.DrawLine(posA, posB);

        UnityEditor.Handles.Label(center + Vector3.up * 0.3f,
            $"Distance: {faceDistance:F2} m  |  B offset: {soldierBCycleOffset:F2}");
    }
#endif
}