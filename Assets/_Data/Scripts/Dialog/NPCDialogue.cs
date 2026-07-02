// NPCDialogue.cs
// Hộp thoại NPC dùng trong Tutorial — có typing sound theo từng ký tự hiển thị,
// hoàn toàn giống cơ chế StoryUIController (maxVisibleCharacters + AudioSettings.dspTime).
// Code cũ TutorialManager.cs KHÔNG cần sửa gì — chỉ gán thêm AudioSource trong Inspector.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel chứa toàn bộ hộp thoại NPC. Show/Hide tự động.")]
    public GameObject dialoguePanel;

    [Tooltip("Text hiển thị nội dung thoại (TextMeshPro).")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("(Tuỳ chọn) Text hiển thị tên NPC.")]
    public TextMeshProUGUI speakerNameText;

    [Tooltip("(Tuỳ chọn) Tên NPC hiển thị cố định (có thể để trống).")]
    public string speakerName = "Phó Lý";

    [Header("Typing Effect")]
    [Tooltip("Khoảng thời gian giữa mỗi ký tự (giây). Nên để 0.03 – 0.05.")]
    public float typingSpeed = 0.04f;

    [Header("Typing Audio")]
    [Tooltip("AudioSource dùng để phát âm thanh gõ chữ. Gán Audio Source có clip 'tick' ngắn.")]
    [SerializeField] private AudioSource typingAudioSource;

    // Ký tự im lặng — không phát âm thanh (giống StoryUIController)
    private static readonly HashSet<char> silentChars = new HashSet<char> { ' ', '\n', '\t' };

    // Trạng thái nội bộ
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool waitingForContinue = false;
    public GameObject dialogueRoot;
    public System.Action onContinueEvent;

    // =========================================================
    //  PUBLIC API — dùng từ TutorialManager (không thay đổi)
    // =========================================================

    /// <summary>
    /// Hiện hộp thoại ngay (không chờ người chơi bấm tiếp tục).
    /// Dùng cho các dòng "npc.Show(...)" dạng đặt text rồi tiếp tục luồng.
    /// </summary>
    public void Show(string message)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerNameText != null) speakerNameText.text = speakerName;

        StopTyping();
        typingCoroutine = StartCoroutine(TypeText(message));
    }
    public void HideNow()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// Hiện hộp thoại VÀ chờ người chơi bấm Continue (hoặc bấm bỏ qua khi đang gõ).
    /// Dùng trong TutorialManager: yield return npc.ShowAndWait("...");
    /// </summary>
    public IEnumerator ShowAndWait(string message)
    {
        Show(message);

        // Chờ đến khi gõ xong
        yield return new WaitUntil(() => !isTyping);

        // Chờ người chơi bấm Continue
        waitingForContinue = true;
        yield return new WaitUntil(() => !waitingForContinue);
    }

    /// <summary>
    /// Ẩn hộp thoại. Gọi từ TutorialManager khi muốn dọn UI.
    /// </summary>
    public void Hide()
    {
        StopTyping();
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // =========================================================
    //  NÚT "TIẾP TỤC" — gán onClick vào Button trong Inspector
    // =========================================================

    /// <summary>
    /// Gắn hàm này vào Button "Tiếp Tục" trong Inspector (onClick).
    /// - Nếu đang gõ chữ: hiện full text ngay + tắt âm thanh (skip typing).
    /// - Nếu đã gõ xong và đang chờ: thoát vòng chờ ShowAndWait.
    /// </summary>
    public void OnContinueClicked()
    {
        if (isTyping)
        {
            StopTyping();
            if (dialogueText != null)
                dialogueText.maxVisibleCharacters = int.MaxValue;

            isTyping = false;
        }

        if (waitingForContinue)
        {
            waitingForContinue = false;

            // 🔥 AUTO CLOSE DIALOG KHI BẤM TIẾP TỤC
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }
    }
    // =========================================================
    //  CORE: TYPING COROUTINE (copy logic từ StoryUIController)
    // =========================================================

    private IEnumerator TypeText(string content)
    {
        isTyping = true;

        // Gán toàn bộ text (kể cả rich-text tag) rồi dùng maxVisibleCharacters tăng dần.
        // TMP tự bỏ tag khỏi đếm — âm thanh luôn khớp 1-1 với ký tự thật sự hiển thị.
        if (dialogueText == null) { isTyping = false; yield break; }

        dialogueText.text = content;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        string parsedText = dialogueText.GetParsedText(); // chuỗi thuần, không tag
        int visibleCount = parsedText.Length;

        // Dùng AudioSettings.dspTime để phát âm thanh chính xác theo audio thread,
        // tránh lệch tích luỹ do frame rate — giống hệt StoryUIController.
        double nextDspTime = AudioSettings.dspTime;

        for (int i = 0; i < visibleCount; i++)
        {
            char c = parsedText[i];
            dialogueText.maxVisibleCharacters = i + 1;

            if (!silentChars.Contains(c)
                && typingAudioSource != null
                && typingAudioSource.clip != null)
            {
                typingAudioSource.pitch = Random.Range(0.95f, 1.05f); // tránh nghe đơn điệu
                typingAudioSource.Stop();
                typingAudioSource.PlayScheduled(nextDspTime);
            }

            yield return new WaitForSeconds(typingSpeed);
            nextDspTime = AudioSettings.dspTime; // chốt mốc audio thực tế cho ký tự tiếp theo
        }

        isTyping = false;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (typingAudioSource != null)
            typingAudioSource.Stop(); // tắt âm thanh đang phát dở khi bị ngắt
        isTyping = false;
    }

    // =========================================================
    //  EDITOR WARNING (giống StoryUIController)
    // =========================================================
#if UNITY_EDITOR
    void OnValidate()
    {
        if (typingAudioSource != null && typingAudioSource.clip != null)
        {
            if (typingAudioSource.clip.length > typingSpeed)
            {
                Debug.LogWarning(
                    $"[NPCDialogue] Clip '{typingAudioSource.clip.name}' dài " +
                    $"{typingAudioSource.clip.length:F3}s > typingSpeed {typingSpeed:F3}s. " +
                    $"Âm thanh sẽ bị Stop() cắt ngang ở mỗi ký tự — nên dùng clip ngắn (tick đơn).",
                    this);
            }
        }
    }
#endif
}