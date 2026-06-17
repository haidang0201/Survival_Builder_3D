// StoryUIController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StoryUIController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject storyPanel;
    public Image portraitImage;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Button continueButton;
    public Button skipButton;

    [Header("Story Data")]
    public StoryLineData[] storyLines;

    [Header("Typing Effect")]
    public float typingSpeed = 0.03f;

    [Header("Events")]
    public UnityEvent onStoryFinished; // gắn GameManager.StartMainGame() vào đây

    private int currentIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    [SerializeField] private AudioSource typingAudioSource;

    // Bỏ qua âm thanh cho khoảng trắng/dấu câu để đỡ rối
    private static readonly HashSet<char> silentChars = new HashSet<char> { ' ', '\n', '\t' };
    void Start()
    {
        continueButton.onClick.AddListener(OnContinue);
        //skipButton.onClick.AddListener(SkipAllStory);

        if (storyLines.Length > 0)
        {
            storyPanel.SetActive(true);
            ShowLine(0);
        }
        else
        {
            EndStory();
        }
    }


    void ShowLine(int index)
    {
        currentIndex = index;
        var line = storyLines[index];

        speakerNameText.text = line.speakerName;
        portraitImage.sprite = line.portrait;
        portraitImage.enabled = line.portrait != null;

        // Ghép title + content
        string fullText = $"<b>{line.title}</b>\n\n{line.content}";

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(fullText));
    }

    private IEnumerator TypeText(string content)
    {
        dialogueText.text = "";
        foreach (char c in content)
        {
            dialogueText.text += c;

            if (!silentChars.Contains(c) && typingAudioSource != null)
            {
                typingAudioSource.pitch = Random.Range(0.95f, 1.05f); // tránh nghe nhàm/máy móc
                typingAudioSource.PlayOneShot(typingAudioSource.clip);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void OnContinue()
    {
        // Nếu đang gõ chữ, bấm để hiện full text ngay
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            var line = storyLines[currentIndex];
            dialogueText.text = $"<b>{line.title}</b>\n\n{line.content}";
            isTyping = false;
            return;
        }

        int next = currentIndex + 1;
        if (next < storyLines.Length)
        {
            ShowLine(next);
        }
        else
        {
            EndStory();
        }
    }

    // public void SkipAllStory()
    // {
    //     if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    //     EndStory();
    // }

    void EndStory()
    {
        storyPanel.SetActive(false);
        onStoryFinished?.Invoke();
    }
    public void LoadSceneGamePlay()
    {
        SceneManager.LoadScene(2);
    }
}