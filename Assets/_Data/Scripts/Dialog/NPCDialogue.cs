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
    private bool isTypingDone = true;   // flag riêng — tránh race condition 1 frame

    private static readonly HashSet<char> silentChars =
        new HashSet<char> { ' ', '\n', '\t', '.', ',', '!', '?' };

    // ══════════════════════════════════════════════════════
    void Awake()
    {
        if (panel == null)
            Debug.LogError("[NPC] ✗ panel NULL!");
        if (text == null)
            Debug.LogError("[NPC] ✗ text NULL!");

        // Tắt panel từ Awake — trước cả Start
        if (panel != null) panel.SetActive(false);
    }

    void Start()
    {
        // Gán Skip button
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipTyping);
    }

    // ══════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════

    /// <summary>Hiện panel và chạy text từng ký tự</summary>
    public void Show(string msg)
    {
        Debug.Log($"<color=cyan>[NPC] Show → \"{msg?.Substring(0, Mathf.Min(40, msg?.Length ?? 0))}...\"</color>");

        if (panel == null)
        {
            Debug.LogError("[NPC] ✗ panel NULL trong Show()!");
            return;
        }

        currentMsg = msg ?? "";
        isTypingDone = false;           // đánh dấu ĐANG chạy — trước khi start coroutine
        panel.SetActive(true);

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeText(currentMsg));
    }

    /// <summary>Ẩn panel và dừng typing</summary>
    public void Hide()
    {
        Debug.Log("<color=cyan>[NPC] Hide()</color>");

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        StopAudio();

        if (panel != null) panel.SetActive(false);
    }

    /// <summary>Skip typing — hiện hết text ngay</summary>
    public void SkipTyping()
    {
        if (typingRoutine == null) return; // đã xong rồi

        Debug.Log("<color=cyan>[NPC] SkipTyping</color>");

        StopCoroutine(typingRoutine);
        typingRoutine = null;
        isTypingDone = true;

        StopAudio();

        if (text != null) text.text = currentMsg;
    }

    /// <summary>True khi text đã chạy xong hoàn toàn</summary>
    public bool IsTypingDone() => isTypingDone;

    // ══════════════════════════════════════════════════════
    //  INTERNAL
    // ══════════════════════════════════════════════════════
    private IEnumerator TypeText(string msg)
    {
        if (text == null)
        {
            typingRoutine = null;
            yield break;
        }

        text.text = "";

        foreach (char c in msg)
        {
            if (panel == null || !panel.activeSelf)
            {
                StopAudio();
                typingRoutine = null;
                yield break;
            }

            text.text += c;

            if (!silentChars.Contains(c))
                PlayTock();

            yield return new WaitForSeconds(typeSpeed);
        }

        // Text xong → dừng audio ngay
        StopAudio();
        typingRoutine = null;
        isTypingDone = true;
        Debug.Log("<color=cyan>[NPC] TypeText hoàn thành — audio dừng</color>");
    }

    private void PlayTock()
    {
        // Chỉ phát khi panel đang hiển thị VÀ text đang chạy
        if (panel == null || !panel.activeSelf) return;
        if (typingRoutine == null) return; // text đã xong rồi
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