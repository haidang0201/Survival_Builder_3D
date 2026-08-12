using UnityEngine;

/// <summary>
/// WorkerHarvest.cs — Script DUY NHẤT cần cho worker kho tài nguyên (gỗ/lúa/đá).
/// Worker chỉ đứng tại chỗ và phát animation khai thác lặp.
/// Không có pathfinding, không stamina, không chạy trốn.
/// </summary>
public class WorkerHarvest : MonoBehaviour
{
    public enum HarvestType { Wood, Rice, Stone }

    [Header("Loại Worker")]
    public HarvestType harvestType = HarvestType.Wood;

    [Header("Animation")]
    [Tooltip("Tự tìm Animator nếu để trống.")]
    public Animator animator;

    [Header("Tên Trigger trong Animator")]
    [Tooltip("Trigger dùng cho Worker Gỗ")]
    public string chopTrigger    = "Chop";
    [Tooltip("Trigger dùng cho Worker Lúa")]
    public string harvestTrigger = "Harvest";
    [Tooltip("Trigger dùng cho Worker Đá")]
    public string mineTrigger    = "Mine";

    [Header("Looping")]
    [Tooltip("Nếu bật, tự động phát lại trigger sau mỗi X giây (dùng khi animation không loop).")]
    public bool  forceRetrigger    = true;
    public float retriggerInterval = 1.5f;
    private float retriggerTimer   = 0f;

    [Header("Tên State trong Animator (Dự phòng)")]
    [Tooltip("Tên State sẽ dùng Play() trực tiếp nếu Trigger không hoạt động. Để trống nếu không cần.")]
    public string directStateName  = "";
    [Tooltip("Layer index chứa State trên (Base Layer = 0, Sub Layer = 1...)")]
    public int    directLayerIndex = 0;

    // ── Private ──────────────────────────────────────────────────────────────
    private string ActiveTrigger
    {
        get
        {
            switch (harvestType)
            {
                case HarvestType.Wood:  return chopTrigger;
                case HarvestType.Rice:  return harvestTrigger;
                case HarvestType.Stone: return mineTrigger;
                default:                return harvestTrigger;
            }
        }
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"[WorkerHarvest] '{name}' không có Animator!");
            return;
        }

        // Đợi 2 frame để Animator controller hoàn toàn sẵn sàng
        StartCoroutine(PlayAfterDelay());
    }

    private System.Collections.IEnumerator PlayAfterDelay()
    {
        yield return null;
        yield return null;
        PlayAnimation();
    }

    void Update()
    {
        if (!forceRetrigger || animator == null) return;

        retriggerTimer += Time.deltaTime;
        if (retriggerTimer >= retriggerInterval)
        {
            retriggerTimer = 0f;
            PlayAnimation();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Phát animation khai thác tương ứng.
    /// </summary>
    public void PlayAnimation()
    {
        if (animator == null) return;

        // Ưu tiên Play theo tên State trực tiếp nếu người dùng điền directStateName
        if (!string.IsNullOrEmpty(directStateName))
        {
            animator.Play(directStateName, directLayerIndex, 0f);
            return;
        }

        string trigger = ActiveTrigger;

        if (string.IsNullOrEmpty(trigger))
        {
            Debug.LogWarning($"[WorkerHarvest] '{name}' tên trigger rỗng!");
            return;
        }

        animator.ResetTrigger(trigger);
        animator.SetTrigger(trigger);
        Debug.Log($"[WorkerHarvest] '{name}' phát trigger: {trigger}");
    }

    /// <summary>
    /// Thay đổi loại worker và cập nhật animation ngay lập tức.
    /// </summary>
    public void SetHarvestType(HarvestType newType)
    {
        harvestType = newType;
        PlayAnimation();
    }
}
