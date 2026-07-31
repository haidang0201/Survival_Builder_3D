using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DialogueData
{
    public string speakerName = "Cố vấn Marcus";
    public Sprite speakerAvatar;
    [TextArea(2, 5)] public string message;
}

public class NPCDialogueUI : MonoBehaviour
{
    public static NPCDialogueUI Ins { get; private set; }

    // 🛠️ THÊM PROPERTY NÀY ĐỂ KÍCH HOẠT CHECK TRẠNG THÁI CHO TUTORIAL MANAGER
    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeInHierarchy;

    [Header("UI Elements")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.025f;

    private DialogueData[] currentDialogues;
    private int currentIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private System.Action onCompleteCallback;

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnClickNext);

        HideDialogue();
    }

    public void ShowDialogueSequence(DialogueData[] dialogues, System.Action onComplete = null)
    {
        if (dialoguePanel == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (dialogues == null || dialogues.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        currentDialogues = dialogues;
        currentIndex = 0;
        onCompleteCallback = onComplete;

        // Pause Game kiểu Rise of Kingdoms
        Time.timeScale = 1f;

        dialoguePanel.SetActive(true);
        DisplayLine();
    }

    private void DisplayLine()
    {
        DialogueData line = currentDialogues[currentIndex];
        if (speakerNameText != null) speakerNameText.text = line.speakerName;
        
        if (line.speakerAvatar != null && avatarImage != null)
        {
            avatarImage.gameObject.SetActive(true);
            avatarImage.sprite = line.speakerAvatar;
        }
        else if (avatarImage != null)
        {
            avatarImage.gameObject.SetActive(false);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextRoutine(line.message));
    }

    private IEnumerator TypeTextRoutine(string text)
    {
        isTyping = true;
        if (dialogueText != null) dialogueText.text = "";

        foreach (char c in text.ToCharArray())
        {
            if (dialogueText != null) dialogueText.text += c;
            // Dùng WaitForSecondsRealtime vì game đang ở Time.timeScale = 0
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    private void OnClickNext()
    {
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (dialogueText != null) dialogueText.text = currentDialogues[currentIndex].message;
            isTyping = false;
            return;
        }

        currentIndex++;
        if (currentIndex < currentDialogues.Length)
        {
            DisplayLine();
        }
        else
        {
            HideDialogue();
            onCompleteCallback?.Invoke();
        }
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }
}