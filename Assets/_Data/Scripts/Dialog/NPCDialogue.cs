using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NPCDialogue : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI text;
    public Button continueButton;
    public Button skipButton;

    [Header("Typing Settings")]
    public float typeSpeed = 0.03f;

    [Header("Audio")]
    public AudioSource typingAudioSource;
    public AudioClip typingClip;

    // ── Runtime ──────────────────────────────────────────
    private Coroutine typingRoutine;
    private string currentMsg = "";
    private bool isTypingDone = true;
    private bool continueClicked = false;
    private bool showContinueAfterTyping = true;
    private bool buttonsBound = false;

    private static readonly HashSet<char> silentChars =
        new HashSet<char> { ' ', '\n', '\t', '.', ',', '!', '?' };

    // ══════════════════════════════════════════════════════
    void Awake()
    {
        if (panel == null)
            Debug.LogError("[NPC] ✗ panel NULL!");

        if (text == null)
            Debug.LogError("[NPC] ✗ text NULL!");

        BindButtons();

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        // Tắt panel từ Awake — nhưng Show() sẽ bật lại
        if (panel != null)
            panel.SetActive(false);
    }

    void Start()
    {
        BindButtons();

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Hiện panel và chạy text từng ký tự.
    /// Mặc định: text chạy xong sẽ hiện nút Tiếp tục.
    /// </summary>
    public void Show(string msg)
    {
        Show(msg, true);
    }

    /// <summary>
    /// Hiện nhiệm vụ nhưng KHÔNG hiện nút Tiếp tục.
    /// Dùng cho step chờ tài nguyên: chờ wood >= 5, stone > 0...
    /// </summary>
    public void ShowObjective(string msg)
    {
        Show(msg, false);
    }

    /// <summary>
    /// Hiện panel và chạy text.
    /// showContinue = true → text xong hiện nút Tiếp tục.
    /// showContinue = false → text xong không hiện nút.
    /// </summary>
    public void Show(string msg, bool showContinue)
    {
        BindButtons();

        Debug.Log($"<color=cyan>[NPC] Show → \"{msg?.Substring(0, Mathf.Min(40, msg?.Length ?? 0))}...\"</color>");

        if (panel == null)
        {
            Debug.LogError("[NPC] ✗ panel NULL trong Show()!");
            return;
        }

        currentMsg = msg ?? "";
        isTypingDone = false;
        continueClicked = false;
        showContinueAfterTyping = showContinue;

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        SetContinueVisible(false);

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeText(currentMsg));
    }

    /// <summary>
    /// Show text → đợi text chạy xong → hiện nút Tiếp tục → đợi người chơi bấm.
    /// </summary>
    public IEnumerator ShowAndWait(string msg)
    {
        Show(msg, true);

        yield return new WaitUntil(() => isTypingDone);

        // Nếu quên gán continueButton thì vẫn cho test bằng Space / Enter / Click
        if (continueButton == null)
        {
            Debug.LogWarning("[NPC] continueButton NULL. Dùng Space / Enter / Click để tiếp tục.");
            yield return StartCoroutine(WaitForFallbackContinue());
            yield break;
        }

        yield return new WaitUntil(() => continueClicked);
    }

    /// <summary>
    /// Ẩn panel và dừng typing.
    /// </summary>
    public void Hide()
    {
        Debug.Log("<color=cyan>[NPC] Hide()</color>");

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        StopAudio();

        if (panel != null)
            panel.SetActive(false);

        SetContinueVisible(false);
    }

    /// <summary>
    /// Skip typing — hiện hết text ngay.
    /// Nếu step này cần Continue thì sau khi skip sẽ hiện nút Tiếp tục.
    /// </summary>
    public void SkipTyping()
    {
        if (typingRoutine == null)
            return;

        Debug.Log("<color=cyan>[NPC] SkipTyping</color>");

        StopCoroutine(typingRoutine);
        typingRoutine = null;
        isTypingDone = true;

        StopAudio();

        if (text != null)
            text.text = currentMsg;

        if (showContinueAfterTyping)
            SetContinueVisible(true);
    }

    /// <summary>
    /// True khi text đã chạy xong hoàn toàn.
    /// </summary>
    public bool IsTypingDone()
    {
        return isTypingDone;
    }

    // ══════════════════════════════════════════════════════
    //  INTERNAL
    // ══════════════════════════════════════════════════════

    private IEnumerator TypeText(string msg)
    {
        if (text == null)
        {
            typingRoutine = null;
            isTypingDone = true;
            yield break;
        }

        text.text = "";

        foreach (char c in msg)
        {
            if (panel == null || !panel.activeSelf)
            {
                StopAudio();
                typingRoutine = null;
                isTypingDone = true;
                yield break;
            }

            text.text += c;

            if (!silentChars.Contains(c))
                PlayTock();

            yield return new WaitForSeconds(typeSpeed);
        }

        StopAudio();

        typingRoutine = null;
        isTypingDone = true;

        if (showContinueAfterTyping)
            SetContinueVisible(true);

        Debug.Log("<color=cyan>[NPC] TypeText hoàn thành — chờ bấm Tiếp tục</color>");
    }

    private void BindButtons()
    {
        if (buttonsBound)
            return;

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipTyping);
            skipButton.onClick.AddListener(SkipTyping);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        buttonsBound = true;
    }

    private void OnContinueClicked()
    {
        // Nếu đang typing mà bấm Continue thì skip text trước
        if (!isTypingDone)
        {
            SkipTyping();
            return;
        }

        continueClicked = true;
        SetContinueVisible(false);

        Debug.Log("<color=cyan>[NPC] Continue clicked</color>");
    }

    private void SetContinueVisible(bool visible)
    {
        if (continueButton == null)
            return;

        continueButton.gameObject.SetActive(visible);
        continueButton.interactable = visible;
    }

    private IEnumerator WaitForFallbackContinue()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetMouseButtonDown(0))
            {
                yield break;
            }

            yield return null;
        }
    }

    private void PlayTock()
    {
        if (panel == null || !panel.activeSelf) return;
        if (typingRoutine == null) return;
        if (typingAudioSource == null || typingClip == null) return;

        typingAudioSource.pitch = Random.Range(0.95f, 1.05f);
        typingAudioSource.PlayOneShot(typingClip);
    }

    private void StopAudio()
    {
        if (typingAudioSource != null && typingAudioSource.isPlaying)
            typingAudioSource.Stop();
    }
}