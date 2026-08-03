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

    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeInHierarchy;

    [Header("UI Elements")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton; // Có thể giữ hoặc ẩn trên Canvas UI

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.025f;

    private DialogueData[] currentDialogues;
    private int currentIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool skipFirstFrameInput = false; // 🔥 Chống bị trigger click ngay frame mở thoại
    private System.Action onCompleteCallback;

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnClickNext);

        HideDialogue();
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        // 🔥 Chống việc nhấp chuột ở gameplay vô tình kích hoạt click thoại ngay frame đầu
        if (skipFirstFrameInput)
        {
            skipFirstFrameInput = false;
            return;
        }

        // 🔥 YÊU CẦU 4: Bấm bất kỳ vị trí nào trên màn hình (Chuột trái hoặc Cảm ứng) để qua bài
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnClickNext();
        }
    }

    public void ShowDialogueSequence(DialogueData[] dialogues, System.Action onComplete = null)
    {
        if (dialoguePanel == null || dialogues == null || dialogues.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        currentDialogues = dialogues;
        currentIndex = 0;
        onCompleteCallback = onComplete;
        skipFirstFrameInput = true; // Đánh dấu bỏ qua click của frame hiện tại

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
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    public void OnClickNext()
    {
        // Click lần 1: Hoàn thành ngay câu chữ đang gõ
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (dialogueText != null) dialogueText.text = currentDialogues[currentIndex].message;
            isTyping = false;
            return;
        }

        // Click lần 2: Chuyển sang câu thoại kế tiếp
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