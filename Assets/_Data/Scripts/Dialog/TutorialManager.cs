using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Core Systems")]
    public NPCDialogue npc;
    public UIHighlightSystem highlight;

    [Header("Nút Tiếp tục trên NPCPanel")]
    public Button continueButton;

    [Header("Danh sách bước tutorial theo thứ tự")]
    public List<TutorialStepSO> steps;

    // ── Runtime ──────────────────────────────────────────
    private bool continuePressed = false;

    // ══════════════════════════════════════════════════════
    void Awake() { Instance = this; }

    void Start()
    {
        Debug.Log("<color=cyan>[TUTORIAL] ▶ Start()</color>");

        if (npc == null)
            Debug.LogError("[TUTORIAL] ✗ npc NULL!");
        if (highlight == null)
            Debug.LogError("[TUTORIAL] ✗ highlight NULL!");
        if (continueButton == null)
            Debug.LogError("[TUTORIAL] ✗ continueButton NULL!");

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);

        StartCoroutine(RunTutorial());
    }

    // ══════════════════════════════════════════════════════
    //  MAIN FLOW
    // ══════════════════════════════════════════════════════
    IEnumerator RunTutorial()
    {
        // Chờ 1 frame để scene load xong
        yield return null;

        Debug.Log($"<color=cyan>[TUTORIAL] Bắt đầu — {steps.Count} bước</color>");

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] == null)
            {
                Debug.LogWarning($"[TUTORIAL] Step {i} là null — bỏ qua");
                continue;
            }
            yield return RunStep(steps[i], i + 1);
        }

        // Xong tất cả
        Debug.Log("<color=cyan>[TUTORIAL] ✓ Hoàn thành!</color>");
        highlight.ClearAll();
        npc.Hide();
    }

    // ══════════════════════════════════════════════════════
    //  MỖI BƯỚC
    // ══════════════════════════════════════════════════════
    IEnumerator RunStep(TutorialStepSO step, int stepNum)
    {
        Debug.Log($"<color=yellow>[STEP {stepNum}] Bắt đầu — icon='{step.iconName}'</color>");

        // ── 1. KHOÁ nút Tiếp tục, hiện dialog ──────────
        continuePressed = false;          // reset SỚM — tránh sót true từ step trước
        SetContinueButton(false);
        npc.Show(step.dialogContent);

        // ── 2. Chờ text chạy xong ────────────────────────
        Debug.Log($"<color=yellow>[STEP {stepNum}] Chờ text xong...</color>");
        yield return new WaitUntil(() => npc.IsTypingDone());
        Debug.Log($"<color=yellow>[STEP {stepNum}] ✓ Text xong</color>");

        // ── 3. Nếu có icon → highlight MINH HỌA, sáng cho tới khi bấm Tiếp tục ──
        // FIX theo yêu cầu mới: icon KHÔNG tự tắt sau 1 khoảng thời gian cố định nữa.
        // Icon sẽ sáng liên tục, mở nút Tiếp tục NGAY, và chỉ tắt sáng khi người chơi
        // thực sự bấm Tiếp tục (xử lý ở bước 5, sau WaitForContinue()).
        bool hasIcon = !string.IsNullOrEmpty(step.iconName);
        if (hasIcon)
        {
            GameObject icon = FindIconByName(step.iconName);

            if (icon == null)
            {
                Debug.LogError(
                    $"[STEP {stepNum}] ✗ Không tìm thấy icon '{step.iconName}'!\n" +
                    "→ Kiểm tra tên trong TutorialStepSO hoặc kéo vào UIHighlightSystem");
                // Không có icon để highlight — bỏ qua, vẫn cho qua bước bình thường
            }
            else
            {
                Debug.Log($"<color=yellow>[STEP {stepNum}] Highlight {icon.name} — sáng cho tới khi bấm Tiếp tục</color>");
                highlight.HighlightUI(icon);
            }
        }

        // ── 4. Mở nút Tiếp tục NGAY (không chờ thêm) ────
        Debug.Log($"<color=cyan>[STEP {stepNum}] Mở nút Tiếp tục</color>");
        SetContinueButton(true);

        // ── 5. Chờ bấm Tiếp tục, RỒI MỚI tắt highlight ──
        yield return WaitForContinue();
        Debug.Log($"<color=lime>[STEP {stepNum}] ✓ Tiếp tục — sang step tiếp</color>");

        if (hasIcon)
        {
            highlight.ClearAll(); // FIX: chỉ tắt sáng khi đã bấm Tiếp tục, không tắt sớm
        }

        // ── 6. Mở nhiệm vụ nếu có ───────────────────────
        if (!string.IsNullOrEmpty(step.questDescription))
        {
            Debug.Log($"<color=lime>[QUEST] Mở: {step.questDescription}</color>");
            // TODO: QuestManager.Instance.OpenQuest(step.questDescription);
        }

        yield return new WaitForSeconds(0.2f);
    }

    // ══════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════

    /// <summary>Tìm icon theo tên — ưu tiên field trong UIHighlightSystem</summary>
    GameObject FindIconByName(string name)
    {
        // Tìm qua field RectTransform trong UIHighlightSystem
        var rt = highlight.GetIconRT(name);
        if (rt != null) return rt.gameObject;

        // Fallback: GameObject.Find
        var go = GameObject.Find(name);
        if (go != null) return go;

        return null;
    }

    /// <summary>Khoá/mở nút Tiếp tục + đổi màu</summary>
    void SetContinueButton(bool interactable)
    {
        if (continueButton == null) return;

        continueButton.interactable = interactable;

        var colors = continueButton.colors;
        colors.normalColor = interactable
            ? new Color(0.2f, 0.65f, 0.2f)   // xanh lá = mở
            : new Color(0.35f, 0.35f, 0.35f); // xám = khoá
        continueButton.colors = colors;

        Debug.Log($"<color=cyan>[TUTORIAL] Nút Tiếp tục: {(interactable ? "✓ MỞ" : "✗ KHOÁ")}</color>");
    }

    /// <summary>Chờ bấm nút Tiếp tục</summary>
    IEnumerator WaitForContinue()
    {
        continuePressed = false;
        yield return new WaitUntil(() => continuePressed);
    }

    // ── Callback ─────────────────────────────────────────
    void OnContinuePressed()
    {
        if (continueButton != null && !continueButton.interactable) return;
        Debug.Log("<color=lime>[TUTORIAL] ✓ Tiếp tục được bấm</color>");
        SetContinueButton(false);   // khoá ngay — tránh bấm 2 lần / sót sang step sau
        continuePressed = true;
    }

    void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
    }
}